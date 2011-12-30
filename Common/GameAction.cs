﻿using System;
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
		// XXX. Server gen creates negative numbers, client positive
		public static Func<int> MagicNumberGenerator;

		[SaveGameProperty]
		public int MagicNumber { get; private set; }

		protected GameAction()
		{
			this.MagicNumber = MagicNumberGenerator();
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

	public enum MineActionType
	{
		/// <summary>
		/// Mine can be done for planar directions, and the roof of the mined tile stays,
		/// or up, if the current tile has stairs
		/// </summary>
		Mine,
		/// <summary>
		/// Stairs can be created for planar directions or down. To create stairs up, we need stairs already in the current tile.
		/// </summary>
		Stairs,
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
		public IntRectZ Area { get; private set; }
		[SaveGameProperty]
		public BuildingID BuildingID { get; private set; }

		public ConstructBuildingAction(IEnvironmentObject env, IntRectZ area, BuildingID buildingID)
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
	public sealed class DropAction : ItemAction
	{
		public DropAction(IItemObject item)
			: base(item)
		{
		}

		DropAction(SaveGameContext ctx)
			: base(ctx)
		{
		}
	}

	[Serializable]
	[SaveGameObjectByRef]
	public sealed class GetAction : ItemAction
	{
		public GetAction(IItemObject item)
			: base(item)
		{
		}

		GetAction(SaveGameContext ctx)
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
