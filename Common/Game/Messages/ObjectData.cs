using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public sealed class WorldData
	{
		public int Tick;
		public int Year;
		public GameSeason Season;
		public LivingVisionMode LivingVisionMode;
		public GameMode GameMode;
	}

	[Serializable]
	public abstract class BaseGameObjectData
	{
		public ObjectID ObjectID;

		public DateTime CreationTime;
		public int CreationTick;

		public KeyValuePair<PropertyID, object>[] Properties;
	}

	[Serializable]
	public abstract class GameObjectData : BaseGameObjectData
	{
	}

	[Serializable]
	public sealed class EnvironmentObjectData : GameObjectData
	{
		public VisibilityMode VisibilityMode;
		public IntSize3 Size;
	}

	[Serializable]
	public abstract class MovableObjectData : GameObjectData
	{
		public IntPoint3 Location;
		public ObjectID Parent;
	}

	[Serializable]
	public sealed class ItemData : MovableObjectData
	{
		public ItemID ItemID;

		public override string ToString()
		{
			return String.Format("ItemData {0}", this.ObjectID);
		}
	}

	[Serializable]
	public sealed class LivingData : MovableObjectData
	{
		public LivingID LivingID;
		public GameAction CurrentAction;
		public ActionPriority ActionPriority;
		public int ActionTicksUsed;
		public int ActionTotalTicks;

		public KeyValuePair<SkillID, byte>[] Skills;

		public override string ToString()
		{
			return String.Format("LivingData {0}", this.ObjectID);
		}
	}
}
