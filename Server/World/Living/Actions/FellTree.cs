using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
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

	}
}
