using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class World
	{
		[SaveGameProperty]
		Dictionary<ObjectID, BaseObject> m_objectMap;

		[SaveGameProperty]
		int[] m_objectIDcounterArray;

		public IEnumerable<BaseObject> AllObjects { get { return m_objectMap.Values.AsEnumerable(); } }

		internal void AddGameObject(BaseObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				m_objectMap.Add(ob.ObjectID, ob);
		}

		internal void RemoveGameObject(BaseObject ob)
		{
			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
				if (m_objectMap.Remove(ob.ObjectID) == false)
					throw new Exception();
		}

		public BaseObject FindObject(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			lock (m_objectMap)
			{
				BaseObject ob;
				return m_objectMap.TryGetValue(objectID, out ob) ? ob : null;
			}
		}

		public T FindObject<T>(ObjectID objectID) where T : BaseObject
		{
			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}

		public BaseObject GetObject(ObjectID objectID)
		{
			var ob = FindObject(objectID);

			if (ob == null)
				throw new Exception();

			return ob;
		}

		public T GetObject<T>(ObjectID objectID) where T : BaseObject
		{
			return (T)GetObject(objectID);
		}

		internal ObjectID GetNewObjectID(ObjectType objectType)
		{
			// XXX overflows
			//return new ObjectID(objectType, Interlocked.Increment(ref m_objectIDcounterArray[(int)objectType]));
			// XXX use a common counter to make debugging simpler
			// XXX check wrapping and int -> uint
			return new ObjectID(objectType, (uint)Interlocked.Increment(ref m_objectIDcounterArray[0]));
		}
	}
}
