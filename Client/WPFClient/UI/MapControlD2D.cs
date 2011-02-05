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

using Dwarrowdelf.Client.TileControl;

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControlD2D : UserControl, INotifyPropertyChanged, IDisposable
	{
		TileControl.TileControlD2D m_tileControlD2D;
		RenderViewDetailed m_renderView;

		World m_world;
		Environment m_env;
		int m_z;

		public event Action TileArrangementChanged;

		public MapControlD2D()
		{
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_renderView = new RenderViewDetailed();

			m_tileControlD2D = new TileControl.TileControlD2D();
			m_tileControlD2D.SetRenderData(m_renderView.RenderData);
			m_tileControlD2D.TileLayoutChanged += OnTileArrangementChanged;
			m_tileControlD2D.AboutToRender += OnAboutToRender;
			AddChild(m_tileControlD2D);
		}

		void OnTileArrangementChanged(IntSize gridSize, Point centerPos)
		{
			System.Diagnostics.Debug.Print("OnTileArrangementChanged( gs {0}, cp {1:F2} )", gridSize, centerPos);

			m_renderView.RenderData.Size = gridSize;

			m_renderView.CenterPos = new IntPoint3D((int)Math.Round(centerPos.X), (int)Math.Round(centerPos.Y), this.Z);

			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		void OnAboutToRender()
		{
			System.Diagnostics.Debug.Print("OnAboutToRender");

			if (m_env == null)
				return;

			m_renderView.Resolve();
		}

		public IntSize GridSize { get { return m_tileControlD2D.GridSize; } }

		public Point CenterPos
		{
			get { return m_tileControlD2D.CenterPos; }
			set { m_tileControlD2D.CenterPos = value; }
		}

		public Point ScreenPointToScreenLocation(Point p)
		{
			return m_tileControlD2D.ScreenPointToScreenLocation(p);
		}

		public Point ScreenLocationToScreenPoint(Point loc)
		{
			return m_tileControlD2D.ScreenLocationToScreenPoint(loc);
		}

		public Point ScreenPointToMapLocation(Point p)
		{
			return m_tileControlD2D.ScreenPointToMapLocation(p);
		}

		public Point MapLocationToScreenPoint(Point ml)
		{
			return m_tileControlD2D.MapLocationToScreenPoint(ml);
		}

		public Point MapLocationToScreenLocation(Point ml)
		{
			return m_tileControlD2D.MapLocationToScreenLocation(ml);
		}

		public Point ScreenLocationToMapLocation(Point sl)
		{
			return m_tileControlD2D.ScreenLocationToMapLocation(sl);
		}

		public void InvalidateTiles()
		{
			m_tileControlD2D.InvalidateTileData();
		}

		public double TileSize
		{
			get { return m_tileControlD2D.TileSize; }

			set
			{
				value = MyMath.Clamp(value, 64, 2);

				m_tileControlD2D.TileSize = value;
			}
		}

		public void InvalidateSymbols()
		{
			m_tileControlD2D.InvalidateSymbols();
			InvalidateTiles();
		}

		public bool ShowVirtualSymbols
		{
			get { return m_renderView.ShowVirtualSymbols; }

			set
			{
				m_renderView.ShowVirtualSymbols = value;
				InvalidateTiles();
				Notify("ShowVirtualSymbols");
			}
		}

		public Environment Environment
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
				}

				m_env = value;
				m_renderView.Environment = value;

				if (m_env != null)
				{
					m_env.MapTileTerrainChanged += MapChangedCallback;
					m_env.MapTileObjectChanged += MapObjectChangedCallback;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
						m_tileControlD2D.SymbolDrawingCache = m_world.SymbolDrawingCache;
					}
				}
				else
				{
					m_world = null;
				}

				InvalidateTiles();

				Notify("Environment");
			}
		}

		public int Z
		{
			get { return m_z; }

			set
			{
				if (m_z == value)
					return;

				m_z = value;

				m_renderView.CenterPos = new IntPoint3D((int)this.CenterPos.X, (int)this.CenterPos.Y, value);

				InvalidateTiles();

				Notify("Z");
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		void MapObjectChangedCallback(ClientGameObject ob, IntPoint3D l, MapTileObjectChangeType changetype)
		{
			InvalidateTiles();
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		#region IDispobable
		public void Dispose()
		{
			if (m_tileControlD2D != null)
			{
				m_tileControlD2D.Dispose();
				m_tileControlD2D = null;
			}
		}
		#endregion
	}
}
