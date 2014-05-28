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
		bool m_initialized;
		RenderViewXY m_renderView;
		DataGrid2D<TileControl.RenderTile> m_renderData;
		TileControl.SceneHostWPF m_renderer;
		TileControl.TileMapScene m_scene;

		EnvironmentObject m_env;
		public event Action<EnvironmentObject> EnvironmentChanged;
		public event Action<int> ZChanged;

		const double MINTILESIZE = 2;

		public MapControl()
		{
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

		protected override void OnInitialized(EventArgs e)
		{
			if (DesignerProperties.GetIsInDesignMode(this))
				return;

			m_renderData = new DataGrid2D<TileControl.RenderTile>();
			m_renderView = new RenderViewXY(m_renderData);
			m_renderer = new TileControl.SceneHostWPF();
			m_scene = new TileControl.TileMapScene();
			m_renderer.Scene = m_scene;

			this.TileLayoutChanged += OnTileLayoutChanged;

			m_scene.SetTileSet(GameData.Data.TileSet);
			GameData.Data.TileSetChanged += OnTileSetChanged;

			m_initialized = true;

			base.OnInitialized(e);
		}

		IntSize2 m_bufferSize;

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			if (!m_initialized)
				return base.ArrangeOverride(arrangeBounds);

			var renderSize = arrangeBounds;

			var columns = (int)Math.Ceiling(renderSize.Width / MINTILESIZE + 1) | 1;
			var rows = (int)Math.Ceiling(renderSize.Height / MINTILESIZE + 1) | 1;

			var bufferSize = new IntSize2(columns, rows);

			if (bufferSize != m_bufferSize)
			{
				m_bufferSize = bufferSize;
				m_renderView.SetMaxSize(bufferSize);
				m_scene.SetupTileBuffer(bufferSize);
				m_renderer.SetRenderSize(new IntSize2((int)Math.Ceiling(arrangeBounds.Width), (int)Math.Ceiling(arrangeBounds.Height)));
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
			if (!m_initialized)
				return;

			//System.Diagnostics.Debug.Print("OnTileArrangementChanged( gs {0}, ts {1:F2}, cp {2:F2} )", gridSize, tileSize, centerPos);

			m_renderView.SetSize(gridSize);

			m_renderView.CenterPos = new IntPoint3((int)Math.Round(centerPos.X), (int)Math.Round(centerPos.Y), this.Z);

			m_scene.SetTileSize((float)tileSize);
			m_scene.SetRenderOffset((float)this.RenderOffset.X, (float)this.RenderOffset.Y);
		}

		protected override void OnRenderTiles(DrawingContext drawingContext, Size renderSize, TileControl.TileRenderContext ctx)
		{
			if (!m_initialized)
				return;

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
			get { return m_renderView != null && m_renderView.IsVisibilityCheckEnabled; }

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
			Rect r = new Rect(ContentTileToScreenPoint(new Point(ir.X1 - 0.5, ir.Y1 - 0.5)),
				new Size(ir.Columns * this.TileSize, ir.Rows * this.TileSize));
			return r;
		}

		public IntPoint3 ScreenPointToMapLocation(Point p)
		{
			var ct = ScreenPointToContentTile(p);
			return new IntPoint3((int)Math.Round(ct.X), (int)Math.Round(ct.Y), this.Z);
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
