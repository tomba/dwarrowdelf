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
		}

		public void InvalidateTiles()
		{
			m_mapControl.InvalidateTiles();
		}

		public int Columns { get { return m_mapControl.GridSize.Width; } }
		public int Rows { get { return m_mapControl.GridSize.Height; } }

		public double TileSize
		{
			get
			{
				return m_mapControl.TileSize;
			}

			set
			{
				m_mapControl.TileSize = value;

				UpdateScaleTransform();

				UpdateSelectionRect();
			}
		}

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

			// Zoom so that the tile under the mouse stays under the mouse
			// XXX this could be improved. Somehow it doesn't feel quite right...

			var p = e.GetPosition(this);
			var ml1 = ScreenPointToMapLocation(p);

			if (e.Delta > 0)
				this.TileSize *= 2;
			else
				this.TileSize /= 2;

			var ml2 = ScreenPointToMapLocation(p);
			var d = ml2 - this.CenterPos;
			var l = ml1 - d;

			this.CenterPos = l;

			UpdateSelectionRect();

			e.Handled = true;
		}

		// Called when underlying MapControl changes
		void OnTileArrangementChanged(IntSize gridSize, Point centerPos)
		{
			UpdateTranslateTransform();
			UpdateSelectionRect();
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
			UpdateHoverTileInfo(e.GetPosition(this));

			if (!IsMouseCaptured)
			{
				base.OnMouseMove(e);
				return;
			}

			Point pos = e.GetPosition(this);

			int limit = 4;
			int cx = this.CenterPos.X;
			int cy = this.CenterPos.Y;

			if (this.ActualWidth - pos.X < limit)
				++cx;
			else if (pos.X < limit)
				--cx;

			if (this.ActualHeight - pos.Y < limit)
				--cy;
			else if (pos.Y < limit)
				++cy;

			var p = new IntPoint(cx, cy);
			this.CenterPos = p;

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


		public IntPoint CenterPos
		{
			get { return new IntPoint((int)Math.Round(m_mapControl.CenterPos.X), (int)Math.Round(m_mapControl.CenterPos.Y)); }
			set
			{
				m_mapControl.CenterPos = new Point(value.X, value.Y);

				UpdateHoverTileInfo(Mouse.GetPosition(this));
				UpdateSelectionRect();

				UpdateTranslateTransform();

				Notify("CenterPos");
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

		bool m_tileSetHack;
		public bool TileSetHack
		{
			get { return m_tileSetHack; }

			set
			{
				m_tileSetHack = value;

				if (m_tileSetHack)
					m_world.SymbolDrawingCache.Load(new Uri("/Symbols/SymbolInfosChar.xaml", UriKind.Relative));
				else
					m_world.SymbolDrawingCache.Load(new Uri("/Symbols/SymbolInfosGfx.xaml", UriKind.Relative));

				m_mapControl.InvalidateSymbols();

				Notify("TileSetHack");
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

		void AddElement(IDrawableElement element)
		{
			var e = element.Element;

			if (e != null)
			{
				var r = element.Area;
				Canvas.SetLeft(e, r.X);
				Canvas.SetTop(e, r.Y);
				SetZ(e, r.Z);

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


		public static int GetZ(DependencyObject obj)
		{
			return (int)obj.GetValue(ZProperty);
		}

		public static void SetZ(DependencyObject obj, int value)
		{
			obj.SetValue(ZProperty, value);
		}

		public static readonly DependencyProperty ZProperty =
			DependencyProperty.RegisterAttached("Z", typeof(int), typeof(MasterMapControl), new UIPropertyMetadata(0));

		public int Z
		{
			get { return m_mapControl.Z; }

			set
			{
				if (m_mapControl.Z == value)
					return;

				m_mapControl.Z = value;

				foreach (FrameworkElement child in m_elementCanvas.Children)
				{
					if (GetZ(child) != value)
						child.Visibility = System.Windows.Visibility.Hidden;
					else
						child.Visibility = System.Windows.Visibility.Visible;
				}

				if (IsMouseCaptured)
				{
					Point pos = Mouse.GetPosition(this);
					var newEnd = new IntPoint3D(ScreenPointToMapLocation(pos), this.Z);
					this.Selection = new MapSelection(this.Selection.SelectionStart, newEnd);
				}
				UpdateSelectionRect();

				Notify("Z");
				UpdateHoverTileInfo(Mouse.GetPosition(this));
			}
		}

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
			Notify("Floor");
			Notify("FloorMaterial");
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

		public MaterialInfo FloorMaterial
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetFloorMaterial(m_location);
			}
		}

		public FloorInfo Floor
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetFloor(m_location);
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
			Notify("Floors");
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

		public IEnumerable<Tuple<FloorInfo, MaterialInfo>> Floors
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => Tuple.Create(m_env.GetFloor(p), m_env.GetFloorMaterial(p))).
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
