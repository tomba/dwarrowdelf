using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
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

	}
}
