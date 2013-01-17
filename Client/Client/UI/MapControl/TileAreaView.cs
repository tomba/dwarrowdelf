using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{
	sealed class TileAreaView : INotifyPropertyChanged
	{
		EnvironmentObject m_environment;
		IntGrid3 m_box;
		MovableObjectCollection m_objects;

		public TileAreaView()
		{
			m_objects = new MovableObjectCollection();
			this.Objects = new ReadOnlyMovableObjectCollection(m_objects);
		}

		public EnvironmentObject Environment
		{
			get { return m_environment; }

			set
			{
				if (m_environment == value)
					return;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged -= OnMapTerrainChanged;
					m_environment.MapTileObjectChanged -= OnMapObjectChanged;
				}

				m_environment = value;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged += OnMapTerrainChanged;
					m_environment.MapTileObjectChanged += OnMapObjectChanged;
				}

				Notify("Environment");
				NotifyTileTerrainChanges();
				NotifyTileObjectChanges();
			}
		}

		public IntGrid3 Box
		{
			get { return m_box; }

			set
			{
				if (m_box == value)
					return;

				var old = m_box;

				m_box = value;

				Notify("Box");
				NotifyTileTerrainChanges();
				NotifyTileObjectChanges(old, m_box);
			}
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Interiors");
			Notify("Terrains");
			Notify("WaterLevels");
			Notify("AreaElements");
			Notify("Flags");
		}

		void NotifyTileObjectChanges()
		{
			m_objects.Clear();

			if (this.Environment == null)
				return;

			var obs = m_box.Range().SelectMany(p => m_environment.GetContents(p));
			foreach (var ob in obs)
				m_objects.Add(ob);
		}

		void NotifyTileObjectChanges(IntGrid3 oldGrid, IntGrid3 newGrid)
		{
			if (this.Environment == null)
			{
				m_objects.Clear();
				return;
			}

			var rm = oldGrid.Range().Except(newGrid.Range())
				.SelectMany(p => this.Environment.GetContents(p));

			foreach (var ob in rm)
				m_objects.Remove(ob);

			var add = newGrid.Range().Except(oldGrid.Range())
				.SelectMany(p => this.Environment.GetContents(p));

			foreach (var ob in add)
				m_objects.Add(ob);
		}

		void OnMapTerrainChanged(IntPoint3 l)
		{
			if (!m_box.Contains(l))
				return;

			NotifyTileTerrainChanges();
		}

		void OnMapObjectChanged(MovableObject ob, IntPoint3 l, MapTileObjectChangeType changeType)
		{
			if (!m_box.Contains(l))
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

		public IEnumerable<Tuple<InteriorInfo, MaterialInfo>> Interiors
		{
			get
			{
				if (m_environment == null)
					return null;

				return m_box.Range().
					Select(p => Tuple.Create(m_environment.GetInterior(p), m_environment.GetInteriorMaterial(p))).
					Distinct();
			}
		}

		public IEnumerable<Tuple<TerrainInfo, MaterialInfo>> Terrains
		{
			get
			{
				if (m_environment == null)
					return null;

				return m_box.Range().
					Select(p => Tuple.Create(m_environment.GetTerrain(p), m_environment.GetTerrainMaterial(p))).
					Distinct();
			}
		}

		public IEnumerable<byte> WaterLevels
		{
			get
			{
				if (m_environment == null)
					return null;

				return m_box.Range().
					Select(p => m_environment.GetWaterLevel(p)).
					Distinct();
			}
		}

		public IEnumerable<IAreaElement> AreaElements
		{
			get
			{
				if (m_environment == null)
					return null;

				return m_box.Range().
					Select(p => m_environment.GetElementAt(p)).
					Where(b => b != null).
					Distinct();
			}
		}

		public TileFlags Flags
		{
			get
			{
				if (m_environment == null)
					return TileFlags.None;

				return m_box.Range()
					.Select(p => m_environment.GetTileFlags(p))
					.Aggregate(TileFlags.None, (f, v) => f |= v);
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
