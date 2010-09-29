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

namespace Dwarrowdelf.Client
{
	interface IMapControl
	{
		int Columns { get; }
		int Rows { get; }
		int TileSize { get; set; }
		bool ShowVirtualSymbols { get; set; }

		Environment Environment { get; set; }
		int Z { get; set; }
		IntPoint CenterPos { get; set; }

		event Action TileArrangementChanged;

		void InvalidateDrawings();

		IntPoint ScreenPointToScreenLocation(Point p);
		IntPoint ScreenPointToMapLocation(Point p);
		Point ScreenLocationToScreenPoint(IntPoint loc);
		Point MapLocationToScreenPoint(IntPoint loc);
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

	/// <summary>
	/// Handles selection rectangles etc. extra stuff
	/// </summary>
	class MasterMapControl : UserControl, INotifyPropertyChanged
	{
		World m_world;

		Environment m_env;
		int m_z;

		public HoverTileInfo HoverTileInfo { get; private set; }

		IMapControl m_mapControl;

		IntPoint m_centerPos;

		MapSelection m_selection;
		Rectangle m_selectionRect;

		Canvas m_canvas;
		Canvas m_buildingCanvas;
		Dictionary<IDrawableArea, Rectangle> m_buildingRectMap;

		public MasterMapControl()
		{
			this.UseLayoutRounding = true;

			this.HoverTileInfo = new HoverTileInfo();
			this.SelectedTileAreaInfo = new TileAreaInfo();

			var grid = new Grid();
			AddChild(grid);

			IMapControl mc = new MapControlD2D();

			grid.Children.Add((UIElement)mc);
			m_mapControl = mc;
			m_mapControl.TileArrangementChanged += OnTileArrangementChanged;

			m_canvas = new Canvas();
			m_canvas.ClipToBounds = true;
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

			m_buildingCanvas = new Canvas();
			m_buildingCanvas.ClipToBounds = true;
			grid.Children.Add(m_buildingCanvas);

			m_buildingRectMap = new Dictionary<IDrawableArea, Rectangle>();

			this.TileSize = 32;
		}

		public int Columns { get { return m_mapControl.Columns; } }
		public int Rows { get { return m_mapControl.Rows; } }

		public int TileSize
		{
			get
			{
				return m_mapControl.TileSize;
			}

			set
			{
				m_mapControl.TileSize = value;
				UpdateSelectionRect();
				UpdateAreaPositions();
			}
		}

		protected override void OnMouseWheel(MouseWheelEventArgs e)
		{
			if (this.IsMouseCaptured)
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
			var ml1 = m_mapControl.ScreenPointToMapLocation(p);

			if (e.Delta > 0)
				this.TileSize *= 2;
			else
				this.TileSize /= 2;

			var ml2 = m_mapControl.ScreenPointToMapLocation(p);
			var d = ml2 - this.CenterPos;
			var l = ml1 - d;

			this.CenterPos = l;

			UpdateSelectionRect();
			UpdateAreaPositions();

			e.Handled = true;

			base.OnMouseWheel(e);
		}

		// Called when underlying MapControl changes
		void OnTileArrangementChanged()
		{
			UpdateSelectionRect();
			UpdateAreaPositions();
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
			Rect r = new Rect(MapLocationToScreenPoint(new IntPoint(ir.X1, ir.Y2 - 1)),
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

			if (this.Selection.SelectionCuboid.Z1 > this.Z || this.Selection.SelectionCuboid.Z2 < this.Z)
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


		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		public IntPoint CenterPos
		{
			get { return m_centerPos; }
			set
			{
				if (value == this.CenterPos)
					return;
				IntVector dv = m_centerPos - value;
				m_centerPos = value;
				m_mapControl.CenterPos = value;
				UpdateHoverTileInfo(Mouse.GetPosition(this));
				UpdateSelectionRect();

				double dx = dv.X * this.TileSize;
				double dy = -dv.Y * this.TileSize;

				foreach (var kvp in m_buildingRectMap)
					UpdateAreaRectangle(kvp.Key, kvp.Value);

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

				m_mapControl.InvalidateDrawings();

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
					m_env.Buildings.CollectionChanged -= OnAreaCollectionChanged;
					((INotifyCollectionChanged)Designation.Designations).CollectionChanged -= OnAreaCollectionChanged;
				}

				m_env = value;

				if (m_env != null)
				{
					m_env.Buildings.CollectionChanged += OnAreaCollectionChanged;
					((INotifyCollectionChanged)Designation.Designations).CollectionChanged += OnAreaCollectionChanged;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
					}
				}
				else
				{
					m_world = null;
				}

				this.Selection = new MapSelection();
				UpdateAreas();

				Notify("Environment");
			}
		}

