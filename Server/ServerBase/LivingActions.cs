using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class Living
	{
		void InitializeAction(GameAction action, out int ticks)
		{
			if (action is WaitAction)
			{
				ticks = ((WaitAction)action).WaitTicks;
			}
			else if (action is MineAction)
			{
				ticks = 3;
			}
			else if (action is FellTreeAction)
			{
				ticks = 5;
			}
			else if (action is MoveAction)
			{
				ticks = 1;
			}
			else if (action is BuildItemAction)
			{
				ticks = 8;
			}
			else if (action is ConsumeAction)
			{
				ticks = 6;
			}
			else
			{
				ticks = 1;
			}
		}


		void Perform(GameAction action, out bool success)
		{
			if (action is MoveAction)
			{
				PerformMove((MoveAction)action, out success);
			}
			else if (action is WaitAction)
			{
				PerformWait((WaitAction)action, out success);
			}
			else if (action is GetAction)
			{
				PerformGet((GetAction)action, out success);
			}
			else if (action is DropAction)
			{
				PerformDrop((DropAction)action, out success);
			}
			else if (action is ConsumeAction)
			{
				PerformConsume((ConsumeAction)action, out success);
			}
			else if (action is MineAction)
			{
				PerformMine((MineAction)action, out success);
			}
			else if (action is FellTreeAction)
			{
				PerformFellTree((FellTreeAction)action, out success);
			}
			else if (action is BuildItemAction)
			{
				PerformBuildItem((BuildItemAction)action, out success);
			}
			else if (action is AttackAction)
			{
				PerformAttack((AttackAction)action, out success);
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		void PerformGet(GetAction action, out bool success)
		{
			success = false;

			if (this.Environment == null)
				return;

			if (this.ActionTicksLeft > 0)
			{
				success = true;
				return;
			}

			var list = this.Environment.GetContents(this.Location);

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var item = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (item == null)
				{
					Trace.TraceWarning("{0} tried to pick up {1}, but it's not there", this, itemID);
					return;
				}

				if (item.MoveTo(this) == false)
				{
					Trace.TraceWarning("{0} tried to pick up {1}, but it doesn't move", this, itemID);
					return;
				}
			}

			success = true;
		}

		void PerformDrop(DropAction action, out bool success)
		{
			success = false;

			if (this.Environment == null)
				return;

			if (this.ActionTicksLeft > 0)
			{
				success = true;
				return;
			}

			var list = this.Inventory;

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var ob = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (ob == null)
				{
					Trace.TraceWarning("{0} tried to drop {1}, but it's not in inventory", this, itemID);
					return;
				}

				if (ob.MoveTo(this.Environment, this.Location) == false)
				{
					Trace.TraceWarning("{0} tried to drop {1}, but it doesn't move", this, itemID);
					return;
				}
			}

			success = true;
		}

		void PerformConsume(ConsumeAction action, out bool success)
		{
			success = false;

			if (this.ActionTicksLeft > 0)
			{
				success = true;
				return;
			}

			var ob = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemObjectID);
			var item = ob as ItemObject;

			if (item == null)
			{
				Trace.TraceWarning("{0} tried to eat {1}, but it't not in inventory", this, action.ItemObjectID);
				success = false;
				return;
			}

			var refreshment = item.RefreshmentValue;
			var nutrition = item.NutritionalValue;

			if (refreshment == 0 && nutrition == 0)
			{
				success = false;
				return;
			}

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			success = true;
		}

		void PerformMove(MoveAction action, out bool success)
		{
			// this should check if movement is blocked, even when TicksLeft > 0
			if (this.ActionTicksLeft == 0)
				success = MoveDir(action.Direction);
			else
				success = true;
		}

		void PerformMine(MineAction action, out bool success)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var inter = this.Environment.GetInterior(p);
			var id = inter.ID;

			if (!inter.IsMineable)
			{
				Trace.TraceWarning("{0} tried to mine {1}, but it't a wall", this, p);
				success = false;
				return;
			}

			success = true;

			if (this.ActionTicksLeft > 0)
				return;

			switch (action.MineActionType)
			{
				case MineActionType.Mine:
					if (!action.Direction.IsPlanar() && action.Direction != Direction.Up)
					{
						success = false;
						Trace.TraceWarning("Mine: not Planar or Up direction");
						return;
					}

					if (!EnvironmentHelpers.CanMoveFrom(this.Environment, this.Location, action.Direction))
					{
						success = false;
						Trace.TraceWarning("Mine: unable to move to {0}", action.Direction);
						return;
					}

					var material = this.Environment.GetInteriorMaterial(p);

					this.Environment.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);

					ItemID itemID;

					switch (material.MaterialClass)
					{
						case MaterialClass.Rock:
							itemID = ItemID.Rock;
							break;

						case MaterialClass.Mineral:
							itemID = ItemID.Ore;
							break;

						case MaterialClass.Gem:
							itemID = ItemID.UncutGem;
							break;

						default:
							throw new Exception();
					}

					var item = new ItemObject(itemID, material.ID);

					item.Initialize(this.World);

					var ok = item.MoveTo(this.Environment, p);
					if (!ok)
						throw new Exception();
					break;

				case MineActionType.Stairs:
					if (!action.Direction.IsPlanarUpDown())
					{
						success = false;
						Trace.TraceWarning("MineStairs: not PlanarUpDown direction");
						return;
					}

					// We can always create stairs down, but for other dirs we need access there
					if (action.Direction != Direction.Down &&
						!EnvironmentHelpers.CanMoveFrom(this.Environment, this.Location, action.Direction))
					{
						success = false;
						Trace.TraceWarning("MineStairs: unable to move to {0}", action.Direction);
						return;
					}

					this.Environment.SetInterior(p, InteriorID.Stairs, this.Environment.GetInteriorMaterialID(p));
					if (this.Environment.GetFloorID(p + Direction.Up) == FloorID.NaturalFloor)
						this.Environment.SetFloor(p + Direction.Up, FloorID.Hole, this.Environment.GetFloorMaterialID(p));
					break;

				default:
					throw new Exception();
			}
		}

		void PerformFellTree(FellTreeAction action, out bool success)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id == InteriorID.Tree)
			{
				if (this.ActionTicksLeft == 0)
				{
					var material = this.Environment.GetInteriorMaterialID(p);
					this.Environment.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					var log = new ItemObject(ItemID.Log, material)
					{
						Name = "Log",
						Color = GameColor.SaddleBrown,
					};
					log.Initialize(this.World);
					var ok = log.MoveTo(this.Environment, p);
					Debug.Assert(ok);
				}
				success = true;
			}
			else
			{
				Trace.TraceWarning("{0} tried to fell tree {1}, but it't a tree", this, p);
				success = false;
			}
		}

		void PerformWait(WaitAction action, out bool success)
		{
			success = true;
		}

		void PerformBuildItem(BuildItemAction action, out bool success)
		{
			var building = this.Environment.GetBuildingAt(this.Location);

			if (building == null)
			{
				success = false;
				return;
			}

			if (this.ActionTicksLeft != 0)
			{
				success = building.VerifyBuildItem(this, action.SourceObjectIDs, action.DstItemID);
			}
			else
			{
				success = building.PerformBuildItem(this, action.SourceObjectIDs, action.DstItemID);
			}
		}

		Random m_random = new Random();

		void PerformAttack(AttackAction action, out bool success)
		{
			if (this.ActionTicksLeft != 0)
			{
				success = true;
				return;
			}

			var attacker = this;
			var attackee = this.World.FindObject<Living>(action.Target);

			if (attackee == null)
			{
				Trace.TraceWarning("{0} tried to attack {1}, but it doesn't exist", attacker, action.Target);
				success = false;
				return;
			}

			if (!attacker.Location.IsAdjacentTo(attackee.Location, DirectionSet.Planar))
			{
				Trace.TraceWarning("{0} tried to attack {1}, but it wasn't near", attacker, attackee);
				success = false;
				return;
			}

			success = true;

			var roll = m_random.Next(20) + 1;
			bool hit;

			if (roll == 1)
			{
				hit = false;
			}
			else if (roll == 20)
			{
				hit = true;
			}
			else
			{
				var ac = attackee.ArmorClass;
				hit = roll >= ac;
			}

			if (!hit)
			{
				Trace.TraceInformation("{0} misses {1}", attacker, attackee);
				return;
			}

			var damage = m_random.Next(3) + 1;
			Trace.TraceInformation("{0} hits {1}, {2} damage", attacker, attackee, damage);

			attackee.ReceiveDamage(damage);
		}
	}
}
