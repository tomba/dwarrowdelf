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
		uint[] m_objectIDcounterArray;

		public EnvironmentObject HackGetFirstEnv()
		{
			return m_objectMap.Values.OfType<EnvironmentObject>().First();
		}

		internal void AddGameObject(BaseObject ob)
		{
			VerifyAccess();

			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			m_objectMap.Add(ob.ObjectID, ob);
		}

		internal void RemoveGameObject(BaseObject ob)
		{
			VerifyAccess();

			if (ob.ObjectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			if (m_objectMap.Remove(ob.ObjectID) == false)
				throw new Exception();
		}

		public BaseObject FindObject(ObjectID objectID)
		{
			VerifyAccess();

			if (objectID == ObjectID.NullObjectID)
				throw new ArgumentException("Null ObjectID");

			BaseObject ob;
			return m_objectMap.TryGetValue(objectID, out ob) ? ob : null;
		}

		public T FindObject<T>(ObjectID objectID) where T : BaseObject
		{
			VerifyAccess();

			var ob = FindObject(objectID);

			if (ob == null)
				return null;

			return (T)ob;
		}

		public BaseObject GetObject(ObjectID objectID)
		{
			VerifyAccess();

			var ob = FindObject(objectID);

			if (ob == null)
				throw new Exception();

			return ob;
		}

		public T GetObject<T>(ObjectID objectID) where T : BaseObject
		{
			VerifyAccess();

			return (T)GetObject(objectID);
		}

		internal ObjectID GetNewObjectID(ObjectType objectType)
		{
			VerifyAccess();

			// XXX overflows
			//return new ObjectID(objectType, Interlocked.Increment(ref m_objectIDcounterArray[(int)objectType]));
			// XXX use a common counter to make debugging simpler
			// XXX check wrapping
			uint id = m_objectIDcounterArray[0]++;
			return new ObjectID(objectType, id);
		}
	}
}
