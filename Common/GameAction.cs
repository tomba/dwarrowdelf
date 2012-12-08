using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	public enum ActionState
	{
		/// <summary>
		/// None
		/// </summary>
		None,

		/// <summary>
		/// Action progressing ok
		/// </summary>
		Ok,

		/// <summary>
		/// Action failed
		/// </summary>
		Fail,

		/// <summary>
		/// Action aborted by somebody
		/// </summary>
		Abort,

		/// <summary>
		/// Action done successfully
		/// </summary>
		Done,
	}

	public enum ActionPriority
	{
		Undefined = 0,
		Idle,
		User,
		High,
	}

	[Serializable]
	[SaveGameObjectByRef]
	public abstract class GameAction
	{
		[SaveGameProperty]
		public int MagicNumber { get; set; }

		protected GameAction()
		{
		}

		protected GameAction(SaveGameContext ctx)
		{
		}

		public sealed override string ToString()
		{
			return String.Format("{0} ({1})", GetType().Name, this.GetParams());
		}

		protected abstract string GetParams();
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class MoveAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		public MoveAction(Direction direction)
		{
			this.Direction = direction;
		}

		MoveAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class HaulAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		[SaveGameProperty]
		public ObjectID ItemID { get; private set; }

		public HaulAction(Direction direction, IItemObject item)
		{
			this.Direction = direction;
			this.ItemID = item.ObjectID;
		}

		HaulAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Format("{0}, {1}", this.Direction.ToString(), this.ItemID.ToString());
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class WaitAction : GameAction
	{
		[SaveGameProperty]
		public int WaitTicks { get; private set; }

		public WaitAction(int ticks)
		{
			this.WaitTicks = ticks;
		}

		WaitAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.WaitTicks.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class SleepAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID Bed { get; private set; }
		[SaveGameProperty]
		public int SleepTicks { get; private set; }

		public SleepAction(IItemObject bed, int ticks)
		{
			this.Bed = bed.ObjectID;
			this.SleepTicks = ticks;
		}

		SleepAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Format("bed: {0}, ticks {1}", this.Bed, this.SleepTicks);
		}
	}

	public enum MineActionType
	{
		None,
		/// <summary>
		/// Mine can be done for planar directions, and the roof of the mined tile stays,
		/// or up, if the current tile has stairs
		/// </summary>
		Mine,
		/// <summary>
		/// Stairs can be created for planar directions or down. To create stairs up, we need stairs already in the current tile.
		/// </summary>
		Stairs,
		/// <summary>
		/// Channel can be done in planar directions.
		/// Channel removes the floor of the selected tile, and the interior of the tile below.
		/// </summary>
		Channel,
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class MineAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }
		[SaveGameProperty]
		public MineActionType MineActionType { get; private set; }

		public MineAction(Direction dir, MineActionType mineActionType)
		{
			this.Direction = dir;
			this.MineActionType = mineActionType;
		}

		MineAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class FellTreeAction : GameAction
	{
		[SaveGameProperty]
		public Direction Direction { get; private set; }

		public FellTreeAction(Direction dir)
		{
			this.Direction = dir;
		}

		FellTreeAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Direction.ToString();
		}
	}


	public enum ConstructMode
	{
		None = 0,
		Wall,
		Floor,
		Pavement,
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class ConstructAction : GameAction
	{
		public ConstructMode Mode { get; private set; }
		public IntPoint3 Location { get; private set; }
		public ObjectID[] ItemObjectIDs { get; private set; }

		public ConstructAction(ConstructMode mode, IntPoint3 location, IEnumerable<IItemObject> items)
		{
			this.Mode = mode;
			this.Location = location;
			this.ItemObjectIDs = items.Select(i => i.ObjectID).ToArray();
		}

		ConstructAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Format("{0}", this.Mode);
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class BuildItemAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID[] SourceObjectIDs { get; private set; }
		[SaveGameProperty]
		public string BuildableItemKey { get; private set; }

		public BuildItemAction(string buildableItemKey, IEnumerable<IMovableObject> sourceItems)
		{
			this.SourceObjectIDs = sourceItems.Select(i => i.ObjectID).ToArray();
			this.BuildableItemKey = buildableItemKey;
		}

		BuildItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.SourceObjectIDs.Select(i => i.ToString()).ToArray());
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class AttackAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID Target { get; private set; }

		public AttackAction(ILivingObject target)
		{
			this.Target = target.ObjectID;
		}

		AttackAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.Target.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class ConstructBuildingAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID EnvironmentID { get; private set; }
		[SaveGameProperty]
		public IntGrid2Z Area { get; private set; }
		[SaveGameProperty]
		public BuildingID BuildingID { get; private set; }

		public ConstructBuildingAction(IEnvironmentObject env, IntGrid2Z area, BuildingID buildingID)
		{
			this.EnvironmentID = env.ObjectID;
			this.Area = area;
			this.BuildingID = buildingID;
		}

		ConstructBuildingAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return String.Join(", ", this.EnvironmentID.ToString(), this.Area.ToString(), this.BuildingID.ToString());
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class DestructBuildingAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID BuildingID { get; private set; }

		public DestructBuildingAction(ObjectID buildingID)
		{
			this.BuildingID = buildingID;
		}

		DestructBuildingAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.BuildingID.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public abstract class ItemAction : GameAction
	{
		[SaveGameProperty]
		public ObjectID ItemID { get; private set; }

		protected ItemAction(IItemObject item)
		{
			this.ItemID = item.ObjectID;
		}

		protected ItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}

		protected override string GetParams()
		{
			return this.ItemID.ToString();
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class DropItemAction : ItemAction
	{
		public DropItemAction(IItemObject item)
			: base(item)
		{
		}

		DropItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class GetItemAction : ItemAction
	{
		public GetItemAction(IItemObject item)
			: base(item)
		{
		}

		GetItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class CarryItemAction : ItemAction
	{
		public CarryItemAction(IItemObject item)
			: base(item)
		{
		}

		CarryItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class ConsumeAction : ItemAction
	{
		public ConsumeAction(IItemObject consumable)
			: base(consumable)
		{
		}

		ConsumeAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	public enum InstallMode
	{
		None = 0,
		Install,
		Uninstall,
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class InstallItemAction : ItemAction
	{
		[SaveGameProperty]
		public InstallMode Mode { get; private set; }

		public InstallItemAction(IItemObject item, InstallMode mode)
			: base(item)
		{
			this.Mode = mode;
		}

		InstallItemAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class WearArmorAction : ItemAction
	{
		public WearArmorAction(IItemObject item)
			: base(item)
		{
		}

		WearArmorAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class RemoveArmorAction : ItemAction
	{
		public RemoveArmorAction(IItemObject item)
			: base(item)
		{
		}

		RemoveArmorAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class WieldWeaponAction : ItemAction
	{
		public WieldWeaponAction(IItemObject item)
			: base(item)
		{
		}

		WieldWeaponAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class RemoveWeaponAction : ItemAction
	{
		public RemoveWeaponAction(IItemObject item)
			: base(item)
		{
		}

		RemoveWeaponAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}
}
