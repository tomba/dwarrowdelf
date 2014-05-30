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
	enum MapControlOrientation
	{
		XY,
		XZ,
		ZY,
	}

	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControl : TileControl.TileControlCore, INotifyPropertyChanged, IDisposable
	{
		bool m_initialized;
		DataGrid2D<TileControl.RenderTile> m_renderData;
		TileControl.SceneHostWPF m_renderer;
		TileControl.TileMapScene m_scene;

		public MapControlOrientation Orientation { get; set; }

		EnvironmentObject m_env;
		public event Action<EnvironmentObject> EnvironmentChanged;
		public event Action<int> ZChanged;

		const double MINTILESIZE = 2;

		bool m_isVisibilityCheckEnabled;
		IntSize2 m_bufferSize;
		IntGrid3 m_bounds;
		IntPoint3 m_oldCenterPos;

		/* How many levels to show */
		const int MAXLEVEL = 4;

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
			m_renderer = new TileControl.SceneHostWPF();
			m_scene = new TileControl.TileMapScene();
			m_renderer.Scene = m_scene;

			this.TileLayoutChanged += OnTileLayoutChanged;

			m_scene.SetTileSet(GameData.Data.TileSet);
			GameData.Data.TileSetChanged += OnTileSetChanged;

			GameData.Data.IsVisibilityCheckEnabledChanged += v =>
			{
				m_isVisibilityCheckEnabled = v;
				InvalidateTileData();
			};

			m_initialized = true;

			base.OnInitialized(e);
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			if (!m_initialized)
				return base.ArrangeOverride(arrangeBounds);

			var maxColumns = (int)Math.Ceiling(arrangeBounds.Width / MINTILESIZE + 1) | 1;
			var maxRows = (int)Math.Ceiling(arrangeBounds.Height / MINTILESIZE + 1) | 1;

			var bufferSize = new IntSize2(maxColumns, maxRows);

			if (bufferSize != m_bufferSize)
			{
				m_bufferSize = bufferSize;
				m_renderData.SetMaxSize(bufferSize);
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

			if (gridSize != m_renderData.Size)
			{
				m_renderData.SetSize(gridSize);
			}

			var cp = ContentTileToMapLocation(centerPos);

			if (!m_renderData.Invalid)
			{
				var diff = cp - m_oldCenterPos;

				// We should never hit this, as the renderdata is invalid when Z changes
				//if (diff.Z != 0)
				//	throw new Exception();

#warning foo
				InvalidateRenderViewTiles();
				//m_renderData.Scroll(new IntVector2(diff.X, diff.Y));
			}

			m_oldCenterPos = cp;

			var s = gridSize;
			m_bounds = new IntGrid3(new IntPoint3(cp.X - s.Width / 2, cp.Y - s.Height / 2, cp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));

			m_scene.SetTileSize((float)tileSize);
			m_scene.SetRenderOffset((float)this.RenderOffset.X, (float)this.RenderOffset.Y);
		}

		protected override void OnRenderTiles(DrawingContext drawingContext, Size renderSize, TileControl.TileRenderContext ctx)
		{
			if (!m_initialized)
				return;

			if (ctx.TileDataInvalid)
			{
				var baseLoc = ScreenTileToMapLocation(new System.Windows.Point(0, 0));
				var xInc = this.XInc;
				var yInc = this.YInc;
				bool symbolToggler = false;
				// XXX
				RenderResolver.Resolve(m_env, m_renderData, m_isVisibilityCheckEnabled,
					baseLoc, xInc, yInc, symbolToggler);

				if (m_renderData.Size != ctx.RenderGridSize)
					throw new Exception();

				m_scene.SendMapData(m_renderData.Grid, m_renderData.Width, m_renderData.Height);
			}

			m_renderer.Render(drawingContext);
		}

		/// <summary>
		/// Mark the all tile's datacontent as invalid.
		/// Use when tile's visual changes from other reason than normal TileData change.
		/// </summary>
		public void InvalidateRenderViewTiles()
		{
			m_renderData.Invalid = true;
			InvalidateTileData();
		}

		/// <summary>
		/// Mark the tile's datacontent as invalid.
		/// Use when tile's visual changes from other reason than normal TileData change.
		/// </summary>
		public void InvalidateRenderViewTile(IntPoint3 ml)
		{
#warning foo
			InvalidateRenderViewTiles();
			return;

			if (!m_bounds.Contains(ml))
				return;

			var p = MapLocationToIntScreenTile(ml);
			int idx = m_renderData.GetIdx(p);
			m_renderData.Grid[idx].IsValid = false;

			InvalidateTileData();
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

				if (m_env != null)
				{
					m_env.MapTileTerrainChanged += MapChangedCallback;
					m_env.MapTileObjectChanged += MapObjectChangedCallback;
					m_env.MapTileExtraChanged += OnMapTileExtraChanged;
				}

				InvalidateRenderViewTiles();

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

#warning testing
			var t0 = new Point((int)Math.Round(ct.X), (int)Math.Round(ct.Y));
			var t1 = ContentTileToMapLocation(ct);
			var t2 = MapLocationToContentTile(t1);
			if (t0 != t2)
				throw new Exception();

			return ContentTileToMapLocation(ct);
		}

		public IntPoint3 ScreenTileToMapLocation(Point p)
		{
			var ct = ScreenTileToContentTile(p);
			return ContentTileToMapLocation(ct);
		}

		public IntPoint3 ContentTileToMapLocation(Point p)
		{
			return ContentTileToMapLocation(p, this.Z);
		}

		public IntPoint3 ContentTileToMapLocation(Point p, int z)
		{
			int x = (int)Math.Round(p.X);
			int y = (int)Math.Round(p.Y);

			switch (this.Orientation)
			{
				case MapControlOrientation.XY:
					return new IntPoint3(x, y, z);
				case MapControlOrientation.XZ:
					return new IntPoint3(x, z, this.GridSize.Height - y);
				case MapControlOrientation.ZY:
					return new IntPoint3(z, y, x);
				default:
					throw new NotImplementedException();
			}
		}

		public Point MapLocationToContentTile(IntPoint3 p)
		{
			switch (this.Orientation)
			{
				case MapControlOrientation.XY:
					return new Point(p.X, p.Y);
				case MapControlOrientation.XZ:
					return new Point(p.X, this.GridSize.Height - p.Z);
				case MapControlOrientation.ZY:
					return new Point(p.Z, p.Y);
				default:
					throw new NotImplementedException();
			}
		}

		public IntPoint2 MapLocationToIntScreenTile(IntPoint3 p)
		{
			var ct = MapLocationToContentTile(p);
			var st = ContentTileToScreenTile(ct);
			return new IntPoint2((int)Math.Round(st.X), (int)Math.Round(st.Y));
		}

		IntVector3 XInc
		{
			get
			{
				switch (this.Orientation)
				{
					case MapControlOrientation.XY:
						return new IntVector3(1, 0, 0);
					case MapControlOrientation.XZ:
						return new IntVector3(1, 0, 0);
					case MapControlOrientation.ZY:
						return new IntVector3(0, 0, 1);
					default:
						throw new NotImplementedException();
				}
			}
		}

		IntVector3 YInc
		{
			get
			{
				switch (this.Orientation)
				{
					case MapControlOrientation.XY:
						return new IntVector3(0, 1, 0);
					case MapControlOrientation.XZ:
						return new IntVector3(0, 0, -1);
					case MapControlOrientation.ZY:
						return new IntVector3(0, 1, 0);
					default:
						throw new NotImplementedException();
				}
			}
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

			var cp = mc.ContentTileToMapLocation(p, val);

			mc.m_oldCenterPos = cp;

			var s = mc.GridSize;
			mc.m_bounds = new IntGrid3(new IntPoint3(cp.X - s.Width / 2, cp.Y - s.Height / 2, cp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));

			mc.InvalidateRenderViewTiles();

			if (mc.ZChanged != null)
				mc.ZChanged(val);
		}


		void MapChangedCallback(IntPoint3 l)
		{
			InvalidateRenderViewTile(l);
		}

		void MapObjectChangedCallback(MovableObject ob, IntPoint3 l, MapTileObjectChangeType changetype)
		{
			InvalidateRenderViewTile(l);
		}

		void OnMapTileExtraChanged(IntPoint3 p)
		{
			InvalidateRenderViewTile(p);
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
