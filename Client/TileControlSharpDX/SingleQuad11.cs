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
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;

namespace Dwarrowdelf.Client.TileControl
{
	sealed class SingleQuad11 : Component
	{
		[StructLayout(LayoutKind.Sequential)]
		struct MyVertex
		{
			public Vector3 pos;
		}

		Device m_device;
		Texture2D m_renderTarget;
		RenderTargetView m_renderTargetView;
		RasterizerState m_rasterizerState;

		SingleQuad11VS m_vertexShader;
		SingleQuad11PS m_pixelShader;

		public SingleQuad11(Device device)
		{
			m_device = device;

			m_vertexShader = ToDispose(new SingleQuad11VS(device));
			m_pixelShader = ToDispose(new SingleQuad11PS(device));

			var vertexSize = Marshal.SizeOf(typeof(MyVertex));

			var context = m_device.ImmediateContext;

			/* Create vertices */

			using (var stream = new DataStream(4 * vertexSize, true, true))
			{
				stream.Write(new MyVertex() { pos = new Vector3(0.0f, 0.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(0.0f, 1.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(1.0f, 0.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(1.0f, 1.0f, 0.0f), });

				stream.Position = 0;

				var vertexBuffer = ToDispose(new SharpDX.Direct3D11.Buffer(m_device, stream, new BufferDescription()
				{
					BindFlags = BindFlags.VertexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = 4 * vertexSize,
					Usage = ResourceUsage.Immutable,
				}));

				context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
				context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, vertexSize, 0));
			}

			/* input layout */

			var layout = ToDispose(new InputLayout(m_device, m_vertexShader.Signature, new[] {
				new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
			}));

			context.InputAssembler.InputLayout = layout;
		}

		public void SetRenderTarget(Texture2D renderTexture)
		{
			SetupRenderTarget(renderTexture);

			var renderWidth = renderTexture.Description.Width;
			var renderHeight = renderTexture.Description.Height;
			var renderTargetSize = new IntSize2(renderWidth, renderHeight);

			m_pixelShader.SetupTileBuffer(renderTargetSize);
		}

		void SetupRenderTarget(Texture2D renderTexture)
		{
			SafeDispose(ref m_renderTargetView);
			SafeDispose(ref m_rasterizerState);

			var renderWidth = renderTexture.Description.Width;
			var renderHeight = renderTexture.Description.Height;
			var device = renderTexture.Device;
			var context = device.ImmediateContext;

			/* Setup render target */

			m_renderTarget = renderTexture;

			m_renderTargetView = ToDispose(new RenderTargetView(device, renderTexture));

			m_rasterizerState = ToDispose(new RasterizerState(device, new RasterizerStateDescription()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.None,
				IsDepthClipEnabled = false,
			}));

			context.OutputMerger.SetTargets(m_renderTargetView);
			context.Rasterizer.SetViewports(new Viewport(0, 0, renderWidth, renderHeight, 0.0f, 1.0f));
			context.Rasterizer.State = m_rasterizerState;
		}

		public void SetTileTextures(Texture2D textureArray)
		{
			m_pixelShader.SetTileTextures(textureArray);
		}

		public void SendMapData(RenderData<RenderTileDetailed> mapData, int columns, int rows)
		{
			m_pixelShader.SendMapData(mapData, columns, rows);
		}

		public void Render(float tileSize, System.Windows.Point renderOffset)
		{
			if (m_renderTargetView == null)
				return;

			m_pixelShader.Setup(tileSize, renderOffset);

			var context = m_device.ImmediateContext;

			context.Draw(4, 0);
		}
	}
}
