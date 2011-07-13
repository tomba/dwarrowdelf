using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class Living
	{
		class ActionData
		{
			public Func<Living, GameAction, bool> ActionHandler;
			public Func<Living, GameAction, int> TickInitializer;
		}

		static Dictionary<Type, ActionData> s_actionMethodMap;

		static Living()
		{
			var actionTypes = Helpers.GetNonabstractSubclasses(typeof(GameAction));

			s_actionMethodMap = new Dictionary<Type, ActionData>(actionTypes.Count());

			foreach (var type in actionTypes)
			{
				var actionHandler = WrapperGenerator.CreateFuncWrapper<Living, GameAction, bool>("PerformAction", type);
				if (actionHandler == null)
					throw new Exception();

				var tickInitializer = WrapperGenerator.CreateFuncWrapper<Living, GameAction, int>("InitializeAction", type);
				if (tickInitializer == null)
					throw new Exception();

				s_actionMethodMap[type] = new ActionData()
				{
					ActionHandler = actionHandler,
					TickInitializer = tickInitializer,
				};
			}
		}


		int InitializeAction(GameAction action)
		{
			var method = s_actionMethodMap[action.GetType()].TickInitializer;
			return method(this, action);
		}

		bool PerformAction(GameAction action)
		{
			var method = s_actionMethodMap[action.GetType()].ActionHandler;
			return method(this, action);
		}



		int InitializeAction(GetAction action)
		{
			return 1;
		}

		bool PerformAction(GetAction action)
		{
			if (this.Environment == null)
				return false;

			if (this.ActionTicksLeft > 0)
				return true;

			var list = this.Environment.GetContents(this.Location);

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var item = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (item == null)
				{
					Trace.TraceWarning("{0} tried to pick up {1}, but it's not there", this, itemID);
					return false;
				}

				if (item.MoveTo(this) == false)
				{
					Trace.TraceWarning("{0} tried to pick up {1}, but it doesn't move", this, itemID);
					return false;
				}
			}

			return true;
		}

		int InitializeAction(DropAction action)
		{
			return 1;
		}

		bool PerformAction(DropAction action)
		{
			if (this.Environment == null)
				return false;

			if (this.ActionTicksLeft > 0)
				return true;

			var list = this.Inventory;

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var ob = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (ob == null)
				{
					Trace.TraceWarning("{0} tried to drop {1}, but it's not in inventory", this, itemID);
					return false;
				}

				if (ob.MoveTo(this.Environment, this.Location) == false)
				{
					Trace.TraceWarning("{0} tried to drop {1}, but it doesn't move", this, itemID);
					return false;
				}
			}

			return true;
		}

		int InitializeAction(ConsumeAction action)
		{
			return 6;
		}

		bool PerformAction(ConsumeAction action)
		{
			if (this.ActionTicksLeft > 0)
				return true;

			var ob = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemObjectID);
			var item = ob as ItemObject;

			if (item == null)
			{
				Trace.TraceWarning("{0} tried to eat {1}, but it't not in inventory", this, action.ItemObjectID);
				return false;
			}

			var refreshment = item.RefreshmentValue;
			var nutrition = item.NutritionalValue;

			if (refreshment == 0 && nutrition == 0)
			{
				return false;
			}

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			return true;
		}

		int InitializeAction(MoveAction action)
		{
			return 1;
		}

		bool PerformAction(MoveAction action)
		{
			// this should check if movement is blocked, even when TicksLeft > 0
			if (this.ActionTicksLeft == 0)
				return MoveDir(action.Direction);
			else
				return true;
		}

		int InitializeAction(MineAction action)
		{
			var skill = GetSkillLevel(SkillID.Mining);
			return 10 / (skill / 26 + 1);
		}

		bool PerformAction(MineAction action)
		{
			var env = this.Environment;

			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var terrain = env.GetTerrain(p);
			var id = terrain.ID;

			if (!terrain.IsMinable)
			{
				Trace.TraceWarning("{0} tried to mine {1}, but it's not minable", this, p);
				return false;
			}

			if (this.ActionTicksLeft > 0)
				return true;

			switch (action.MineActionType)
			{
				case MineActionType.Mine:
					if (!action.Direction.IsPlanar() && action.Direction != Direction.Up)
					{
						Trace.TraceWarning("Mine: not Planar or Up direction");
						return false;
					}

					if (!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
					{
						Trace.TraceWarning("Mine: unable to move to {0}", action.Direction);
						return false;
					}

					ItemID itemID = ItemID.Undefined;
					MaterialInfo material = null;

					if (id == TerrainID.NaturalWall && this.World.Random.Next(21) >= GetSkillLevel(SkillID.Mining) / 25 + 10)
					{
						if (env.GetInteriorID(p) == InteriorID.Ore)
						{
							material = env.GetInteriorMaterial(p);
						}
						else
						{
							material = env.GetTerrainMaterial(p);
						}

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
					}

					env.SetTerrain(p, TerrainID.NaturalFloor, env.GetTerrainMaterialID(p));
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);

					if (itemID != ItemID.Undefined)
					{
						var builder = new ItemObjectBuilder(itemID, material.ID);
						var item = builder.Create(this.World);

						var ok = item.MoveTo(this.Environment, p);
						if (!ok)
							throw new Exception();
					}

					break;

				case MineActionType.Stairs:
					if (!action.Direction.IsPlanarUpDown())
					{
						Trace.TraceWarning("MineStairs: not PlanarUpDown direction");
						return false;
					}

					// We can always create stairs down, but for other dirs we need access there
					if (action.Direction != Direction.Down &&
						!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
					{
						Trace.TraceWarning("MineStairs: unable to move to {0}", action.Direction);
						return false;
					}

					env.SetTerrain(p, TerrainID.NaturalFloor, env.GetTerrainMaterialID(p));

					if (id == TerrainID.NaturalWall)
					{
						env.SetInterior(p, InteriorID.Stairs, env.GetTerrainMaterialID(p));

						if (env.GetTerrainID(p + Direction.Up) == TerrainID.NaturalFloor)
							env.SetTerrain(p + Direction.Up, TerrainID.Hole, env.GetTerrainMaterialID(p + Direction.Up));
					}
					else
					{
						env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					}

					break;

				default:
					throw new Exception();
			}

			return true;
		}

		int InitializeAction(FellTreeAction action)
		{
			return 5;
		}

		bool PerformAction(FellTreeAction action)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id != InteriorID.Tree && id != InteriorID.Sapling)
			{
				Trace.TraceWarning("{0} tried to fell tree {1}, but it't a tree", this, p);
				return false;
			}

			if (this.ActionTicksLeft == 0)
			{
				if (id == InteriorID.Tree)
				{
					var material = this.Environment.GetInteriorMaterialID(p);
					this.Environment.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					var builder = new ItemObjectBuilder(ItemID.Log, material)
					{
						Name = "Log",
						Color = GameColor.SaddleBrown,
					};
					var log = builder.Create(this.World);
					var ok = log.MoveTo(this.Environment, p);
					Debug.Assert(ok);
				}
				else
				{
					this.Environment.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				}
			}

			return true;
		}

		int InitializeAction(WaitAction action)
		{
			return action.WaitTicks;
		}

		bool PerformAction(WaitAction action)
		{
			return true;
		}

		int InitializeAction(BuildItemAction action)
		{
			return 8;
		}

		bool PerformAction(BuildItemAction action)
		{
			var building = this.Environment.GetBuildingAt(this.Location);

			if (building == null)
				return false;

			if (this.ActionTicksLeft != 0)
			{
				return building.VerifyBuildItem(this, action.SourceObjectIDs, action.DstItemID);
			}
			else
			{
				return building.PerformBuildItem(this, action.SourceObjectIDs, action.DstItemID);
			}
		}

		int InitializeAction(AttackAction action)
		{
			return 1;
		}

		Random m_random = new Random();

		bool PerformAction(AttackAction action)
		{
			if (this.ActionTicksLeft != 0)
				return true;

			var attacker = this;
			var attackee = this.World.FindObject<Living>(action.Target);

			if (attackee == null)
			{
				Trace.TraceWarning("{0} tried to attack {1}, but it doesn't exist", attacker, action.Target);
				return false;
			}

			if (!attacker.Location.IsAdjacentTo(attackee.Location, DirectionSet.Planar))
			{
				Trace.TraceWarning("{0} tried to attack {1}, but it wasn't near", attacker, attackee);
				return false;
			}

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
			}
			else
			{
				var damage = m_random.Next(3) + 1;
				Trace.TraceInformation("{0} hits {1}, {2} damage", attacker, attackee, damage);

				attackee.ReceiveDamage(damage);
			}

			return true;
		}
	}
}
