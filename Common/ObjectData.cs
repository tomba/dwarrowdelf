using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public sealed class WorldData
	{
		public int Tick { get; set; }
		public int Year { get; set; }
		public GameSeason Season { get; set; }
		public LivingVisionMode LivingVisionMode { get; set; }
		public GameMode GameMode { get; set; }
	}

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
		public IntSize3 Size { get; set; }
	}

	[Serializable]
	public abstract class MovableObjectData : GameObjectData
	{
		public IntPoint3 Location { get; set; }
		public ObjectID Parent { get; set; }
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

		//public Tuple<ArmorSlot, ObjectID>[] ArmorSlots { get; set; }

		//public ObjectID WeaponID { get; set; }

		public override string ToString()
		{
			return String.Format("LivingData {0}", this.ObjectID);
		}
	}
}
