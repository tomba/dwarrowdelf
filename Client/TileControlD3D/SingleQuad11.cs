using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using Device = SlimDX.Direct3D11.Device;
using System.Runtime.InteropServices;
using Dwarrowdelf;

namespace Dwarrowdelf.Client.TileControl
{
	class SingleQuad11 : IDisposable
	{
		[StructLayout(LayoutKind.Sequential)]
		struct MyVertex
		{
			public Vector3 pos;
		}

		Device m_device;
		Texture2D m_renderTexture;
		RenderTargetView m_renderTargetView;
		RasterizerState m_rasterizerState;

		InputLayout m_layout;
		SlimDX.Direct3D11.Buffer m_vertexBuffer;

		Effect m_effect;
		EffectTechnique m_technique;
		EffectPass m_pass;

		EffectMatrixVariable m_worldMatrixEffectVariable;

		EffectScalarVariable m_tileSizeVariable;
		EffectVectorVariable m_colrowVariable;
		EffectVectorVariable m_sizeVariable;

		int m_tileSize = 32;
		int m_columns;
		int m_rows;

		int m_numTiles;

		SlimDX.Direct3D11.Buffer m_tileBuffer;
		ShaderResourceView m_tileBufferView;
		int m_tileBufferWidth;
		int m_tileBufferHeight;

		ShaderResourceView m_tileTextureView;

		RenderData<RenderTileDetailed> m_map;

		int m_width;
		int m_height;

		bool m_invalid = true;

		ShaderResourceView m_colorBufferView;

