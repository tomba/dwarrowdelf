using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// Tracks all items in an env.
	/// This could/should keep different item categories in different lists, and
	/// use http://blogs.msdn.com/b/devdev/archive/2007/06/07/k-nearest-neighbor-spatial-search.aspx
	/// to speed up the search by distance.
	/// </summary>
	public class ItemTracker
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

		public IEnumerable<ItemObject> GetItemsByDistance(IntPoint3 location, Func<ItemObject, bool> filter)
		{
			var items = m_items
				.Where(filter)
				.OrderBy(i => (i.Location - location).ManhattanLength);

			return items;
		}

		public IEnumerable<ItemObject> GetItemsByDistance(IntPoint3 location, ItemCategory category, Func<ItemObject, bool> filter)
		{
			var items = m_items
				.Where(i => i.ItemCategory == category)
				.Where(filter)
				.OrderBy(i => (i.Location - location).ManhattanLength);

			return items;
		}

		public ItemObject GetReachableItemByDistance(IntPoint3 location, IItemFilter filter,
			Unreachables unreachables)
		{
			var items = m_items
				.Where(i => i.IsReserved == false && i.IsInstalled == false && unreachables.IsUnreachable(i.Location) == false)
				.Where(i => filter.Match(i))
				.OrderBy(i => (i.Location - location).ManhattanLength);

			foreach (var item in items)
			{
				var found = AStar.AStarFinder.CanReach(m_env, location, item.Location, DirectionSet.Exact);

				if (found)
					return item;

				unreachables.Add(item.Location);
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

	public class Unreachables
	{
		// location : expiration tick
		Dictionary<IntPoint3, int> m_map = new Dictionary<IntPoint3, int>();

		World m_world;

		public Unreachables(World world)
		{
			m_world = world;
		}

		public void Add(IntPoint3 p)
		{
			m_map[p] = m_world.TickNumber + 25;
		}

		public bool IsUnreachable(IntPoint3 p)
		{
			int tick;

			if (m_map.TryGetValue(p, out tick) == false)
				return false;

			if (m_world.TickNumber >= tick)
			{
				m_map.Remove(p);
				return false;
			}

			return true;
		}
	}
}
