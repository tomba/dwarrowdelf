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
	public sealed class MineActionReport : ActionReport
	{
		public Direction Direction { get; private set; }
		public MineActionType MineActionType { get; private set; }
		public TerrainID TerrainID { get; private set; }
		public MaterialID MaterialID { get; set; }

		public MineActionReport(ILivingObject living, Direction direction, MineActionType mineActionType, TerrainID terrainID)
			: base(living)
		{
			this.Direction = direction;
			this.MineActionType = mineActionType;
			this.TerrainID = terrainID;
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
		public ItemID ItemID { get; private set; }
		public ObjectID ItemObjectID { get; set; }

		public BuildItemActionReport(ILivingObject living, ItemID itemID)
			: base(living)
		{
			this.ItemID = itemID;
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
	public sealed class GetActionReport : ItemActionReport
	{
		public GetActionReport(ILivingObject living, IItemObject item)
			: base(living, item)
		{
		}
	}

	[Serializable]
	public sealed class DropActionReport : ItemActionReport
	{
		public DropActionReport(ILivingObject living, IItemObject item)
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
}
