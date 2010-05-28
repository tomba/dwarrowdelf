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
	class MyMapControlD2D : UserControl, INotifyPropertyChanged
	{
		World m_world;
		SymbolBitmapCache m_bitmapCache;

		Environment m_env;
		int m_z;

		public HoverTileInfo HoverTileInfo { get; private set; }

		bool m_showVirtualSymbols = true;

		MapControlD2D m_mcd2d;
		BitmapSource[] m_bmpArray;

		DispatcherTimer m_updateTimer;

		public MyMapControlD2D()
		{
			this.HoverTileInfo = new HoverTileInfo();
			this.SelectedTileInfo = new TileInfo();

			m_updateTimer = new DispatcherTimer(DispatcherPriority.Normal);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(100);

			this.TileSize = 32;

			m_mcd2d = new MapControlD2D();
			AddChild(m_mcd2d);
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			m_updateTimer.Stop();
			UpdateTiles();
		}

		void Upda()
		{
			if (m_bitmapCache == null)
			{
				m_mcd2d.SetTiles(null, 0);
			}
			else
			{
				var arr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
				var len = (int)arr.Max() + 1;
				m_bmpArray = new BitmapSource[len];
				for (int i = 0; i < len; ++i)
					m_bmpArray[i] = m_bitmapCache.GetBitmap((SymbolID)i, Colors.Black, false);
				m_mcd2d.SetTiles(m_bmpArray, this.TileSize);
			}
		}

		/*
		void OnTileSizeChanged(object ob, EventArgs e)
		{
			if (m_bitmapCache != null)
				m_bitmapCache.TileSize = this.TileSize;
		}
		*/
		void OnCenterPosChanged(object ob, EventArgs e)
		{
			UpdateHoverTileInfo(Mouse.GetPosition(this));
		}

		public void InvalidateTiles()
		{
			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
		}

		void UpdateTiles()
		{
			int columns = this.Columns;
			int rows = this.Rows;
			var map = m_mcd2d.TileMap;

			for (int y = 0; y < rows; ++y)
			{
				for (int x = 0; x < columns; ++x)
				{
					UpdateTile(x, y, map);
				}
			}

			m_mcd2d.Render();
		}

		public int TileSize { get; set; }
		public int Columns { get { return m_mcd2d.Columns; } }
		public int Rows { get { return m_mcd2d.Rows; } }

		public IntRect SelectionRect { get; set; }

		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		public static readonly DependencyProperty CenterPosProperty = DependencyProperty.Register(
			"CenterPos", typeof(IntPoint), typeof(MapControlD2D),
			new FrameworkPropertyMetadata(new IntPoint(), FrameworkPropertyMetadataOptions.None));

		public IntPoint CenterPos
		{
			get { return (IntPoint)GetValue(CenterPosProperty); }
			set
			{
				if (value == this.CenterPos)
					return;

				SetValue(CenterPosProperty, value);
				UpdateTiles();
			}
		}

		public bool SelectionEnabled { get; set; }

		void UpdateTile(int x, int y, MapD2DData[, ,] map)
		{
			//IntPoint3D ml = new IntPoint3D(_ml.X, _ml.Y, this.Z);
			var ml = new IntPoint3D(x + this.CenterPos.X, y + this.CenterPos.Y, this.Z);

			SymbolID floorBitmap;
			SymbolID interiorBitmap;
			SymbolID objectBitmap;
			SymbolID topBitmap;

			bool lit1 = false;
			bool lit2 = false;
			bool lit3 = false;
			bool lit4 = false;

			if (this.Environment == null)
			{
				floorBitmap = SymbolID.Undefined;
				interiorBitmap = SymbolID.Undefined;
				objectBitmap = SymbolID.Undefined;
				topBitmap = SymbolID.Undefined;
			}
			else
			{
				if (GameData.Data.IsSeeAll)
					lit1 = lit2 = lit3 = lit4 = true;
				else
					lit1 = lit2 = lit3 = lit4 = TileVisible(ml);

				floorBitmap = GetFloorBitmap(ml, ref lit1);
				interiorBitmap = GetInteriorBitmap(ml, ref lit2);

				if (GameData.Data.DisableLOS)
					lit3 = true; // lit always so we see what server sends

				objectBitmap = lit3 ? GetObjectBitmap(ml, ref lit3) : SymbolID.Undefined;

				topBitmap = GetTopBitmap(ml, ref lit4);
			}

			map[y, x, 0].SymbolID = (byte)floorBitmap;
			map[y, x, 1].SymbolID = (byte)interiorBitmap;
			map[y, x, 2].SymbolID = (byte)objectBitmap;
			map[y, x, 3].SymbolID = (byte)topBitmap;

			map[y, x, 0].Dark = !lit1;
			map[y, x, 1].Dark = !lit2;
			map[y, x, 2].Dark = !lit3;
			map[y, x, 3].Dark = !lit4;
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

		SymbolID GetFloorBitmap(IntPoint3D ml, ref bool lit)
		{
			var flrInfo = this.Environment.GetFloor(ml);

			if (flrInfo == null || flrInfo == Floors.Undefined)
				return SymbolID.Undefined;

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

			return id;
		}

		SymbolID GetInteriorBitmap(IntPoint3D ml, ref bool lit)
		{
			SymbolID id;

			var intInfo = this.Environment.GetInterior(ml);
			var intInfo2 = this.Environment.GetInterior(ml + Direction.Down);

			var intID = intInfo.ID;
			var intID2 = intInfo2.ID;

			if (intInfo == null || intInfo == Interiors.Undefined)
				return SymbolID.Undefined;

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

			return id;
		}

		SymbolID GetObjectBitmap(IntPoint3D ml, ref bool lit)
		{
			IList<ClientGameObject> obs = this.Environment.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				var id = obs[0].SymbolID;
				Color c = obs[0].Color;
				return id;
			}
			else
				return SymbolID.Undefined;
		}

		SymbolID GetTopBitmap(IntPoint3D ml, ref bool lit)
		{
			int wl = this.Environment.GetWaterLevel(ml);

			if (wl == 0)
				return SymbolID.Undefined;

			SymbolID id;

			wl = wl * 100 / TileData.MaxWaterLevel;

			if (wl > 80)
				id = SymbolID.Water100;
			else if (wl > 60)
				id = SymbolID.Water80;
			else if (wl > 40)
				id = SymbolID.Water60;
			else if (wl > 20)
				id = SymbolID.Water40;
			else
				id = SymbolID.Water20;

			return id;
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
						Upda();
					}
				}
				else
				{
					m_world = null;
					m_bitmapCache = null;
					Upda();
				}

				this.SelectionRect = new IntRect();
				UpdateTiles();
				UpdateBuildings();

				Notify("Environment");
			}
		}

		void UpdateBuildings()
		{
			/*
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
			*/
		}

		void OnBuildingsChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			/*
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
			 */
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
