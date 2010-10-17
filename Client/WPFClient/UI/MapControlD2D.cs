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

using Dwarrowdelf.Client.TileControlD2D;

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControlD2D : UserControl, IMapControl, INotifyPropertyChanged
	{
		World m_world;

		Environment m_env;
		int m_z;

		TileControlD2D.TileControlD2D m_tileControlD2D;

		IntPoint m_centerPos;
		int m_tileSize;

		public event Action TileArrangementChanged;

		IRenderView m_renderView;
		RenderViewSimple m_renderViewSimple;
		RenderViewDetailed m_renderViewDetailed;

		public MapControlD2D()
		{
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_tileControlD2D = new TileControlD2D.TileControlD2D();
			m_tileControlD2D.SizeChanged += OnTileControlSizeChanged;
			AddChild(m_tileControlD2D);

			m_renderViewSimple = new RenderViewSimple();
			m_renderViewDetailed = new RenderViewDetailed();

			m_renderView = m_renderViewDetailed;
			//m_renderView = m_renderViewSimple;
			m_tileControlD2D.Renderer = m_renderView.Renderer;
		}

		void OnTileControlSizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (TileArrangementChanged != null)
				TileArrangementChanged();
		}

		public int Columns { get { return m_tileControlD2D.Columns; } }
		public int Rows { get { return m_tileControlD2D.Rows; } }

		public void InvalidateTiles()
		{
			m_tileControlD2D.InvalidateRender();
		}

		public int TileSize
		{
			get { return m_tileSize; }

			set
			{
				value = MyMath.IntClamp(value, 256, 1);

				if (value == m_tileSize)
					return;

				m_tileSize = value;
				m_tileControlD2D.TileSize = value;

				if (m_tileSize <= 8)
					m_renderView = m_renderViewSimple;
				else
					m_renderView = m_renderViewDetailed;

				m_renderView.CenterPos = new IntPoint3D(m_centerPos, this.Z);
				m_renderView.Environment = m_env;

				m_tileControlD2D.Renderer = m_renderView.Renderer;
			}
		}

		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		IntPoint BottomLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, -this.Rows / 2); }
		}

		public IntPoint CenterPos
		{
			get { return m_centerPos; }
			set
			{
				if (value == this.CenterPos)
					return;

				m_centerPos = value;

				m_renderView.CenterPos = new IntPoint3D(value, this.Z);

				InvalidateTiles();
			}
		}

		public void InvalidateDrawings()
		{
			//m_bitmapCache.Invalidate();
			//m_tileControlD2D.InvalidateBitmaps();
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
					m_env.MapTileChanged -= MapChangedCallback;
				}

				m_env = value;
				m_renderView.Environment = value;

				if (m_env != null)
				{
					m_env.MapTileChanged += MapChangedCallback;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
						m_renderViewDetailed.SymbolDrawingCache = m_world.SymbolDrawingCache;
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

				m_renderView.CenterPos = new IntPoint3D(m_centerPos, value);

				InvalidateTiles();

				Notify("Z");
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			return ScreenLocationToMapLocation(sl);
		}

		public Point MapLocationToScreenPoint(IntPoint ml)
		{
			var sl = MapLocationToScreenLocation(ml);
			return ScreenLocationToScreenPoint(sl);
		}

		public IntPoint MapLocationToScreenLocation(IntPoint ml)
		{
			return new IntPoint(ml.X - this.TopLeftPos.X, -(ml.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenLocationToMapLocation(IntPoint sl)
		{
			return new IntPoint(sl.X + this.TopLeftPos.X, -(sl.Y - this.TopLeftPos.Y));
		}

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			return m_tileControlD2D.ScreenPointToScreenLocation(p);
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			return m_tileControlD2D.ScreenLocationToScreenPoint(loc);
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
	}
}
