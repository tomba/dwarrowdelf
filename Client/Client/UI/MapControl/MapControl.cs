using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControl : TileControl.TileControlCore, INotifyPropertyChanged, IDisposable
	{
		RenderView m_renderView;
		TileControl.RendererD3DSharpDX m_renderer;

		EnvironmentObject m_env;
		public event Action<EnvironmentObject> EnvironmentChanged;
		public event Action<int> ZChanged;

		const double MINTILESIZE = 2;

		public MapControl()
		{
		}

		bool IsD3D10Supported()
		{
			if (DesignerProperties.GetIsInDesignMode(this))
				return false;

			var os = System.Environment.OSVersion;

			if (os.Version.Major >= 6)
				return true;
			else
				return false;
		}

		#region IDisposable

		bool m_disposed;

		~MapControl()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				//TODO: Managed cleanup code here, while managed refs still valid
			}

			DH.Dispose(ref m_renderer);

			m_disposed = true;
		}
		#endregion

		TileControl.SingleQuad11 m_scene;
		TileControl.RenderData<TileControl.RenderTile> m_renderData;

		protected override void OnInitialized(EventArgs e)
		{
			if (!IsD3D10Supported())
				return;

			m_renderData = new TileControl.RenderData<TileControl.RenderTile>();
			m_renderView = new RenderView(m_renderData);
			m_renderer = new TileControl.RendererD3DSharpDX();
			//m_renderer.Scene = new Dwarrowdelf.Client.TileControl.TestScene();
			m_scene = new TileControl.SingleQuad11();
			var testScene = new Dwarrowdelf.Client.TileControl.TestScene();
			var twoScene = new Dwarrowdelf.Client.TileControl.TwoScene(m_scene, testScene);

			m_renderer.Scene = twoScene;

			this.TileLayoutChanged += OnTileLayoutChanged;

			m_scene.SetTileSet(GameData.Data.TileSet);
			GameData.Data.TileSetChanged += OnTileSetChanged;

			base.OnInitialized(e);
		}

		IntSize2 m_bufferSize;

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			if (m_renderView != null)
			{
				var renderSize = arrangeBounds;

				var columns = (int)Math.Ceiling(renderSize.Width / MINTILESIZE + 1) | 1;
				var rows = (int)Math.Ceiling(renderSize.Height / MINTILESIZE + 1) | 1;

				var bufferSize = new IntSize2(columns, rows);

				if (bufferSize != m_bufferSize)
				{
					m_bufferSize = bufferSize;
					m_renderView.SetMaxSize(bufferSize);
					m_scene.SetupTileBuffer(bufferSize);
					m_renderer.SetRenderRectangle(new Rect(arrangeBounds));
				}
			}

			return base.ArrangeOverride(arrangeBounds);
		}

		void OnTileSetChanged()
		{
			m_scene.SetTileSet(GameData.Data.TileSet);
			InvalidateTileRender();
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize, Point centerPos)
		{
			//System.Diagnostics.Debug.Print("OnTileArrangementChanged( gs {0}, ts {1:F2}, cp {2:F2} )", gridSize, tileSize, centerPos);

			m_renderView.SetSize(gridSize);

			m_renderView.CenterPos = new IntPoint3((int)Math.Round(centerPos.X), (int)Math.Round(centerPos.Y), this.Z);

			m_scene.SetTileSize((float)tileSize);
			m_scene.SetRenderOffset((float)this.RenderOffset.X, (float)this.RenderOffset.Y);
		}

		protected override void OnRenderTiles(DrawingContext drawingContext, Size renderSize, TileControl.TileRenderContext ctx)
		{
			if (ctx.TileDataInvalid)
			{
				m_renderView.Resolve();

				if (m_renderData.Size != ctx.RenderGridSize)
					throw new Exception();

				m_scene.SendMapData(m_renderData.Grid, m_renderData.Width, m_renderData.Height);
			}

			m_renderer.Render(drawingContext);

			//, ctx.RenderGridSize, (float)ctx.TileSize, ctx.RenderOffset,
			//	ctx.TileDataInvalid);
		}

		/// <summary>
		/// Mark the all tile's datacontent as invalid.
		/// Use when tile's visual changes from other reason than normal TileData change.
		/// </summary>
		public void InvalidateRenderViewTiles()
		{
			m_renderView.Invalidate();
			InvalidateTileData();
		}

		/// <summary>
		/// Mark the tile's datacontent as invalid.
		/// Use when tile's visual changes from other reason than normal TileData change.
		/// </summary>
		public void InvalidateRenderViewTile(IntPoint3 ml)
		{
			if (m_renderView.Invalidate(ml))
				InvalidateTileData();
		}

		public bool IsVisibilityCheckEnabled
		{
			get { return m_renderView.IsVisibilityCheckEnabled; }

			set
			{
				m_renderView.IsVisibilityCheckEnabled = value;
				InvalidateTileData();
				Notify("IsVisibilityCheckEnabled");
			}
		}

		public EnvironmentObject Environment
		{
			get { return m_env; }

			set
			{
				if (m_env == value)
					return;

				if (m_env != null)
				{
					m_env.MapTileTerrainChanged -= MapChangedCallback;
					m_env.MapTileObjectChanged -= MapObjectChangedCallback;
					m_env.MapTileExtraChanged -= OnMapTileExtraChanged;
				}

				m_env = value;
				m_renderView.Environment = value;

				if (m_env != null)
				{
					m_env.MapTileTerrainChanged += MapChangedCallback;
					m_env.MapTileObjectChanged += MapObjectChangedCallback;
					m_env.MapTileExtraChanged += OnMapTileExtraChanged;
				}

				InvalidateTileData();

				if (this.EnvironmentChanged != null)
					this.EnvironmentChanged(value);

				Notify("Environment");
			}
		}

		public Rect MapRectToScreenPointRect(IntGrid2 ir)
		{
			Rect r = new Rect(MapTileToScreenPoint(new Point(ir.X1 - 0.5, ir.Y1 - 0.5)),
				new Size(ir.Columns * this.TileSize, ir.Rows * this.TileSize));
			return r;
		}

		public IntPoint3 ScreenPointToMapLocation(Point p)
		{
			var ml = ScreenPointToMapTile(p);
			return new IntPoint3((int)Math.Round(ml.X), (int)Math.Round(ml.Y), this.Z);
		}

		public int Z
		{
			get { return (int)GetValue(ZProperty); }
			set { SetValue(ZProperty, value); }
		}

		public static readonly DependencyProperty ZProperty =
			DependencyProperty.Register("Z", typeof(int), typeof(MapControl), new UIPropertyMetadata(0, OnZChanged));

		static void OnZChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mc = (MapControl)d;
			var val = (int)e.NewValue;

			var p = mc.CenterPos;

			mc.m_renderView.CenterPos = new IntPoint3((int)Math.Round(p.X), (int)Math.Round(p.Y), val);

			mc.InvalidateTileData();

			if (mc.ZChanged != null)
				mc.ZChanged(val);
		}


		void MapChangedCallback(IntPoint3 l)
		{
			if (!m_renderView.Contains(l))
				return;

			InvalidateTileData();
		}

		void MapObjectChangedCallback(MovableObject ob, IntPoint3 l, MapTileObjectChangeType changetype)
		{
			if (!m_renderView.Contains(l))
				return;

			InvalidateTileData();
		}

		void OnMapTileExtraChanged(IntPoint3 p)
		{
			if (!m_renderView.Contains(p))
				return;

			InvalidateTileData();
		}

		protected void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
	}
}
