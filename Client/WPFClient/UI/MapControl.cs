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

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Wraps low level tilemap. Handles Environment, position.
	/// </summary>
	class MapControl : TileControl.TileControlBase, INotifyPropertyChanged
	{
		RenderViewDetailed m_renderView;

		World m_world;
		Environment m_env;

		public MapControl()
		{
		}

		protected override void OnInitialized(EventArgs e)
		{
			m_renderView = new RenderViewDetailed();

			var renderer = new TileControl.RendererWPF();
			//var renderer = new TileControl.RendererD3D();
			renderer.RenderData = m_renderView.RenderData;
			SetRenderer(renderer);

			this.TileLayoutChanged += OnTileArrangementChanged;
			this.AboutToRender += OnAboutToRender;

			base.OnInitialized(e);
		}

		void OnTileArrangementChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			//System.Diagnostics.Debug.Print("OnTileArrangementChanged( gs {0}, ts {1:F2}, cp {2:F2} )", gridSize, tileSize, centerPos);

			m_renderView.RenderData.SetSize(gridSize);

			m_renderView.CenterPos = new IntPoint3D((int)Math.Round(centerPos.X), (int)Math.Round(centerPos.Y), this.Z);
		}

		void OnAboutToRender()
		{
			//System.Diagnostics.Debug.Print("OnAboutToRender");

			if (m_env == null)
				return;

			m_renderView.Resolve();
		}

		public void InvalidateSymbols()
		{
			// XXX ???
		}

		public void InvalidateTiles()
		{
			this.InvalidateTileData();
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
						this.SymbolDrawingCache = m_world.SymbolDrawingCache;
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
			get { return (int)GetValue(ZProperty); }
			set { SetValue(ZProperty, value); }
		}

		public static readonly DependencyProperty ZProperty =
			DependencyProperty.Register("Z", typeof(int), typeof(MapControl), new UIPropertyMetadata(0, OnZChanged));

		static void OnZChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var mc = (MapControl)d;
			var val = (int)e.NewValue;

			var p = mc.m_renderView.CenterPos;

			mc.m_renderView.CenterPos = new IntPoint3D(p.X, p.Y, val);

			mc.InvalidateTiles();
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
	}
}
