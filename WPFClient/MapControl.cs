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

		Dictionary<string, SymbolInfo> m_symbolLookupCache = new Dictionary<string, SymbolInfo>();
		SymbolInfo GetSymbol(string symbolName)
		{
			SymbolInfo symbolInfo;
			if (!m_symbolLookupCache.TryGetValue(symbolName, out symbolInfo))
			{
				symbolInfo = this.Environment.World.AreaData.Symbols.Single(s => s.Name == symbolName);
				m_symbolLookupCache[symbolInfo.Name] = symbolInfo;
			}
			return symbolInfo;
		}

		BitmapSource GetFloorBitmap(IntPoint3D ml, bool lit)
		{
			int id;
			Color c;

			var flrInfo = this.Environment.GetFloor(ml);

			if (flrInfo == null || flrInfo == Floors.Undefined)
				return null;

			string symName;

			if (flrInfo == Floors.Hole)
			{
				symName = Floors.NaturalFloor.Name;
			}
			else if (flrInfo == Floors.Empty)
			{
				symName = null;
			}
			else
			{
				symName = flrInfo.Name;
			}

			if (m_showVirtualSymbols)
			{
				if (flrInfo == Floors.Empty)
				{
					symName = Floors.NaturalFloor.Name;
					lit = false;
				}
			}

			if (symName == null)
				return null;

			var symbolInfo = GetSymbol(symName);
			id = symbolInfo.ID;
			c = Colors.Black;

			return m_bitmapCache.GetBitmap(id, c, !lit);
		}

		BitmapSource GetInteriorBitmap(IntPoint3D ml, bool lit)
		{
			int id;
			Color c;

			var intInfo = this.Environment.GetInterior(ml);
			var intInfo2 = this.Environment.GetInterior(ml + Direction.Down);

			var intID = intInfo.ID;
			var intID2 = intInfo2.ID;

			if (intInfo == null || intInfo == Interiors.Undefined)
				return null;

			string symbolName;

			if (intID == InteriorID.Stairs)
			{
				symbolName = "StairsUp";
			}
			else if (intID.IsSlope())
			{
				switch (Interiors.GetDirFromSlope(intID))
				{
					case Direction.North:
						symbolName = "SlopeUpNorth";
						break;
					case Direction.South:
						symbolName = "SlopeUpSouth";
						break;
					case Direction.East:
						symbolName = "SlopeUpEast";
						break;
					case Direction.West:
						symbolName = "SlopeUpWest";
						break;
					default:
						throw new Exception();
				}
			}
			else if (intInfo != Interiors.Empty)
			{
				symbolName = intInfo.Name;
			}
			else
			{
				symbolName = null;
			}

			if (m_showVirtualSymbols)
			{
				if (intID == InteriorID.Stairs && intID2 == InteriorID.Stairs)
				{
					symbolName = "StairsUpDown";
				}
				else if (intID == InteriorID.Empty && intID2.IsSlope())
				{
					symbolName = "SlopeDown" + Interiors.GetDirFromSlope(intID2).Reverse().ToString();
				}
			}

			if (symbolName == null)
				return null;

			var symbolInfo = GetSymbol(symbolName);
			id = symbolInfo.ID;
			c = Colors.Black;

			return m_bitmapCache.GetBitmap(id, c, !lit);
		}

		BitmapSource GetObjectBitmap(IntPoint3D ml, bool lit)
		{
			IList<ClientGameObject> obs = this.Environment.GetContents(ml);
			if (obs != null && obs.Count > 0)
			{
				int id = obs[0].SymbolID;
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
					m_env.MapChanged -= MapChangedCallback;

				m_env = value;

				if (m_env != null)
				{
					m_env.MapChanged += MapChangedCallback;

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

				InvalidateTiles();

				Notify("Environment");
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
				InvalidateTiles();
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
			set
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

		public BuildingData Building
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
