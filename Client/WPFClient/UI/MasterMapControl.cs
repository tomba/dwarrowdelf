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

namespace Dwarrowdelf.Client
{

	/// <summary>
	/// Handles selection rectangles etc. extra stuff
	/// </summary>
	class MasterMapControl : UserControl, INotifyPropertyChanged, IDisposable
	{
		Environment m_env;

		public HoverTileInfo HoverTileInfo { get; private set; }
		public TileAreaInfo SelectedTileAreaInfo { get; private set; }

		MapControl m_mapControl;

		Canvas m_canvas;
		Canvas m_elementCanvas;
		Dictionary<IDrawableElement, FrameworkElement> m_elementMap;

		ScaleTransform m_scaleTransform;
		TranslateTransform m_translateTransform;

		const int ANIM_TIME_MS = 200;

		const double MAXTILESIZE = 64;
		const double MINTILESIZE = 2;

		double? m_targetTileSize;
		IntVector m_scrollVector;

		MapControlToolTipService m_toolTipService;
		MapControlSelectionService m_selectionService;

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
			m_mapControl.TileLayoutChanged += OnTileArrangementChanged;
			m_mapControl.ZChanged += OnZChanged;
			m_mapControl.CenterPosChanged += cp => Notify("CenterPos");

			m_elementCanvas = new Canvas();
			grid.Children.Add(m_elementCanvas);

			var group = new TransformGroup();
			m_scaleTransform = new ScaleTransform();
			m_translateTransform = new TranslateTransform();
			group.Children.Add(m_scaleTransform);
			group.Children.Add(m_translateTransform);
			m_elementCanvas.RenderTransform = group;

			m_elementMap = new Dictionary<IDrawableElement, FrameworkElement>();



			m_canvas = new Canvas();
			grid.Children.Add(m_canvas);

			this.TileSize = 16;

			m_toolTipService = new MapControlToolTipService(m_mapControl);
			m_toolTipService.IsToolTipEnabled = true;

			m_selectionService = new MapControlSelectionService(m_mapControl, m_canvas);
			m_selectionService.RequestScroll += v => ScrollToDirection(v);
			m_selectionService.GotSelection += s => { if (this.GotSelection != null) this.GotSelection(s); };
			m_selectionService.SelectionChanged += OnSelectionChanged;

			this.HoverTileInfo = new HoverTileInfo(m_mapControl);
		}

		public void InvalidateTiles()
		{
			m_mapControl.InvalidateTiles();
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

			var ml = m_mapControl.ScreenPointToMapLocation(p);
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


		// Called when underlying MapControl changes (tile positioning, tile size)
		void OnTileArrangementChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			UpdateTranslateTransform();
			UpdateScaleTransform();
		}

		void UpdateTranslateTransform()
		{
			var p = m_mapControl.MapLocationToScreenPoint(new Point(-0.5, -0.5));
			m_translateTransform.X = p.X;
			m_translateTransform.Y = p.Y;
		}

		void UpdateScaleTransform()
		{
			m_scaleTransform.ScaleX = m_mapControl.TileSize;
			m_scaleTransform.ScaleY = -m_mapControl.TileSize;
		}

		public MapSelectionMode SelectionMode
		{
			get { return m_selectionService.SelectionMode; }
			set { m_selectionService.SelectionMode = value; }
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
				this.SelectedTileAreaInfo.Selection = this.Selection;
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

		public void ScrollTo(Environment env, IntPoint3D p)
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

		public Environment Environment
		{
			get { return m_env; }

			set
			{
				if (m_env == value)
					return;

				m_mapControl.Environment = value;

				if (m_env != null)
				{
					m_env.Buildings.CollectionChanged -= OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.Stockpiles).CollectionChanged -= OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.ConstructionSites).CollectionChanged -= OnElementCollectionChanged;
				}

				m_env = value;

				if (m_env != null)
				{
					m_env.Buildings.CollectionChanged += OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.Stockpiles).CollectionChanged += OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.ConstructionSites).CollectionChanged += OnElementCollectionChanged;

					m_mapControl.CenterPos = new Point(m_env.HomeLocation.X, m_env.HomeLocation.Y);
					this.Z = m_env.HomeLocation.Z;
				}

				this.Selection = new MapSelection();
				UpdateElements();

				Notify("Environment");
			}
		}

		public int Z
		{
			get { return m_mapControl.Z; }
			set { m_mapControl.Z = value; }
		}

		void OnZChanged(int z)
		{
			foreach (FrameworkElement child in m_elementCanvas.Children)
			{
				if (GetElementZ(child) != z)
					child.Visibility = System.Windows.Visibility.Hidden;
				else
					child.Visibility = System.Windows.Visibility.Visible;
			}

			Notify("Z");
		}

		void AddElement(IDrawableElement element)
		{
			var e = element.Element;

			if (e != null)
			{
				var r = element.Area;
				Canvas.SetLeft(e, r.X);
				Canvas.SetTop(e, r.Y);
				SetElementZ(e, r.Z);

				m_elementCanvas.Children.Add(e);
				m_elementMap[element] = e;
			}
		}

		void RemoveElement(IDrawableElement element)
		{
			var e = m_elementMap[element];
			m_elementCanvas.Children.Remove(e);
			m_elementMap.Remove(element);
		}

		void UpdateElements()
		{
			m_elementCanvas.Children.Clear();
			m_elementMap.Clear();

			if (m_env != null)
			{
				var elements = m_env.Buildings.Cast<IDrawableElement>()
					.Concat(m_env.Stockpiles)
					.Concat(m_env.ConstructionSites);

				foreach (IDrawableElement element in elements)
				{
					if (element.Environment == m_env)
						AddElement(element);
				}
			}
		}

		void OnElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (IDrawableElement b in e.NewItems)
						if (b.Environment == m_env)
							AddElement(b);
					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (IDrawableElement b in e.OldItems)
						if (b.Environment == m_env)
							RemoveElement(b);

					break;

				default:
					throw new Exception();
			}
		}




		public static int GetElementZ(DependencyObject obj)
		{
			return (int)obj.GetValue(ElementZProperty);
		}

		public static void SetElementZ(DependencyObject obj, int value)
		{
			obj.SetValue(ElementZProperty, value);
		}

		public static readonly DependencyProperty ElementZProperty =
			DependencyProperty.RegisterAttached("ElementZ", typeof(int), typeof(MasterMapControl), new UIPropertyMetadata(0));


		public IntPoint ScreenPointToMapLocation(Point p)
		{
			p = m_mapControl.ScreenPointToMapLocation(p);
			return new IntPoint((int)Math.Round(p.X), (int)Math.Round(p.Y));
		}

		Point MapLocationToScreenPoint(Point loc)
		{
			return m_mapControl.MapLocationToScreenPoint(loc);
		}

		IntPoint ScreenPointToScreenLocation(Point p)
		{
			p = m_mapControl.ScreenPointToScreenLocation(p);
			return new IntPoint((int)Math.Round(p.X), (int)Math.Round(p.Y));
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
				m_mapControl.TileLayoutChanged -= OnTileArrangementChanged;
				m_mapControl.Dispose();
				m_mapControl = null;
			}
		}
		#endregion
	}
}
