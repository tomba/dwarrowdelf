using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Dwarrowdelf.Client.UI
{
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
}
