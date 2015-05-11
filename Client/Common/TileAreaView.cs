using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{
	public sealed class TileAreaView : INotifyPropertyChanged
	{
		EnvironmentObject m_environment;
		IntGrid3 m_box;
		IntGrid3 m_adjustedBox;	// adjusted to fit inside env
		MovableObjectCollection m_objects;

		public TileAreaView()
		{
			m_objects = new MovableObjectCollection();
			this.Objects = new ReadOnlyMovableObjectCollection(m_objects);
		}

		public bool IsNotEmpty { get { return m_environment != null && !m_box.IsNull; } }

		public void ClearTarget()
		{
			SetTarget(null, new IntGrid3());
		}

		public IntGrid3 Box { get { return m_box; } }

		public void SetTarget(EnvironmentObject env, IntVector3 p)
		{
			SetTarget(env, new IntGrid3(p, new IntSize3(1, 1, 1)));
		}

		public void SetTarget(EnvironmentObject env, IntGrid3 box)
		{
			if (env == m_environment && m_box == box)
				return;

			if (env != m_environment)
			{
				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged -= OnMapTerrainChanged;
					m_environment.MapTileObjectChanged -= OnMapObjectChanged;
				}

				m_environment = env;

				if (m_environment != null)
				{
					m_environment.MapTileTerrainChanged += OnMapTerrainChanged;
					m_environment.MapTileObjectChanged += OnMapObjectChanged;
				}

				Notify("Environment");
				NotifyTileObjectChanges();
			}

			if (box != m_box)
			{
				var old = m_box;

				m_box = box;
				if (m_box.IsNull || m_environment == null)
					m_adjustedBox = m_box;
				else
					m_adjustedBox = m_box.Intersect(new IntGrid3(m_environment.Size));

				Notify("Box");
				NotifyTileObjectChanges(old, m_box);
			}

			NotifyTileTerrainChanges();
			Notify("IsNotEmpty");
		}

		void NotifyTileTerrainChanges()
		{
			Notify("Tiles");
			Notify("WaterLevels");
			Notify("AreaElements");
			Notify("Flags");
		}

		void NotifyTileObjectChanges()
		{
			m_objects.Clear();

			if (m_environment == null)
				return;

			var obs = m_adjustedBox.Range().
				SelectMany(m_environment.GetContents);
			foreach (var ob in obs)
				m_objects.Add(ob);
		}

		void NotifyTileObjectChanges(IntGrid3 oldGrid, IntGrid3 newGrid)
		{
			if (m_environment == null)
			{
				m_objects.Clear();
				return;
			}

			var rm = oldGrid.Range().Except(newGrid.Range())
				.SelectMany(p => m_environment.GetContents(p));

			foreach (var ob in rm)
				m_objects.Remove(ob);

			var add = newGrid.Range().Except(oldGrid.Range())
				.SelectMany(p => m_environment.GetContents(p));

			foreach (var ob in add)
				m_objects.Add(ob);
		}

		void OnMapTerrainChanged(IntVector3 l)
		{
			if (!m_box.Contains(l))
				return;

			NotifyTileTerrainChanges();
		}

		void OnMapObjectChanged(MovableObject ob, IntVector3 l, MapTileObjectChangeType changeType)
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

		public IEnumerable<Tuple<TileID, MaterialInfo>> Tiles
		{
			get
			{
				if (m_environment == null)
					return null;

				return m_adjustedBox.Range().
					Select(p => Tuple.Create(m_environment.GetTileID(p), m_environment.GetMaterial(p))).
					Distinct();
			}
		}

		public IEnumerable<byte> WaterLevels
		{
			get
			{
				if (m_environment == null)
					return null;

				return m_adjustedBox.Range().
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

				return m_adjustedBox.Range().
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

				return m_adjustedBox.Range()
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
