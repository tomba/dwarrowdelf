using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Tracks items matching give criteria, sorted by the distance from a given location
	/// </summary>
	class TargetItemTracker
	{
		EnvironmentObject m_env;

		SortedDictionary<IntPoint3, List<ItemObject>> m_heap;

		Func<ItemObject, bool> m_filter;

		public bool IsEnabled { get; private set; }

		public TargetItemTracker(EnvironmentObject env, IntPoint3 center, Func<ItemObject, bool> filter)
		{
			m_env = env;
			m_filter = filter;
			m_heap = new SortedDictionary<IntPoint3, List<ItemObject>>(new LocationComparer(center));
		}

		public void Enable()
		{
			if (this.IsEnabled)
				throw new Exception();

			Debug.Print("ItemObjectView: Enable");

			ScanAll();

			m_env.ObjectAdded += Environment_ObjectAdded;
			m_env.ObjectRemoved += Environment_ObjectRemoved;
			m_env.ObjectMoved += Environment_ObjectMoved;

			this.IsEnabled = true;
		}

		public void Disable()
		{
			if (!this.IsEnabled)
				throw new Exception();

			Debug.Print("ItemObjectView: Disable");

			m_env.ObjectAdded -= Environment_ObjectAdded;
			m_env.ObjectRemoved -= Environment_ObjectRemoved;
			m_env.ObjectMoved -= Environment_ObjectMoved;

			m_heap.Clear();

			this.IsEnabled = false;
		}

		public void Refresh()
		{
			m_heap.Clear();
			ScanAll();
		}

		void ScanAll()
		{
			Debug.Print("ItemObjectView: ScanAll");

			foreach (var item in m_env.Inventory.OfType<ItemObject>())
			{
				if (m_filter(item) == false)
					continue;

				Add(item);
			}
		}

		public void Update(ItemObject item)
		{
			if (item.Parent != m_env)
				return;

			RemoveIfExists(item, item.Location);

			if (m_filter(item) == false)
				return;

			Add(item);
		}

		void RemoveIfExists(ItemObject item, IntPoint3 pos)
		{
			List<ItemObject> l;

			if (m_heap.TryGetValue(pos, out l) == false)
				return;

			l.Remove(item);

			if (l.Count == 0)
				m_heap.Remove(pos);
		}

		void Remove(ItemObject item, IntPoint3 pos)
		{
			List<ItemObject> l = m_heap[pos];
			var ok = l.Remove(item);
			Debug.Assert(ok);

			if (l.Count == 0)
				m_heap.Remove(pos);
		}

		void Add(ItemObject item)
		{
			Debug.Assert(item.Parent == m_env);

			List<ItemObject> l;

			if (m_heap.TryGetValue(item.Location, out l) == false)
			{
				l = new List<ItemObject>();
				m_heap[item.Location] = l;
			}

			l.Add(item);
		}

		void Environment_ObjectMoved(MovableObject obj, IntPoint3 oldPos)
		{
			var item = obj as ItemObject;

			if (item != null)
			{
				// If the item doesn't pass the filter, it cannot be in our list.
				if (m_filter(item) == false)
				{
					Debug.Assert(GetEnumerable().Contains(item) == false);
					return;
				}

				Remove(item, oldPos);

				Add(item);
			}
		}

		void Environment_ObjectRemoved(MovableObject obj)
		{
			var item = obj as ItemObject;

			if (item != null)
			{
				// If the item doesn't pass the filter, it cannot be in our list. But is it faster just to remove?
				if (m_filter(item) == false)
				{
					Debug.Assert(GetEnumerable().Contains(item) == false);
					return;
				}

				Remove(item, item.Location);
			}
		}

		void Environment_ObjectAdded(MovableObject obj)
		{
			var item = obj as ItemObject;

			if (item != null)
			{
				if (m_filter(item) == false)
					return;

				Add(item);
			}
		}

		public ItemObject GetFirst()
		{
			return m_heap.SelectMany(kvp => kvp.Value).FirstOrDefault();
		}

		public IEnumerable<ItemObject> GetEnumerable()
		{
			return m_heap.SelectMany(kvp => kvp.Value);
		}

		class LocationComparer : IComparer<IntPoint3>
		{
			IntPoint3 m_center;

			public LocationComparer(IntPoint3 center)
			{
				m_center = center;
			}

			#region IComparer<Obu> Members

			public int Compare(IntPoint3 x, IntPoint3 y)
			{
				var d1 = x - m_center;
				var d2 = y - m_center;

				var l1 = d1.ManhattanLength;
				var l2 = d2.ManhattanLength;

				return l1 - l2;
			}

			#endregion
		}
	}
}
