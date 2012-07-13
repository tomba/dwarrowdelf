using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		sealed class ActionData
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

		int GetTicks(SkillID skillID)
		{
			var lvl = GetSkillLevel(skillID);
			return 20 / (lvl / 26 + 1);
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

			// send report before destruct
			SendReport(new DestructBuildingActionReport(this, building));

			building.Destruct();

			return true;
		}

		int GetTotalTicks(GetItemAction action)
		{
			return 1;
		}

		bool PerformAction(GetItemAction action)
		{
			if (this.Environment == null)
			{
				SendFailReport(new GetItemActionReport(this, null), "no environment");
				return false;
			}

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new GetItemActionReport(this, item), "item not found");
				return false;
			}

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(new GetItemActionReport(this, item), "item not there");
				return false;
			}

			if (item.MoveTo(this) == false)
			{
				SendFailReport(new GetItemActionReport(this, item), "failed to move");
				return false;
			}

			SendReport(new GetItemActionReport(this, item));

			return true;
		}

		int GetTotalTicks(DropItemAction action)
		{
			return 1;
		}

		bool PerformAction(DropItemAction action)
		{
			if (this.Environment == null)
			{
				SendFailReport(new DropItemActionReport(this, null), "no environment");
				return false;
			}

			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new DropItemActionReport(this, item), "item not found");
				return false;
			}

			if (item.Parent != this)
			{
				SendFailReport(new DropItemActionReport(this, item), "not in inventory");
				return false;
			}

			if (item.IsWorn)
			{
				SendFailReport(new DropItemActionReport(this, item), "item worn");
				return false;
			}

			if (item.IsWielded)
			{
				SendFailReport(new DropItemActionReport(this, item), "item wielded");
				return false;
			}

			if (item.MoveTo(this.Environment, this.Location) == false)
			{
				SendFailReport(new DropItemActionReport(this, item), "failed to move");
				return false;
			}

			if (this.CarriedItem == item)
				this.CarriedItem = null;

			SendReport(new DropItemActionReport(this, item));

			return true;
		}

		int GetTotalTicks(CarryItemAction action)
		{
			return 1;
		}

		bool PerformAction(CarryItemAction action)
		{
			var item = this.World.FindObject<ItemObject>(action.ItemID);

			var report = new CarryItemActionReport(this, item);

			if (item == null)
			{
				SendFailReport(report, "item not found");
				return false;
			}

			if (this.CarriedItem != null)
			{
				SendFailReport(report, "already carrying an item");
				return false;
			}

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(report, "item not there");
				return false;
			}

			if (item.MoveTo(this) == false)
			{
				SendFailReport(report, "failed to move");
				return false;
			}

			this.CarriedItem = item;

			SendReport(report);

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

			// Send report before destruct
			SendReport(new ConsumeActionReport(this, item));

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			return true;
		}

		int GetTotalTicks(InstallItemAction action)
		{
			return 6;
		}

		bool PerformAction(InstallItemAction action)
		{
			var item = this.World.FindObject<ItemObject>(action.ItemID);

			if (item == null)
			{
				SendFailReport(new InstallItemActionReport(this, null, action.Mode), "item doesn't exists");
				return false;
			}

			var report = new InstallItemActionReport(this, item, action.Mode);

			if (item.Environment != this.Environment || item.Location != this.Location)
			{
				SendFailReport(report, "item not here");
				return false;
			}

			switch (action.Mode)
			{
				case InstallMode.Install:

					if (item.IsInstalled)
					{
						SendFailReport(report, "item already installed");
						return false;
					}

					item.IsInstalled = true;

					break;

				case InstallMode.Uninstall:

					if (!item.IsInstalled)
					{
						SendFailReport(report, "item not installed");
						return false;
					}

					item.IsInstalled = false;

					break;

				default:
					throw new Exception();
			}

			SendReport(report);

			return true;
		}

		int GetTotalTicks(MoveAction action)
		{
			var obs = this.Environment.GetContents(this.Location + action.Direction);
			return obs.OfType<LivingObject>().Count() + 1;
		}

		bool PerformAction(MoveAction action)
		{
			if (this.CarriedItem != null)
			{
				var moveOk = this.CarriedItem.MoveTo(this.Environment, this.Location);

				Debug.Assert(moveOk);

				if (!moveOk)
					Trace.TraceWarning("unable to drop carried item");

				this.CarriedItem = null;
			}

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

		int GetTotalTicks(HaulAction action)
		{
			var dir = action.Direction;

			var obs = this.Environment.GetContents(this.Location + dir);
			return obs.OfType<LivingObject>().Count() + 2;
		}

		bool PerformAction(HaulAction action)
		{
			var dir = action.Direction;
			var itemID = action.ItemID;
			var item = this.World.FindObject<ItemObject>(itemID);

			var report = new HaulActionReport(this, dir, item);

			if (item == null)
			{
				SendFailReport(report, "object doesn't exist");
				return false;
			}

			if (this.CarriedItem == null)
			{
				SendFailReport(report, "not carrying anything");
				return false;
			}

			Debug.Assert(this.CarriedItem.Parent == this);

			if (this.CarriedItem != item)
			{
				SendFailReport(report, "already carrying another item");
				return false;
			}

			var ok = MoveDir(dir);

			if (!ok)
			{
				SendFailReport(new HaulActionReport(this, action.Direction, item), "could not move (blocked?)");
				return false;
			}

			SendReport(report);

			return true;
		}

		int GetTotalTicks(MineAction action)
		{
			return GetTicks(SkillID.Mining);
		}

		bool PerformAction(MineAction action)
		{
			var env = this.Environment;

			var p = this.Location + action.Direction;

			var report = new MineActionReport(this, p, action.Direction, action.MineActionType);

			switch (action.MineActionType)
			{
				case MineActionType.Mine:
					{
						var terrain = env.GetTerrain(p);
						var id = terrain.ID;

						if (!terrain.IsMinable)
						{
							SendFailReport(report, "not mineable");
							return false;
						}

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
							Flags = TileFlags.None,
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
						var terrain = env.GetTerrain(p);
						var id = terrain.ID;

						if (!terrain.IsMinable)
						{
							SendFailReport(report, "not mineable");
							return false;
						}

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

						var td2 = env.GetTileData(p + Direction.Up);
						if (td2.TerrainID == TerrainID.NaturalFloor)
						{
							td2.TerrainID = TerrainID.Hole;
							if (td2.InteriorID == InteriorID.Grass)
							{
								td2.InteriorID = InteriorID.Empty;
								td2.InteriorMaterialID = Dwarrowdelf.MaterialID.Undefined;
							}
							env.SetTileData(p + Direction.Up, td2);
						}

						var td = new TileData()
						{
							TerrainID = TerrainID.NaturalFloor,
							TerrainMaterialID = env.GetTerrainMaterialID(p),
							InteriorID = InteriorID.Stairs,
							InteriorMaterialID = env.GetTerrainMaterialID(p),
							Flags = TileFlags.None,
							WaterLevel = 0,
						};

						env.SetTileData(p, td);
					}
					break;

				case MineActionType.Channel:
					{
						if (!action.Direction.IsPlanar())
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

						var td = env.GetTileData(p);

						if (td.IsClear == false)
						{
							SendFailReport(report, "wrong type of tile");
							return false;
						}

						if (!env.Contains(p + Direction.Down))
						{
							SendFailReport(report, "tile not inside map");
							return false;
						}

						var tdd = env.GetTileData(p + Direction.Down);

						bool clearDown;

						if (tdd.TerrainID == TerrainID.NaturalWall)
						{
							clearDown = true;
						}
						else if (tdd.InteriorID == InteriorID.Empty)
						{
							clearDown = false;
						}
						else
						{
							SendFailReport(report, "tile down not empty");
							return false;
						}

						td.TerrainID = TerrainID.Empty;
						td.TerrainMaterialID = Dwarrowdelf.MaterialID.Undefined;
						td.InteriorID = InteriorID.Empty;
						td.InteriorMaterialID = Dwarrowdelf.MaterialID.Undefined;
						env.SetTileData(p, td);

						if (clearDown)
						{
							tdd.TerrainID = TerrainID.NaturalFloor;
							tdd.InteriorID = InteriorID.Empty;
							tdd.InteriorMaterialID = Dwarrowdelf.MaterialID.Undefined;
							env.SetTileData(p + Direction.Down, tdd);
						}
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
			return GetTicks(SkillID.WoodCutting);
		}

		bool PerformAction(FellTreeAction action)
		{
			IntPoint3 p = this.Location + new IntVector3(action.Direction);

			var td = this.Environment.GetTileData(p);
			var id = td.InteriorID;

			var report = new FellTreeActionReport(this, action.Direction);

			if (id != InteriorID.Tree && id != InteriorID.Sapling)
			{
				SendFailReport(report, "not a tree");
				return false;
			}

			var material = td.InteriorMaterialID;

			report.InteriorID = id;
			report.MaterialID = material;

			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			td.InteriorID = InteriorID.Grass;
			td.InteriorMaterialID = grassMaterials[this.World.Random.Next(grassMaterials.Length)].ID;

			this.Environment.SetTileData(p, td);

			if (id == InteriorID.Tree)
			{
				var builder = new ItemObjectBuilder(ItemID.Log, material)
				{
					Name = "Log",
					Color = GameColor.SaddleBrown,
				};
				var log = builder.Create(this.World);
				var ok = log.MoveTo(this.Environment, p);
				Debug.Assert(ok);
			}

			SendReport(report);

			return true;
		}

		int GetTotalTicks(ConstructAction action)
		{
			return 6;
			//return GetTicks(SkillID.WoodCutting);
		}

		bool PerformAction(ConstructAction action)
		{
			var obs = action.ItemObjectIDs.Select(oid => this.World.FindObject<ItemObject>(oid)).ToArray();

			var report = new ConstructActionReport(this, action.Mode);

			if (obs.Any(ob => ob == null))
			{
				SendFailReport(report, "object not found");
				return false;
			}

			if (obs.Length == 0)
			{
				SendFailReport(report, "no objects given");
				return false;
			}

			if (obs.Length != 1)
			{
				SendFailReport(report, "too many objects given");
				return false;
			}

			if (obs.Any(ob => ob.Location != this.Location))
			{
				SendFailReport(report, "objects somewhere else");
				return false;
			}

			var item = obs[0];

			var env = this.Environment;

			var td = env.GetTileData(action.Location);

			DirectionSet positioning;

			switch (action.Mode)
			{
				case ConstructMode.Floor:
					if (WorkHelpers.ConstructFloorFilter.Match(td) == false)
					{
						SendFailReport(report, "unsuitable terrain");
						return false;
					}

					if (WorkHelpers.ConstructFloorItemFilter.Match(item) == false)
					{
						SendFailReport(report, "bad materials");
						return false;
					}

					positioning = DirectionSet.Planar;

					break;

				case ConstructMode.Pavement:
					if (WorkHelpers.ConstructPavementFilter.Match(td) == false)
					{
						SendFailReport(report, "unsuitable terrain");
						return false;
					}

					if (WorkHelpers.ConstructPavementItemFilter.Match(item) == false)
					{
						SendFailReport(report, "bad materials");
						return false;
					}

					positioning = DirectionSet.Exact;

					break;

				case ConstructMode.Wall:
					if (WorkHelpers.ConstructWallFilter.Match(td) == false)
					{
						SendFailReport(report, "unsuitable terrain");
						return false;
					}

					if (WorkHelpers.ConstructWallItemFilter.Match(item) == false)
					{
						SendFailReport(report, "bad materials");
						return false;
					}

					if (env.GetContents(action.Location).Any())
					{
						SendFailReport(report, "location not empty");
						return false;
					}

					positioning = DirectionSet.Planar;

					break;

				default:
					throw new Exception();
			}

			if (this.Location.IsAdjacentTo(action.Location, positioning) == false)
			{
				SendFailReport(report, "bad location");
				return false;
			}



			foreach (var ob in obs)
				ob.Destruct();

			switch (action.Mode)
			{
				case ConstructMode.Floor:
					td.TerrainID = TerrainID.BuiltFloor;
					td.TerrainMaterialID = item.MaterialID;
					break;

				case ConstructMode.Pavement:
					td.InteriorID = InteriorID.Pavement;
					td.InteriorMaterialID = item.MaterialID;
					break;

				case ConstructMode.Wall:
					td.InteriorID = InteriorID.BuiltWall;
					td.InteriorMaterialID = item.MaterialID;
					break;

				default:
					throw new Exception();
			}

			env.SetTileData(action.Location, td);

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
			var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);
			if (building == null)
				throw new Exception();

			var buildableItem = building.BuildingInfo.FindBuildableItem(action.BuildableItemKey);
			if (buildableItem == null)
				throw new Exception();

			return GetTicks(buildableItem.SkillID);
		}

		bool PerformAction(BuildItemAction action)
		{
			var building = this.Environment.GetLargeObjectAt<BuildingObject>(this.Location);

			var report = new BuildItemActionReport(this, action.BuildableItemKey);

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

			var bi = building.BuildingInfo.FindBuildableItem(action.BuildableItemKey);

			var item = building.PerformBuildItem(this, bi, action.SourceObjectIDs);

			if (item != null)
			{
				report.ItemObjectID = item.ObjectID;
				SendReport(report);
			}
			else
			{
				SendFailReport(report, "unable to build the item");
			}

			return item != null;
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
				SendFailReport(new AttackActionReport(this, null), "target doesn't exist");
				return false;
			}

			if (!attacker.Location.IsAdjacentTo(target.Location, DirectionSet.Planar))
			{
				SendFailReport(new AttackActionReport(this, target), "target isn't near");
				return false;
			}

			var roll = m_random.Next(20) + 1;
			bool hit;

			var str = attacker.Strength;
			str = (int)((20.0 / 100) * str);
			var strBonus = (str / 2) - 5;
			if (strBonus < 0)
				strBonus = 0;

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
				var dex = target.Dexterity;
				dex = (int)((20.0 / 100) * dex);
				var dexBonus = (dex / 2) - 5;
				if (dexBonus < 0)
					dexBonus = 0;

				var ac = 10 + target.ArmorClass + dexBonus;

				hit = roll + strBonus >= ac;

				Trace.TraceInformation("{0} attacks {1}: {2} + {3} >= 10 + {4} + {5} == {6} >= {7}",
					attacker, target,
					roll, strBonus,
					target.ArmorClass, dexBonus,
					roll + strBonus, ac);
			}

			int damage;
			DamageCategory damageCategory;

			if (hit)
			{
				var weapon = attacker.Weapon;
				int dieSides;

				if (weapon == null)
					dieSides = 3;
				else
					dieSides = weapon.WeaponInfo.WC;

				damage = m_random.Next(dieSides) + 1 + strBonus;
				damageCategory = DamageCategory.Melee;
				Trace.TraceInformation("{0} hits {1}, {2} damage", attacker, target, damage);
			}
			else
			{
				damage = 0;
				damageCategory = DamageCategory.None;
				Trace.TraceInformation("{0} misses {1}", attacker, target);
			}

			SendReport(new AttackActionReport(this, target) { IsHit = hit, Damage = damage, DamageCategory = damageCategory });

			if (hit)
				target.ReceiveDamage(attacker, damageCategory, damage);

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

			if (item.IsWorn)
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

			if (item.IsWorn == false)
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

			if (item.IsWielded)
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

			if (item.IsWielded == false)
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
