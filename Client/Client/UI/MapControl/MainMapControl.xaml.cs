using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class MainMapControl : UserControl, IDisposable
	{
		List<MasterMapControl> m_mapList = new List<MasterMapControl>();

		public event Action<MapSelection> GotSelection;

		public MapSelectionMode SelectionMode
		{
			get { return m_mapList.First().SelectionMode; }

			set
			{
				foreach (var map in m_mapList)
					map.SelectionMode = value;
			}
		}

		public MapSelection Selection
		{
			get { return m_mapList.First().Selection; }

			set
			{
				foreach (var map in m_mapList)
					map.Selection = value;
			}
		}

		public EnvironmentObject Environment { get { return m_mapList.First().Environment; } }

		public TileView HoverTileView { get { return m_mapList.First().HoverTileView; } }
		public TileAreaView SelectionTileAreaView { get { return m_mapList.First().SelectionTileAreaView; } }

		public MainMapControl()
		{
			this.Initialized += MainMapControl_Initialized;
			InitializeComponent();
		}

		void MainMapControl_Initialized(object sender, EventArgs e)
		{
			m_mapList.Add(mapXY);
			m_mapList.Add(mapXZ);
			m_mapList.Add(mapZY);

			foreach (var map in m_mapList)
				map.GotSelection += s => { if (this.GotSelection != null) this.GotSelection(s); };

			foreach (var map in m_mapList)
				map.MapCenterPosChanged += map_MapCenterPosChanged;

			foreach (var map in m_mapList)
				map.TileSizeChanged += map_TileSizeChanged;
		}

		bool m_syncingTileSize;

		void map_TileSizeChanged(object ob, double tileSize)
		{
			if (m_syncingTileSize)
				return;

			m_syncingTileSize = true;

			foreach (var map in m_mapList)
			{
				if (map == ob)
					continue;

				map.TileSize = tileSize;
			}

			m_syncingTileSize = false;
		}

		bool m_syncingPos;

		void map_MapCenterPosChanged(object mc, DoublePoint3 ml, IntVector3 diff)
		{
			if (m_syncingPos)
				return;

			m_syncingPos = true;

			foreach (var map in m_mapList)
			{
				if (map == mc)
					continue;

				map.GoTo(ml);
			}

			m_syncingPos = false;
		}


		public void Blink()
		{
			foreach (var map in m_mapList)
				map.InvalidateTileData();
		}

		public void GoTo(MovableObject ob)
		{
			if (ob == null)
				GoTo(null, new IntPoint3());
			else
				GoTo(ob.Environment, ob.Location);
		}

		public void GoTo(EnvironmentObject env, IntPoint3 p)
		{
			foreach (var map in m_mapList)
				map.Environment = env;

			m_mapList.First().GoTo(p);
		}

		public void ScrollTo(MovableObject ob)
		{
			if (ob == null)
				ScrollTo(null, new IntPoint3());
			else
				ScrollTo(ob.Environment, ob.Location);
		}

		public void ScrollTo(EnvironmentObject env, IntPoint3 p)
		{
			foreach (var map in m_mapList)
				map.Environment = env;

			m_mapList.First().ScrollTo(p);
		}

		public void Dispose()
		{
			foreach (var map in m_mapList)
				map.Dispose();
		}

		void OnMapMouseClicked(object sender, MouseButtonEventArgs ev)
		{
			if (this.SelectionMode != MapSelectionMode.None)
				return;

			var mapControl = sender as MapControl;

			var env = this.Environment;

			if (env == null)
				return;

			var ml = mapControl.RenderPointToMapLocation(ev.GetPosition(mapControl));

			var obs = new List<object>();

			var elem = env.GetElementAt(ml);
			if (elem != null)
				obs.Add(env.GetElementAt(ml));

			obs.AddRange(env.GetContents(ml));

			if (obs.Count == 1)
			{
				object ob = obs[0];
				ShowObjectInDialog(ob);
			}
			else if (obs.Count > 0)
			{
				var ctxMenu = (ContextMenu)this.FindResource("objectSelectorContextMenu");

				ctxMenu.ItemsSource = obs;
				ctxMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
				ctxMenu.PlacementTarget = mapControl;
				var rect = mapControl.MapCubeToRenderPointRect(new IntGrid3(ml, new IntSize3(1, 1, 1)));
				ctxMenu.PlacementRectangle = rect;

				ctxMenu.IsOpen = true;
			}

			ev.Handled = true;
		}

		void OnObjectSelectContextMenuClick(object sender, RoutedEventArgs e)
		{
			var item = (MenuItem)e.OriginalSource;
			var ob = item.Header;
			ShowObjectInDialog(ob);
		}

		void ShowObjectInDialog(object ob)
		{
			var dlg = new ObjectEditDialog();
			dlg.DataContext = ob;
			dlg.Owner = Window.GetWindow(this);
			dlg.Show();
		}
	}
}
