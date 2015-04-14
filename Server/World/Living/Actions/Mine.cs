using System;
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

			var td = env.GetTileData(p);

			switch (action.MineActionType)
			{
				case MineActionType.Mine:
					{
						if (!td.IsMinable)
						{
							SendFailReport(report, "not mineable");
							return ActionState.Fail;
						}

						if (!action.Direction.IsPlanar() && action.Direction != Direction.Up)
						{
							SendFailReport(report, "not planar or up direction");
							return ActionState.Fail;
						}

						// XXX is this necessary for planar dirs? we can always move in those dirs
						if (!env.CanMoveFrom(this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
							return ActionState.Fail;
						}

						env.SetTileData(p, TileData.EmptyTileData);

						if (td.ID == TileID.NaturalWall)
						{
							MaterialInfo material = Materials.GetMaterial(td.MaterialID);
							ItemID itemID = ItemID.Undefined;

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
								case MaterialCategory.Soil:
									break;

								default:
									throw new Exception();
							}

							if (itemID != ItemID.Undefined)
							{
								if (this.World.Random.Next(21) >= GetSkillLevel(SkillID.Mining) / 25 + 10)
								{
									var builder = new ItemObjectBuilder(itemID, material.ID);
									var item = builder.Create(this.World);
									var ok = item.MoveTo(this.Environment, p);
									if (!ok)
										throw new Exception();

								}
							}
						}
					}
					break;

				case MineActionType.Stairs:
					{
						if (!td.IsMinable)
						{
							SendFailReport(report, "not mineable");
							return ActionState.Fail;
						}

						if (!action.Direction.IsPlanarUpDown())
						{
							SendFailReport(report, "not PlanarUpDown direction");
							return ActionState.Fail;
						}

						if (td.ID != TileID.NaturalWall)
						{
							SendFailReport(report, "not natural wall");
							return ActionState.Fail;
						}

						// We can always create stairs down, but for other dirs we need access there
						// XXX ??? When we cannot move in planar dirs?
						if (action.Direction != Direction.Down && !env.CanMoveFrom(this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
							return ActionState.Fail;
						}

						td.ID = TileID.Stairs;
						env.SetTileData(p, td);
					}
					break;

				case MineActionType.Channel:
					{
						if (!action.Direction.IsPlanar())
						{
							SendFailReport(report, "not planar direction");
							return ActionState.Fail;
						}

						// XXX is this necessary for planar dirs? we can always move in those dirs
						if (!env.CanMoveFrom(this.Location, action.Direction))
						{
							SendFailReport(report, "cannot reach");
							return ActionState.Fail;
						}

						if (td.IsClearFloor == false)
						{
							SendFailReport(report, "wrong type of tile");
							return ActionState.Fail;
						}

						if (!env.Contains(p.Down))
						{
							SendFailReport(report, "tile not inside map");
							return ActionState.Fail;
						}

						if (env.HasContents(p))
						{
							SendFailReport(report, "tile contains objects");
							return ActionState.Abort;
						}


						td = TileData.EmptyTileData;
						env.SetTileData(p, td);
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
