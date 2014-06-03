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

		KeyHandler m_keyHandler;

		const double INITIALTILESIZE = 16;
		const double MAXTILESIZE = 64;
		const double MINTILESIZE = 2;

		public MasterMapControl()
		{
			m_vc = new VisualCollection(this);

			this.Focusable = true;

			this.SelectionTileAreaView = new TileAreaView();

			m_keyHandler = new KeyHandler(this);
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

			this.ScreenCenterPosChanged += OnScreenCenterPosChanged;
			this.EnvironmentChanged += OnEnvironmentChanged;
			this.TileLayoutChanged += OnTileLayoutChanged;
			this.MouseMove += OnMouseMove;
			this.MouseLeave += OnMouseLeave;

			this.TileSize = INITIALTILESIZE;

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
			m_dragService.IsEnabled = m_selectionService.SelectionMode == MapSelectionMode.None;

			if (ClientConfig.ShowMapDebug)
			{
				var bar = new System.Windows.Controls.Primitives.StatusBar();
				bar.HorizontalAlignment = System.Windows.HorizontalAlignment.Right;
				bar.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
				m_overlayGrid.Children.Add(bar);

				TextBlock textBlock = new TextBlock();
				var binding = new Binding("ScreenCenterPos");
				binding.Source = this;
				binding.Converter = new CoordinateValueConverter();
				textBlock.SetBinding(TextBlock.TextProperty, binding);
				bar.Items.Add(textBlock);

				bar.Items.Add(new Separator());

				textBlock = new TextBlock();
				binding = new Binding("TileSize");
				binding.Source = this;
				binding.StringFormat = "{0:F2}";
				textBlock.SetBinding(TextBlock.TextProperty, binding);
				bar.Items.Add(textBlock);
			}
		}

		void OnEnvironmentChanged(EnvironmentObject env)
		{
			this.Selection = new MapSelection();
			UpdateHoverTileInfo(false);
		}

		void OnScreenCenterPosChanged(object control, DoublePoint3 centerPos, IntVector3 diff)
		{
			if (diff.Z != 0)
				UpdateHoverTileInfo(false);
		}

		void OnMouseLeave(object sender, MouseEventArgs e)
		{
			UpdateHoverTileInfo(false);
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			UpdateHoverTileInfo(true);
		}

		void OnTileLayoutChanged(IntSize2 gridSize, double tileSize)
		{
			UpdateHoverTileInfo(false);
		}

		// used when tracking mouse position for HoverTileView
		bool m_updateHoverTileInfoQueued;
		bool m_hoverTileMouseMove;

		void UpdateHoverTileInfo(bool mouseMove)
		{
			m_hoverTileMouseMove = mouseMove;

			if (m_updateHoverTileInfoQueued)
				return;

			m_updateHoverTileInfoQueued = true;
			Dispatcher.BeginInvoke(new Action(_UpdateHoverTileInfo));
		}

		void _UpdateHoverTileInfo()
		{
			Point p;

			if (!this.IsMouseOver || !m_hoverTileMouseMove)
			{
				p = new Point();

				this.HoverTileView.ClearTarget();
			}
			else
			{
				p = Mouse.GetPosition(this);
				var ml = RenderPointToMapLocation(p);

				if (this.Environment != null && this.Environment.Contains(ml))
				{
					this.HoverTileView.SetTarget(this.Environment, ml);
				}
				else
				{
					this.HoverTileView.ClearTarget();
				}
			}

			if (ClientConfig.ShowMouseDebug)
			{
				var data = MapControlDebugData.Data;

				var sp = Mouse.GetPosition(this);

				data.ScreenPos = sp;
				data.ScreenTile = RenderPointToRenderTile(sp);
				data.MapTile = RenderPointToScreen(sp);
				data.MapLocation = RenderPointToMapLocation(sp);
				data.Update();
			}

			m_updateHoverTileInfoQueued = false;
		}

		public int Columns { get { return this.GridSize.Width; } }
		public int Rows { get { return this.GridSize.Height; } }

		// Override the TileSize property, so that we can stop the animation
		public new double TileSize
		{
			get { return base.TileSize; }

			set
			{
				m_targetTileSize = null;
				BeginAnimation(MapControl.TileSizeProperty, null);

				value = MyMath.Clamp(value, MAXTILESIZE, MINTILESIZE);
				base.TileSize = value;
			}
		}

		// Override the MapCenterPos property, so that we can stop the animation
		public new DoublePoint3 MapCenterPos
		{
			get { return base.MapCenterPos; }

			set
			{
				BeginAnimation(MapControl.ScreenCenterPosProperty, null);
				base.MapCenterPos = value;
			}
		}

		// Override the ScreenCenterPos property, so that we can stop the animation
		public new DoublePoint3 ScreenCenterPos
		{
			get { return base.ScreenCenterPos; }

			set
			{
				BeginAnimation(MapControl.ScreenCenterPosProperty, null);
				base.ScreenCenterPos = value;
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
					this.ScreenCenterPos += Direction.Down;
				else
					this.ScreenCenterPos += Direction.Up;

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

			var ct = RenderPointToScreen(p);
			var targetCenterPos = ct - v;

			var dp = ScreenTileToMapPoint(targetCenterPos, this.ScreenCenterPos.Z);

			ZoomTo(targetTileSize);
			ScrollTo(dp, targetTileSize);

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
				this.SelectionTileAreaView.Box = new IntGrid3();
			}
			else
			{
				this.SelectionTileAreaView.Environment = this.Environment;
				this.SelectionTileAreaView.Box = selection.SelectionBox;
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

		public void Pan(Vector p)
		{
			var mp = this.MapCenterPos + ScreenToMap(new DoubleVector3(p.X, p.Y, 0));
			GoTo(mp);
		}

		public void GoTo(IntPoint3 p)
		{
			GoTo(p.ToDoublePoint3());
		}

		public void GoTo(DoublePoint3 p)
		{
			this.MapCenterPos = p;
		}

		public void ScrollTo(IntPoint3 p)
		{
			ScrollTo(p.ToDoublePoint3());
		}

		void ScrollTo(DoublePoint3 p, double? targetTileSize = null)
		{
			StopScrollToDir();

			if (this.MapCenterPos == p)
				return;

			p = MapToScreen(p);

			var target = new System.Windows.Media.Media3D.Point3D(p.X, p.Y, p.Z);

			var anim = new Point3DAnimation(target, new Duration(TimeSpan.FromMilliseconds(ANIM_TIME_MS)), FillBehavior.HoldEnd);
			if (targetTileSize.HasValue)
				anim.EasingFunction = new MyEase(this.TileSize, targetTileSize.Value);
			BeginAnimation(MapControl.ScreenCenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
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

			var _p3d = this.ScreenCenterPos + new DoubleVector3(v.X, v.Y, 0);
			var p3d = new System.Windows.Media.Media3D.Point3D(_p3d.X, _p3d.Y, _p3d.Z);

			var anim = new Point3DAnimation(p3d, new Duration(TimeSpan.FromMilliseconds(1000)), FillBehavior.HoldEnd);
			anim.Completed += (o, args) =>
			{
				if (m_scrollVector != new IntVector2())
					BeginScrollToDir();
			};
			BeginAnimation(MapControl.ScreenCenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		void StopScrollToDir()
		{
			if (m_scrollVector != new IntVector2())
			{
				m_scrollVector = new IntVector2();
				var cp = this.ScreenCenterPos;
				BeginAnimation(MapControl.ScreenCenterPosProperty, null);
				this.ScreenCenterPos = cp;
			}
		}

		string m_tileSet = "Char";
		public string TileSet
		{
			get { return m_tileSet; }

			set
			{
				throw new NotImplementedException();
				/*
				string xaml;

				switch (value)
				{
					case "Char":
						xaml = "DefaultTileSet";
						break;

					case "Gfx":
						xaml = "DefaultTileSet";
						break;

					default:
						throw new Exception();
				}

				var tileSet = new Dwarrowdelf.Client.Symbols.TileSet(xaml);
				tileSet.Load();

				GameData.Data.TileSet = tileSet;

				m_tileSet = value;

				Notify("TileSet");
				 */
			}
		}
	}
}
