using System;
using System.Windows;

using SharpDX.Direct3D11;
using System.IO;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RendererD3DSharpDX : ISymbolTileRenderer
	{
		D3DImageSharpDX m_interopImageSource;
		SingleQuad11 m_scene;

		Device m_device;
		Texture2D m_renderTexture;

		Texture2D m_tileTextureArray;

		ITileSet m_tileSet;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControl");

		public RendererD3DSharpDX()
		{
			m_interopImageSource = new D3DImageSharpDX();
			m_interopImageSource.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;

			m_device = Helpers11.CreateDevice();
			m_scene = new SingleQuad11(m_device);
		}

		void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			// This fires when the screensaver kicks in, the machine goes into sleep or hibernate
			// and any other catastrophic losses of the d3d device from WPF's point of view

			if (m_interopImageSource.IsFrontBufferAvailable)
			{
				trace.TraceInformation("Frontbuffer available");

				m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);
				m_interopImageSource.InvalidateD3DImage();
			}
			else
			{
				trace.TraceInformation("Frontbuffer not available");
			}
		}



		void InitTextureRenderSurface(int width, int height)
		{
			if (m_renderTexture != null)
			{
				m_interopImageSource.SetBackBufferSlimDX(null);
				m_renderTexture.Dispose();
				m_renderTexture = null;
			}

			if (width == 0 || height == 0)
				throw new Exception();

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);
			m_renderTexture = Helpers11.CreateTextureRenderSurface(m_device, width, height);
			m_scene.SetRenderTarget(m_renderTexture);
			m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);
		}




		public void Render(System.Windows.Media.DrawingContext drawingContext, Size renderSize, TileRenderContext ctx)
		{
			if (m_disposed)
				return;

			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			m_interopImageSource.Lock();

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
			{
				InitTextureRenderSurface(renderWidth, renderHeight);
				// force a full update if we re-allocate the render surface
				ctx.TileDataInvalid = true;
				ctx.TileRenderInvalid = true;
			}

			if (ctx.TileDataInvalid)
				m_scene.SendMapData(this.RenderData, ctx.RenderGridSize.Width, ctx.RenderGridSize.Height);

			if (ctx.TileRenderInvalid)
			{
				m_scene.Render((float)ctx.TileSize, ctx.RenderOffset);
				m_device.ImmediateContext.Flush();
			}

			m_interopImageSource.InvalidateD3DImage();

			m_interopImageSource.Unlock();

			drawingContext.DrawImage(m_interopImageSource, new Rect(renderSize));
		}

		public RenderData<RenderTileDetailed> RenderData { get; set; }

		public ITileSet TileSet
		{
			get { return m_tileSet; }

			set
			{
				m_tileSet = value;
				OnDrawingsChanged();
			}
		}

		void OnDrawingsChanged()
		{
			if (m_tileTextureArray != null)
			{
				m_tileTextureArray.Dispose();
				m_tileTextureArray = null;
			}

			// XXX what would be a better place for the cache?
			var cachePath = Environment.GetFolderPath(Environment.SpecialFolder.InternetCache);
			var textureName = m_tileSet.Name;

			var texturePath = Path.Combine(cachePath, textureName + ".dds");
			var textureInfoPath = Path.Combine(cachePath, textureName + ".dds.info");

			var assDate = File.GetLastWriteTime(System.Reflection.Assembly.GetExecutingAssembly().Location);

			if (File.Exists(texturePath) && File.Exists(textureInfoPath))
			{
				try
				{
					var lines = File.ReadAllLines(textureInfoPath);
					var tileSetModTime = DateTime.FromBinary(long.Parse(lines[0]));
					var assModTime = DateTime.FromBinary(long.Parse(lines[1]));

					if (tileSetModTime == m_tileSet.ModTime && assModTime == assDate)
					{
						trace.TraceInformation("Loading textures from cache");
						m_tileTextureArray = Texture2D.FromFile<Texture2D>(m_device, texturePath);
					}
				}
				catch (Exception)
				{
				}
			}

			if (m_tileTextureArray == null)
			{
				m_tileTextureArray = Helpers11.CreateTextures11(m_device, m_tileSet);

				// texture saving requires d3dx11.dll
				/*
				Texture2D.ToFile(m_device.ImmediateContext, m_tileTextureArray, ImageFileFormat.Dds, texturePath);

				File.WriteAllLines(textureInfoPath, new string[] {
					m_tileSet.ModTime.ToBinary().ToString(),
					assDate.ToBinary().ToString(),
				});
				 */
			}

			if (m_scene != null)
				m_scene.SetTileTextures(m_tileTextureArray);
		}

		#region IDisposable
		bool m_disposed;

		~RendererD3DSharpDX()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				// Dispose managed resources.
			}

			// Dispose unmanaged resources

			if (m_scene != null)
			{
				m_scene.Dispose();
				m_scene = null;
			}

			if (m_interopImageSource != null)
			{
				m_interopImageSource.Dispose();
				m_interopImageSource = null;
			}

			if (m_renderTexture != null)
			{
				m_renderTexture.Dispose();
				m_renderTexture = null;
			}

			if (m_tileTextureArray != null)
			{
				m_tileTextureArray.Dispose();
				m_tileTextureArray = null;
			}

			if (m_device != null)
			{
				m_device.Dispose();
				m_device = null;
			}

			m_disposed = true;
		}
		#endregion
	}
}
