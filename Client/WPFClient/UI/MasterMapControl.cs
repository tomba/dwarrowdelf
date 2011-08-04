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

namespace Dwarrowdelf.Client
{
	enum SelectionMode
	{
		None,
		Point,
		Rectangle,
		Cuboid,
	}

	/// <summary>
	/// Handles selection rectangles etc. extra stuff
	/// </summary>
	class MasterMapControl : UserControl, INotifyPropertyChanged, IDisposable
	{
		World m_world;
		Environment m_env;

		public UI.HoverTileInfo HoverTileInfo { get; private set; }

		MapControl m_mapControl;

		MapSelection m_selection;
		Rectangle m_selectionRect;

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

		Dwarrowdelf.Client.UI.MapControlToolTipService m_toolTipService;

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

			grid.Children.Add((UIElement)mc);
			m_mapControl = mc;
			m_mapControl.TileLayoutChanged += OnTileArrangementChanged;

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

			m_selectionRect = new Rectangle();
			m_selectionRect.Visibility = Visibility.Hidden;
			m_selectionRect.Stroke = Brushes.Blue;
			m_selectionRect.StrokeThickness = 1;
			m_selectionRect.Fill = new SolidColorBrush(Colors.Blue);
			m_selectionRect.Fill.Opacity = 0.2;
			m_selectionRect.Fill.Freeze();
			m_canvas.Children.Add(m_selectionRect);

			this.TileSize = 16;

			{
				var propDesc = DependencyPropertyDescriptor.FromProperty(MapControl.CenterPosProperty, typeof(MapControl));
				propDesc.AddValueChanged(m_mapControl, delegate { Notify("CenterPos"); });
			}

			{
				var propDesc = DependencyPropertyDescriptor.FromProperty(MapControl.ZProperty, typeof(MapControl));
				propDesc.AddValueChanged(m_mapControl, OnZChanged);
			}

			m_toolTipService = new UI.MapControlToolTipService(m_mapControl);
			m_toolTipService.IsToolTipEnabled = true;

			this.HoverTileInfo = new UI.HoverTileInfo(m_mapControl);
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
			var pos = Mouse.GetPosition(this);

			UpdateTranslateTransform();
			UpdateScaleTransform();
			if (this.IsMouseCaptured)
			{
				UpdateSelection(pos);
			}
			else
			{
				UpdateSelectionRect();
			}
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

		SelectionMode m_selectionMode;
		public SelectionMode SelectionMode
		{
			get { return m_selectionMode; }
			set { m_selectionMode = value; }
		}

		public MapSelection Selection
		{
			get
			{
				return m_selection;
			}

			set
			{
				if (m_selection.IsSelectionValid == value.IsSelectionValid &&
					m_selection.SelectionStart == value.SelectionStart &&
					m_selection.SelectionEnd == value.SelectionEnd)
					return;

				m_selection = value;

				if (!m_selection.IsSelectionValid)
				{
					this.SelectedTileAreaInfo.Environment = null;
				}
				else
				{
					this.SelectedTileAreaInfo.Environment = this.Environment;
					this.SelectedTileAreaInfo.Selection = this.Selection;
				}

				UpdateSelectionRect();

				Notify("Selection");
			}
		}

		void UpdateSelection(Point mousePos)
		{
			IntPoint3D start;

			var end = new IntPoint3D(ScreenPointToMapLocation(mousePos), this.Z);

			switch (m_selectionMode)
			{
				case Client.SelectionMode.Point:
					start = end;
					break;

				case Client.SelectionMode.Rectangle:
					start = new IntPoint3D(this.Selection.SelectionStart.ToIntPoint(), this.Z);
					break;

				case Client.SelectionMode.Cuboid:
					start = this.Selection.SelectionStart;
					break;

				default:
					throw new Exception();
			}

			this.Selection = new MapSelection(start, end);
		}

		void UpdateSelectionRect()
		{
			if (!this.Selection.IsSelectionValid)
			{
				m_selectionRect.Visibility = Visibility.Hidden;
				return;
			}

			if (this.Selection.SelectionCuboid.Z1 > this.Z || this.Selection.SelectionCuboid.Z2 - 1 < this.Z)
			{
				m_selectionRect.Visibility = Visibility.Hidden;
				return;
			}

			var ir = new IntRect(this.Selection.SelectionStart.ToIntPoint(), this.Selection.SelectionEnd.ToIntPoint());
			ir = ir.Inflate(1, 1);

			var r = m_mapControl.MapRectToScreenPointRect(ir);

			Canvas.SetLeft(m_selectionRect, r.Left);
			Canvas.SetTop(m_selectionRect, r.Top);
			m_selectionRect.Width = r.Width;
			m_selectionRect.Height = r.Height;

			m_selectionRect.Visibility = Visibility.Visible;
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
			{
				base.OnMouseDown(e);
				return;
			}

			if (this.SelectionMode == Client.SelectionMode.None)
				return;

			Point pos = e.GetPosition(this);
			var ml = new IntPoint3D(ScreenPointToMapLocation(pos), this.Z);

			if (this.Selection.IsSelectionValid && this.Selection.SelectionCuboid.Contains(ml))
			{
				this.Selection = new MapSelection();
				return;
			}

			this.Selection = new MapSelection(ml, ml);

			CaptureMouse();

			m_toolTipService.IsToolTipEnabled = false;

			e.Handled = true;

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_mapControl == null)
				return;

