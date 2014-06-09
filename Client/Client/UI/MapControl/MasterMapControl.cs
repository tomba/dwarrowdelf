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

		const double ANIM_TIME = 0.2;

		double? m_targetTileSize;

		AnimBase<DoublePoint3> m_scrollAnim;
		AnimBase<double> m_cameraZAnim;

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

			this.KeyDown += OnKeyDown;
		}

		double TileSizeToCameraZ(double tileSize)
		{
			return MAXTILESIZE * MIN_CAMERA_Z / tileSize;
		}

		double CameraZToTileSize(double cameraZ)
		{
			return MAXTILESIZE * MIN_CAMERA_Z / cameraZ;
		}

		const double MIN_CAMERA_Z = 4;
		const double MAX_CAMERA_Z = MIN_CAMERA_Z / (MINTILESIZE / MAXTILESIZE);

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			/* for testing */
			if (e.Key == Key.D1)
				ScrollTo(new DoublePoint3(0, 0, this.MapCenterPos.Z));
			else if (e.Key == Key.D2)
				ScrollTo(new DoublePoint3(this.Environment.Size.Width - 1, 0, this.MapCenterPos.Z));
			else if (e.Key == Key.D3)
				ScrollTo(new DoublePoint3(this.Environment.Size.Width - 1, this.Environment.Size.Height - 1, this.MapCenterPos.Z));
			else if (e.Key == Key.D4)
				ScrollTo(new DoublePoint3(0, this.Environment.Size.Height - 1, this.MapCenterPos.Z));
			else if (e.Key == Key.D5)
				ScrollTo(new DoublePoint3(this.Environment.Size.Width / 2, this.Environment.Size.Height / 2, this.MapCenterPos.Z));
			else if (e.Key == Key.D0)
				SetScrollAnim(new ContinuousCircle3DAnim(this.ScreenCenterPos, 50));
		}

		bool m_renderingRegistered;

		void SetScrollAnim(AnimBase<DoublePoint3> anim)
		{
			if (!m_renderingRegistered)
			{
				CompositionTarget.Rendering += CompositionTarget_Rendering;
				m_renderingRegistered = true;
			}

			m_scrollAnim = anim;
		}

		void SetCameraAnim(AnimBase<double> anim)
		{
			if (!m_renderingRegistered)
			{
				CompositionTarget.Rendering += CompositionTarget_Rendering;
				m_renderingRegistered = true;
			}

			m_cameraZAnim = anim;
		}

		TimeSpan m_lastRender;

		void CompositionTarget_Rendering(object sender, EventArgs e)
		{
			var args = (RenderingEventArgs)e;

			if (args.RenderingTime == m_lastRender)
				return;

			var diff = args.RenderingTime - m_lastRender;

			/*
			// Skip every other frame
			if (diff.TotalMilliseconds < 20)
				return;
			*/

			m_lastRender = args.RenderingTime;
			var now = args.RenderingTime;

			if (m_scrollAnim != null)
			{
				var anim = m_scrollAnim;

				if (anim.Initialized == false)
					anim.Init(now);

				var p = anim.GetValue(now);

				this.ScreenCenterPos = p;

				if (anim.Finished(now))
					m_scrollAnim = null;
			}

			if (m_cameraZAnim != null)
			{
				var anim = m_cameraZAnim;

				if (anim.Initialized == false)
					anim.Init(now);

				var z = anim.GetValue(now);

				this.TileSize = CameraZToTileSize(z);

				if (anim.Finished(now))
					m_cameraZAnim = null;
			}

			if (m_scrollAnim == null && m_cameraZAnim == null)
			{
				CompositionTarget.Rendering -= CompositionTarget_Rendering;
				m_renderingRegistered = false;
			}
		}

		public void Zoom(double zoom)
		{
			zoom *= 60;

			if (zoom == 0)
				m_cameraZAnim = null;
			else
				SetCameraAnim(new Continuous1DAnim(this) { Zoom = zoom });
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

			if (this.IsMouseOver && ClientConfig.ShowMouseDebug)
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

			if (tileSize == this.TileSize)
				return;

			m_targetTileSize = tileSize;

			SetCameraAnim(new Linear1DAnim(TileSizeToCameraZ(this.TileSize), TileSizeToCameraZ(tileSize), ANIM_TIME));
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

			var p = e.GetPosition(this);
			ZoomToMouse(p, e.Delta);
		}

		/// <summary>
		/// Zoom so that the same map position stays under the given screen position
		/// </summary>
		void ZoomToMouse(Point p, int delta)
		{
			p = RenderPointToScreen(p);

			if (m_cameraZAnim != null)
				return;

			double currentCameraZ = TileSizeToCameraZ(this.TileSize);
			double destinationCameraZ = currentCameraZ;

			if (delta > 0)
				destinationCameraZ /= 2;
			else
				destinationCameraZ *= 2;

			destinationCameraZ = MyMath.Clamp(destinationCameraZ, MAX_CAMERA_Z, MIN_CAMERA_Z);

			if (destinationCameraZ == currentCameraZ)
				return;

			var src = new DoublePoint3(this.ScreenCenterPos.X, this.ScreenCenterPos.Y, currentCameraZ);
			var dst = new DoublePoint3(p.X, p.Y, 0);

			// Vector from eye to the surface
			var v = dst - src;

			// Vector from eye to the targetZ level
			v *= (currentCameraZ - destinationCameraZ) / currentCameraZ;

			var scrollDestination = new DoublePoint3(src.X + v.X, src.Y + v.Y, this.ScreenCenterPos.Z);

			var duration = ANIM_TIME;

			SetScrollAnim(new Linear3DAnim(this.ScreenCenterPos, scrollDestination, duration));
			SetCameraAnim(new Linear1DAnim(currentCameraZ, destinationCameraZ, duration));
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
			m_scrollAnim = null;
			m_cameraZAnim = null;
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

		void ScrollTo(DoublePoint3 p)
		{
			//StopScrollToDir();

			if (this.MapCenterPos == p)
				return;

			var src = this.ScreenCenterPos;
			var dst = MapToScreen(p);
			var v = dst - src;
			var duration = MyMath.LinearInterpolation(4, 128, 0.1, 2, v.Length);

			SetScrollAnim(new Linear3DAnim(src, dst, duration));

			var currentCameraZ = TileSizeToCameraZ(this.TileSize);

			var m = MyMath.LinearInterpolation(4, 128, 1, 8, v.Length);
			var maxZ = Math.Min(MAX_CAMERA_Z, currentCameraZ * m);

			SetCameraAnim(new Pow1DAnim(currentCameraZ, maxZ, duration));
		}

		public void ScrollToDirection(IntVector2 vector)
		{
			if (vector.IsNull)
			{
				m_scrollAnim = null;
				return;
			}

			var v = new DoubleVector3(vector.X, vector.Y, 0);

			var anim = m_scrollAnim as Continuous3DAnim;
			if (anim == null)
			{
				anim = new Continuous3DAnim(this);
				SetScrollAnim(anim);
			}

			anim.Direction = v;
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

		abstract class AnimBase<T>
		{
			public TimeSpan StartTime { get; private set; }
			public bool Initialized { get; private set; }

			protected AnimBase()
			{
			}

			public void Init(TimeSpan startTime)
			{
				this.StartTime = startTime;
				this.Initialized = true;
			}

			public virtual bool Finished(TimeSpan now)
			{
				return false;
			}

			public abstract T GetValue(TimeSpan now);
		}

		class Continuous1DAnim : AnimBase<double>
		{
			public double Zoom { get; set; }
			MasterMapControl m_mapControl;
			TimeSpan m_last;

			public Continuous1DAnim(MasterMapControl mapControl)
			{
				m_mapControl = mapControl;
			}

			public override double GetValue(TimeSpan now)
			{
				double t;

				if (m_last.Ticks == 0)
					t = 1.0 / 60;
				else
					t = (now - m_last).TotalSeconds;

				m_last = now;

				var v = this.Zoom;

				v *= t;

				var z = m_mapControl.TileSizeToCameraZ(m_mapControl.TileSize) - v;
				z = MyMath.Clamp(z, MAX_CAMERA_Z, MIN_CAMERA_Z);

				return z;
			}
		}

		class Continuous3DAnim : AnimBase<DoublePoint3>
		{
			public DoubleVector3 Direction { get; set; }
			MapControl m_mapControl;
			TimeSpan m_last;

			public Continuous3DAnim(MapControl mapControl)
			{
				m_mapControl = mapControl;
			}

			public override DoublePoint3 GetValue(TimeSpan now)
			{
				double t;

				if (m_last.Ticks == 0)
					t = 1.0 / 60;
				else
					t = (now - m_last).TotalSeconds;

				m_last = now;


				var v = this.Direction;
#if !ALT
				double tilesPerSec = Math.Sqrt(MAXTILESIZE / m_mapControl.TileSize) * 32;
#elif !TILES_PER_SEC
				double tilesPerSec = 60;
#else // PIXELS_PER_SEC
				const double pixPerSec = 256;
				double tilesPerSec = pixPerSec / this.TileSize;
#endif
				v *= tilesPerSec;
				v *= t;

				return m_mapControl.ScreenCenterPos + v;
			}
		}

		abstract class FinishableAnimBase<T> : AnimBase<T>
		{
			public double Duration { get; private set; }

			protected FinishableAnimBase(double duration)
			{
				this.Duration = duration;
			}

			public override bool Finished(TimeSpan now)
			{
				var t = (now - this.StartTime).TotalSeconds / this.Duration;
				return t >= 1;
			}

			public override T GetValue(TimeSpan now)
			{
				Debug.Assert(this.Initialized);

				var t = (now - this.StartTime).TotalSeconds / this.Duration;
				return GetValue(t);
			}

			protected abstract T GetValue(double t);
		}

		class Linear3DAnim : FinishableAnimBase<DoublePoint3>
		{
			public DoublePoint3 Source { get; private set; }
			public DoublePoint3 Destination { get; private set; }

			public Linear3DAnim(DoublePoint3 src, DoublePoint3 dst, double duration)
				: base(duration)
			{
				this.Source = src;
				this.Destination = dst;
			}

			protected override DoublePoint3 GetValue(double t)
			{
				if (t >= 1)
					return this.Destination;

				var v = this.Destination - this.Source;

				var p = this.Source + v * t;

				return p;
			}
		}

		class Linear1DAnim : FinishableAnimBase<double>
		{
			public double Source { get; private set; }
			public double Destination { get; private set; }

			public Linear1DAnim(double src, double dst, double duration)
				: base(duration)
			{
				this.Source = src;
				this.Destination = dst;
			}

			protected override double GetValue(double t)
			{
				if (t >= 1)
					return this.Destination;

				var v = this.Destination - this.Source;

				var p = this.Source + v * t;

				return p;
			}
		}

		class Pow1DAnim : FinishableAnimBase<double>
		{
			public double Min { get; private set; }
			public double Max { get; private set; }

			public Pow1DAnim(double min, double max, double duration)
				: base(duration)
			{
				this.Min = min;
				this.Max = max;
			}

			protected override double GetValue(double t)
			{
				if (t >= 1)
					return this.Min;

				// y = [0,1]
				var y = -Math.Pow(t * 2 - 1, 2) + 1;

				y = MyMath.LinearInterpolation(this.Min, this.Max, y);

				return y;
			}
		}

		class ContinuousCircle3DAnim : AnimBase<DoublePoint3>
		{
			DoublePoint3 m_center;
			double m_radius;

			public ContinuousCircle3DAnim(DoublePoint3 center, double radius)
			{
				m_center = center;
				m_radius = radius;
			}

			public override DoublePoint3 GetValue(TimeSpan now)
			{
				double t = (now - this.StartTime).TotalSeconds;

				double x = Math.Cos(t) * m_radius;
				double y = Math.Sin(t) * m_radius;

				return new DoublePoint3(m_center.X + x, m_center.Y + y, m_center.Z);
			}
		}
	}
}
