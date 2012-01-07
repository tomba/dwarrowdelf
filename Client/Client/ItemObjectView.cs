using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class ItemObjectView
	{
		EnvironmentObject m_env;

		SortedDictionary<IntPoint3D, List<ItemObject>> m_heap;

		Func<ItemObject, bool> m_filter;

		public ItemObjectView(EnvironmentObject env, IntPoint3D origin)
		{
			m_env = env;

			m_heap = new SortedDictionary<IntPoint3D, List<ItemObject>>(new LocationComparer(origin));
			m_filter = o => true;

			Reset();

			{
				foreach (var kvp in m_heap)
					foreach (var o in kvp.Value)
						Debug.Print("{0}  {1}", o, o.Location);

				Debug.Print("");
			}

			{
				foreach (var o in Get())
					Debug.Print("{0}  {1}", o, o.Location);
			}

			env.ObjectAdded += new Action<MovableObject>(env_ObjectAdded);
			env.ObjectRemoved += new Action<MovableObject>(env_ObjectRemoved);
			env.ObjectMoved += new Action<MovableObject, IntPoint3D>(env_ObjectMoved);
		}

		public void Stop()
		{
			m_env.ObjectAdded -= new Action<MovableObject>(env_ObjectAdded);
			m_env.ObjectRemoved -= new Action<MovableObject>(env_ObjectRemoved);
			m_env.ObjectMoved -= new Action<MovableObject, IntPoint3D>(env_ObjectMoved);
		}

		public void SetFilter(Func<ItemObject, bool> filter)
		{
			m_filter = filter;
			Reset();
		}

		void Reset()
		{
			m_heap.Clear();

			foreach (var item in m_env.GetContents().OfType<ItemObject>())
			{
				if (m_filter(item) == false)
					return;

				Add(item);
			}
		}

		void Remove(ItemObject item, IntPoint3D pos)
		{
			List<ItemObject> l = m_heap[pos];
			var ok = l.Remove(item);
			Debug.Assert(ok);
		}

		void Add(ItemObject item)
		{
			List<ItemObject> l;

			if (m_heap.TryGetValue(item.Location, out l) == false)
			{
				l = new List<ItemObject>();
				m_heap[item.Location] = l;
			}

			l.Add(item);
		}

		void env_ObjectMoved(MovableObject obj, IntPoint3D oldPos)
		{
			var item = obj as ItemObject;

			if (item != null)
			{
				if (m_filter(item) == false)
					return;

				Remove(item, oldPos);

				Add(item);
			}
		}

		void env_ObjectRemoved(MovableObject obj)
		{
			var item = obj as ItemObject;

			if (item != null)
			{
				if (m_filter(item) == false)
					return;

				Remove(item, item.Location);
			}
		}

		void env_ObjectAdded(MovableObject obj)
		{
			var item = obj as ItemObject;

			if (item != null)
			{
				if (m_filter(item) == false)
					return;

				Add(item);
			}
		}

		public IEnumerable<ItemObject> Get()
		{
			return m_heap.SelectMany(kvp => kvp.Value);
		}

		class LocationComparer : IComparer<IntPoint3D>
		{
			IntPoint3D m_origin;

			public LocationComparer(IntPoint3D origin)
			{
				m_origin = origin;
			}

			#region IComparer<Obu> Members

			public int Compare(IntPoint3D x, IntPoint3D y)
			{
				var d1 = x - m_origin;
				var d2 = y - m_origin;

				var l1 = d1.ManhattanLength;
				var l2 = d2.ManhattanLength;

				return l1 - l2;
			}

			#endregion
		}
	}
}
