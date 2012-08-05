using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{
	sealed class TileView : INotifyPropertyChanged
	{
		EnvironmentObject m_environment;
		IntPoint3 m_location;
		MovableObjectCollection m_objects;
		TileData m_tileData;

		public TileView()
		{
			m_objects = new MovableObjectCollection();
			this.Objects = new ReadOnlyMovableObjectCollection(m_objects);
		}

		public bool IsEnabled { get { return m_environment != null; } }

		public void ClearTarget()
		{
			SetTarget(null, new IntPoint3());
		}

		public void SetTarget(EnvironmentObject environment, IntPoint3 location)
		{
			bool update = false;

			var oldEnv = m_environment;
			var newEnv = environment;

			if (oldEnv != newEnv)
			{
				if (oldEnv != null)
				{
					oldEnv.MapTileTerrainChanged -= OnMapTerrainChanged;
					oldEnv.MapTileObjectChanged -= OnMapObjectChanged;
				}

				m_environment = newEnv;

				if (newEnv != null)
				{
					newEnv.MapTileTerrainChanged += OnMapTerrainChanged;
					newEnv.MapTileObjectChanged += OnMapObjectChanged;
				}

				update = true;
			}

			var newLoc = location;
			var oldLoc = m_location;

			if (oldLoc != newLoc)
			{
				m_location = newLoc;
				update = true;
			}

			if (update)
			{
				if (newEnv != null)
					m_tileData = newEnv.GetTileData(newLoc);
				else
					m_tileData = TileData.UndefinedTileData;

				UpdateObjectList();

				if (oldEnv != newEnv)
					Notify("Environment");

				if (oldLoc != newLoc)
					Notify("Location");

				NotifyTileTerrainChanges();

				if ((oldEnv == null) != (newEnv == null))
					Notify("IsEnabled");
			}
		}

		public EnvironmentObject Environment
		{
			get { return m_environment; }
		}

		public IntPoint3? Location
		{
			get { if (m_tileData.IsUndefined) return null; else return m_location; }
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interior");
			Notify("Terrain");
			Notify("TerrainMaterial");
			Notify("InteriorMaterial");
			Notify("WaterLevel");
			Notify("Flags");
			Notify("AreaElement");
		}

		void UpdateObjectList()
		{
			if (m_objects.Count > 0)
				m_objects.Clear();

			if (this.Environment != null)
			{
				var obs = this.Environment.GetContents(m_location);
				foreach (var ob in obs)
					m_objects.Add(ob);
			}
		}

		void OnMapTerrainChanged(IntPoint3 l)
		{
			if (l != this.Location)
				return;

			m_tileData = m_environment.GetTileData(l);

			NotifyTileTerrainChanges();
		}

		void OnMapObjectChanged(MovableObject ob, IntPoint3 l, MapTileObjectChangeType changeType)
		{
			if (l != this.Location)
				return;

			switch (changeType)
			{
				case MapTileObjectChangeType.Add:
					Debug.Assert(!m_objects.Contains(ob));
					m_objects.Add(ob);
					break;

				case MapTileObjectChangeType.Remove:
					bool ok = m_objects.Remove(ob);
					Debug.Assert(ok);
					break;

				case MapTileObjectChangeType.Update:
					break;

				default:
					throw new Exception();
			}
		}

		public ReadOnlyMovableObjectCollection Objects { get; private set; }

		public TerrainInfo Terrain
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return Terrains.GetTerrain(m_tileData.TerrainID);
			}
		}

		public MaterialInfo TerrainMaterial
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return Materials.GetMaterial(m_tileData.TerrainMaterialID);
			}
		}

		public InteriorInfo Interior
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return Interiors.GetInterior(m_tileData.InteriorID);
			}
		}

		public MaterialInfo InteriorMaterial
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return Materials.GetMaterial(m_tileData.InteriorMaterialID);
			}
		}

		public byte? WaterLevel
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return m_tileData.WaterLevel;
			}
		}

		public TileFlags? Flags
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return m_tileData.Flags;
			}
		}

		public IAreaElement AreaElement
		{
			get
			{
				if (this.Environment == null || m_tileData.IsUndefined)
					return null;

				return this.Environment.GetElementAt(m_location);
			}
		}

		#region INotifyPropertyChanged Members

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
