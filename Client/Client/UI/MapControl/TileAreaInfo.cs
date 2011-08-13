using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{
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
			Notify("MapElements");
			Notify("Grasses");
		}

		void MapTerrainChanged(IntPoint3D l)
		{
			if (!m_selection.SelectionCuboid.Contains(l))
				return;

			NotifyTileTerrainChanges();
		}

		void MapObjectChanged(GameObject ob, IntPoint3D l, MapTileObjectChangeType changetype)
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

		public IEnumerable<GameObject> Objects
		{
			get
			{
				return m_objects;
			}
		}

		public IEnumerable<IDrawableElement> MapElements
		{
			get
			{
				if (m_env == null)
					return null;

				return m_selection.SelectionCuboid.Range().
					Select(p => m_env.GetElementAt(p)).
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
}
