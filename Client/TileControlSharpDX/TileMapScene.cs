using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dwarrowdelf;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using Buffer = SharpDX.Direct3D11.Buffer;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class TileMapScene : IScene
	{
		Device m_device;

		VertexShader m_vertexShader;
		PixelShader m_pixelShader;

		Buffer m_vertexBuffer;
		VertexBufferBinding m_vertexBufferBinding;
		InputLayout m_layout;

		[StructLayout(LayoutKind.Sequential, Size = 32)]
		struct ShaderDataPerFrame
		{
			public Vector2 ColRow;	/* columns, rows */
			public Vector2 RenderOffset;
			public float TileSize;
			public bool Rotate90Clockwise;
		}

		ShaderDataPerFrame m_shaderDataPerFrame;
		Buffer m_shaderDataBufferPerFrame;

		Buffer m_colorBuffer;
		ShaderResourceView m_colorBufferView;

		Buffer m_tileBuffer;
		ShaderResourceView m_tileBufferView;

		Texture2D m_tileTextureArray;
		ShaderResourceView m_tileTextureView;
		SamplerState m_sampler;

		public TileMapScene(bool rotate90 = false)
		{
			m_shaderDataPerFrame.Rotate90Clockwise = rotate90;
		}

		void IScene.Attach(ISceneHost host)
		{
			var device = host.Device;

			m_device = device;

			var ass = System.Reflection.Assembly.GetCallingAssembly();

			using (var stream = ass.GetManifestResourceStream("Dwarrowdelf.Client.TileControl.TileMapVS.hlslo"))
			{
				var bytecode = ShaderBytecode.FromStream(stream);
				CreateVS(bytecode);
			}

			using (var stream = ass.GetManifestResourceStream("Dwarrowdelf.Client.TileControl.TileMapPS.hlslo"))
			{
				var bytecode = ShaderBytecode.FromStream(stream);
				CreatePS(bytecode);
			}
		}

		void CreateVS(ShaderBytecode bytecode)
		{
			m_vertexShader = new VertexShader(m_device, bytecode);

			m_layout = new InputLayout(m_device, bytecode, new[] {
					new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
				});

			var vertexData = new Vector2[] {
				new Vector2(0.0f, 0.0f),
				new Vector2(0.0f, 1.0f),
				new Vector2(1.0f, 0.0f),
				new Vector2(1.0f, 1.0f),
			};

			m_vertexBuffer = Buffer.Create<Vector2>(m_device, BindFlags.VertexBuffer, vertexData, usage: ResourceUsage.Immutable);

			m_vertexBufferBinding = new VertexBufferBinding(m_vertexBuffer, Vector2.SizeInBytes, 0);
		}

		void CreatePS(ShaderBytecode bytecode)
		{
			m_pixelShader = new PixelShader(m_device, bytecode);

			/* Constant buffer per frame */
			m_shaderDataBufferPerFrame = new Buffer(m_device, Utilities.SizeOf<ShaderDataPerFrame>(),
				ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

			/* color buffer */
			m_colorBuffer = Helpers11.CreateGameColorBuffer(m_device);

			m_colorBufferView = new ShaderResourceView(m_device, m_colorBuffer, new ShaderResourceViewDescription()
			{
				Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
				Dimension = ShaderResourceViewDimension.Buffer,
				Buffer = new ShaderResourceViewDescription.BufferResource()
				{
					ElementWidth = m_colorBuffer.Description.SizeInBytes / sizeof(uint),
					ElementOffset = 0,
				},
			});

			/* Texture sampler */
			m_sampler = new SamplerState(m_device, new SamplerStateDescription()
			{
				/* Use point filtering as linear and anisotropic filtering causes artifacts:
				 * at some (uneven) zoom levels the tiles have artifacts in the edges.
				 * Sampling goes over limits?
				 */
				Filter = Filter.MinMagMipPoint,
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
				BorderColor = new Color4(0),
				ComparisonFunction = Comparison.Never,
				MipLodBias = 0,
				MinimumLod = 0,
				MaximumLod = float.MaxValue,
			});
		}

		void IScene.Detach()
		{
			DH.Dispose(ref m_tileBufferView);
			DH.Dispose(ref m_tileBuffer);

			DH.Dispose(ref m_tileTextureView);
			DH.Dispose(ref m_tileTextureArray);

			DH.Dispose(ref m_sampler);
			DH.Dispose(ref m_colorBufferView);
			DH.Dispose(ref m_colorBuffer);
			DH.Dispose(ref m_shaderDataBufferPerFrame);
			DH.Dispose(ref m_pixelShader);

			DH.Dispose(ref m_vertexBuffer);
			DH.Dispose(ref m_layout);
			DH.Dispose(ref m_vertexShader);
		}

		void IScene.OnRenderSizeChanged(IntSize2 renderSize)
		{
		}

		void IScene.Update(TimeSpan timeSpan)
		{
		}

		public void SetTileSet(ITileSet tileSet)
		{
			DH.Dispose(ref m_tileTextureView);
			DH.Dispose(ref m_tileTextureArray);

			m_tileTextureArray = Helpers11.CreateTextures11(m_device, tileSet);
			m_tileTextureView = new ShaderResourceView(m_device, m_tileTextureArray);
		}

		public void SetupTileBuffer(IntSize2 gridSize)
		{
			DH.Dispose(ref m_tileBufferView);
			DH.Dispose(ref m_tileBuffer);

			m_tileBuffer = new SharpDX.Direct3D11.Buffer(m_device, new BufferDescription()
			{
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.BufferStructured,
				SizeInBytes = gridSize.Area * Marshal.SizeOf(typeof(RenderTile)),
				StructureByteStride = Marshal.SizeOf(typeof(RenderTile)),
				Usage = ResourceUsage.Dynamic,
			});

			m_tileBufferView = new ShaderResourceView(m_device, m_tileBuffer);
		}

		public void SendMapData(RenderTile[] mapData, int columns, int rows)
		{
			m_shaderDataPerFrame.ColRow = new Vector2(columns, rows);

			DataStream stream;
			var box = m_device.ImmediateContext.MapSubresource(m_tileBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
			stream.WriteRange(mapData, 0, columns * rows);
			m_device.ImmediateContext.UnmapSubresource(m_tileBuffer, 0);
			stream.Dispose();
		}

		public void SetTileSize(float tileSize)
		{
			m_shaderDataPerFrame.TileSize = tileSize;
		}

		public void SetRenderOffset(float offsetX, float offsetY)
		{
			m_shaderDataPerFrame.RenderOffset = new Vector2(offsetX, offsetY);
		}

		void IScene.Render()
		{
			var context = m_device.ImmediateContext;

			context.InputAssembler.InputLayout = m_layout;
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			context.InputAssembler.SetVertexBuffers(0, m_vertexBufferBinding);

			context.VertexShader.Set(m_vertexShader);

			context.UpdateSubresource(ref m_shaderDataPerFrame, m_shaderDataBufferPerFrame);

			context.PixelShader.Set(m_pixelShader);
			context.PixelShader.SetConstantBuffer(0, m_shaderDataBufferPerFrame);
			context.PixelShader.SetShaderResource(0, m_tileTextureView);
			context.PixelShader.SetShaderResource(1, m_colorBufferView);
			context.PixelShader.SetShaderResource(2, m_tileBufferView);
			context.PixelShader.SetSampler(0, m_sampler);

			context.Draw(4, 0);
		}
	}
}
