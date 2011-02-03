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
	public class TileControlD3D : FrameworkElement, ITileControl, IDisposable
	{
		D3DImageSlimDX m_interopImageSource;
		SingleQuad11 m_scene;

		IntSize m_gridSize;

		Point m_renderOffset;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Render", "TileControlD3D");

		bool m_tileLayoutInvalid;
		bool m_tileRenderInvalid;

		public event Action<IntSize> TileLayoutChanged;
		public event Action<Size> AboutToRender;

		Device m_device;
		Texture2D m_renderTexture;

		Texture2D m_tileTextureArray;

		SlimDX.Direct3D11.Buffer m_colorBuffer;

		RenderData<RenderTileDetailedD3D> m_map;
		ISymbolDrawingCache m_symbolDrawingCache;

		public TileControlD3D()
		{
			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			m_interopImageSource = new D3DImageSlimDX();

			this.Loaded += new RoutedEventHandler(OnLoaded);

			m_device = Helpers11.CreateDevice();
			m_colorBuffer = Helpers11.CreateGameColorBuffer(m_device);
			m_scene = new SingleQuad11(m_device, m_colorBuffer);
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			trace.TraceInformation("OnLoaded");

			if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
				return;

			InvalidateVisual();
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

		public double TileSize
		{
			get { return (double)GetValue(TileSizeProperty); }
			set { SetValue(TileSizeProperty, value); }
		}


		// Using a DependencyProperty as the backing store for TileSize.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty TileSizeProperty =
				DependencyProperty.Register("TileSize", typeof(double), typeof(TileControlD3D), new UIPropertyMetadata(16.0, OnTileSizeChanged));

		static void OnTileSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			TileControlD3D tc = (TileControlD3D)d;
			var ts = (double)e.NewValue;

			tc.trace.TraceInformation("TileSize = {0}", ts);

			tc.UpdateTileLayout(tc.RenderSize);
		}






		public Point CenterPos
		{
			get { return (Point)GetValue(CenterPosProperty); }
			set { SetValue(CenterPosProperty, value); }
		}

		public static readonly DependencyProperty CenterPosProperty =
			DependencyProperty.Register("CenterPos", typeof(Point), typeof(TileControlD3D), new UIPropertyMetadata(new Point(), OnCenterPosChanged));


		static void OnCenterPosChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var tc = (TileControlD3D)d;

			var p = (Point)e.NewValue;
			var po = (Point)e.OldValue;

			var x = Math.Round(p.X);
			var y = Math.Round(p.Y);
			tc.trace.TraceVerbose("CenterPos {0:F2}    {1},{2}", p, x, y);

			// XXX
			//if (Math.Round(p.X) != Math.Round(po.X) || Math.Round(p.Y) != Math.Round(po.Y))
			{
				tc.InvalidateMapData();
				//tc.m_mapDataInvalid = true;
			}

			//tc.tileControl_TileArrangementChanged(tc.tileControl.GridSize); // XXX

			tc.UpdateTileLayout(tc.RenderSize);
		}




		Vector ScreenMapDiff { get { return new Vector(Math.Round(this.CenterPos.X), Math.Round(this.CenterPos.Y)); } }

		public Point MapLocationToScreenLocation(Point ml)
		{
			var gridSize = this.GridSize;

			var p = ml - this.ScreenMapDiff;
			p = new Point(p.X, -p.Y);
			p += new Vector(gridSize.Width / 2, gridSize.Height / 2);

			return p;
		}

		public Point ScreenLocationToMapLocation(Point sl)
		{
			var gridSize = this.GridSize;

			var v = sl - new Vector(gridSize.Width / 2, gridSize.Height / 2);
			v = new Point(v.X, -v.Y);
			v += this.ScreenMapDiff;

			return v;
		}

		public Point ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(Point ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public Point ScreenPointToScreenLocation(Point p)
		{
			p -= new Vector(m_renderOffset.X, m_renderOffset.Y);
			return new Point(p.X / this.TileSize - 0.5, p.Y / this.TileSize - 0.5);
		}

		public Point ScreenLocationToScreenPoint(Point loc)
		{
			loc.Offset(0.5, 0.5);
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p += new Vector(m_renderOffset.X, m_renderOffset.Y);
			return p;
		}



		public IntSize GridSize
		{
			get { return m_gridSize; }
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			trace.TraceInformation("ArrangeOverride({0})", arrangeBounds);

			var renderSize = arrangeBounds;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
				UpdateTileLayout(renderSize);

			return base.ArrangeOverride(arrangeBounds);
		}

		protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
		{
			trace.TraceInformation("OnRender");

			var renderSize = this.RenderSize;
			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			if (m_tileLayoutInvalid)
			{
				if (TileLayoutChanged != null)
					TileLayoutChanged(m_gridSize);

				m_tileLayoutInvalid = false;
			}

			if (m_tileRenderInvalid)
			{
				if (this.AboutToRender != null)
					this.AboutToRender(renderSize);

				m_interopImageSource.Lock();
				if (m_interopImageSource.PixelWidth != renderWidth || m_interopImageSource.PixelHeight != renderHeight)
					InitTextureRenderSurface(renderWidth, renderHeight);
				DoRender();
				m_interopImageSource.InvalidateD3DImage();
				m_interopImageSource.Unlock();

				m_tileRenderInvalid = false;
			}

			drawingContext.DrawImage(m_interopImageSource, new System.Windows.Rect(renderSize));

			trace.TraceInformation("OnRender End");
		}

		public void InvalidateRender()
		{
			if (m_tileRenderInvalid == false)
			{
				trace.TraceInformation("InvalidateRender");
				m_tileRenderInvalid = true;
				InvalidateVisual();
			}
		}

		public void InvalidateMapData()
		{
			m_scene.InvalidateMapData();
		}


		void UpdateTileLayout(Size renderSize)
		{
			var tileSize = this.TileSize;

			var renderWidth = (int)Math.Ceiling(renderSize.Width);
			var renderHeight = (int)Math.Ceiling(renderSize.Height);

			var columns = (int)Math.Ceiling(renderSize.Width / tileSize + 1) | 1;
			var rows = (int)Math.Ceiling(renderSize.Height / tileSize + 1) | 1;
			m_gridSize = new IntSize(columns, rows);

			var renderOffsetX = (int)(renderWidth - tileSize * m_gridSize.Width) / 2;
			var renderOffsetY = (int)(renderHeight - tileSize * m_gridSize.Height) / 2;

			var cx = -(this.CenterPos.X - Math.Round(this.CenterPos.X)) * this.TileSize;
			var cy = (this.CenterPos.Y - Math.Round(this.CenterPos.Y)) * this.TileSize;

			m_renderOffset = new Point(renderOffsetX + cx, renderOffsetY + cy);

			if (m_scene != null)
			{
				m_scene.RenderOffset = new Vector(m_renderOffset.X, m_renderOffset.Y);
				m_scene.TileSize = (float)tileSize;
			}


			m_tileLayoutInvalid = true;

			trace.TraceInformation("UpdateTileLayout(rs {0}, gs {1}, ts {2}) -> Off {3:F2}, Grid {4}", renderSize, m_gridSize, tileSize,
				m_renderOffset, m_gridSize);

			InvalidateRender();
		}


		public void SetRenderData(IRenderData renderData)
		{
			if (!(renderData is RenderData<RenderTileDetailedD3D>))
				throw new NotSupportedException();

			m_map = (RenderData<RenderTileDetailedD3D>)renderData;

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

		void DoRender()
		{
			trace.TraceInformation("DoRender");

			if (m_scene == null)
				return;

			if (this.TileSize == 0)
				return;

			m_scene.Render();
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
