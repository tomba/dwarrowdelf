using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace MyGame.Client
{
	class IdentifiableCollection : ObservableKeyedCollection<ObjectID, IIdentifiable>
	{
		protected override ObjectID GetKeyForItem(IIdentifiable item)
		{
			return item.ObjectID;
		}
	}

	class ReadOnlyIdentifiableCollection : ReadOnlyObservableKeyedCollection<ObjectID, IIdentifiable>
	{
		public ReadOnlyIdentifiableCollection(IdentifiableCollection collection)
			: base(collection)
		{
		}
	}

	class IdentifiableCollection<T> : ObservableKeyedCollection<ObjectID, T> where T : IIdentifiable
	{
		protected override ObjectID GetKeyForItem(T item)
		{
			return item.ObjectID;
		}
	}

	class ReadOnlyIdentifiableCollection<T> : ReadOnlyObservableKeyedCollection<ObjectID, T> where T : IIdentifiable
	{
		public ReadOnlyIdentifiableCollection(IdentifiableCollection<T> collection)
			: base(collection)
		{
		}
	}

	class ObjectCollection : IdentifiableCollection<ClientGameObject> { }
	class ReadOnlyObjectCollection : ReadOnlyIdentifiableCollection<ClientGameObject>
	{
		public ReadOnlyObjectCollection(ObjectCollection collection)
			: base(collection)
		{
		}
	}

	class LivingCollection : IdentifiableCollection<Living> { }
	class ReadOnlyLivingCollection : ReadOnlyIdentifiableCollection<Living>
	{
		public ReadOnlyLivingCollection(LivingCollection collection)
			: base(collection)
		{
		}
	}

	class EnvironmentCollection : IdentifiableCollection<Environment> { }
	class ReadOnlyEnvironmentCollection : ReadOnlyIdentifiableCollection<Environment>
	{
		public ReadOnlyEnvironmentCollection(EnvironmentCollection collection)
			: base(collection)
		{
		}
	}

	class ActionCollection : ObservableKeyedCollection<int, GameAction>
	{
		protected override int GetKeyForItem(GameAction item)
		{
			return item.TransactionID;
		}
	}
}
