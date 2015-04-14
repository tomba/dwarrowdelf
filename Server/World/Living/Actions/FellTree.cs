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

			var material = td.InteriorMaterialID;
			var id = td.InteriorID;

			report.InteriorID = id;
			report.MaterialID = material;

			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			td.InteriorID = InteriorID.Grass;
			td.InteriorMaterialID = grassMaterials[this.World.Random.Next(grassMaterials.Length)].ID;

			this.Environment.SetTileData(p, td);

			if (td.HasFellableTree)
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

			return ActionState.Done;
		}
	}
}
