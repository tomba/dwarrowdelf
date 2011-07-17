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
	/// <summary>
	/// Handles selection rectangles etc. extra stuff
	/// </summary>
	class MasterMapControl : UserControl, INotifyPropertyChanged, IDisposable
	{
		World m_world;
		Environment m_env;

		public HoverTileInfo HoverTileInfo { get; private set; }

		MapControl m_mapControl;

		MapSelection m_selection;
		Rectangle m_selectionRect;

		Canvas m_canvas;
		Canvas m_elementCanvas;
		Dictionary<IDrawableElement, FrameworkElement> m_elementMap;

		ScaleTransform m_scaleTransform;
		TranslateTransform m_translateTransform;

		public MasterMapControl()
		{
			this.HoverTileInfo = new HoverTileInfo();
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
			m_selectionRect.Width = this.TileSize;
			m_selectionRect.Height = this.TileSize;
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
		}

		public void InvalidateTiles()
		{
			m_mapControl.InvalidateTiles();
		}

		public int Columns { get { return m_mapControl.GridSize.Width; } }
		public int Rows { get { return m_mapControl.GridSize.Height; } }

		public void BeginTileSizeAnim(double targetTileSize)
		{
			var anim = new DoubleAnimation(targetTileSize, new Duration(TimeSpan.FromMilliseconds(200)), FillBehavior.Stop);
			anim.Completed += delegate { m_mapControl.TileSize = targetTileSize; };
			m_mapControl.BeginAnimation(MapControl.TileSizeProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		public double TileSize
		{
			get { return m_mapControl.TileSize; }

			set
			{
				var v = value;
				v = Math.Log(v, 2);
				v = Math.Round(v);
				v = Math.Pow(2, v);

				m_mapControl.TileSize = v;
			}
		}

		double m_animTargetTileSize;

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (this.IsMouseCaptured || (Keyboard.Modifiers & ModifierKeys.Control) != 0)
			{
				if (e.Delta > 0)
					this.Z--;
				else
					this.Z++;

				e.Handled = true;

				return;
			}

			// Zoom so that the position under the mouse stays under the mouse

			e.Handled = true;

			if (m_animTargetTileSize == 0)
				m_animTargetTileSize = m_mapControl.TileSize;

			var origTileSize = m_animTargetTileSize;
			var origCenter = m_mapControl.CenterPos;


			double targetTileSize = origTileSize;

			if (e.Delta > 0)
			{
				if (origTileSize == m_mapControl.MaxTileSize)
					return;

				targetTileSize *= 2;
			}
			else
			{
				if (origTileSize == m_mapControl.MinTileSize)
					return;

				targetTileSize /= 2;
			}

			targetTileSize = MyMath.Clamp(targetTileSize, m_mapControl.MaxTileSize, m_mapControl.MinTileSize);

			m_animTargetTileSize = targetTileSize;



			var p = e.GetPosition(this);

			Vector v = p - new Point(m_mapControl.ActualWidth / 2, m_mapControl.ActualHeight / 2);
			v /= targetTileSize;
			v.Y = -v.Y;
			//v = new Vector(Math.Round(v.X), Math.Round(v.Y));

			var ml = m_mapControl.ScreenPointToMapLocation(p);
			//ml = new Point(Math.Round(ml.X), Math.Round(ml.Y));
			var targetCenter = ml - v;

			//targetCenter = new Point(Math.Round(targetCenter.X), Math.Round(targetCenter.Y));

#if NOANIM
			m_mapControl.TileSize = targetTileSize;
			m_mapControl.CenterPos = targetCenter;
#else
			var anim = new DoubleAnimation(targetTileSize, new Duration(TimeSpan.FromMilliseconds(200)), FillBehavior.Stop);
			anim.Completed += delegate { m_mapControl.TileSize = targetTileSize; };
			m_mapControl.BeginAnimation(MapControl.TileSizeProperty, anim, HandoffBehavior.SnapshotAndReplace);

			var anim2 = new PointAnimation(targetCenter, new Duration(TimeSpan.FromMilliseconds(200)), FillBehavior.Stop);
			anim2.EasingFunction = new MyEase(m_mapControl.TileSize, targetTileSize);
			anim2.Completed += delegate { m_mapControl.CenterPos = targetCenter; };
			m_mapControl.BeginAnimation(MapControl.CenterPosProperty, anim2, HandoffBehavior.SnapshotAndReplace);
#endif

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
			UpdateSelectionRect();
			UpdateHoverTileInfo(Mouse.GetPosition(this));
		}

		void UpdateTranslateTransform()
		{
			var p = m_mapControl.MapLocationToScreenPoint(new Point(-0.5, -0.5));
			m_translateTransform.X = p.X;
			m_translateTransform.Y = p.Y;
		}

		void UpdateScaleTransform()
		{
			m_scaleTransform.ScaleX = this.TileSize;
			m_scaleTransform.ScaleY = -this.TileSize;
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

		Rect MapRectToScreenPointRect(IntRect ir)
		{
			Rect r = new Rect(MapLocationToScreenPoint(new Point(ir.X1 - 0.5, ir.Y2 - 0.5)),
				new Size(ir.Width * this.TileSize, ir.Height * this.TileSize));
			return r;
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

			var r = MapRectToScreenPointRect(ir);

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

			Point pos = e.GetPosition(this);
			var ml = new IntPoint3D(ScreenPointToMapLocation(pos), this.Z);

			if (this.Selection.IsSelectionValid && this.Selection.SelectionCuboid.Contains(ml))
			{
				this.Selection = new MapSelection();
				return;
			}

			this.Selection = new MapSelection(ml, ml);

			CaptureMouse();

			e.Handled = true;

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (m_mapControl == null)
				return;

			UpdateHoverTileInfo(e.GetPosition(this));

			if (!IsMouseCaptured)
			{
				base.OnMouseMove(e);
				return;
			}

			Point pos = e.GetPosition(this);

			int limit = 4;
			var cx = m_mapControl.CenterPos.X;
			var cy = m_mapControl.CenterPos.Y;

			int incX = 4;
			int incY = 4;

			if (this.ActualWidth - pos.X < limit)
				cx += incX;
			else if (pos.X < limit)
				cx -= incX;

			if (this.ActualHeight - pos.Y < limit)
				cy -= incY;
			else if (pos.Y < limit)
				cy += incY;

			if (cx != m_mapControl.CenterPos.X || cy != m_mapControl.CenterPos.Y)
			{
				var p = new IntPoint((int)Math.Round(cx), (int)Math.Round(cy));
				this.CenterPos = p;
			}

			var newEnd = new IntPoint3D(ScreenPointToMapLocation(pos), this.Z);
			this.Selection = new MapSelection(this.Selection.SelectionStart, newEnd);

			e.Handled = true;

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			ReleaseMouseCapture();

			base.OnMouseUp(e);
		}

		public void BeginCenterPosAnim(IntPoint targetCenterPos)
		{
			var center = new Point(targetCenterPos.X, targetCenterPos.Y);
			var anim = new PointAnimation(center, new Duration(TimeSpan.FromMilliseconds(200)), FillBehavior.Stop);
			anim.Completed += delegate { m_mapControl.CenterPos = center; };
			m_mapControl.BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		public void BeginCenterPosAnim(IntVector centerPosDiff)
		{
			var v = new Vector(centerPosDiff.X, centerPosDiff.Y);
			var center = m_mapControl.CenterPos + v;
			var anim = new PointAnimation(center, new Duration(TimeSpan.FromMilliseconds(200)), FillBehavior.Stop);
			anim.Completed += delegate { m_mapControl.CenterPos = center; };
			m_mapControl.BeginAnimation(MapControl.CenterPosProperty, anim, HandoffBehavior.SnapshotAndReplace);
		}

		public IntPoint CenterPos
		{
			get { return new IntPoint((int)Math.Round(m_mapControl.CenterPos.X), (int)Math.Round(m_mapControl.CenterPos.Y)); }
			set
			{
				var center = new Point(value.X, value.Y);
				m_mapControl.CenterPos = center;
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
				}

				m_env = value;

				if (m_env != null)
				{
					if (m_world != m_env.World)
						m_world = m_env.World;

					m_env.Buildings.CollectionChanged += OnElementCollectionChanged;
					((INotifyCollectionChanged)m_env.Stockpiles).CollectionChanged += OnElementCollectionChanged;

					this.CenterPos = m_env.HomeLocation.ToIntPoint();
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

			if (this.IsMouseCaptured)
			{
				Point pos = Mouse.GetPosition(this);
				var newEnd = new IntPoint3D(ScreenPointToMapLocation(pos), z);
				Selection = new MapSelection(Selection.SelectionStart, newEnd);
			}
			UpdateSelectionRect();

			Notify("Z");
			UpdateHoverTileInfo(Mouse.GetPosition(this));
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
					.Concat(m_env.Stockpiles);

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

		void UpdateHoverTileInfo(Point p)
		{
			var sl = ScreenPointToScreenLocation(p);
			var ml = new IntPoint3D(ScreenPointToMapLocation(p), m_mapControl.Z);

			if (p != this.HoverTileInfo.MousePos ||
				sl != this.HoverTileInfo.ScreenLocation ||
				ml != this.HoverTileInfo.MapLocation)
			{
				this.HoverTileInfo.MousePos = p;
				this.HoverTileInfo.ScreenLocation = sl;
				this.HoverTileInfo.MapLocation = ml;
				Notify("HoverTileInfo");
			}
		}

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

		public IntCuboid SelectionCuboid
		{
			get
			{
				if (!this.IsSelectionValid)
					return new IntCuboid();

				return new IntCuboid(this.SelectionStart, this.SelectionEnd).Inflate(1, 1, 1);
			}
		}
	}






	class HoverTileInfo
	{
		public Point MousePos { get; set; }
		public IntPoint3D MapLocation { get; set; }
		public IntPoint ScreenLocation { get; set; }
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

		void MapObjectChanged(ClientGameObject ob, IntPoint3D l, MapTileObjectChangeType changeType)
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

		void MapObjectChanged(ClientGameObject ob, IntPoint3D l, MapTileObjectChangeType changetype)
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

		public IEnumerable<ClientGameObject> Objects
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
