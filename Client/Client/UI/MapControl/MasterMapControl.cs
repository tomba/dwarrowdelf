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
using System.Windows.Media.Animation;
using Dwarrowdelf.Client.UI;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Handles selection rectangles etc. extra stuff
	/// </summary>
	class MasterMapControl : UserControl, INotifyPropertyChanged, IDisposable
	{
		public TileInfo TileInfo { get; private set; }
		public TileAreaInfo SelectedTileAreaInfo { get; private set; }

		public MapControl MapControl { get { return m_mapControl; } }
		MapControl m_mapControl;

		Canvas m_selectionCanvas;
		Canvas m_elementCanvas;

		const int ANIM_TIME_MS = 200;

		const double MAXTILESIZE = 64;
		const double MINTILESIZE = 2;

		double? m_targetTileSize;
		IntVector m_scrollVector;

		MapControlToolTipService m_toolTipService;
		MapControlSelectionService m_selectionService;
		MapControlElementsService m_elementsService;
		MapControlDragService m_dragService;

		public event Action<MapSelection> GotSelection;

		public MasterMapControl()
		{
			this.Focusable = true;

			this.SelectedTileAreaInfo = new TileAreaInfo();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var grid = new Grid();
			grid.ClipToBounds = true;
			AddChild(grid);

			MapControl mc = new MapControl();
			
			grid.Children.Add(mc);
			m_mapControl = mc;
			m_mapControl.ZChanged += OnZChanged;
			m_mapControl.EnvironmentChanged += OnEnvironmentChanged;
			m_mapControl.CenterPosChanged += cp => Notify("CenterPos");

			m_elementCanvas = new Canvas();
			grid.Children.Add(m_elementCanvas);

			m_selectionCanvas = new Canvas();
			grid.Children.Add(m_selectionCanvas);

			this.TileSize = 16;

			m_toolTipService = new MapControlToolTipService(m_mapControl);
			m_toolTipService.IsToolTipEnabled = true;

			m_selectionService = new MapControlSelectionService(this, m_selectionCanvas);
			m_selectionService.GotSelection += s => { if (this.GotSelection != null) this.GotSelection(s); };
			m_selectionService.SelectionChanged += OnSelectionChanged;

			m_elementsService = new MapControlElementsService(m_mapControl, m_elementCanvas);

			m_dragService = new MapControlDragService(this);

			this.TileInfo = new TileInfo(m_mapControl);
		}

		public void InvalidateTileData()
		{
			if (m_mapControl != null)
				m_mapControl.InvalidateTileData();
		}

		public int Columns { get { return m_mapControl.GridSize.Width; } }
		public int Rows { get { return m_mapControl.GridSize.Height; } }

		public double TileSize
		{
			get { return m_mapControl.TileSize; }

			set
			{
				m_targetTileSize = null;
				m_mapControl.BeginAnimation(MapControl.TileSizeProperty, null);

				value = MyMath.Clamp(value, MAXTILESIZE, MINTILESIZE);
				m_mapControl.TileSize = value;
			}
		}

		public void ZoomIn()
		{
			var ts = m_targetTileSize ?? this.TileSize;
			ts *= 2;
			ZoomTo(ts);
		}

		public void ZoomOut()
		{
			var ts = m_targetTileSize ?? this.TileSize;
			ts /= 2;
			ZoomTo(ts);
		}

		void ZoomTo(double tileSize)
		{
			tileSize = MyMath.Clamp(tileSize, MAXTILESIZE, MINTILESIZE);

			m_targetTileSize = tileSize;

			var anim = new DoubleAnimation(tileSize, new Duration(TimeSpan.FromMilliseconds(ANIM_TIME_MS)), FillBehavior.HoldEnd);
			m_mapControl.BeginAnimation(MapControl.TileSizeProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			e.Handled = true;

			if (this.IsMouseCaptured || (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				if (e.Delta > 0)
					this.Z--;
				else
					this.Z++;

				return;
			}

			// Zoom so that the position under the mouse stays under the mouse

			var origTileSize = m_targetTileSize ?? this.TileSize;

			double targetTileSize = origTileSize;

			if (e.Delta > 0)
				targetTileSize *= 2;
			else
				targetTileSize /= 2;

			targetTileSize = MyMath.Clamp(targetTileSize, MAXTILESIZE, MINTILESIZE);

			if (targetTileSize == origTileSize)
				return;



			var p = e.GetPosition(this);

			Vector v = p - new Point(m_mapControl.ActualWidth / 2, m_mapControl.ActualHeight / 2);
			v /= targetTileSize;
			v.Y = -v.Y;

			var ml = m_mapControl.ScreenPointToMapTile(p);
			var targetCenterPos = ml - v;

			ZoomTo(targetTileSize);
			ScrollTo(targetCenterPos, targetTileSize);

			//Debug.Print("Wheel zoom {0:F2} -> {1:F2}, Center {2:F2} -> {3:F2}", origTileSize, targetTileSize, origCenter, targetCenter);
		}

		/// <summary>
		/// Easing function to adjust map centerpos according to the tilesize change
		/// </summary>
		class MyEase : EasingFunctionBase
		{
			double t0;
			double tn;

			public MyEase(double tileSizeStart, double tileSizeEnd)
			{
				t0 = tileSizeStart;
				tn = tileSizeEnd;
			}

			protected override double EaseInCore(double normalizedTime)
			{
				if (t0 == tn)
					return normalizedTime;

				normalizedTime = 1.0 - normalizedTime;

				double left = 1.0 / t0 - 1.0 / tn;
				double right = 1.0 / t0 - 1.0 / (t0 + (tn - t0) * normalizedTime);

				double res = right / left;

				return 1.0 - res;
			}

			protected override Freezable CreateInstanceCore()
			{
				return new MyEase(t0, tn);
			}
		}

		public MapSelectionMode SelectionMode
		{
			get { return m_selectionService.SelectionMode; }
			set
			{
				m_dragService.IsEnabled = value == MapSelectionMode.None;
				m_selectionService.SelectionMode = value;

				if (value == MapSelectionMode.None)
					ClearValue(UserControl.CursorProperty);
				else
					Cursor = Cursors.Cross;
			}
		}

		public MapSelection Selection
		{
			get { return m_selectionService.Selection; }
			set { m_selectionService.Selection = value; }
		}

		void OnSelectionChanged(MapSelection selection)
		{
			if (!selection.IsSelectionValid)
			{
				this.SelectedTileAreaInfo.Environment = null;
			}
			else
			{
				this.SelectedTileAreaInfo.Environment = this.Environment;
				this.SelectedTileAreaInfo.Selection = selection;
			}

			Notify("Selection");
		}

		protected override void OnGotMouseCapture(MouseEventArgs e)
		{
			m_toolTipService.IsToolTipEnabled = false;
			base.OnGotMouseCapture(e);
		}

		protected override void OnLostMouseCapture(MouseEventArgs e)
		{
			m_toolTipService.IsToolTipEnabled = true;
			StopScrollToDir();
			base.OnLostMouseCapture(e);
		}

		public Point CenterPos
		{
			get { return m_mapControl.CenterPos; }

			set
			{
				m_mapControl.BeginAnimation(MapControl.CenterPosProperty, null);
				m_mapControl.CenterPos = value;
			}
		}

		public void ScrollTo(EnvironmentObject env, IntPoint3D p)
		{
			this.Environment = env;
			this.Z = p.Z;
			ScrollTo(new Point(p.X, p.Y));
		}

		void ScrollTo(Point target)
		{
			StopScrollToDir();

			if (this.CenterPos == target)
				return;

			var anim = new PointAnimation(target, new Duration(TimeSpan.FromMilliseconds(ANIM_TIME_MS)), FillBehavior.HoldEnd);
			m_mapControl.BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		void ScrollTo(Point target, double targetTileSize)
		{
			StopScrollToDir();

			if (this.CenterPos == target)
				return;

			var anim = new PointAnimation(target, new Duration(TimeSpan.FromMilliseconds(ANIM_TIME_MS)), FillBehavior.HoldEnd);
			anim.EasingFunction = new MyEase(m_mapControl.TileSize, targetTileSize);
			m_mapControl.BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		public void ScrollToDirection(IntVector vector)
		{
			if (vector == m_scrollVector)
				return;

			if (vector == new IntVector())
			{
				StopScrollToDir();
			}
			else
			{
				m_scrollVector = vector;
				BeginScrollToDir();
			}
		}

		void BeginScrollToDir()
		{
			int m = (int)(Math.Sqrt(MAXTILESIZE / this.TileSize) * 32);
			var v = m_scrollVector * m;

			var cp = this.CenterPos + new Vector(v.X, v.Y);

			var anim = new PointAnimation(cp, new Duration(TimeSpan.FromMilliseconds(1000)), FillBehavior.HoldEnd);
			anim.Completed += (o, args) =>
			{
				if (m_scrollVector != new IntVector())
					BeginScrollToDir();
			};
			m_mapControl.BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		void StopScrollToDir()
		{
			if (m_scrollVector != new IntVector())
			{
				m_scrollVector = new IntVector();
				var cp = this.CenterPos;
				m_mapControl.BeginAnimation(MapControl.CenterPosProperty, null);
				this.CenterPos = cp;
			}
		}

		public bool ShowVirtualSymbols
		{
			get { return m_mapControl.ShowVirtualSymbols; }
			set
			{
				m_mapControl.ShowVirtualSymbols = value;
				Notify("ShowVirtualSymbols");
			}
		}

		string m_tileSet = "Char";
		public string TileSet
		{
			get { return m_tileSet; }

			set
			{
				string xaml;

				switch (value)
				{
					case "Char":
						xaml = "SymbolInfosChar.xaml";
						break;

					case "Gfx":
						xaml = "SymbolInfosGfx.xaml";
						break;

					default:
						throw new Exception();
				}

				GameData.Data.SymbolDrawingCache.Load(xaml);
				m_tileSet = value;

				Notify("TileSet");
			}
		}

		public EnvironmentObject Environment
		{
			get { return m_mapControl.Environment; }
			set { m_mapControl.Environment = value; }
		}

		void OnEnvironmentChanged(EnvironmentObject env)
		{
			if (env != null)
			{
				m_mapControl.CenterPos = new Point(env.HomeLocation.X, env.HomeLocation.Y);
				this.Z = env.HomeLocation.Z;
			}

			this.Selection = new MapSelection();

			Notify("Environment");
		}

		public int Z
		{
			get { return m_mapControl.Z; }
			set { m_mapControl.Z = value; }
		}

		void OnZChanged(int z)
		{
			Notify("Z");
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
			if (m_mapControl != null)
			{
				m_mapControl.Dispose();
				m_mapControl = null;
			}
		}
		#endregion
	}
}
