using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class LivingObject
	{
		class ActionData
		{
			public Func<LivingObject, GameAction, bool> ActionHandler;
			public Func<LivingObject, GameAction, int> GetTotalTicks;
		}

		static Dictionary<Type, ActionData> s_actionMethodMap;

		static LivingObject()
		{
			var actionTypes = Helpers.GetNonabstractSubclasses(typeof(GameAction));

			s_actionMethodMap = new Dictionary<Type, ActionData>(actionTypes.Count());

			foreach (var type in actionTypes)
			{
				var actionHandler = WrapperGenerator.CreateFuncWrapper<LivingObject, GameAction, bool>("PerformAction", type);
				if (actionHandler == null)
					throw new Exception(String.Format("No PerformAction method found for {0}", type.Name));

				var tickInitializer = WrapperGenerator.CreateFuncWrapper<LivingObject, GameAction, int>("GetTotalTicks", type);
				if (tickInitializer == null)
					throw new Exception(String.Format("No GetTotalTicks method found for {0}", type.Name));

				s_actionMethodMap[type] = new ActionData()
				{
					ActionHandler = actionHandler,
					GetTotalTicks = tickInitializer,
				};
			}
		}


		int GetActionTotalTicks(GameAction action)
		{
			var method = s_actionMethodMap[action.GetType()].GetTotalTicks;
			return method(this, action);
		}

		bool PerformAction(GameAction action)
		{
			Debug.Assert(this.ActionTotalTicks <= this.ActionTicksUsed);

			var method = s_actionMethodMap[action.GetType()].ActionHandler;
			return method(this, action);
		}


		int GetTotalTicks(ConstructBuildingAction action)
		{
			return 10;
		}

		bool PerformAction(ConstructBuildingAction action)
		{
			var env = this.World.FindObject<EnvironmentObject>(action.EnvironmentID);
			if (env == null)
			{
				SetActionError("no env");
				return false;
			}

			if (!action.Area.Contains(this.Location))
			{
				SetActionError("living not at the building site");
				return false;
			}

			if (BuildingObject.VerifyBuildSite(env, action.Area) == false)
			{
				SetActionError("Build site not clean");
				return false;
			}

			var builder = new BuildingObjectBuilder(action.BuildingID, action.Area);
			var building = builder.Create(this.World, env);

			return true;
		}

		int GetTotalTicks(DestructBuildingAction action)
		{
			return 10;
		}

		bool PerformAction(DestructBuildingAction action)
		{
			var building = this.World.FindObject<BuildingObject>(action.BuildingID);
			if (building == null)
			{
				SetActionError("no building");
				return false;
			}

			if (!building.Area.Contains(this.Location))
			{
				SetActionError("living not at the building site");
				return false;
			}

			building.Destruct();

			return true;
		}

		int GetTotalTicks(GetAction action)
		{
			return 1;
		}

		bool PerformAction(GetAction action)
		{
			if (this.Environment == null)
			{
				SetActionError("no env");
				return false;
			}

			var list = this.Environment.GetContents(this.Location);

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
				{
					SetActionError("object cannot get itself");
					return false;
				}

				var item = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (item == null)
				{
					SetActionError("{0} tried to pick up {1}, but it's not there", this, itemID);
					return false;
				}

				if (item.MoveTo(this) == false)
				{
					SetActionError("{0} tried to pick up {1}, but it doesn't move", this, itemID);
					return false;
				}
			}

			return true;
		}

		int GetTotalTicks(DropAction action)
		{
			return 1;
		}

		bool PerformAction(DropAction action)
		{
			if (this.Environment == null)
			{
				SetActionError("no env");
				return false;
			}

			var list = this.Inventory;

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
				{
					SetActionError("object cannot drop itself");
					return false;
				}

				var ob = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (ob == null)
				{
					SetActionError("{0} tried to drop {1}, but it's not in inventory", this, itemID);
					return false;
				}

				if (ob.MoveTo(this.Environment, this.Location) == false)
				{
					SetActionError("{0} tried to drop {1}, but it doesn't move", this, itemID);
					return false;
				}
			}

			return true;
		}

		int GetTotalTicks(ConsumeAction action)
		{
			return 6;
		}

		bool PerformAction(ConsumeAction action)
		{
			var ob = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemObjectID);
			var item = ob as ItemObject;

			if (item == null)
			{
				SetActionError("{0} tried to eat {1}, but it't not in inventory", this, action.ItemObjectID);
				return false;
			}

			var refreshment = item.RefreshmentValue;
			var nutrition = item.NutritionalValue;

			if (refreshment == 0 && nutrition == 0)
			{
				SetActionError("cannot eat a non-digestible item");
				return false;
			}

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			return true;
		}

		int GetTotalTicks(MoveAction action)
		{
			var obs = this.Environment.GetContents(this.Location + action.Direction);
			return obs.OfType<LivingObject>().Count() + 1;
		}

		bool PerformAction(MoveAction action)
		{
			var ok = MoveDir(action.Direction);

			if (!ok)
			{
				SetActionError("could not move (blocked?)");
				var report = new MoveActionReport(this, action.Direction, "could not move (blocked?)");
				SetActionReport(report);
			}
			else
			{
				var report = new MoveActionReport(this, action.Direction);
				SetActionReport(report);
			}

			return ok;
		}

		int GetTotalTicks(MineAction action)
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
				SetActionError("{0} tried to mine {1}, but it's not minable", this, p);
				return false;
			}

			switch (action.MineActionType)
			{
				case MineActionType.Mine:
					{
						if (!action.Direction.IsPlanar() && action.Direction != Direction.Up)
						{
							SetActionError("Mine: not Planar or Up direction");
							return false;
						}

						// XXX is this necessary for planar dirs? we can always move in those dirs
						if (!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SetActionError("Mine: unable to move to {0}", action.Direction);
							return false;
						}

						ItemObject item = null;

						if (id == TerrainID.NaturalWall && this.World.Random.Next(21) >= GetSkillLevel(SkillID.Mining) / 25 + 10)
						{
							ItemID itemID;
							MaterialInfo material;

							if (env.GetInteriorID(p) == InteriorID.Ore)
							{
								material = env.GetInteriorMaterial(p);
							}
							else
							{
								material = env.GetTerrainMaterial(p);
							}

							switch (material.Category)
							{
								case MaterialCategory.Rock:
									itemID = ItemID.Rock;
									break;

								case MaterialCategory.Mineral:
									itemID = ItemID.Ore;
									break;

								case MaterialCategory.Gem:
									itemID = ItemID.UncutGem;
									break;

								default:
									throw new Exception();
							}

							var builder = new ItemObjectBuilder(itemID, material.ID);
							item = builder.Create(this.World);
						}

						var td = new TileData()
						{
							TerrainID = TerrainID.NaturalFloor,
							TerrainMaterialID = env.GetTerrainMaterialID(p),
							InteriorID = InteriorID.Empty,
							InteriorMaterialID = Dwarrowdelf.MaterialID.Undefined,
							Grass = false,
							WaterLevel = 0,
						};

						env.SetTileData(p, td);

						if (item != null)
						{
							var ok = item.MoveTo(this.Environment, p);
							if (!ok)
								throw new Exception();
						}
					}
					break;

				case MineActionType.Stairs:
					{
						if (!action.Direction.IsPlanarUpDown())
						{
							SetActionError("MineStairs: not PlanarUpDown direction");
							return false;
						}

						if (id != TerrainID.NaturalWall)
						{
							SetActionError("stairs can only be mined at a wall");
							return false;
						}

						// We can always create stairs down, but for other dirs we need access there
						// XXX ??? When we cannot move in planar dirs?
						if (action.Direction != Direction.Down &&
							!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SetActionError("MineStairs: unable to move to {0}", action.Direction);
							return false;
						}

						var td = new TileData()
						{
							TerrainID = TerrainID.NaturalFloor,
							TerrainMaterialID = env.GetTerrainMaterialID(p),
							InteriorID = InteriorID.Stairs,
							InteriorMaterialID = env.GetTerrainMaterialID(p),
							Grass = false,
							WaterLevel = 0,
						};

						env.SetTileData(p, td);

						if (env.GetTerrainID(p + Direction.Up) == TerrainID.NaturalFloor)
							env.SetTerrain(p + Direction.Up, TerrainID.Hole, env.GetTerrainMaterialID(p + Direction.Up));
					}
					break;

				default:
					throw new Exception();
			}

			return true;
		}

		int GetTotalTicks(FellTreeAction action)
		{
			return 5;
		}

		bool PerformAction(FellTreeAction action)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id != InteriorID.Tree && id != InteriorID.Sapling)
			{
				SetActionError("{0} tried to fell tree {1}, but it't a tree", this, p);
				return false;
			}

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

			return true;
		}

		int GetTotalTicks(WaitAction action)
		{
			return action.WaitTicks;
		}

		bool PerformAction(WaitAction action)
		{
			return true;
		}

		int GetTotalTicks(BuildItemAction action)
		{
			return 8;
		}

		bool PerformAction(BuildItemAction action)
		{
			var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);

			if (building == null)
			{
				SetActionError("cannot find building");
				return false;
			}
			/*
						if (this.ActionTicksLeft != 0)
						{
							var ok = building.VerifyBuildItem(this, action.SourceObjectIDs, action.DstItemID);
							if (!ok)
								SetActionError("build item request is invalid");
							return ok;
						}
			 */

			var ok = building.PerformBuildItem(this, action.SourceObjectIDs, action.DstItemID);
			if (!ok)
				SetActionError("unable to build the item");
			return ok;
		}

		int GetTotalTicks(AttackAction action)
		{
			return 1;
		}

		Random m_random = new Random();

		bool PerformAction(AttackAction action)
		{
			var attacker = this;
			var target = this.World.FindObject<LivingObject>(action.Target);

			if (target == null)
			{
				SetActionError("{0} tried to attack {1}, but it doesn't exist", attacker, action.Target);
				return false;
			}

			if (!attacker.Location.IsAdjacentTo(target.Location, DirectionSet.Planar))
			{
				SetActionError("{0} tried to attack {1}, but it wasn't near", attacker, target);
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
				hit = (roll - target.ArmorClass) > 0;
			}

			if (!hit)
			{
				Trace.TraceInformation("{0} misses {1}", attacker, target);

				var c = new DamageChange(target, attacker, DamageCategory.Melee, 0) { IsHit = false };
				this.World.AddChange(c);
			}
			else
			{
				var damage = m_random.Next(attacker.Strength / 10) + 1;

				Trace.TraceInformation("{0} hits {1}, {2} damage", attacker, target, damage);

				var c = new DamageChange(target, attacker, DamageCategory.Melee, damage) { IsHit = true };
				this.World.AddChange(c);

				target.ReceiveDamage(attacker, DamageCategory.Melee, damage);
			}

			return true;
		}

		bool CheckWearArmor(WearArmorAction action)
		{
			var itemID = action.ItemID;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == itemID);

			if (item == null)
			{
				SetActionError("{0} tried to wear {1}, but doesn't have the object", this, itemID);
				return false;
			}

			if (!item.IsArmor)
			{
				SetActionError("{0} tried to wear {1}, but it's not an armor", this, item);
				return false;
			}

			if (item.Wearer != null)
			{
				SetActionError("{0} tried to wear {1}, but it's already worn", this, item);
				return false;
			}

			return true;
		}

		int GetTotalTicks(WearArmorAction action)
		{
			if (CheckWearArmor(action) == false)
				return -1;

			return 10;
		}

		bool PerformAction(WearArmorAction action)
		{
			if (CheckWearArmor(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.WearArmor(item);

			return true;
		}

		int GetTotalTicks(RemoveArmorAction action)
		{
			return 10;
		}

		bool PerformAction(RemoveArmorAction action)
		{
			var itemID = action.ItemID;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == itemID);

			if (item == null)
			{
				SetActionError("{0} tried to remove {1}, but doesn't have the object", this, itemID);
				return false;
			}

			if (!item.IsArmor)
			{
				SetActionError("{0} tried to remove {1}, but it's not an armor", this, item);
				return false;
			}

			if (item.Wearer == null)
			{
				SetActionError("{0} tried to remove {1}, but it's not worn", this, item);
				return false;
			}

			this.RemoveArmor(item);

			return true;
		}

		int GetTotalTicks(WieldWeaponAction action)
		{
			return 3;
		}

		bool PerformAction(WieldWeaponAction action)
		{
			var itemID = action.ItemID;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == itemID);

			if (item == null)
			{
				SetActionError("{0} tried to wield {1}, but doesn't have the object", this, itemID);
				return false;
			}

			if (!item.IsWeapon)
			{
				SetActionError("{0} tried to wield {1}, but it's not a weapon", this, item);
				return false;
			}

			if (item.Wearer != null)
			{
				SetActionError("{0} tried to wield {1}, but it's already wielded", this, item);
				return false;
			}

			this.WieldWeapon(item);

			return true;
		}

		int GetTotalTicks(RemoveWeaponAction action)
		{
			return 2;
		}

		bool PerformAction(RemoveWeaponAction action)
		{
			var itemID = action.ItemID;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == itemID);

			if (item == null)
			{
				SetActionError("{0} tried to remove {1}, but doesn't have the object", this, itemID);
				return false;
			}

			if (!item.IsWeapon)
			{
				SetActionError("{0} tried to remove {1}, but it's not a weapon", this, item);
				return false;
			}

			if (item.Wearer == null)
			{
				SetActionError("{0} tried to remove {1}, but it's not wielded", this, item);
				return false;
			}

			this.RemoveWeapon(item);

			return true;
		}
	}
}
