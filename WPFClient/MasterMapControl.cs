﻿using System;
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

namespace MyGame.Client
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

		DispatcherTimer m_hoverTimer;

		IntPoint m_centerPos;
		int m_tileSize = 32;

		Rectangle m_selectionRect;
		IntPoint m_selectionStart;
		IntPoint m_selectionEnd;

		Canvas m_canvas;
		Canvas m_buildingCanvas;
		Dictionary<BuildingObject, Rectangle> m_buildingRectMap;

		ToolTip m_tileToolTip;

		public MasterMapControl()
		{
			this.HoverTileInfo = new HoverTileInfo();
			this.SelectedTileInfo = new TileInfo();

			m_hoverTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_hoverTimer.Tick += HoverTimerTick;
			m_hoverTimer.Interval = TimeSpan.FromMilliseconds(500);

			var grid = new Grid();
			AddChild(grid);

			//var mc = new MapControlD2D();
			var mc = new MapControl();
			grid.Children.Add(mc);
			m_mapControl = mc;

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

			m_buildingRectMap = new Dictionary<BuildingObject, Rectangle>();

			m_tileToolTip = new ToolTip();
			m_tileToolTip.Content = new ObjectInfoControl();
			m_tileToolTip.PlacementTarget = this;
			m_tileToolTip.Placement = System.Windows.Controls.Primitives.PlacementMode.RelativePoint;
			m_tileToolTip.IsOpen = false;
		}

		public int Columns { get { return m_mapControl.Columns; } }
		public int Rows { get { return m_mapControl.Rows; } }

		public int TileSize
		{
			get
			{
				return m_tileSize;
			}

			set
			{
				m_tileSize = value;
				m_mapControl.TileSize = value;
				UpdateSelectionRect();
				foreach (var kvp in m_buildingRectMap)
					UpdateBuildingRect(kvp.Key, kvp.Value);
			}
		}

		public bool SelectionEnabled { get; set; }

		void OnSelectionChanged()
		{
			IntRect sel = this.SelectionRect;

			if (sel.Width != 1 || sel.Height != 1)
			{
				this.SelectedTileInfo.Environment = null;
				return;
			}

			this.SelectedTileInfo.Environment = this.Environment;
			this.SelectedTileInfo.Location = new IntPoint3D(sel.X1Y1, this.Z);
		}

		public IntRect SelectionRect
		{
			get
			{
				if (m_selectionRect.Visibility != Visibility.Visible)
					return new IntRect();

				var p1 = m_selectionStart;
				var p2 = m_selectionEnd;

				IntRect r = new IntRect(p1, p2);
				r = r.Inflate(1, 1);
				return r;
			}

			set
			{
				if (this.SelectionEnabled == false)
					return;

				if (value.Width == 0 || value.Height == 0)
				{
					m_selectionRect.Visibility = Visibility.Hidden;
					OnSelectionChanged();
					return;
				}

				var newStart = value.X1Y1;
				var newEnd = value.X2Y2 - new IntVector(1, 1);

				if ((newStart != m_selectionStart) || (newEnd != m_selectionEnd))
				{
					m_selectionStart = newStart;
					m_selectionEnd = newEnd;
					UpdateSelectionRect();
				}

				m_selectionRect.Visibility = Visibility.Visible;

				OnSelectionChanged();
			}
		}

		void UpdateSelectionRect()
		{
			var ir = new IntRect(m_selectionStart, m_selectionEnd);
			ir = new IntRect(ir.X1Y1, new IntSize(ir.Width + 1, ir.Height + 1));

			Rect r = new Rect(MapLocationToScreenPoint(new IntPoint(ir.X1, ir.Y2)),
				new Size(ir.Width * this.TileSize, ir.Height * this.TileSize));

			m_selectionRect.Width = r.Width;
			m_selectionRect.Height = r.Height;
			Canvas.SetLeft(m_selectionRect, r.Left);
			Canvas.SetTop(m_selectionRect, r.Top);
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				base.OnMouseDown(e);
				return;
			}

			Point pos = e.GetPosition(this);

			var newStart = ScreenPointToMapLocation(pos);
			var newEnd = newStart;

			if ((newStart != m_selectionStart) || (newEnd != m_selectionEnd))
			{
				m_selectionStart = newStart;
				m_selectionEnd = newEnd;
				UpdateSelectionRect();
			}

			m_selectionRect.Visibility = Visibility.Visible;

			CaptureMouse();

			e.Handled = true;

			OnSelectionChanged();

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			UpdateHoverTileInfo(e.GetPosition(this));

			m_hoverTimer.Stop();
			m_hoverTimer.Start();

			if (this.SelectionEnabled == false)
				return;

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

			var newEnd = ScreenPointToMapLocation(pos);

			if (newEnd != m_selectionEnd)
			{
				m_selectionEnd = newEnd;
				UpdateSelectionRect();
			}

			e.Handled = true;

			OnSelectionChanged();

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			ReleaseMouseCapture();

			base.OnMouseUp(e);
		}

		void HoverTimerTick(object sender, EventArgs e)
		{
			m_hoverTimer.Stop();

			var p = Mouse.GetPosition(this);

			if (this.Environment == null || !new Rect(this.RenderSize).Contains(p))
			{
				m_tileToolTip.IsOpen = false;
				this.ToolTip = null;
				return;
			}

			var ml = new IntPoint3D(ScreenPointToMapLocation(p), this.Z);
			var objectList = this.Environment.GetContents(ml);
			if (objectList == null || objectList.Count == 0)
			{
				m_tileToolTip.IsOpen = false;
				this.ToolTip = null;
				return;
			}

			m_tileToolTip.DataContext = objectList[0];

			m_tileToolTip.HorizontalOffset = p.X;
			m_tileToolTip.VerticalOffset = p.Y;
			m_tileToolTip.IsOpen = true;
			this.ToolTip = m_tileToolTip;
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

				double dx = dv.X * m_tileSize;
				double dy = -dv.Y * m_tileSize;

				foreach (var kvp in m_buildingRectMap)
					UpdateBuildingRect(kvp.Key, kvp.Value);
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
					m_env.Buildings.CollectionChanged -= OnBuildingsChanged;
				}

				m_env = value;

				if (m_env != null)
				{
					m_env.Buildings.CollectionChanged += OnBuildingsChanged;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
					}
				}
				else
				{
					m_world = null;
				}

				this.SelectionRect = new IntRect();
				UpdateBuildings();

				Notify("Environment");
			}
		}

		void UpdateBuildingRect(BuildingObject b, Rectangle rect)
		{
			var sp = MapLocationToScreenPoint(new IntPoint(b.Area.X1, b.Area.Y2));
			Canvas.SetLeft(rect, sp.X);
			Canvas.SetTop(rect, sp.Y);
			rect.Width = b.Area.Width * m_tileSize;
			rect.Height = b.Area.Height * m_tileSize;
		}

		void UpdateBuildings()
		{
			m_buildingCanvas.Children.Clear();
			m_buildingRectMap.Clear();

			if (m_env != null)
			{
				foreach (var b in m_env.Buildings)
				{
					if (b.Environment == m_env && b.Z == m_z)
					{
						var rect = new Rectangle();
						rect.Stroke = Brushes.DarkGray;
						rect.StrokeThickness = 4;
						m_buildingCanvas.Children.Add(rect);
						m_buildingRectMap[b] = rect;
						UpdateBuildingRect(b, rect);
					}
				}
			}
		}

		void OnBuildingsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				foreach (BuildingObject b in e.NewItems)
				{
					var rect = new Rectangle();
					rect.Stroke = Brushes.DarkGray;
					rect.StrokeThickness = 4;
					m_buildingCanvas.Children.Add(rect);
					m_buildingRectMap[b] = rect;
					UpdateBuildingRect(b, rect);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				m_buildingCanvas.Children.Clear();
			}
			else
			{
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
				UpdateBuildings();

				Notify("Z");
				UpdateHoverTileInfo(Mouse.GetPosition(this));
			}
		}

		public TileInfo SelectedTileInfo { get; private set; }

		void UpdateHoverTileInfo(Point mousePos)
		{
			IntPoint ml = ScreenPointToMapLocation(mousePos);
			var p = new IntPoint3D(ml, m_z);

			if (p != this.HoverTileInfo.Location)
			{
				this.HoverTileInfo.Location = p;
				Notify("HoverTileInfo");
			}
		}

		IntPoint ScreenPointToScreenLocation(Point p)
		{
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var loc = ScreenPointToScreenLocation(p);
			loc = new IntPoint(loc.X, -loc.Y);
			return loc + (IntVector)this.TopLeftPos;
		}

		Point MapLocationToScreenPoint(IntPoint loc)
		{
			loc -= (IntVector)this.TopLeftPos;
			loc = new IntPoint(loc.X, -loc.Y + 1);
			return new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
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



	class HoverTileInfo : INotifyPropertyChanged
	{
		public IntPoint3D Location { get; set; }

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion
	}

	class TileInfo : INotifyPropertyChanged
	{
		Environment m_env;
		IntPoint3D m_location;
		ObservableCollection<ClientGameObject> m_obs;

		public TileInfo()
		{
			m_obs = new ObservableCollection<ClientGameObject>();
		}

		void NotifyTileChanges()
		{
			m_obs.Clear();
			if (m_env != null)
			{
				var list = m_env.GetContents(m_location);
				if (list != null)
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
					m_env.MapChanged -= MapChanged;

				m_env = value;

				if (m_env == null)
					m_location = new IntPoint3D();

				if (m_env != null)
					m_env.MapChanged += MapChanged;

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

		public ObservableCollection<ClientGameObject> Objects
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
}
