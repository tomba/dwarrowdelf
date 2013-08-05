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

		Texture2D m_tileTextureArray;

		Buffer m_vertexBuffer;
		VertexBufferBinding m_vertexBufferBinding;
		InputLayout m_layout;

		[StructLayout(LayoutKind.Sequential, Size = 32)]
		struct ShaderDataPerFrame
		{
			public Vector2 ColRow;	/* columns, rows */
			public Vector2 RenderOffset;
			public float TileSize;
		}

		ShaderDataPerFrame m_shaderDataPerFrame;
		Buffer m_shaderDataBufferPerFrame;

		ShaderResourceView m_colorBufferView;
		SamplerState m_sampler;

		Buffer m_tileBuffer;
		ShaderResourceView m_tileBufferView;

		ShaderResourceView m_tileTextureView;

		public TileMapScene()
		{
		}

		T ToDispose<T>(T ob)
		{
			return ob;
		}

		public void Dispose()
		{
		}

		public void Attach(ISceneHost host)
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

			m_layout = ToDispose(new InputLayout(m_device, bytecode, new[] {
					new InputElement("POSITION", 0, Format.R32G32_Float, 0, 0),
				}));

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
			m_pixelShader = ToDispose(new PixelShader(m_device, bytecode));

			/* Constant buffer per frame */
			m_shaderDataBufferPerFrame = ToDispose(new Buffer(m_device, Utilities.SizeOf<ShaderDataPerFrame>(),
				ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

			/* color buffer */
			var colorBuffer = ToDispose(Helpers11.CreateGameColorBuffer(m_device));

			m_colorBufferView = ToDispose(new ShaderResourceView(m_device, colorBuffer, new ShaderResourceViewDescription()
			{
				Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
				Dimension = ShaderResourceViewDimension.Buffer,
				Buffer = new ShaderResourceViewDescription.BufferResource()
				{
					ElementWidth = colorBuffer.Description.SizeInBytes / sizeof(uint),
					ElementOffset = 0,
				},
			}));

			/* Texture sampler */
			m_sampler = ToDispose(new SamplerState(m_device, new SamplerStateDescription()
			{
				/* Use point filtering as linear filtering causes artifacts */
				Filter = Filter.MinMagMipPoint,
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
				BorderColor = new Color4(0),
				ComparisonFunction = Comparison.Never,
				MipLodBias = 0,
				MinimumLod = 0,
				MaximumLod = float.MaxValue,
			}));
		}

		public void Detach()
		{
		}

		public void Update(TimeSpan timeSpan)
		{
		}

		public void SetTileSet(ITileSet tileSet)
		{
			m_tileTextureArray = Helpers11.CreateTextures11(m_device, tileSet);

			//RemoveAndDispose(ref m_tileTextureView);

			m_tileTextureView = ToDispose(new ShaderResourceView(m_device, m_tileTextureArray));
		}

		public void SetupTileBuffer(IntSize2 gridSize)
		{
			//RemoveAndDispose(ref m_tileBuffer);
			//RemoveAndDispose(ref m_tileBufferView);

			m_tileBuffer = ToDispose(new SharpDX.Direct3D11.Buffer(m_device, new BufferDescription()
			{
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.BufferStructured,
				SizeInBytes = gridSize.Area * Marshal.SizeOf(typeof(RenderTile)),
				StructureByteStride = Marshal.SizeOf(typeof(RenderTile)),
				Usage = ResourceUsage.Dynamic,
			}));

			m_tileBufferView = ToDispose(new ShaderResourceView(m_device, m_tileBuffer));
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

		public void Render()
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
