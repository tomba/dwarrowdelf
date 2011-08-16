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
	class MapControl : TileControl.TileControlBase, INotifyPropertyChanged
	{
		IRenderView m_renderView;

		Environment m_env;
		public event Action<Environment> EnvironmentChanged;
		public event Action<int> ZChanged;

		TileControl.ISymbolTileRenderer m_renderer;

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

		protected override void OnInitialized(EventArgs e)
		{
			TileControl.ISymbolTileRenderer renderer;
			IRenderView renderView;

			if (IsD3D10Supported())
			{
				var renderViewDetailed = new RenderViewDetailed();
				var rendererD3D = new TileControl.RendererD3D();
				rendererD3D.RenderData = renderViewDetailed.RenderData;

				renderer = rendererD3D;
				renderView = renderViewDetailed;
			}
			else
			{
				bool detailed = true;

				if (detailed)
				{
					var renderViewDetailed = new RenderViewDetailed();
					renderer = new TileControl.RendererDetailedWPF(renderViewDetailed.RenderData);

					renderView = renderViewDetailed;
				}
				else
				{
					var renderViewSimple = new RenderViewSimple();
					renderer = new TileControl.RendererSimpleWPF(renderViewSimple.RenderData);

					renderView = renderViewSimple;
				}
			}

			m_renderer = renderer;
			m_renderView = renderView;

			SetRenderer(renderer);

			this.TileLayoutChanged += OnTileArrangementChanged;
			this.AboutToRender += OnAboutToRender;

			var symbolDrawingCache = GameData.Data.SymbolDrawingCache;
			symbolDrawingCache.DrawingsChanged += OnSymbolDrawingCacheChanged;
			m_renderer.SymbolDrawingCache = symbolDrawingCache;

			base.OnInitialized(e);
		}

		void OnSymbolDrawingCacheChanged()
		{
			InvalidateTileRender();
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

		public bool ShowVirtualSymbols
		{
			get { return m_renderView.ShowVirtualSymbols; }

			set
			{
				m_renderView.ShowVirtualSymbols = value;
				InvalidateTileData();
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
				}

				InvalidateTileData();

				if (this.EnvironmentChanged != null)
					this.EnvironmentChanged(value);

				Notify("Environment");
			}
		}

		public Rect MapRectToScreenPointRect(IntRect ir)
		{
			Rect r = new Rect(MapTileToScreenPoint(new Point(ir.X1 - 0.5, ir.Y2 - 0.5)),
				new Size(ir.Width * this.TileSize, ir.Height * this.TileSize));
			return r;
		}

		public IntPoint3D ScreenPointToMapLocation(Point p)
		{
			var ml = ScreenPointToMapTile(p);
			return new IntPoint3D((int)Math.Round(ml.X), (int)Math.Round(ml.Y), this.Z);
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

			mc.InvalidateTileData();

			if (mc.ZChanged != null)
				mc.ZChanged(val);
		}


		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTileData();
		}

		void MapObjectChangedCallback(GameObject ob, IntPoint3D l, MapTileObjectChangeType changetype)
		{
			InvalidateTileData();
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
