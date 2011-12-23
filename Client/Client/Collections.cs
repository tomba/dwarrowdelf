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

	sealed class BaseGameObjectCollection : IdentifiableCollection<BaseObject> { }
	sealed class ReadOnlyBaseGameObjectCollection : ReadOnlyIdentifiableCollection<BaseObject>
	{
		public ReadOnlyBaseGameObjectCollection(BaseGameObjectCollection collection)
			: base(collection)
		{
		}
	}

	sealed class MovableObjectCollection : IdentifiableCollection<MovableObject> { }
	sealed class ReadOnlyMovableObjectCollection : ReadOnlyIdentifiableCollection<MovableObject>
	{
		public ReadOnlyMovableObjectCollection(MovableObjectCollection collection)
			: base(collection)
		{
		}
	}

	sealed class LivingCollection : IdentifiableCollection<LivingObject> { }
	sealed class ReadOnlyLivingCollection : ReadOnlyIdentifiableCollection<LivingObject>
	{
		public ReadOnlyLivingCollection(LivingCollection collection)
			: base(collection)
		{
		}
	}

	sealed class EnvironmentCollection : IdentifiableCollection<EnvironmentObject> { }
	sealed class ReadOnlyEnvironmentCollection : ReadOnlyIdentifiableCollection<EnvironmentObject>
	{
		public ReadOnlyEnvironmentCollection(EnvironmentCollection collection)
			: base(collection)
		{
		}
	}

	sealed class BuildingCollection : ObservableKeyedCollection<ObjectID, BuildingObject>
	{
		protected override ObjectID GetKeyForItem(BuildingObject building)
		{
			return building.ObjectID;
		}
	}

	sealed class ReadOnlyBuildingCollection : ReadOnlyObservableKeyedCollection<ObjectID, BuildingObject>
	{
		public ReadOnlyBuildingCollection(BuildingCollection collection)
			: base(collection)
		{
		}
	}
}
