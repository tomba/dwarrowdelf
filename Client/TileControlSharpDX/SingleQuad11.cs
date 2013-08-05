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
	public sealed class SingleQuad11 : IScene
	{
		[StructLayout(LayoutKind.Sequential)]
		struct MyVertex
		{
			public Vector3 pos;
		}

		Device m_device;
		RasterizerState m_rasterizerState;

		SingleQuad11VS m_vertexShader;
		SingleQuad11PS m_pixelShader;

		Texture2D m_tileTextureArray;

		Buffer m_vertexBuffer;
		InputLayout m_layout;

		public SingleQuad11()
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

			m_vertexShader = ToDispose(new SingleQuad11VS(device));
			m_pixelShader = ToDispose(new SingleQuad11PS(device));

			var vertexSize = Marshal.SizeOf(typeof(MyVertex));

			/* Create vertices */

			using (var stream = new DataStream(4 * vertexSize, true, true))
			{
				stream.Write(new MyVertex() { pos = new Vector3(0.0f, 0.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(0.0f, 1.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(1.0f, 0.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(1.0f, 1.0f, 0.0f), });

				stream.Position = 0;

				m_vertexBuffer = ToDispose(new SharpDX.Direct3D11.Buffer(m_device, stream, new BufferDescription()
				{
					BindFlags = BindFlags.VertexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = 4 * vertexSize,
					Usage = ResourceUsage.Immutable,
				}));
			}

			/* input layout */

			m_layout = ToDispose(new InputLayout(m_device, m_vertexShader.Bytecode, new[] {
				new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
			}));

			m_rasterizerState = ToDispose(new RasterizerState(device, new RasterizerStateDescription()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.None,
				IsDepthClipEnabled = false,
			}));
		}

		public void Detach()
		{
		}

		public void Update(TimeSpan timeSpan)
		{
		}

		public void Render()
		{
			var context = m_device.ImmediateContext;


			context.InputAssembler.InputLayout = m_layout;

			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			var vertexSize = Marshal.SizeOf(typeof(MyVertex));
			context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_vertexBuffer, vertexSize, 0));

			context.Rasterizer.State = m_rasterizerState;

			m_vertexShader.Update();
			m_pixelShader.Update();
			context.Draw(4, 0);
		}

		public void SetupTileBuffer(IntSize2 gridSize)
		{
			m_pixelShader.SetupTileBuffer(gridSize);
		}

		public void SetTileSet(ITileSet tileSet)
		{
			m_tileTextureArray = Helpers11.CreateTextures11(m_device, tileSet);
			m_pixelShader.SetTileTextures(m_tileTextureArray);
		}

		public void SetTileSize(float tileSize)
		{
			m_pixelShader.SetTileSize(tileSize);
		}

		public void SetRenderOffset(float offsetX, float offsetY)
		{
			m_pixelShader.SetRenderOffset(new Vector2(offsetX, offsetY));
		}

		public void SendMapData(RenderTile[] mapData, int columns, int rows)
		{
			m_pixelShader.SendMapData(mapData, columns, rows);
		}
	}
}
