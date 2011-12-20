using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows;

namespace Dwarrowdelf.Client.UI
{
	class TileInfo : INotifyPropertyChanged
	{
		MapControl m_mapControl;

		public Point MousePos { get; private set; }
		public IntPoint ScreenLocation { get; private set; }

		public TileInfo(MapControl mapControl)
		{
			m_mapControl = mapControl;

			m_mapControl.MouseMove += OnMouseMove;
			m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
			m_mapControl.EnvironmentChanged += OnEnvironmentChanged;

			this.Objects = new MovableObjectCollection();
		}

		void OnEnvironmentChanged(EnvironmentObject env)
		{
			if (this.Environment == env)
				return;

			if (this.Environment != null)
			{
				this.Environment.MapTileTerrainChanged -= MapTerrainChanged;
				this.Environment.MapTileObjectChanged -= MapObjectChanged;
			}

			this.Environment = env;

			if (this.Environment != null)
			{
				this.Environment.MapTileTerrainChanged += MapTerrainChanged;
				this.Environment.MapTileObjectChanged += MapObjectChanged;
			}

			Notify("Environment");
			NotifyTileChanges();
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			var p = e.GetPosition(m_mapControl);
			UpdateHoverTileInfo(p);
		}

		void OnTileLayoutChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			var p = Mouse.GetPosition(m_mapControl);
			UpdateHoverTileInfo(p);
		}

		void UpdateHoverTileInfo(Point p)
		{
			var sl = m_mapControl.ScreenPointToIntScreenTile(p);
			var ml = m_mapControl.ScreenPointToMapLocation(p);

			if (p != this.MousePos)
			{
				this.MousePos = p;
				Notify("MousePos");
			}

			if (sl != this.ScreenLocation)
			{
				this.ScreenLocation = sl;
				Notify("ScreenLocation");
			}

			if (ml != this.Location)
			{
				this.Location = ml;
				Notify("Location");
				NotifyTileChanges();
			}
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
			Notify("Grass");
			Notify("MapElement");
		}

		void NotifyTileObjectChanges()
		{
			this.Objects.Clear();
			if (this.Environment != null)
			{
				var list = this.Environment.GetContents(this.Location);
				foreach (var o in list)
					this.Objects.Add(o);
			}

			Notify("Objects");
		}

		void MapTerrainChanged(IntPoint3D l)
		{
			if (l != this.Location)
				return;

			NotifyTileTerrainChanges();
		}

		void MapObjectChanged(MovableObject ob, IntPoint3D l, MapTileObjectChangeType changeType)
		{
			if (l != this.Location)
				return;

			NotifyTileObjectChanges();
		}

		public EnvironmentObject Environment { get; private set; }
		public IntPoint3D Location { get; private set; }
		public MovableObjectCollection Objects { get; private set; }

		public InteriorInfo Interior
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetInterior(this.Location);
			}
		}

		public MaterialInfo InteriorMaterial
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetInteriorMaterial(this.Location);
			}
		}

		public MaterialInfo TerrainMaterial
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetTerrainMaterial(this.Location);
			}
		}

		public TerrainInfo Terrain
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetTerrain(this.Location);
			}
		}

		public byte WaterLevel
		{
			get
			{
				if (this.Environment == null)
					return 0;
				return this.Environment.GetWaterLevel(this.Location);
			}
		}

		public bool Grass
		{
			get
			{
				if (this.Environment == null)
					return false;
				return this.Environment.GetGrass(this.Location);
			}
		}

		public IDrawableElement MapElement
		{
			get
			{
				if (this.Environment == null)
					return null;
				return this.Environment.GetElementAt(this.Location);
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
