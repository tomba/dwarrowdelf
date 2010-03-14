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

namespace MyGame.Client
{
	class MapControl : MapControlBase, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		TileInfo m_selectedTileInfo;
		public HoverTileInfo HoverTileInfo { get; private set; }

		bool m_showVirtualSymbols = true;

		public MapControl()
		{
			this.HoverTileInfo = new HoverTileInfo();

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

		protected override void UpdateTile(UIElement _tile, IntPoint _ml)
		{
			var tile = (MapControlTile)_tile;
			bool lit = false;
			IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);

			BitmapSource floorBitmap;
			BitmapSource interiorBitmap;
			BitmapSource objectBitmap;

			if (this.Environment == null)
			{
				floorBitmap = null;
				interiorBitmap = null;
				objectBitmap = null;
			}
			else
			{
				if (GameData.Data.IsSeeAll)
					lit = true;
				else
					lit = TileVisible(ml);

				floorBitmap = GetFloorBitmap(ml, lit);
				interiorBitmap = GetInteriorBitmap(ml, lit);

				if (GameData.Data.DisableLOS)
					lit = true; // lit always so we see what server sends

				objectBitmap = lit ? GetObjectBitmap(ml, lit) : null;
			}

			bool update = tile.FloorBitmap != floorBitmap || tile.InteriorBitmap != interiorBitmap || tile.ObjectBitmap != objectBitmap;

			if (update)
			{
				tile.FloorBitmap = floorBitmap;
				tile.InteriorBitmap = interiorBitmap;
				tile.ObjectBitmap = objectBitmap;
				tile.InvalidateVisual();
			}
		}

