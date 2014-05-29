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
		// XXX create proper WPF event
		public event Action<MapSelection> GotSelection;

		public bool IsVisibilityCheckEnabled { get; set; }

		public MapSelectionMode SelectionMode { get; set; }

		public EnvironmentObject Environment { get { return mapXY.Environment; } }

		public MapSelection Selection { get; set; }

		public TileView FocusedTileView { get { return mapXY.FocusedTileView; } }

		public MainMapControl()
		{
			InitializeComponent();
		}

		public void Blink()
		{
			mapXY.InvalidateTileData();
			mapXZ.InvalidateTileData();
			mapYZ.InvalidateTileData();
		}

		public void ScrollToImmediate(EnvironmentObject env, IntPoint3 p)
		{
			mapXY.ScrollToImmediate(env, p);
		}

		public void ScrollTo(EnvironmentObject env, IntPoint3 p)
		{
			mapXY.ScrollTo(env, p);
		}

		public void Dispose()
		{
			mapXY.Dispose();
			mapXZ.Dispose();
			mapYZ.Dispose();
		}

		public IntPoint3 ScreenPointToMapLocation(Point p)
		{
			return mapXY.ScreenPointToMapLocation(p);
		}

		public Rect MapRectToScreenPointRect(IntGrid2 ir)
		{
			return mapXY.MapRectToScreenPointRect(ir);
		}
	}
}
