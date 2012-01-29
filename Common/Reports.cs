using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	[Serializable]
	public abstract class GameReport
	{
	}

	[Serializable]
	public abstract class LivingReport : GameReport
	{
		[NonSerialized]
		ILivingObject m_living;
		public ILivingObject Living { get { return m_living; } }
		public ObjectID LivingObjectID { get; private set; }

		protected LivingReport(ILivingObject living)
		{
			m_living = living;
			this.LivingObjectID = living.ObjectID;
		}
	}

	[Serializable]
	public sealed class DeathReport : LivingReport
	{
		public DeathReport(ILivingObject living)
			: base(living)
		{
		}
	}

	[Serializable]
	public abstract class ActionReport : LivingReport
	{
		public string FailReason { get; private set; }
		public bool Success { get { return this.FailReason == null; } }

		protected ActionReport(ILivingObject living)
			: base(living)
		{
		}

		public void SetFail(string str)
		{
			this.FailReason = str;
		}
	}

	[Serializable]
	public sealed class ConstructBuildingActionReport : ActionReport
	{
		public BuildingID BuildingID { get; private set; }

		public ConstructBuildingActionReport(ILivingObject living, BuildingID buildingID)
			: base(living)
		{
			this.BuildingID = buildingID;
		}
	}

	[Serializable]
	public sealed class DestructBuildingActionReport : ActionReport
	{
		public ObjectID BuildingObjectID { get; private set; }

		public DestructBuildingActionReport(ILivingObject living, IBuildingObject building)
			: base(living)
		{
			this.BuildingObjectID = building != null ? building.ObjectID : ObjectID.NullObjectID;
		}
	}

	[Serializable]
	public sealed class MoveActionReport : ActionReport
	{
		public Direction Direction { get; private set; }

		public MoveActionReport(ILivingObject living, Direction direction)
			: base(living)
		{
			this.Direction = direction;
		}
	}

	[Serializable]
	public sealed class HaulActionReport : ActionReport
	{
		public Direction Direction { get; private set; }
		public ObjectID ItemObjectID { get; private set; }

		public HaulActionReport(ILivingObject living, Direction direction, IItemObject item)
			: base(living)
		{
			this.Direction = direction;
			this.ItemObjectID = item != null ? item.ObjectID : ObjectID.NullObjectID;
		}
	}

	[Serializable]
	public sealed class MineActionReport : ActionReport
	{
		public IntPoint3 Location { get; private set; }
		public Direction Direction { get; private set; }
		public MineActionType MineActionType { get; private set; }

		public MineActionReport(ILivingObject living, IntPoint3 location, Direction direction, MineActionType mineActionType)
			: base(living)
		{
			this.Location = location;
			this.Direction = direction;
			this.MineActionType = mineActionType;
		}
	}

	[Serializable]
	public sealed class FellTreeActionReport : ActionReport
	{
		public Direction Direction { get; private set; }
		public InteriorID InteriorID { get; set; }
		public MaterialID MaterialID { get; set; }

		public FellTreeActionReport(ILivingObject living, Direction direction)
			: base(living)
		{
			this.Direction = direction;
		}
	}

	[Serializable]
	public sealed class BuildItemActionReport : ActionReport
	{
		public string BuildableItemKey { get; private set; }
		public ObjectID ItemObjectID { get; set; }

		public BuildItemActionReport(ILivingObject living, string buildableItemKey)
			: base(living)
		{
			this.BuildableItemKey = buildableItemKey;
		}
	}


	[Serializable]
	public sealed class ConstructActionReport : ActionReport
	{
		public ConstructMode Mode { get; private set; }

		public ConstructActionReport(ILivingObject living, ConstructMode mode)
			: base(living)
		{
			this.Mode = mode;
		}
	}

	public enum DamageCategory
	{
		None,
		Melee,
	}

	[Serializable]
	public sealed class AttackActionReport : ActionReport
	{
		public ObjectID TargetObjectID { get; private set; }

		public DamageCategory DamageCategory { get; set; }
		public int Damage { get; set; }

		public bool IsHit { get; set; }

		public AttackActionReport(ILivingObject living, ILivingObject target)
			: base(living)
		{
			this.TargetObjectID = target != null ? target.ObjectID : ObjectID.NullObjectID;
		}
	}

	[Serializable]
	public abstract class ItemActionReport : ActionReport
	{
		public ObjectID ItemObjectID { get; private set; }

		protected ItemActionReport(ILivingObject living, IItemObject item)
			: base(living)
		{
			this.ItemObjectID = item != null ? item.ObjectID : ObjectID.NullObjectID;
		}
	}

	[Serializable]
	public sealed class WearArmorActionReport : ItemActionReport
	{
		public WearArmorActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class RemoveArmorActionReport : ItemActionReport
	{
		public RemoveArmorActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class WieldWeaponActionReport : ItemActionReport
	{
		public WieldWeaponActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class RemoveWeaponActionReport : ItemActionReport
	{
		public RemoveWeaponActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class GetItemActionReport : ItemActionReport
	{
		public GetItemActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class DropItemActionReport : ItemActionReport
	{
		public DropItemActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class CarryItemActionReport : ItemActionReport
	{
		public CarryItemActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class ConsumeActionReport : ItemActionReport
	{
		public ConsumeActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class InstallItemActionReport : ItemActionReport
	{
		public InstallMode Mode { get; private set; }

		public InstallItemActionReport(ILivingObject living, IItemObject item, InstallMode mode)
			: base(living, item)
		{
			this.Mode = mode;
		}
	}
}
