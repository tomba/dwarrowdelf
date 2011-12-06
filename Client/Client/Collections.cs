using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Dwarrowdelf.Client
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

	class BaseGameObjectCollection : IdentifiableCollection<BaseObject> { }
	class ReadOnlyBaseGameObjectCollection : ReadOnlyIdentifiableCollection<BaseObject>
	{
		public ReadOnlyBaseGameObjectCollection(BaseGameObjectCollection collection)
			: base(collection)
		{
		}
	}

	class GameObjectCollection : IdentifiableCollection<MovableObject> { }
	class ReadOnlyGameObjectCollection : ReadOnlyIdentifiableCollection<MovableObject>
	{
		public ReadOnlyGameObjectCollection(GameObjectCollection collection)
			: base(collection)
		{
		}
	}

	class LivingCollection : IdentifiableCollection<LivingObject> { }
	class ReadOnlyLivingCollection : ReadOnlyIdentifiableCollection<LivingObject>
	{
		public ReadOnlyLivingCollection(LivingCollection collection)
			: base(collection)
		{
		}
	}

	class EnvironmentCollection : IdentifiableCollection<EnvironmentObject> { }
	class ReadOnlyEnvironmentCollection : ReadOnlyIdentifiableCollection<EnvironmentObject>
	{
		public ReadOnlyEnvironmentCollection(EnvironmentCollection collection)
			: base(collection)
		{
		}
	}

	class BuildingCollection : ObservableKeyedCollection<ObjectID, BuildingObject>
	{
		protected override ObjectID GetKeyForItem(BuildingObject building)
		{
			return building.ObjectID;
		}
	}

	class ReadOnlyBuildingCollection : ReadOnlyObservableKeyedCollection<ObjectID, BuildingObject>
	{
		public ReadOnlyBuildingCollection(BuildingCollection collection)
			: base(collection)
		{
		}
	}
}
