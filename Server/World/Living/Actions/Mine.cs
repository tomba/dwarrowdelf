﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(MineAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = GetTicks(SkillID.Mining);

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

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
							return ActionState.Fail;
						}

						if (!action.Direction.IsPlanar() && action.Direction != Direction.Up)
						{
							SendFailReport(report, "not not planar or up direction");
							return ActionState.Fail;
						}

						// XXX is this necessary for planar dirs? we can always move in those dirs
						if (!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
							return ActionState.Fail;
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
							return ActionState.Fail;
						}

						if (!action.Direction.IsPlanarUpDown())
						{
							SendFailReport(report, "not PlanarUpDown direction");
							return ActionState.Fail;
						}

						if (id != TerrainID.NaturalWall)
						{
							SendFailReport(report, "not natural wall");
							return ActionState.Fail;
						}

						// We can always create stairs down, but for other dirs we need access there
						// XXX ??? When we cannot move in planar dirs?
						if (action.Direction != Direction.Down &&
							!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
							return ActionState.Fail;
						}

						var td2 = env.GetTileData(p + Direction.Up);
						if (td2.TerrainID == TerrainID.NaturalFloor)
						{
							td2.TerrainID = TerrainID.StairsDown;
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
							return ActionState.Fail;
						}

						// XXX is this necessary for planar dirs? we can always move in those dirs
						if (!EnvironmentHelpers.CanMoveFrom(env, this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
							return ActionState.Fail;
						}

						var td = env.GetTileData(p);

						if (td.IsClear == false)
						{
							SendFailReport(report, "wrong type of tile");
							return ActionState.Fail;
						}

						if (!env.Contains(p + Direction.Down))
						{
							SendFailReport(report, "tile not inside map");
							return ActionState.Fail;
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
							return ActionState.Fail;
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

			return ActionState.Done;
		}
	}
}
