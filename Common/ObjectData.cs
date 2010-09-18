using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public abstract class BaseGameObjectData
	{
		public ObjectID ObjectID { get; set; }
	}

	[Serializable]
	public abstract class GameObjectData : BaseGameObjectData
	{
		public IntPoint3D Location { get; set; }
		public ObjectID Environment { get; set; }

		public Tuple<PropertyID, object>[] Properties { get; set; }
	}

	/* Item in inventory or floor */
	[Serializable]
	public class ItemData : GameObjectData
	{
		public override string ToString()
		{
			return String.Format("ItemData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class LivingData : GameObjectData
	{
		public override string ToString()
		{
			return String.Format("LivingData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public class BuildingData : BaseGameObjectData
	{
		public BuildingID ID { get; set; }
		public ObjectID Environment { get; set; }
		public int Z { get; set; }
		public IntRect Area { get; set; }

		public override string ToString()
		{
			return String.Format("BuildingData {0}", this.ObjectID);
		}
	}
}
