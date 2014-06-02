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
		public event Action<MapControl, double> ZChanged;

		const double MINTILESIZE = 2;

		bool m_isVisibilityCheckEnabled;
		IntSize2 m_bufferSize;
		IntGrid3 m_bounds;
		IntPoint3 m_oldCenterPos;

		/* How many levels to show */
		const int MAXLEVEL = 4;

		public MapControl()
		{
			base.ScreenCenterPosChanged += MapControl_ScreenCenterPosChanged;
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
			m_renderer = new SceneHostWPF();
			m_scene = new TileMapScene(this.Orientation == TileControlOrientation.ZY ? true : false);
			m_renderer.Scene = m_scene;

			this.TileLayoutChanged += OnTileLayoutChanged;

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

			var bufferSize = new IntSize2(maxColumns, maxRows);

			if (bufferSize != m_bufferSize)
			{
				m_bufferSize = bufferSize;
				m_renderData.SetMaxSize(bufferSize);
				m_scene.SetupTileBuffer(bufferSize);
				m_renderer.SetRenderSize(new IntSize2(MyMath.Ceiling(arrangeBounds.Width), MyMath.Ceiling(arrangeBounds.Height)));
			}

			return base.ArrangeOverride(arrangeBounds);
		}

		void OnTileSetChanged()
		{
			m_scene.SetTileSet(GameData.Data.TileSet);
			InvalidateTileRender();
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			if (!m_initialized)
				return;

			//System.Diagnostics.Debug.Print("OnTileArrangementChanged( gs {0}, ts {1:F2}, cp {2:F2} )", gridSize, tileSize, centerPos);

			if (gridSize != m_renderData.Size)
			{
				m_renderData.SetSize(gridSize);
			}

			var intcp = this.ScreenCenterPos.ToIntPoint3();

			if (!m_renderData.Invalid)
			{
				var diff = intcp - m_oldCenterPos;

				// We should never hit this, as the renderdata is invalid when Z changes
				if (diff.Z != 0)
					//throw new Exception();
					InvalidateRenderViewTiles();
				else
					m_renderData.Scroll(diff.ToIntVector2());
			}

			m_oldCenterPos = intcp;

			var cp = this.MapCenterPos.ToIntPoint3();
			var s = gridSize;
			m_bounds = new IntGrid3(new IntPoint3(cp.X - s.Width / 2, cp.Y - s.Height / 2, cp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));

			m_scene.SetTileSize((float)tileSize);
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

				if (m_renderData.Size != ctx.RenderGridSize)
					throw new Exception();

				m_scene.SendMapData(m_renderData.Grid, m_renderData.Width, m_renderData.Height);
			}

			m_scene.SetRenderOffset((float)ctx.RenderOffset.X, (float)ctx.RenderOffset.Y);
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

		void MapControl_ScreenCenterPosChanged(object ob, DoublePoint3 scp, IntVector3 diff)
		{
			var mcp = ScreenToMap(scp);
			var imcp = mcp.ToIntPoint3();

			var s = this.GridSize;
			m_bounds = new IntGrid3(new IntPoint3(imcp.X - s.Width / 2, imcp.Y - s.Height / 2, imcp.Z - MAXLEVEL + 1),
				new IntSize3(s, MAXLEVEL));

			//if (diff.Z != 0)
			//	InvalidateRenderViewTiles();

			if (this.MapCenterPosChanged != null)
				this.MapCenterPosChanged(this, mcp);

			if (diff.Z != 0)
			{
				if (this.ZChanged != null)
					this.ZChanged(this, scp.Z);
			}
		}

		public event Action<MapControl, DoublePoint3> MapCenterPosChanged;

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
