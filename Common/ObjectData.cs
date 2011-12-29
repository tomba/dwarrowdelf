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

		public DateTime CreationTime { get; set; }
		public int CreationTick { get; set; }

		public Tuple<PropertyID, object>[] Properties { get; set; }
	}

	[Serializable]
	public abstract class GameObjectData : BaseGameObjectData
	{
	}

	[Serializable]
	public sealed class MapData : GameObjectData
	{
		public VisibilityMode VisibilityMode { get; set; }
		public IntCuboid Bounds { get; set; }
		public IntPoint3D HomeLocation { get; set; }
	}

	[Serializable]
	public abstract class MovableObjectData : GameObjectData
	{
		public IntPoint3D Location { get; set; }
		public ObjectID Environment { get; set; }
	}

	/* Item in inventory or floor */
	[Serializable]
	public sealed class ItemData : MovableObjectData
	{
		public ItemID ItemID { get; set; }

		public override string ToString()
		{
			return String.Format("ItemData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public sealed class LivingData : MovableObjectData
	{
		public LivingID LivingID { get; set; }
		public GameAction CurrentAction { get; set; }
		public ActionPriority ActionPriority { get; set; }
		public int ActionTicksUsed { get; set; }
		public int ActionTotalTicks { get; set; }
		public int ActionUserID { get; set; }

		public Tuple<SkillID, byte>[] Skills { get; set; }

		public Tuple<ArmorSlot, ObjectID>[] ArmorSlots { get; set; }

		public ObjectID WeaponID { get; set; }

		public override string ToString()
		{
			return String.Format("LivingData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public sealed class BuildingData : BaseGameObjectData
	{
		public BuildingID BuildingID { get; set; }
		public ObjectID Environment { get; set; }
		public IntRectZ Area { get; set; }

		public override string ToString()
		{
			return String.Format("BuildingData {0}", this.ObjectID);
		}
	}
}
