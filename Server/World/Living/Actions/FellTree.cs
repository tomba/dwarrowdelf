using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		ActionState ProcessAction(FellTreeAction action)
		{
			if (this.ActionTicksUsed == 1)
				this.ActionTotalTicks = GetTicks(SkillID.WoodCutting);

			if (this.ActionTicksUsed < this.ActionTotalTicks)
				return ActionState.Ok;

			IntVector3 p = this.Location + action.Direction;

			var td = this.Environment.GetTileData(p);

			var report = new FellTreeActionReport(this, action.Direction);

			if (td.HasTree == false)
			{
				SendFailReport(report, "not a tree");
				return ActionState.Fail;
			}

			report.TileID = td.ID;
			report.MaterialID = td.MaterialID;

			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();

			this.Environment.SetTileData(p, new TileData()
			{
				ID = TileID.Grass,
				MaterialID = grassMaterials[this.World.Random.Next(grassMaterials.Length)].ID,
			});

			if (td.HasFellableTree)
			{
				var builder = new ItemObjectBuilder(ItemID.Log, td.MaterialID)
				{
					Name = "Log",
					Color = GameColor.SaddleBrown,
				};
				var log = builder.Create(this.World);
				log.MoveToMustSucceed(this.Environment, p);
			}

			SendReport(report);

			return ActionState.Done;
		}
	}
}