		bool TileVisible(IntPoint3D ml)
		{
			if (this.Environment.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			if (this.Environment.GetInterior(ml).ID == InteriorID.Undefined)
				return false;

			var controllables = this.Environment.World.Controllables;

			if (this.Environment.VisibilityMode == VisibilityMode.LOS)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != this.Environment || l.Location.Z != this.Z)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange &&
						l.VisionMap[vp] == true)
						return true;
				}
			}
			else if (this.Environment.VisibilityMode == VisibilityMode.SimpleFOV)
			{
				foreach (var l in controllables)
				{
					if (l.Environment != this.Environment || l.Location.Z != this.Z)
						continue;

					IntPoint vp = new IntPoint(ml.X - l.Location.X, ml.Y - l.Location.Y);

					if (Math.Abs(vp.X) <= l.VisionRange && Math.Abs(vp.Y) <= l.VisionRange)
						return true;
				}
			}
			else
			{
				throw new Exception();
			}

			return false;
		}

		BitmapSource GetFloorBitmap(IntPoint3D ml, bool lit)
		{
			var flrInfo = this.Environment.GetFloor(ml);

			if (flrInfo == null || flrInfo == Floors.Undefined)
				return null;

			FloorID fid = flrInfo.ID;
			SymbolID id;

			switch (fid)
			{
				case FloorID.NaturalFloor:
				case FloorID.Floor:
				case FloorID.Hole:
					id = SymbolID.Floor;
					break;

				case FloorID.Empty:
					id = SymbolID.Undefined;
					break;

				default:
					throw new Exception();
			}

			if (m_showVirtualSymbols)
			{
				if (fid == FloorID.Empty)
				{
					id = SymbolID.Floor;
					lit = false;
				}
			}

			if (id == SymbolID.Undefined)
				return null;

			return m_bitmapCache.GetBitmap(id, Colors.Black, !lit);
		}

		BitmapSource GetInteriorBitmap(IntPoint3D ml, bool lit)
		{
			SymbolID id;

			var intInfo = this.Environment.GetInterior(ml);
			var intInfo2 = this.Environment.GetInterior(ml + Direction.Down);

			var intID = intInfo.ID;
			var intID2 = intInfo2.ID;

			if (intInfo == null || intInfo == Interiors.Undefined)
				return null;

			switch (intID)
			{
				case InteriorID.Stairs:
					id = SymbolID.StairsUp;
					break;

				case InteriorID.Empty:
					id = SymbolID.Undefined;
					break;

				case InteriorID.NaturalWall:
				case InteriorID.Wall:
					id = SymbolID.Wall;
					break;

				case InteriorID.Grass:
					id = SymbolID.Grass;
					break;

				case InteriorID.Portal:
					id = SymbolID.Portal;
					break;

				case InteriorID.Sapling:
					id = SymbolID.Sapling;
					break;

				case InteriorID.Tree:
					id = SymbolID.Tree;
					break;

				case InteriorID.SlopeNorth:
				case InteriorID.SlopeSouth:
				case InteriorID.SlopeEast:
				case InteriorID.SlopeWest:
					{
						switch (Interiors.GetDirFromSlope(intID))
						{
							case Direction.North:
								id = SymbolID.SlopeUpNorth;
								break;
							case Direction.South:
								id = SymbolID.SlopeUpSouth;
								break;
							case Direction.East:
								id = SymbolID.SlopeUpEast;
								break;
							case Direction.West:
								id = SymbolID.SlopeUpWest;
								break;
							default:
								throw new Exception();
						}
					}
					break;

				default:
					throw new Exception();
			}

			if (m_showVirtualSymbols)
			{
				if (intID == InteriorID.Stairs && intID2 == InteriorID.Stairs)
				{
					id = SymbolID.StairsUpDown;
				}
				else if (intID == InteriorID.Empty && intID2.IsSlope())
				{
					switch (intID2)
					{
						case InteriorID.SlopeNorth:
							id = SymbolID.SlopeDownSouth;
							break;

						case InteriorID.SlopeSouth:
							id = SymbolID.SlopeDownNorth;
							break;

						case InteriorID.SlopeEast:
							id = SymbolID.SlopeDownWest;
							break;
						
						case InteriorID.SlopeWest:
							id = SymbolID.SlopeDownEast;
							break;
					}
				}
			}

			if (id == SymbolID.Undefined)
				return null;

			return m_bitmapCache.GetBitmap(id, Colors.Black, !lit);
		}

		BitmapSource GetObjectBitmap(IntPoint3D ml, bool lit)
		{
			IList<ClientGameObject> obs = this.Environment.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				var id = obs[0].SymbolID;
				Color c = obs[0].Color;
				return m_bitmapCache.GetBitmap(id, c, !lit);
			}
			else
				return null;
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

		public TileInfo SelectedTileInfo
		{
			get { return m_selectedTileInfo; }
			private set
			{
				if (m_selectedTileInfo == value)
					return;

				m_selectedTileInfo = value;

				Notify("SelectedTileInfo");
			}
		}

		void OnSelectionChanged()
		{
			IntRect sel = this.SelectionRect;

			if (sel.Width != 1 || sel.Height != 1)
			{
				if (this.SelectedTileInfo != null)
					this.SelectedTileInfo.StopObserve();
				this.SelectedTileInfo = null;
				return;
			}

			if (this.SelectedTileInfo == null)
			{
				this.SelectedTileInfo = new TileInfo(this.Environment, new IntPoint3D(sel.X1Y1, this.Z));
			}
			else
			{
				this.SelectedTileInfo.Environment = this.Environment;
				this.SelectedTileInfo.Location = new IntPoint3D(sel.X1Y1, this.Z);
			}
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

			protected override void OnRender(DrawingContext drawingContext)
			{
				if (this.FloorBitmap != null)
					drawingContext.DrawImage(this.FloorBitmap, new Rect(this.RenderSize));

				if (this.InteriorBitmap != null)
					drawingContext.DrawImage(this.InteriorBitmap, new Rect(this.RenderSize));

				if (this.ObjectBitmap != null)
					drawingContext.DrawImage(this.ObjectBitmap, new Rect(this.RenderSize));
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

		public TileInfo()
		{
		}

		public TileInfo(Environment mapLevel, IntPoint3D location)
		{
			m_env = mapLevel;
			m_location = location;
			if (m_env != null)
				m_env.MapChanged += MapChanged;
		}

		public void StopObserve()
		{
			if (m_env != null)
				m_env.MapChanged -= MapChanged;
		}

		void NotifyTileChanges()
		{
			Notify("Interior");
			Notify("Floor");
			Notify("FloorMaterial");
			Notify("InteriorMaterial");
			Notify("Objects");
			Notify("Building");
		}

		void MapChanged(IntPoint3D l)
		{
			if (l == m_location)
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

		public IList<ClientGameObject> Objects
		{
			get
			{
				if (m_env == null)
					return null;
				return m_env.GetContents(m_location);
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
