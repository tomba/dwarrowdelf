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

namespace MyGame.Client
{
	class MapControl : MapControlBase, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		public HoverTileInfo HoverTileInfo { get; private set; }

		bool m_showVirtualSymbols = true;

		public MapControl()
		{
			this.HoverTileInfo = new HoverTileInfo();
			this.SelectedTileInfo = new TileInfo();

			base.SelectionChanged += OnSelectionChanged;

			var dpd = DependencyPropertyDescriptor.FromProperty(MapControlBase.TileSizeProperty,
				typeof(MapControlBase));
			dpd.AddValueChanged(this, OnTileSizeChanged);

			var dpd2 = DependencyPropertyDescriptor.FromProperty(MapControlBase.CenterPosProperty,
				typeof(MapControlBase));
			dpd2.AddValueChanged(this, OnCenterPosChanged);
		}

		void OnTileSizeChanged(object ob, EventArgs e)
		{
			if (m_bitmapCache != null)
				m_bitmapCache.TileSize = this.TileSize;
		}

		void OnCenterPosChanged(object ob, EventArgs e)
		{
			UpdateHoverTileInfo(Mouse.GetPosition(this));
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		MapHelper m_mapHelper = new MapHelper();

		protected override void UpdateTile(UIElement _tile, IntPoint _ml)
		{
			var tile = (MapControlTile)_tile;
			IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);

			var data = m_mapHelper;
			data.Resolve(this.Environment, ml, m_showVirtualSymbols);

			BitmapSource floorBitmap = null;
			BitmapSource interiorBitmap = null;
			BitmapSource objectBitmap = null;
			BitmapSource topBitmap = null;

			if (data.FloorSymbolID != SymbolID.Undefined)
				floorBitmap = m_bitmapCache.GetBitmap(data.FloorSymbolID, Colors.Black, data.FloorDark);

			if (data.InteriorSymbolID != SymbolID.Undefined)
				interiorBitmap = m_bitmapCache.GetBitmap(data.InteriorSymbolID, Colors.Black, data.InteriorDark);

			if (data.ObjectSymbolID != SymbolID.Undefined)
				objectBitmap = m_bitmapCache.GetBitmap(data.ObjectSymbolID, data.ObjectColor, data.ObjectDark);

			if (data.TopSymbolID != SymbolID.Undefined)
				topBitmap = m_bitmapCache.GetBitmap(data.TopSymbolID, Colors.Black, data.TopDark);

			bool update = tile.FloorBitmap != floorBitmap || tile.InteriorBitmap != interiorBitmap || tile.ObjectBitmap != objectBitmap ||
				tile.TopBitmap != topBitmap;

			if (update)
			{
				tile.FloorBitmap = floorBitmap;
				tile.InteriorBitmap = interiorBitmap;
				tile.ObjectBitmap = objectBitmap;
				tile.TopBitmap = topBitmap;
				tile.InvalidateVisual();
			}
		}

		public bool ShowVirtualSymbols
		{
			get { return m_showVirtualSymbols; }

			set
			{
				if (m_showVirtualSymbols == value)
					return;

				m_showVirtualSymbols = value;
				InvalidateTiles();
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

				if (m_env != null)
				{
					m_env.MapChanged -= MapChangedCallback;
					m_env.Buildings.CollectionChanged -= OnBuildingsChanged;
				}

				m_env = value;

				if (m_env != null)
				{
					m_env.MapChanged += MapChangedCallback;
					m_env.Buildings.CollectionChanged += OnBuildingsChanged;

					if (m_world != m_env.World)
					{
						m_world = m_env.World;
						m_bitmapCache = new SymbolBitmapCache(m_world.SymbolDrawingCache, this.TileSize);
					}
				}
				else
				{
					m_world = null;
					m_bitmapCache = null;
				}

				this.SelectionRect = new IntRect();
				UpdateTiles();
				UpdateBuildings();

				Notify("Environment");
			}
		}

		void UpdateBuildings()
		{
			this.Children.Clear();

			if (m_env != null)
			{
				foreach (var b in m_env.Buildings)
				{
					if (b.Environment == m_env && b.Z == m_z)
					{
						var rect = new Rectangle();
						rect.Stroke = Brushes.DarkGray;
						rect.StrokeThickness = 4;
						this.Children.Add(rect);
						SetCorner1(rect, b.Area.X1Y1);
						SetCorner2(rect, b.Area.X2Y2);
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
					this.Children.Add(rect);
					SetCorner1(rect, b.Area.X1Y1);
					SetCorner2(rect, b.Area.X2Y2);
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				this.Children.Clear();
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
				UpdateTiles();
				UpdateBuildings();

				Notify("Z");
				UpdateHoverTileInfo(Mouse.GetPosition(this));
			}
		}

		void MapChangedCallback(IntPoint3D l)
		{
			InvalidateTiles();
		}

		public TileInfo SelectedTileInfo { get; private set; }

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

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			UpdateHoverTileInfo(e.GetPosition(this));
		}

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


		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		#region INotifyPropertyChanged Members
		public event PropertyChangedEventHandler PropertyChanged;
		#endregion

		class MapControlTile : UIElement
		{
			public MapControlTile()
			{
				this.IsHitTestVisible = false;
			}

			public BitmapSource FloorBitmap { get; set; }
			public BitmapSource InteriorBitmap { get; set; }
			public BitmapSource ObjectBitmap { get; set; }
			public BitmapSource TopBitmap { get; set; }

			protected override void OnRender(DrawingContext drawingContext)
			{
				if (this.FloorBitmap != null)
					drawingContext.DrawImage(this.FloorBitmap, new Rect(this.RenderSize));

				if (this.InteriorBitmap != null)
					drawingContext.DrawImage(this.InteriorBitmap, new Rect(this.RenderSize));

				if (this.ObjectBitmap != null)
					drawingContext.DrawImage(this.ObjectBitmap, new Rect(this.RenderSize));

				if (this.TopBitmap != null)
					drawingContext.DrawImage(this.TopBitmap, new Rect(this.RenderSize));
			}
		}
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
