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

						var ds = env.GetPossibleMiningPositioning(p, MineActionType.Mine);

						if (ds.Contains(action.Direction) == false)
						{
							SendFailReport(report, "cannot mine to that direction");
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

				default:
					throw new Exception();
			}

			SendReport(report);

			return ActionState.Done;
		}
	}
}
