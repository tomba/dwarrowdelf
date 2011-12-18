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

			var report = new ConstructBuildingActionReport(this, action.BuildingID);

			if (env == null)
			{
				SendFailReport(report, "no environment specified");
				return false;
			}

			if (!action.Area.Contains(this.Location))
			{
				SendFailReport(report, "not at the construction site");
				return false;
			}

			if (BuildingObject.VerifyBuildSite(env, action.Area) == false)
			{
				SendFailReport(report, "construction site not clean");
				return false;
			}

			var builder = new BuildingObjectBuilder(action.BuildingID, action.Area);
			var building = builder.Create(this.World, env);

			SendReport(report);

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
				SendFailReport(new DestructBuildingActionReport(this, null), "no such building");
				return false;
			}

			if (!building.Area.Contains(this.Location))
			{
				SendFailReport(new DestructBuildingActionReport(this, building), "not at the building");
				return false;
			}

			building.Destruct();

			SendReport(new DestructBuildingActionReport(this, building));

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
				SendFailReport(new GetActionReport(this, null), "no environment");
				return false;
			}

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new GetActionReport(this, item), "item not found");
				return false;
			}

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(new GetActionReport(this, item), "item not there");
				return false;
			}

			if (item.MoveTo(this) == false)
			{
				SendFailReport(new GetActionReport(this, item), "failed to move");
				return false;
			}

			SendReport(new GetActionReport(this, item));

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
				SendFailReport(new DropActionReport(this, null), "no environment");
				return false;
			}

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new DropActionReport(this, item), "item not found");
				return false;
			}

			if (item.Parent != this)
			{
				SendFailReport(new DropActionReport(this, item), "not in inventory");
				return false;
			}

			if (item.MoveTo(this.Environment, this.Location) == false)
			{
				SendFailReport(new DropActionReport(this, item), "failed to move");
				return false;
			}

			SendReport(new DropActionReport(this, item));

			return true;
		}

		int GetTotalTicks(ConsumeAction action)
		{
			return 6;
		}

		bool PerformAction(ConsumeAction action)
		{
			var ob = this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);
			var item = ob as ItemObject;

			if (item == null)
			{
				SendFailReport(new ConsumeActionReport(this, null), "not in inventory");
				return false;
			}

			var refreshment = item.RefreshmentValue;
			var nutrition = item.NutritionalValue;

			if (refreshment == 0 && nutrition == 0)
			{
				SendFailReport(new ConsumeActionReport(this, item), "non-digestible");
				return false;
			}

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			SendReport(new ConsumeActionReport(this, item));

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
				SendFailReport(new MoveActionReport(this, action.Direction), "could not move (blocked?)");
			}
			else
			{
				SendReport(new MoveActionReport(this, action.Direction));
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

			var report = new MineActionReport(this, action.Direction, action.MineActionType);

			if (!terrain.IsMinable)
			{
				SendFailReport(report, "not mineable");
				return false;
			}

			switch (action.MineActionType)
			{
				case MineActionType.Mine:
					{
						if (!action.Direction.IsPlanar() && action.Direction != Direction.Up)
						{
							SendFailReport(report, "not not planar or up direction");
							return false;
						}

						// XXX is this necessary for planar dirs? we can always move in those dirs
						if (!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
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
							SendFailReport(report, "not PlanarUpDown direction");
							return false;
						}

						if (id != TerrainID.NaturalWall)
						{
							SendFailReport(report, "not natural wall");
							return false;
						}

						// We can always create stairs down, but for other dirs we need access there
						// XXX ??? When we cannot move in planar dirs?
						if (action.Direction != Direction.Down &&
							!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
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

			SendReport(report);

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

			var report = new FellTreeActionReport(this, action.Direction);

			if (id != InteriorID.Tree && id != InteriorID.Sapling)
			{
				SendFailReport(report, "not a tree");
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

			SendReport(report);

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

			var report = new BuildItemActionReport(this);

			if (building == null)
			{
				SendFailReport(report, "cannot find building");
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

			if (ok)
				SendReport(report);
			else
				SendFailReport(report, "unable to build the item");

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
				SendFailReport(new AttackActionReport(this), "target doesn't exist");
				return false;
			}

			if (!attacker.Location.IsAdjacentTo(target.Location, DirectionSet.Planar))
			{
				SendFailReport(new AttackActionReport(this), "target isn't near");
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

			SendReport(new AttackActionReport(this));

			return true;
		}

		bool CheckWearArmor(WearArmorAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

			if (item == null)
			{
				var report = new WearArmorActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new WearArmorActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsArmor)
			{
				var report = new WearArmorActionReport(this, item);
				report.SetFail("not an armor");
				SendReport(report);
				return false;
			}

			if (item.Wearer != null)
			{
				var report = new WearArmorActionReport(this, item);
				report.SetFail("already worn");
				SendReport(report);
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

			var report = new WearArmorActionReport(this, item);
			SendReport(report);

			return true;
		}


		bool CheckRemoveArmor(RemoveArmorAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

			if (item == null)
			{
				var report = new RemoveArmorActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new RemoveArmorActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsArmor)
			{
				var report = new RemoveArmorActionReport(this, item);
				report.SetFail("not an armor");
				SendReport(report);
				return false;
			}

			if (item.Wearer == null)
			{
				var report = new RemoveArmorActionReport(this, item);
				report.SetFail("not worn");
				SendReport(report);
				return false;
			}

			return true;
		}

		int GetTotalTicks(RemoveArmorAction action)
		{
			if (CheckRemoveArmor(action) == false)
				return -1;

			return 10;
		}

		bool PerformAction(RemoveArmorAction action)
		{
			if (CheckRemoveArmor(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.RemoveArmor(item);

			var report = new RemoveArmorActionReport(this, item);
			SendReport(report);

			return true;
		}


		bool CheckWieldWeapon(WieldWeaponAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

			if (item == null)
			{
				var report = new WieldWeaponActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new WieldWeaponActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsWeapon)
			{
				var report = new WieldWeaponActionReport(this, item);
				report.SetFail("not a weapon");
				SendReport(report);
				return false;
			}

			if (item.Wearer != null)
			{
				var report = new WieldWeaponActionReport(this, item);
				report.SetFail("already wielded");
				SendReport(report);
				return false;
			}

			return true;
		}

		int GetTotalTicks(WieldWeaponAction action)
		{
			if (CheckWieldWeapon(action) == false)
				return -1;

			return 3;
		}

		bool PerformAction(WieldWeaponAction action)
		{
			if (CheckWieldWeapon(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.WieldWeapon(item);

			var report = new WieldWeaponActionReport(this, item);
			SendReport(report);

			return true;
		}


		bool CheckRemoveWeapon(RemoveWeaponAction action)
		{
			var itemID = action.ItemID;

			var item = this.World.FindObject<ItemObject>(itemID);

			if (item == null)
			{
				var report = new RemoveWeaponActionReport(this, null);
				report.SetFail("object doesn't exist");
				SendReport(report);
				return false;
			}

			if (item.Parent != this)
			{
				var report = new RemoveWeaponActionReport(this, item);
				report.SetFail("doesn't have the object");
				SendReport(report);
				return false;
			}

			if (!item.IsWeapon)
			{
				var report = new RemoveWeaponActionReport(this, item);
				report.SetFail("not an weapon");
				SendReport(report);
				return false;
			}

			if (item.Wearer == null)
			{
				var report = new RemoveWeaponActionReport(this, item);
				report.SetFail("not wielded");
				SendReport(report);
				return false;
			}

			return true;
		}

		int GetTotalTicks(RemoveWeaponAction action)
		{
			if (CheckRemoveWeapon(action) == false)
				return -1;

			return 2;
		}

		bool PerformAction(RemoveWeaponAction action)
		{
			if (CheckRemoveWeapon(action) == false)
				return false;

			var item = (ItemObject)this.Inventory.FirstOrDefault(o => o.ObjectID == action.ItemID);

			this.RemoveWeapon(item);

			var report = new RemoveWeaponActionReport(this, item);
			SendReport(report);

			return true;
		}
	}
}
