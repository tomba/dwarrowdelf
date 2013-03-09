using Dwarrowdelf.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class DebugScriptMessages
	{
		public static void SendSetTerrains(SetTerrainDialog dialog, EnvironmentObject env, IntGrid3 cube)
		{
			var data = dialog.Data;

			var args = new Dictionary<string, object>()
			{
				{ "envID", env.ObjectID },
				{ "cube", cube },
				{ "terrainID", data.TerrainID },
				{ "terrainMaterialID", data.TerrainMaterialID },
				{ "interiorID", data.InteriorID },
				{ "interiorMaterialID", data.InteriorMaterialID },
				{ "waterLevel", data.Water.HasValue ? (data.Water == true ? (byte?)TileData.MaxWaterLevel : (byte?)0) : null },
			};

			var script =
@"env = world.GetObject(envID)
for p in cube.Range():
	td = env.GetTileData(p)

	if terrainID != None:
		Dwarrowdelf.TileData.TerrainID.SetValue(td, terrainID)
	if terrainMaterialID != None:
		Dwarrowdelf.TileData.TerrainMaterialID.SetValue(td, terrainMaterialID)

	if interiorID != None:
		Dwarrowdelf.TileData.InteriorID.SetValue(td, interiorID)
	if interiorMaterialID != None:
		Dwarrowdelf.TileData.InteriorMaterialID.SetValue(td, interiorMaterialID)

	if waterLevel != None:
		Dwarrowdelf.TileData.WaterLevel.SetValue(td, waterLevel)

	env.SetTileData(p, td)

env.ScanWaterTiles()
";
			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}

		public static void SendCreateItem(CreateItemDialog dialog)
		{
			var args = new Dictionary<string, object>()
			{
				{ "envID", dialog.Environment.ObjectID },
				{ "area", dialog.Area },
				{ "itemID", dialog.ItemID },
				{ "materialID", dialog.MaterialID },
			};

			var script =
@"env = world.GetObject(envID)
for p in area.Range():
	builder = Dwarrowdelf.Server.ItemObjectBuilder(itemID, materialID)
	item = builder.Create(world)
	item.MoveTo(env, p)";

			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}

		public static void SendCreateLiving(CreateLivingDialog dialog)
		{
			var args = new Dictionary<string, object>()
			{
				{ "envID", dialog.Environment.ObjectID },
				{ "area", dialog.Area },
				{ "name", dialog.LivingName },
				{ "livingID", dialog.LivingID },
				{ "isControllable", dialog.IsControllable },
				{ "isGroup", dialog.IsGroup },
			};

			var script =
@"env = world.GetObject(envID)

if isGroup:
	group = Dwarrowdelf.AI.Group()

controllables = [ ]

for p in area.Range():
	livingBuilder = Dwarrowdelf.Server.LivingObjectBuilder(livingID)
	livingBuilder.Name = name
	living = livingBuilder.Create(world)

	if isControllable:
		engine.GameManager.SetupLivingAsControllable(living)
	else:
		if living.LivingCategory == Dwarrowdelf.LivingCategory.Herbivore:
			ai = Dwarrowdelf.AI.HerbivoreAI(living, 0)
			living.SetAI(ai)

			if isGroup:
				ai.Group = group

		elif living.LivingCategory == Dwarrowdelf.LivingCategory.Carnivore:
			ai = Dwarrowdelf.AI.CarnivoreAI(living, 0)
			living.SetAI(ai)

		elif living.LivingCategory == Dwarrowdelf.LivingCategory.Monster:
			ai = Dwarrowdelf.AI.MonsterAI(living, 0)
			living.SetAI(ai)

	if isControllable:
		controllables.append(living)

	living.MoveTo(env, p);

for l in controllables:
	player.AddControllable(l)
";
			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}

		public static void SendSetLivingTraceLevel(LivingObject living, System.Diagnostics.SourceLevels traceLevel)
		{
			var args = new Dictionary<string, object>()
			{
				{ "livingID", living.ObjectID },
				{ "traceLevel", traceLevel.ToString() },
			};

			var script =
@"l = world.GetObject(livingID)
t = l.Trace
tl = t.TraceLevels.Parse(t.TraceLevels.GetType(), traceLevel)
t.TraceLevels = tl
";

			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}
	}
}
