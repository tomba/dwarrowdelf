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
	sealed class MasterMapControl : MapControl
	{
		public TileView HoverTileView { get; private set; }
		public TileAreaView SelectionTileAreaView { get; private set; }

		Grid m_overlayGrid;
		Canvas m_selectionCanvas;
		Canvas m_elementCanvas;

		const int ANIM_TIME_MS = 200;

		double? m_targetTileSize;
		IntVector2 m_scrollVector;

		MapControlToolTipService m_toolTipService;
		MapControlSelectionService m_selectionService;
		MapControlElementsService m_elementsService;
		MapControlDragService m_dragService;

		public event Action<MapSelection> GotSelection;

		public MasterMapControl()
		{
			m_vc = new VisualCollection(this);

			this.Focusable = true;

			this.SelectionTileAreaView = new TileAreaView();
		}

		VisualCollection m_vc;

		protected override Visual GetVisualChild(int index)
		{
			if (index != 0)
				throw new Exception();

			return m_overlayGrid;
		}

		protected override int VisualChildrenCount
		{
			get { return 1; }
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			m_overlayGrid.Arrange(new Rect(arrangeBounds));

			return base.ArrangeOverride(arrangeBounds);
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			this.ZChanged += OnZChanged;
			this.EnvironmentChanged += OnEnvironmentChanged;
			this.TileLayoutChanged += OnTileLayoutChanged;
			this.MouseMove += OnMouseMove;
			this.MouseLeave += OnMouseLeave;

			this.TileSize = 16;

			this.HoverTileView = new TileView();

			m_overlayGrid = new Grid();
			m_overlayGrid.ClipToBounds = true;
			AddVisualChild(m_overlayGrid);

			m_elementCanvas = new Canvas();
			m_overlayGrid.Children.Add(m_elementCanvas);

			m_selectionCanvas = new Canvas();
			m_overlayGrid.Children.Add(m_selectionCanvas);

			m_toolTipService = new MapControlToolTipService(this, this.HoverTileView);
			m_toolTipService.IsToolTipEnabled = true;

			m_selectionService = new MapControlSelectionService(this, m_selectionCanvas);
			m_selectionService.GotSelection += s => { if (this.GotSelection != null) this.GotSelection(s); };
			m_selectionService.SelectionChanged += OnSelectionChanged;

			m_elementsService = new MapControlElementsService(this, m_elementCanvas);

			m_dragService = new MapControlDragService(this);
		}

		void OnEnvironmentChanged(EnvironmentObject env)
		{
			this.Selection = new MapSelection();
			UpdateHoverTileInfo();
		}

		void OnZChanged(int z)
		{
			UpdateHoverTileInfo();
		}

		void OnMouseLeave(object sender, MouseEventArgs e)
		{
			UpdateHoverTileInfo();
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			UpdateHoverTileInfo();
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize, Point centerPos)
		{
			UpdateHoverTileInfo();
		}

		// used when tracking mouse position
		public Point MousePos { get; private set; }
		public IntPoint2 ScreenLocation { get; private set; }
		bool m_updateHoverTileInfoQueued;

		void UpdateHoverTileInfo()
		{
			if (m_updateHoverTileInfoQueued)
				return;

			m_updateHoverTileInfoQueued = true;
			Dispatcher.BeginInvoke(new Action(_UpdateHoverTileInfo));
		}

		void _UpdateHoverTileInfo()
		{
			Point p;
			IntPoint2 sl;

			if (!this.IsMouseOver)
			{
				p = new Point();
				sl = new IntPoint2();

				this.HoverTileView.ClearTarget();
			}
			else
			{
				p = Mouse.GetPosition(this);
				sl = ScreenPointToIntScreenTile(p);
				var ml = ScreenPointToMapLocation(p);

				if (this.Environment != null && this.Environment.Contains(ml))
				{
					this.HoverTileView.SetTarget(this.Environment, ml);
				}
				else
				{
					this.HoverTileView.ClearTarget();
				}
			}

			if (ClientConfig.ShowMousePos)
			{
				if (p != this.MousePos)
				{
					this.MousePos = p;
					Notify("MousePos");
				}

				if (sl != this.ScreenLocation)
				{
					this.ScreenLocation = sl;
					Notify("ScreenLocation");
				}
			}

			m_updateHoverTileInfoQueued = false;
		}

		public int Columns { get { return this.GridSize.Width; } }
		public int Rows { get { return this.GridSize.Height; } }

		public double AnimatedTileSize	// XXX
		{
			get { return this.TileSize; }

			set
			{
				m_targetTileSize = null;
				BeginAnimation(MapControl.TileSizeProperty, null);

				value = MyMath.Clamp(value, MAXTILESIZE, MINTILESIZE);
				this.TileSize = value;
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
			BeginAnimation(MapControl.TileSizeProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		static bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up: break;
				case Key.Down: break;
				case Key.Left: break;
				case Key.Right: break;
				case Key.Home: break;
				case Key.End: break;
				case Key.PageUp: break;
				case Key.PageDown: break;
				default:
					return false;
			}
			return true;
		}

		void SetScrollDirection()
		{
			var dir = Direction.None;

			if (Keyboard.IsKeyDown(Key.Home))
				dir |= Direction.NorthWest;
			else if (Keyboard.IsKeyDown(Key.PageUp))
				dir |= Direction.NorthEast;
			if (Keyboard.IsKeyDown(Key.PageDown))
				dir |= Direction.SouthEast;
			else if (Keyboard.IsKeyDown(Key.End))
				dir |= Direction.SouthWest;

			if (Keyboard.IsKeyDown(Key.Up))
				dir |= Direction.North;
			else if (Keyboard.IsKeyDown(Key.Down))
				dir |= Direction.South;

			if (Keyboard.IsKeyDown(Key.Left))
				dir |= Direction.West;
			else if (Keyboard.IsKeyDown(Key.Right))
				dir |= Direction.East;

			var fast = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

			var v = IntVector2.FromDirection(dir);

			if (fast)
				v *= 4;

			ScrollToDirection(v);
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
			}
			else if (e.Key == Key.Add)
			{
				ZoomIn();
			}
			else if (e.Key == Key.Subtract)
			{
				ZoomOut();
			}

			base.OnKeyDown(e);
		}

		protected override void OnKeyUp(KeyEventArgs e)
		{
			if (KeyIsDir(e.Key) || e.Key == Key.LeftShift || e.Key == Key.RightShift)
			{
				SetScrollDirection();
			}

			base.OnKeyUp(e);
		}

		protected override void OnTextInput(TextCompositionEventArgs e)
		{
			string text = e.Text;

			e.Handled = true;

			if (text == ">")
			{
				this.Z--;
			}
			else if (text == "<")
			{
				this.Z++;
			}
			else
			{
				e.Handled = false;
			}

			base.OnTextInput(e);
		}

		protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
		{
			this.Focus();
			base.OnPreviewMouseDown(e);
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

			Vector v = p - new Point(this.ActualWidth / 2, this.ActualHeight / 2);
			v /= targetTileSize;
			v.Y = -v.Y;

			var ml = ScreenPointToMapTile(p);
			var targetCenterPos = ml - v;

			ZoomTo(targetTileSize);
			ScrollTo(targetCenterPos, targetTileSize);

			//Debug.Print("Wheel zoom {0:F2} -> {1:F2}, Center {2:F2} -> {3:F2}", origTileSize, targetTileSize, origCenter, targetCenter);
		}

		/// <summary>
		/// Easing function to adjust map centerpos according to the tilesize change
		/// </summary>
		sealed class MyEase : EasingFunctionBase
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
				this.SelectionTileAreaView.Environment = null;
			}
			else
			{
				this.SelectionTileAreaView.Environment = this.Environment;
				this.SelectionTileAreaView.Cuboid = selection.SelectionCuboid;
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

		public Point AnimatedCenterPos	// XXX
		{
			get { return this.CenterPos; }

			set
			{
				BeginAnimation(MapControl.CenterPosProperty, null);
				this.CenterPos = value;
			}
		}

		public void ScrollTo(EnvironmentObject env, IntPoint3 p)
		{
			this.Environment = env;
			this.Z = p.Z;
			ScrollTo(new Point(p.X, p.Y));
		}

		public void ScrollTo(Point target)
		{
			StopScrollToDir();

			if (this.CenterPos == target)
				return;

			var anim = new PointAnimation(target, new Duration(TimeSpan.FromMilliseconds(ANIM_TIME_MS)), FillBehavior.HoldEnd);
			BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		void ScrollTo(Point target, double targetTileSize)
		{
			StopScrollToDir();

			if (this.CenterPos == target)
				return;

			var anim = new PointAnimation(target, new Duration(TimeSpan.FromMilliseconds(ANIM_TIME_MS)), FillBehavior.HoldEnd);
			anim.EasingFunction = new MyEase(this.TileSize, targetTileSize);
			BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		public void ScrollToDirection(IntVector2 vector)
		{
			if (vector == m_scrollVector)
				return;

			if (vector == new IntVector2())
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
				if (m_scrollVector != new IntVector2())
					BeginScrollToDir();
			};
			BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		void StopScrollToDir()
		{
			if (m_scrollVector != new IntVector2())
			{
				m_scrollVector = new IntVector2();
				var cp = this.CenterPos;
				BeginAnimation(MapControl.CenterPosProperty, null);
				this.CenterPos = cp;
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

				var tileSet = new Dwarrowdelf.Client.Symbols.VectorTileSet(xaml);
				GameData.Data.TileSet = tileSet;

				m_tileSet = value;

				Notify("TileSet");
			}
		}

	}
}