		public SingleQuad11(Device device, SlimDX.Direct3D11.Buffer colorBuffer)
		{
			m_tileBufferWidth = (1024 / 2) | 1;
			m_tileBufferHeight = (1024 / 2) | 1;

			m_device = device;

			/* Create render target */

			var context = m_device.ImmediateContext;

			/* Create shader */

			using (var byteCode = ShaderBytecode.CompileFromFile("SingleQuad11.fx", "fx_5_0", ShaderFlags.EnableStrictness, EffectFlags.None))
				m_effect = new Effect(m_device, byteCode);

			m_technique = m_effect.GetTechniqueByName("full");
			m_pass = m_technique.GetPassByIndex(0);
			m_layout = new InputLayout(m_device, m_pass.Description.Signature, new[] {
				new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
			});


			/* color buffer view */
			m_colorBufferView = new ShaderResourceView(device, colorBuffer, new ShaderResourceViewDescription()
			{
				Format = SlimDX.DXGI.Format.R32_UInt,
				Dimension = ShaderResourceViewDimension.Buffer,
				ElementWidth = colorBuffer.Description.SizeInBytes / sizeof(uint),
				ElementOffset = 0,
			});

			m_effect.GetVariableByName("g_colorBuffer").AsResource().SetResource(m_colorBufferView);



			/* Create vertices */

			var vertexSize = Marshal.SizeOf(typeof(MyVertex));

			using (var stream = new DataStream(4 * vertexSize, true, true))
			{
				stream.Write(new MyVertex() { pos = new Vector3(0.0f, 0.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(0.0f, 1.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(1.0f, 0.0f, 0.0f), });
				stream.Write(new MyVertex() { pos = new Vector3(1.0f, 1.0f, 0.0f), });

				stream.Position = 0;

				m_vertexBuffer = new SlimDX.Direct3D11.Buffer(m_device, stream, new BufferDescription()
				{
					BindFlags = BindFlags.VertexBuffer,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = 4 * vertexSize,
					Usage = ResourceUsage.Default,
				});
			}



			m_tileSizeVariable = m_effect.GetVariableByName("g_tileSize").AsScalar();
			m_colrowVariable = m_effect.GetVariableByName("g_colrow").AsVector();
			m_sizeVariable = m_effect.GetVariableByName("g_size").AsVector();


			m_worldMatrixEffectVariable = m_effect.GetVariableByName("g_world").AsMatrix();

			//create world matrix
			Matrix w = Matrix.Identity;
			w *= Matrix.Scaling(2.0f, 2.0f, 0);
			w *= Matrix.Translation(-1.0f, -1.0f, 0);
			m_worldMatrixEffectVariable.SetMatrix(w);
		}

		public int TileSize
		{
			set
			{
				m_tileSize = value;

				if (m_tileSize < 2)
					m_tileSize = 2;
				else if (m_tileSize > 512)
					m_tileSize = 512;

				m_invalid = true;
			}

			get { return m_tileSize; }
		}

		public void SetRenderData(RenderData<RenderTileDetailed> renderData)
		{
			m_map = renderData;
		}

		public void SetRenderTarget(Texture2D renderTexture)
		{
			m_renderTexture = renderTexture;

			var width = renderTexture.Description.Width;
			var height = renderTexture.Description.Height;
			var device = renderTexture.Device;

			if (m_renderTargetView != null)
			{
				m_renderTargetView.Dispose();
				m_renderTargetView = null;
			}

			if (m_rasterizerState != null)
			{
				m_rasterizerState.Dispose();
				m_rasterizerState = null;
			}

			m_renderTargetView = new RenderTargetView(device, renderTexture);

			m_rasterizerState = RasterizerState.FromDescription(device, new RasterizerStateDescription()
			{
				FillMode = FillMode.Solid,
				CullMode = CullMode.Back,
				IsDepthClipEnabled = true
			});

			var context = device.ImmediateContext;

			context.OutputMerger.SetTargets(m_renderTargetView);
			context.Rasterizer.SetViewports(new Viewport(0, 0, width, height, 0.0f, 1.0f));
			context.Rasterizer.State = m_rasterizerState;

			m_width = width;
			m_height = height;
			m_tileBufferWidth = (width / 2) | 1;
			m_tileBufferHeight = (height / 2) | 1;


			if (m_tileBuffer != null)
			{
				m_tileBuffer.Dispose();
				m_tileBuffer = null;
			}

			if (m_tileBufferView != null)
			{
				m_tileBufferView.Dispose();
				m_tileBufferView = null;
			}

			AllocateTileBuffer(m_device, m_tileBufferWidth, m_tileBufferHeight, out m_tileBuffer, out m_tileBufferView);

			m_effect.GetVariableByName("g_tileBuffer").AsResource().SetResource(m_tileBufferView);

			var renderWidth = width;
			var renderHeight = height;

			m_columns = (int)Math.Ceiling((double)m_width / m_tileSize) | 1;
			m_rows = (int)Math.Ceiling((double)m_height / m_tileSize) | 1;

			m_invalid = true;
		}

		static void AllocateTileBuffer(Device device, int width, int height, out SlimDX.Direct3D11.Buffer tileBuffer, out ShaderResourceView tileBufferView)
		{
			// uint = packed tilenum
			// uint = packed tile color

			tileBuffer = new SlimDX.Direct3D11.Buffer(device, new BufferDescription()
			{
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.None,
				SizeInBytes = sizeof(int) * width * height * 2,
				Usage = ResourceUsage.Dynamic,
			});

			tileBufferView = new ShaderResourceView(device, tileBuffer, new ShaderResourceViewDescription()
			{
				Format = SlimDX.DXGI.Format.R32G32_UInt,
				Dimension = ShaderResourceViewDimension.Buffer,
				ElementWidth = width * height,
				ElementOffset = 0,
			});
		}

		public void SetTileTextures(Texture2D textureArray)
		{
			m_tileTextureView = new ShaderResourceView(m_device, textureArray, new ShaderResourceViewDescription()
			{
				Format = textureArray.Description.Format,
				Dimension = ShaderResourceViewDimension.Texture2DArray,
				MipLevels = textureArray.Description.MipLevels,
				MostDetailedMip = 0,
				ArraySize = textureArray.Description.ArraySize,
			});

			m_numTiles = textureArray.Description.ArraySize;

			m_effect.GetVariableByName("g_tileTextures").AsResource().SetResource(m_tileTextureView);
		}

		public void Render()
		{
			if (m_renderTargetView == null)
				return;

			if (m_tileTextureView == null)
				return;

			var context = m_device.ImmediateContext;
			m_invalid = true;

			if (m_invalid)
			{
				m_columns = (int)Math.Ceiling((double)m_width / m_tileSize) | 1;
				m_rows = (int)Math.Ceiling((double)m_height / m_tileSize) | 1;

				m_sizeVariable.Set(new Vector2(m_width, m_height));

				m_tileSizeVariable.Set(m_tileSize);

				m_colrowVariable.Set(new Vector2(m_columns, m_rows));

				//System.Diagnostics.Debug.WriteLine("{0}x{1} {2}x{3}  {4}", m_columns, m_rows, m_width, m_height, m_tileSize);

				m_invalid = false;

				var box = m_device.ImmediateContext.MapSubresource(m_tileBuffer, 0, sizeof(int) * m_columns * m_rows * 2,
					MapMode.WriteDiscard, SlimDX.Direct3D11.MapFlags.None);
				var stream = box.Data;

				for (int y = 0; y < m_rows; ++y)
				{
					for (int x = 0; x < m_columns; ++x)
					{
						SymbolID t1 = m_map.ArrayGrid.Grid[y, x].Floor.SymbolID;
						SymbolID t2 = m_map.ArrayGrid.Grid[y, x].Interior.SymbolID;
						SymbolID t3 = m_map.ArrayGrid.Grid[y, x].Object.SymbolID;
						SymbolID t4 = m_map.ArrayGrid.Grid[y, x].Top.SymbolID;

						GameColor c1 = m_map.ArrayGrid.Grid[y, x].Floor.Color;
						GameColor c2 = m_map.ArrayGrid.Grid[y, x].Interior.Color;
						GameColor c3 = m_map.ArrayGrid.Grid[y, x].Object.Color;
						GameColor c4 = m_map.ArrayGrid.Grid[y, x].Top.Color;

						stream.Write<int>((byte)t1 | ((byte)t2 << 8) | ((byte)t3 << 16) | ((byte)t4 << 24));	// tilenum
						stream.Write<int>((byte)c1 | ((byte)c2 << 8) | ((byte)c3 << 16) | ((byte)c4 << 24));	// color
					}
				}

				m_device.ImmediateContext.UnmapSubresource(m_tileBuffer, 0);
			}

			// XXX not needed?
			//context.ClearRenderTargetView(m_renderTargetView, new Color4(1.0f, 0.5f, 0.5f, 0.5f));

			var vertexSize = Marshal.SizeOf(typeof(MyVertex));

			context.InputAssembler.InputLayout = m_layout;
			context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
			context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(m_vertexBuffer, vertexSize, 0));

			for (int i = 0; i < m_technique.Description.PassCount; ++i)
			{
				m_pass.Apply(context);
				context.Draw(4, 0);
			}

			context.Flush();
		}

		#region IDisposable
		bool m_disposed;

		~SingleQuad11()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!m_disposed)
			{
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Dispose unmanaged resources

				if (m_renderTargetView != null) { m_renderTargetView.Dispose(); m_renderTargetView = null; }
				if (m_layout != null) { m_layout.Dispose(); m_layout = null; }
				if (m_vertexBuffer != null) { m_vertexBuffer.Dispose(); m_vertexBuffer = null; }
				if (m_effect != null) { m_effect.Dispose(); m_effect = null; }
				if (m_tileBuffer != null) { m_tileBuffer.Dispose(); m_tileBuffer = null; }
				if (m_tileBufferView != null) { m_tileBufferView.Dispose(); m_tileBufferView = null; }
				if (m_tileTextureView != null) { m_tileTextureView.Dispose(); m_tileTextureView = null; }
				if (m_rasterizerState != null) { m_rasterizerState.Dispose(); m_rasterizerState = null; }
				if (m_colorBufferView != null) { m_colorBufferView.Dispose(); m_colorBufferView = null; }
				
				m_disposed = true;
			}
		}
		#endregion
	}
}
