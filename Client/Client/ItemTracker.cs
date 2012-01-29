using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class ItemTracker
	{
		List<ItemObject> m_items;
		EnvironmentObject m_env;

		public ItemTracker(EnvironmentObject env)
		{
			m_env = env;

			m_items = new List<ItemObject>(1024);

			m_items.AddRange(env.GetContents().OfType<ItemObject>());

			m_env.ObjectAdded += Environment_ObjectAdded;
			m_env.ObjectRemoved += Environment_ObjectRemoved;
			m_env.ObjectMoved += Environment_ObjectMoved;


		}

		public ItemObject FindNearItem(IntPoint3 location, IItemFilter filter)
		{
			var items = m_items
				.Where(i => i.IsReserved == false && i.IsInstalled == false && filter.Match(i))
				.OrderBy(i => (i.Location - location).ManhattanLength);

			foreach (var item in items)
			{
				var found = AStar.AStarFinder.CanReach(m_env, location, item.Location, DirectionSet.Exact);

				if (found)
					return item;
			}

			return null;
		}


		void Environment_ObjectAdded(MovableObject obj)
		{
			var item = obj as ItemObject;

			if (item == null)
				return;

			Debug.Assert(m_items.Contains(item) == false);

			m_items.Add(item);
		}

		void Environment_ObjectRemoved(MovableObject obj)
		{
			var item = obj as ItemObject;

			if (item == null)
				return;

			Debug.Assert(m_items.Contains(item) == true);

			m_items.Remove(item);
		}

		void Environment_ObjectMoved(MovableObject obj, IntPoint3 oldPos)
		{
			var item = obj as ItemObject;

			if (item == null)
				return;

			// nop for now
		}

	}
}