			var pos = e.GetPosition(this);

			if (!IsMouseCaptured)
			{
				base.OnMouseMove(e);
				return;
			}

			int limit = 4;
			int speed = 1;

			int dx = 0;
			int dy = 0;

			if (this.ActualWidth - pos.X < limit)
				dx = speed;
			else if (pos.X < limit)
				dx = -speed;

			if (this.ActualHeight - pos.Y < limit)
				dy = -speed;
			else if (pos.Y < limit)
				dy = speed;

			var v = new IntVector(dx, dy);
			ScrollToDirection(v);

			UpdateSelection(pos);

			e.Handled = true;

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			ReleaseMouseCapture();

			if (this.GotSelection != null)
				this.GotSelection(this.Selection);

			base.OnMouseUp(e);
		}

		public event Action<MapSelection> GotSelection;

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
					if (m_world != m_env.World)
						m_world = m_env.World;

					m_env.Buildings.CollectionChanged += OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.Stockpiles).CollectionChanged += OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.ConstructionSites).CollectionChanged += OnElementCollectionChanged;

					m_mapControl.CenterPos = new Point(m_env.HomeLocation.X, m_env.HomeLocation.Y);
					this.Z = m_env.HomeLocation.Z;
				}
				else
				{
					m_world = null;
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

		void OnZChanged(object sender, EventArgs e)
		{
			var z = this.Z;

			foreach (FrameworkElement child in m_elementCanvas.Children)
			{
				if (GetElementZ(child) != z)
					child.Visibility = System.Windows.Visibility.Hidden;
				else
					child.Visibility = System.Windows.Visibility.Visible;
			}

			Point pos = Mouse.GetPosition(this);

			if (this.IsMouseCaptured)
			{
				UpdateSelection(pos);
			}
			else
			{
				UpdateSelectionRect();
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


		public TileAreaInfo SelectedTileAreaInfo { get; private set; }

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




	struct MapSelection
	{
		public MapSelection(IntPoint3D start, IntPoint3D end)
			: this()
		{
			this.SelectionStart = start;
			this.SelectionEnd = end;
			this.IsSelectionValid = true;
		}

		public MapSelection(IntCuboid cuboid)
			: this()
		{
			if (cuboid.Width == 0 || cuboid.Height == 0 || cuboid.Depth == 0)
			{
				this.IsSelectionValid = false;
			}
			else
			{
				this.SelectionStart = cuboid.Corner1;
				this.SelectionEnd = cuboid.Corner2 - new IntVector3D(1, 1, 1);
				this.IsSelectionValid = true;
			}
		}

		public bool IsSelectionValid { get; set; }
		public IntPoint3D SelectionStart { get; set; }
		public IntPoint3D SelectionEnd { get; set; }

		public IntPoint3D SelectionPoint
		{
			get
			{
				if (this.SelectionStart != this.SelectionEnd)
					throw new Exception();

				return this.SelectionStart;
			}
		}

		public IntCuboid SelectionCuboid
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntCuboid();

				return new IntCuboid(this.SelectionStart, this.SelectionEnd).Inflate(1, 1, 1);
			}
		}

		public IntRectZ SelectionIntRectZ
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntRectZ();

				if (this.SelectionStart.Z != this.SelectionEnd.Z)
					throw new Exception();

				return new IntRectZ(this.SelectionStart.ToIntPoint(), this.SelectionEnd.ToIntPoint(), this.SelectionStart.Z).Inflate(1, 1);
			}
		}
	}






	class TileInfo : INotifyPropertyChanged
	{
		Environment m_env;
		IntPoint3D m_location;
		GameObjectCollection m_obs;

		public TileInfo()
		{
			m_obs = new GameObjectCollection();
		}

		void NotifyTileChanges()
		{
			NotifyTileTerrainChanges();
			NotifyTileObjectChanges();
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interior");
			Notify("Terrain");
			Notify("TerrainMaterial");
			Notify("InteriorMaterial");
			Notify("WaterLevel");
			Notify("Building");
		}

		void NotifyTileObjectChanges()
		{
			m_obs.Clear();
			if (m_env != null)
			{
				var list = m_env.GetContents(m_location);
				foreach (var o in list)
					m_obs.Add(o);
			}

			Notify("Objects");
		}

		void MapTerrainChanged(IntPoint3D l)
		{
			if (l != m_location)
				return;

			NotifyTileTerrainChanges();
		}

		void MapObjectChanged(GameObject ob, IntPoint3D l, MapTileObjectChangeType changeType)
		{
			if (l != m_location)
				return;

			NotifyTileObjectChanges();
		}

		public Environment Environment
		{
			get { return m_env; }
			set
			{
				if (m_env != null)
				{
					m_env.MapTileTerrainChanged -= MapTerrainChanged;
					m_env.MapTileObjectChanged -= MapObjectChanged;
				}

				m_env = value;

				if (m_env == null)
					m_location = new IntPoint3D();

				if (m_env != null)
				{
					m_env.MapTileTerrainChanged += MapTerrainChanged;
					m_env.MapTileObjectChanged += MapObjectChanged;
				}

				Notify("Environment");
				NotifyTileChanges();
			}
		}

		public IntPoint3D Location
		{
			get { return m_location; }
			set
			{
				m_location = value;
				Notify("Location");
				NotifyTileChanges();
			}
		}

		public InteriorInfo Interior
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetInterior(m_location);
			}
		}

		public MaterialInfo InteriorMaterial
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetInteriorMaterial(m_location);
			}
		}

		public MaterialInfo TerrainMaterial
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetTerrainMaterial(m_location);
			}
		}

		public TerrainInfo Terrain
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetTerrain(m_location);
			}
		}

		public byte WaterLevel
		{
			get
			{
				if (m_env == null)
					return 0;
				return m_env.GetWaterLevel(m_location);
			}
		}

		public GameObjectCollection Objects
		{
			get
			{
				return m_obs;
			}
		}

		public BuildingObject Building
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetBuildingAt(m_location);
			}
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

	class TileAreaInfo : INotifyPropertyChanged
	{
		Environment m_env;
		MapSelection m_selection;

		GameObjectCollection m_objects;

		public TileAreaInfo()
		{
			m_objects = new GameObjectCollection();
		}

		void NotifyTileChanges()
		{
			NotifyTileTerrainChanges();
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interiors");
			Notify("Terrains");
			Notify("WaterLevels");
			Notify("Buildings");
			Notify("Grasses");
		}

		void MapTerrainChanged(IntPoint3D l)
		{
			if (!m_selection.SelectionCuboid.Contains(l))
				return;

			NotifyTileTerrainChanges();
		}

		void MapObjectChanged(GameObject ob, IntPoint3D l, MapTileObjectChangeType changetype)
		{
			if (!m_selection.SelectionCuboid.Contains(l))
				return;

			if (changetype == MapTileObjectChangeType.Add)
			{
				Debug.Assert(!m_objects.Contains(ob));
				m_objects.Add(ob);
			}
			else
			{
				bool ok = m_objects.Remove(ob);
				Debug.Assert(ok);
			}
		}

		public Environment Environment
		{
			get { return m_env; }
			set
			{
				if (m_env != null)
				{
					m_env.MapTileTerrainChanged -= MapTerrainChanged;
					m_env.MapTileObjectChanged -= MapObjectChanged;
				}

				m_env = value;
				m_objects.Clear();

				if (m_env == null)
				{
					m_selection = new MapSelection();
					Notify("Selection");
				}

				if (m_env != null)
				{
					m_env.MapTileTerrainChanged += MapTerrainChanged;
					m_env.MapTileObjectChanged += MapObjectChanged;
				}

				Notify("Environment");
				NotifyTileChanges();
			}
		}

		public MapSelection Selection
		{
			get { return m_selection; }
			set
			{
				m_selection = value;
				Notify("Selection");
				NotifyTileChanges();
				m_objects.Clear();
				var obs = m_selection.SelectionCuboid.Range().SelectMany(p => m_env.GetContents(p));
				foreach (var ob in obs)
					m_objects.Add(ob);
			}
		}

		public IEnumerable<Tuple<InteriorInfo, MaterialInfo>> Interiors
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => Tuple.Create(m_env.GetInterior(p), m_env.GetInteriorMaterial(p))).
					Distinct();
			}
		}

		public IEnumerable<Tuple<TerrainInfo, MaterialInfo>> Terrains
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => Tuple.Create(m_env.GetTerrain(p), m_env.GetTerrainMaterial(p))).
					Distinct();
			}
		}

		public IEnumerable<byte> WaterLevels
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => m_env.GetWaterLevel(p)).
					Distinct();
			}
		}

		public IEnumerable<GameObject> Objects
		{
			get
			{
				return m_objects;
			}
		}

		public IEnumerable<BuildingObject> Buildings
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => m_env.GetBuildingAt(p)).
					Where(b => b != null).
					Distinct();
			}
		}

		public IEnumerable<bool> Grasses
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => m_env.GetGrass(p)).
					Distinct();
			}
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

	public class ListConverter<T> : IValueConverter
	{
		Func<T, string> m_converter;

		public ListConverter(Func<T, string> itemConverter)
		{
			m_converter = itemConverter;
		}

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value == null)
				return "";

			var list = (IEnumerable<T>)value;

			return String.Join(", ", list.Select(item => m_converter(item)));
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
