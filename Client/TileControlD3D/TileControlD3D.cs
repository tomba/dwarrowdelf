//#define DEBUG_TEXT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using SlimDX.Direct3D11;
using DXGI = SlimDX.DXGI;
using System.Windows.Threading;

namespace Dwarrowdelf.Client.TileControl
{
	/// <summary>
	/// Shows tilemap. Handles only what is seen on the screen, no knowledge of environment, position, etc.
	/// </summary>
	public class TileControlD3D : UserControl, ITileControl, IDisposable
	{
		Image m_image;
		D3DImageSlimDX m_interopImageSource;
		SingleQuad11 m_scene;

		int m_columns;
		int m_rows;

		// XXX REMVOE??
		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		IntPoint m_renderOffset;

		bool m_invalidateRender;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControlD3D");

		public event Action TileArrangementChanged;
		public event Action AboutToRender;

		Device m_device;
		Texture2D m_renderTexture;

		RenderData<RenderTileDetailedD3D> m_map;
		ISymbolDrawingCache m_symbolDrawingCache;
		Texture2D m_tileTextureArray;

		SlimDX.Direct3D11.Buffer m_colorBuffer;

		public TileControlD3D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D3DImageSlimDX();

			m_image = new Image();
			m_image.Source = m_interopImageSource;
			m_image.Stretch = System.Windows.Media.Stretch.None;
			m_image.HorizontalAlignment = HorizontalAlignment.Left;
			m_image.VerticalAlignment = VerticalAlignment.Top;
			this.Content = m_image;

			this.Loaded += new RoutedEventHandler(OnLoaded);
			this.SizeChanged += new SizeChangedEventHandler(OnSizeChanged);

			m_device = Helpers11.CreateDevice();
			m_colorBuffer = Helpers11.CreateGameColorBuffer(m_device);
			m_scene = new SingleQuad11(m_device, m_colorBuffer);

			this.Background = System.Windows.Media.Brushes.LightGreen;
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			trace.TraceInformation("OnLoaded");

			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_invalidateRender = true;
		}

		void InitTextureRenderSurface(int width, int height)
		{
			if (m_renderTexture != null)
			{
				m_interopImageSource.SetBackBufferSlimDX(null);
				m_renderTexture.Dispose();
				m_renderTexture = null;
			}

			trace.TraceInformation("CreateTextureRenderSurface {0}x{1}", width, height);
			m_renderTexture = Helpers11.CreateTextureRenderSurface(m_device, width, height);
			m_scene.SetRenderTarget(m_renderTexture);
			m_interopImageSource.SetBackBufferSlimDX(m_renderTexture);
		}

		void OnSizeChanged(object sender, SizeChangedEventArgs e)
		{
			trace.TraceInformation("OnSizeChanged({0})", e.NewSize);
			UpdateSizes(e.NewSize, this.TileSize);
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			trace.TraceInformation("OnRender");

			if (m_columns == 0 || m_rows == 0 || m_map == null)
				return;

			if (m_invalidateRender)
			{
				m_interopImageSource.Lock();
				DoRender();
				m_interopImageSource.InvalidateD3DImage();
				m_interopImageSource.Unlock();
			}
		}

		public void InvalidateRender()
		{
			m_invalidateRender = true;
			InvalidateVisual();
		}

		void DoRender()
		{
			trace.TraceInformation("DoRender");

			m_invalidateRender = false;

			if (m_scene == null)
				return;

			if (this.TileSize == 0)
				return;

			if (AboutToRender != null)
				AboutToRender();

			m_scene.Render();
		}





		public int TileSize
		{
			get { return (int)GetValue(TileSizeProperty); }
			set { SetValue(TileSizeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TileSizeProperty =
			DependencyProperty.Register("TileSize", typeof(int), typeof(TileControlD3D), new UIPropertyMetadata(16, OnTileSizeChanged));

		static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TileControlD3D tc = (TileControlD3D)d;
			int ts = (int)e.NewValue;

			tc.trace.TraceInformation("TileSize = {0}", ts);

			if (tc.m_scene != null)
				tc.m_scene.TileSize = ts;

			tc.UpdateSizes(tc.RenderSize, ts);
		}
		
		/*
		public int TileSize
		{
			get { return m_tileSize; }
			set
			{
				if (value == m_tileSize)
					return;

				trace.TraceInformation("TileSize = {0}", value);

				m_tileSize = value;

				if (m_scene != null)
					m_scene.TileSize = value;

				UpdateSizes(this.RenderSize, m_tileSize);
			}
		}
		*/

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			p -= new Vector(m_renderOffset.X, m_renderOffset.Y);
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p += new Vector(m_renderOffset.X, m_renderOffset.Y);
			return p;
		}

		/// <summary>
		/// When TileControl size changes or TileSize changes
		/// </summary>
		void UpdateSizes(Size viewSize, int tileSize)
		{
			var columns = (int)Math.Ceiling(viewSize.Width / tileSize) | 1;
			var rows = (int)Math.Ceiling(viewSize.Height / tileSize) | 1;

			bool invalidate = false;

			if (columns != m_columns || rows != m_rows)
			{
				trace.TraceInformation("Columns x Rows = {0}x{1}", columns, rows);

				m_columns = columns;
				m_rows = rows;

				invalidate = true;
			}

			var renderWidth = (int)Math.Ceiling(viewSize.Width);
			var renderHeight = (int)Math.Ceiling(viewSize.Height);

			var renderOffsetX = (int)(renderWidth - tileSize * columns) / 2;
			var renderOffsetY = (int)(renderHeight - tileSize * rows) / 2;
			var renderOffset = new IntPoint(renderOffsetX, renderOffsetY);

			if (renderOffset != m_renderOffset)
			{
				trace.TraceInformation("RenderOffset {0}", m_renderOffset);

				m_renderOffset = renderOffset;

				invalidate = true;
			}

			if (m_map != null)
				m_map.Size = new IntSize(m_columns, m_rows);
			
			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				SetPixelSize(renderWidth, renderHeight);
			else if (invalidate)
				InvalidateRender();
			
			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		void SetPixelSize(int width, int height)
		{
			trace.TraceInformation("SetPixelSize({0}, {1})", width, height);

			if (m_scene != null)
			{
				m_interopImageSource.Lock();
				InitTextureRenderSurface(width, height);
				DoRender();
				m_interopImageSource.Unlock();
				InvalidateRender();
			}
		}

		public void SetRenderData(IRenderData renderData)
		{
			if (!(renderData is RenderData<RenderTileDetailedD3D>))
				throw new NotSupportedException();

			m_map = (RenderData<RenderTileDetailedD3D>)renderData;
			m_map.Size = new IntSize(m_columns, m_rows);

			if (m_scene != null)
			{
				m_scene.SetRenderData(m_map);
				InvalidateRender();
			}
		}

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;

				if (m_tileTextureArray != null)
				{
					m_tileTextureArray.Dispose();
					m_tileTextureArray = null;
				}

				m_tileTextureArray = Helpers11.CreateTextures11(m_device, m_symbolDrawingCache);

				if (m_scene != null)
				{
					m_scene.SetTileTextures(m_tileTextureArray);
					InvalidateRender();
				}
			}
		}

		#region IDisposable
		bool m_disposed;

		~TileControlD3D()
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

				if (m_colorBuffer != null)
				{
					m_colorBuffer.Dispose();
					m_colorBuffer = null;
				}

				if (m_device != null)
				{
					m_device.Dispose();
					m_device = null;
				}

				m_disposed = true;
			}
		}
		#endregion
	}
}