		void UpdateAreaRectangle(IDrawableArea b, Rectangle rect)
		{
			var r = MapRectToScreenPointRect(b.Area.ToIntRect());

			rect.StrokeThickness = Math.Max(1, this.TileSize / 8);
			Canvas.SetLeft(rect, r.Left);
			Canvas.SetTop(rect, r.Top);
			rect.Width = r.Width;
			rect.Height = r.Height;
		}

		void UpdateAreaPositions()
		{
			foreach (var kvp in m_buildingRectMap)
				UpdateAreaRectangle(kvp.Key, kvp.Value);
		}

		void AddAreaRectangle(IDrawableArea b)
		{
			var rect = new Rectangle();
			rect.Stroke = Brushes.DarkGray;
			rect.Fill = b.Fill;
			rect.Opacity = b.Opacity;
			m_buildingCanvas.Children.Add(rect);
			m_buildingRectMap[b] = rect;
			UpdateAreaRectangle(b, rect);
		}

		void RemoveAreaRectangle(IDrawableArea b)
		{
			var rect = m_buildingRectMap[b];
			m_buildingCanvas.Children.Remove(rect);
			m_buildingRectMap.Remove(b);
		}

		void UpdateAreas()
		{
			m_buildingCanvas.Children.Clear();
			m_buildingRectMap.Clear();

			if (m_env != null)
			{
				var areas = m_env.Buildings.Cast<IDrawableArea>().Concat(Designation.Designations);

				foreach (IDrawableArea area in areas)
				{
					if (area.Environment == m_env && area.Area.ContainsZ(m_z))
						AddAreaRectangle(area);
				}
			}
		}

		void OnAreaCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (IDrawableArea b in e.NewItems)
						if (b.Environment == m_env && b.Area.ContainsZ(m_z))
							AddAreaRectangle(b);
					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (IDrawableArea b in e.OldItems)
						if (b.Environment == m_env && b.Area.ContainsZ(m_z))
							RemoveAreaRectangle(b);

					break;

				default:
					throw new Exception();
			}
		}

		public int Z
		{
			get { return m_z; }

			set
			{
				if (m_z == value)
					return;

				m_z = value;
				m_mapControl.Z = value;
				UpdateAreas();

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
			var sl = m_mapControl.ScreenPointToScreenLocation(p);
			var ml = new IntPoint3D(m_mapControl.ScreenPointToMapLocation(p), m_z);

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
			return m_mapControl.ScreenPointToMapLocation(p);
		}

		public Point MapLocationToScreenPoint(IntPoint loc)
		{
			return m_mapControl.MapLocationToScreenPoint(loc);
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
			m_obs.Clear();
			if (m_env != null)
			{
				var list = m_env.GetContents(m_location);
				foreach (var o in list)
					m_obs.Add(o);
			}

			Notify("Interior");
			Notify("Floor");
			Notify("FloorMaterial");
			Notify("InteriorMaterial");
			Notify("WaterLevel");
			Notify("Objects");
			Notify("Building");
		}

		void MapChanged(IntPoint3D l)
		{
			if (l != m_location)
				return;

			NotifyTileChanges();
		}

		public Environment Environment
		{
			get { return m_env; }
			set
			{
				if (m_env != null)
					m_env.MapTileChanged -= MapChanged;

				m_env = value;

				if (m_env == null)
					m_location = new IntPoint3D();

				if (m_env != null)
					m_env.MapTileChanged += MapChanged;

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
		IEnumerable<ClientGameObject> m_obs;
		MapSelection m_selection;

		public TileAreaInfo()
		{
		}

		void NotifyTileChanges()
		{
			m_obs = null;

			Notify("Interiors");
			Notify("Floors");
			Notify("WaterLevels");
			Notify("Buildings");
			Notify("Objects");
			Notify("Grasses");
		}

		void UpdateObjectList()
		{
			if (m_env != null)
				m_obs = m_selection.SelectionCuboid.Range().SelectMany(p => m_env.GetContents(p));
			else
				m_obs = null;
		}

		void MapChanged(IntPoint3D l)
		{
			if (!m_selection.SelectionCuboid.Contains(l))
				return;

			NotifyTileChanges();
		}

		public Environment Environment
		{
			get { return m_env; }
			set
			{
				if (m_env != null)
					m_env.MapTileChanged -= MapChanged;

				m_env = value;

				if (m_env == null)
				{
					m_selection = new MapSelection();
					Notify("Selection");
				}

				if (m_env != null)
					m_env.MapTileChanged += MapChanged;

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
				if (m_obs == null)
					UpdateObjectList();

				return m_obs;
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
