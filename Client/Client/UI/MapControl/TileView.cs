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
		IntPoint3D m_location;
		MovableObjectCollection m_objects;

		public TileView()
		{
			m_objects = new MovableObjectCollection();
			this.Objects = new ReadOnlyMovableObjectCollection(m_objects);
		}

		public bool IsEnabled { get { return m_environment != null; } }

		public void ClearTarget()
		{
			SetTarget(null, new IntPoint3D());
		}

		public void SetTarget(EnvironmentObject environment, IntPoint3D location)
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

		public IntPoint3D Location
		{
			get { return m_location; }
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interior");
			Notify("Terrain");
			Notify("TerrainMaterial");
			Notify("InteriorMaterial");
			Notify("WaterLevel");
			Notify("Flags");
			Notify("MapElement");
		}

		void UpdateObjectList()
		{
			if (m_objects.Count > 0)
				m_objects.Clear();

			if (this.Environment != null)
			{
				var obs = this.Environment.GetContents(this.Location);
				foreach (var ob in obs)
					m_objects.Add(ob);
			}
		}

		void OnMapTerrainChanged(IntPoint3D l)
		{
			if (l != this.Location)
				return;

			NotifyTileTerrainChanges();
		}

		void OnMapObjectChanged(MovableObject ob, IntPoint3D l, MapTileObjectChangeType changeType)
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

		public TileFlags Flags
		{
			get
			{
				if (this.Environment == null)
					return TileFlags.None;

				return this.Environment.GetTileFlags(this.Location);
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
