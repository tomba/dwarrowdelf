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
using System.Diagnostics;
using Dwarrowdelf.Client.TileControl;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControl : TileControlCoreMap3D, INotifyPropertyChanged, IDisposable
	{
		bool m_initialized;
		DataGrid2D<RenderTile> m_renderData;
		SceneHostWPF m_renderer;
		TileMapScene m_scene;

		EnvironmentObject m_env;
		public event Action<EnvironmentObject> EnvironmentChanged;
		public event CenterPosChanged MapCenterPosChanged;

		const double MINTILESIZE = 2;

		bool m_isVisibilityCheckEnabled;
		IntSize2 m_maxBufferSize;
		IntGrid3 m_bounds;

		/* How many levels to show */
		const int MAXLEVEL = 4;

		public MapControl()
		{
			base.ScreenCenterPosChanged += OnScreenCenterPosChanged;
			this.TileSizeChanged += OnTileSizeChanged;
			base.GridSizeChanged += OnGridSizeChanged;
		}

		void OnTileSizeChanged(object ob, double tileSize)
		{
			Notify("TileSize");
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

			m_renderData = new DataGrid2D<RenderTile>();
			m_renderData.SetMaxSize(new IntSize2(1, 1));
			m_renderer = new SceneHostWPF();
			m_scene = new TileMapScene(this.Orientation == TileControlOrientation.ZY ? true : false);
			m_renderer.Scene = m_scene;

			m_scene.SetTileSet(GameData.Data.TileSet);
			GameData.Data.TileSetChanged += OnTileSetChanged;

			GameData.Data.IsVisibilityCheckEnabledChanged += v =>
			{
				m_isVisibilityCheckEnabled = v;
				InvalidateTileData();
			};

			GameData.Data.Blink += OnBlink;

			m_initialized = true;

			base.OnInitialized(e);
		}

		bool m_symbolToggler;

		void OnBlink()
		{
			// XXX we should invalidate only the needed tiles
			InvalidateRenderViewTiles();
			m_symbolToggler = !m_symbolToggler;
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			if (!m_initialized)
				return base.ArrangeOverride(arrangeBounds);

			var maxColumns = MyMath.Ceiling(arrangeBounds.Width / MINTILESIZE + 1) | 1;
			var maxRows = MyMath.Ceiling(arrangeBounds.Height / MINTILESIZE + 1) | 1;

			var maxBufferSize = new IntSize2(maxColumns, maxRows);

			if (maxBufferSize != m_maxBufferSize)
			{
				m_maxBufferSize = maxBufferSize;
				m_renderData.SetMaxSize(maxBufferSize);
				m_scene.SetupTileBuffer(maxBufferSize);
				InvalidateRenderViewTiles();
			}

			var renderSize = new IntSize2(MyMath.Ceiling(arrangeBounds.Width), MyMath.Ceiling(arrangeBounds.Height));
			m_renderer.SetRenderSize(renderSize);

			return base.ArrangeOverride(arrangeBounds);
		}

		void OnTileSetChanged()
		{
			// XXX we should use the same D3D tileset atlas for all MapControl's with the same tileset
			m_scene.SetTileSet(GameData.Data.TileSet);
			InvalidateTileRender();
		}

		void OnGridSizeChanged(object ob, IntSize2 gridSize)
		{
			if (!m_initialized)
				return;

			if (gridSize != m_renderData.Size)
			{
				m_renderData.SetSize(gridSize);
				UpdateBounds(this.MapCenterPos.ToIntPoint3(), gridSize);
				InvalidateTileData();
			}
		}

		void UpdateBounds(IntPoint3 mapCenterPos, IntSize2 gridSize)
		{
			var cp = mapCenterPos;
			var s = gridSize;
			m_bounds = new IntGrid3(new IntPoint3(cp.X - s.Width / 2, cp.Y - s.Height / 2, cp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));
		}

		protected override void OnRenderTiles(DrawingContext drawingContext, Size renderSize, TileRenderContext ctx)
		{
			if (!m_initialized)
				return;

			if (ctx.TileDataInvalid)
			{
				var baseLoc = RenderTileToMapLocation(new System.Windows.Point(0, 0));
				var xInc = this.XInc;
				var yInc = this.YInc;
				var zInc = this.ZInc;
				// XXX
				RenderResolver.Resolve(m_env, m_renderData, m_isVisibilityCheckEnabled,
					baseLoc, xInc, yInc, zInc, m_symbolToggler);

				Debug.Assert(m_renderData.Size == ctx.RenderGridSize);

				m_scene.SendMapData(m_renderData.Grid, m_renderData.Width, m_renderData.Height);
			}

			m_scene.SetTileSize((float)ctx.TileSize);
			m_scene.SetRenderOffset((float)ctx.RenderOffset.X, (float)ctx.RenderOffset.Y);
			m_renderer.Render(drawingContext);
		}

		/// <summary>
		/// Mark the all tile's datacontent as invalid.
		/// </summary>
		void InvalidateRenderViewTiles()
		{
			m_renderData.Invalid = true;
			InvalidateTileData();
		}

		/// <summary>
		/// Mark the tile's datacontent as invalid.
		/// </summary>
		void InvalidateRenderViewTile(IntPoint3 ml)
		{
			if (!m_bounds.Contains(ml))
				return;

			var p = MapLocationToIntRenderTile(ml);
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

		IntVector3 XInc
		{
			get
			{
				switch (this.Orientation)
				{
					case TileControlOrientation.XY:
						return new IntVector3(1, 0, 0);
					case TileControlOrientation.XZ:
						return new IntVector3(1, 0, 0);
					case TileControlOrientation.ZY:
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
					case TileControlOrientation.XY:
						return new IntVector3(0, 1, 0);
					case TileControlOrientation.XZ:
						return new IntVector3(0, 0, -1);
					case TileControlOrientation.ZY:
						return new IntVector3(0, 1, 0);
					default:
						throw new NotImplementedException();
				}
			}
		}

		IntVector3 ZInc
		{
			get
			{
				switch (this.Orientation)
				{
					case TileControlOrientation.XY:
						return new IntVector3(0, 0, 1);
					case TileControlOrientation.XZ:
						return new IntVector3(0, 1, 0);
					case TileControlOrientation.ZY:
						return new IntVector3(-1, 0, 0);
					default:
						throw new NotImplementedException();
				}
			}
		}

		void OnScreenCenterPosChanged(object ob, DoublePoint3 scp, IntVector3 diff)
		{
			var mcp = ScreenToMap(scp);
			var imcp = mcp.ToIntPoint3();

			UpdateBounds(imcp, this.GridSize);

			if (!diff.IsNull)
			{
				if (diff.Z != 0)
				{
					InvalidateRenderViewTiles();
				}
				else
				{
					m_renderData.Scroll(diff.ToIntVector2());
					InvalidateTileData();
				}
			}

			if (this.MapCenterPosChanged != null)
				this.MapCenterPosChanged(this, mcp, ScreenToMap(diff));

			Notify("ScreenCenterPos");
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
