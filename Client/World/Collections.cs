using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client
{
	public class IdentifiableCollection : ObservableKeyedCollection<ObjectID, IIdentifiable>
	{
		protected override ObjectID GetKeyForItem(IIdentifiable item)
		{
			return item.ObjectID;
		}
	}

	public class ReadOnlyIdentifiableCollection : ReadOnlyObservableKeyedCollection<ObjectID, IIdentifiable>
	{
		public ReadOnlyIdentifiableCollection(IdentifiableCollection collection)
			: base(collection)
		{
		}
	}

	public class IdentifiableCollection<T> : ObservableKeyedCollection<ObjectID, T> where T : IIdentifiable
	{
		protected override ObjectID GetKeyForItem(T item)
		{
			return item.ObjectID;
		}
	}

	public class ReadOnlyIdentifiableCollection<T> : ReadOnlyObservableKeyedCollection<ObjectID, T> where T : IIdentifiable
	{
		public ReadOnlyIdentifiableCollection(IdentifiableCollection<T> collection)
			: base(collection)
		{
		}
	}

	public sealed class BaseGameObjectCollection : IdentifiableCollection<BaseObject> { }
	public sealed class ReadOnlyBaseGameObjectCollection : ReadOnlyIdentifiableCollection<BaseObject>
	{
		public ReadOnlyBaseGameObjectCollection(BaseGameObjectCollection collection)
			: base(collection)
		{
		}
	}

	public sealed class MovableObjectCollection : IdentifiableCollection<MovableObject> { }
	public sealed class ReadOnlyMovableObjectCollection : ReadOnlyIdentifiableCollection<MovableObject>
	{
		public ReadOnlyMovableObjectCollection(MovableObjectCollection collection)
			: base(collection)
		{
		}
	}

	public sealed class LivingCollection : IdentifiableCollection<LivingObject> { }
	public sealed class ReadOnlyLivingCollection : ReadOnlyIdentifiableCollection<LivingObject>
	{
		public ReadOnlyLivingCollection(LivingCollection collection)
			: base(collection)
		{
		}
	}

	public sealed class EnvironmentCollection : IdentifiableCollection<EnvironmentObject> { }
	public sealed class ReadOnlyEnvironmentCollection : ReadOnlyIdentifiableCollection<EnvironmentObject>
	{
		public ReadOnlyEnvironmentCollection(EnvironmentCollection collection)
			: base(collection)
		{
		}
	}
}
