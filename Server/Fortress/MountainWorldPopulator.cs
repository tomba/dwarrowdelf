using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace Dwarrowdelf.Server.Fortress
{
	static class MountainWorldPopulator
	{
		const int NUM_SHEEP = 3;
		const int NUM_ORCS = 3;

		public static void FinalizeEnv(EnvironmentObject env)
		{
			// Add items
			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Gem, GetRandomMaterial(MaterialCategory.Gem), GetRandomSurfaceLocation(env));

			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), GetRandomSurfaceLocation(env));

			//CreateWaterTest(env);

			CreateBuildings(env);

			{
				var p = env.GetSurface(env.Width / 2 - 1, env.Height / 2 - 2);

				var td = env.GetTileData(p);
				td.InteriorID = InteriorID.Empty;
				td.InteriorMaterialID = MaterialID.Undefined;
				env.SetTileData(p, td);

				CreateItem(env, ItemID.Ore, MaterialID.Tin, p);
				CreateItem(env, ItemID.Ore, MaterialID.Tin, p);
				CreateItem(env, ItemID.Ore, MaterialID.Lead, p);
				CreateItem(env, ItemID.Ore, MaterialID.Lead, p);
				CreateItem(env, ItemID.Ore, MaterialID.Iron, p);
				CreateItem(env, ItemID.Ore, MaterialID.Iron, p);

				CreateItem(env, ItemID.Log, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Log, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Log, GetRandomMaterial(MaterialCategory.Wood), p);

				CreateItem(env, ItemID.Door, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Door, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Table, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Barrel, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Bed, GetRandomMaterial(MaterialCategory.Wood), p);
				CreateItem(env, ItemID.Chair, GetRandomMaterial(MaterialCategory.Wood), p);

				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);

				CreateItem(env, ItemID.UncutGem, GetRandomMaterial(MaterialCategory.Gem), p);
				CreateItem(env, ItemID.UncutGem, GetRandomMaterial(MaterialCategory.Gem), p);
				CreateItem(env, ItemID.UncutGem, GetRandomMaterial(MaterialCategory.Gem), p);

				CreateItem(env, ItemID.Bar, GetRandomMaterial(MaterialCategory.Metal), p);
				CreateItem(env, ItemID.Bar, GetRandomMaterial(MaterialCategory.Metal), p);
				CreateItem(env, ItemID.Bar, GetRandomMaterial(MaterialCategory.Metal), p);

				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), p);

				p = p + new IntVector3(8, 0, 0);

				td = env.GetTileData(p);
				td.InteriorID = InteriorID.Grass;
				td.InteriorMaterialID = MaterialID.HairGrass;
				env.SetTileData(p, td);

				var bed = CreateItem(env, ItemID.Bed, GetRandomMaterial(MaterialCategory.Wood), p);
				bed.IsInstalled = true;
			}

			{
				var gen = FoodGenerator.Create(env.World);
				gen.MoveTo(env, env.GetSurface(env.Width / 2 - 2, env.Height / 2 - 2));
			}

			AddMonsters(env);
		}

		static void CreateBuildings(EnvironmentObject env)
		{
			var world = env.World;

			int posx = env.Width / 2 - 10;
			int posy = env.Height / 2 - 10;

			var surface = env.GetDepth(new IntPoint2(posx, posy));

			var floorTile = new TileData()
			{
				TerrainID = TerrainID.NaturalFloor,
				TerrainMaterialID = MaterialID.Granite,
				InteriorID = InteriorID.Empty,
				InteriorMaterialID = MaterialID.Undefined,
			};

			{
				var p = new IntPoint3(posx, posy, surface);
				env.SetTileData(p, floorTile);
				var item = CreateItem(env, ItemID.SmithsWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx += 4;

			{
				var p = new IntPoint3(posx, posy, surface);
				env.SetTileData(p, floorTile);
				var item = CreateItem(env, ItemID.CarpentersWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx += 4;

			{
				var p = new IntPoint3(posx, posy, surface);
				env.SetTileData(p, floorTile);
				var item = CreateItem(env, ItemID.MasonsWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx = env.Width / 2 - 10;

			posy += 4;

			{
				var p = new IntPoint3(posx, posy, surface);
				env.SetTileData(p, floorTile);
				var item = CreateItem(env, ItemID.SmelterWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx += 4;

			{
				var p = new IntPoint3(posx, posy, surface);
				env.SetTileData(p, floorTile);
				var item = CreateItem(env, ItemID.GemcuttersWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}
		}

		static ItemObject CreateItem(EnvironmentObject env, ItemID itemID, MaterialID materialID, IntPoint3 p)
		{
			var builder = new ItemObjectBuilder(itemID, materialID);
			var item = builder.Create(env.World);
			var ok = item.MoveTo(env, p);
			if (!ok)
				throw new Exception();

			return item;
		}

		static void AddMonsters(EnvironmentObject env)
		{
			var world = env.World;

			for (int i = 0; i < NUM_SHEEP; ++i)
			{
				var livingBuilder = new LivingObjectBuilder(LivingID.Sheep)
				{
					Color = GetRandomColor(),
				};

				var living = livingBuilder.Create(world);
				living.SetAI(new Dwarrowdelf.AI.HerbivoreAI(living, 0));

				living.MoveTo(env, GetRandomSurfaceLocation(env));
			}

			for (int i = 0; i < NUM_ORCS; ++i)
			{
				var livingBuilder = new LivingObjectBuilder(LivingID.Orc)
				{
					Color = GetRandomColor(),
				};

				var living = livingBuilder.Create(world);
				living.SetAI(new Dwarrowdelf.AI.MonsterAI(living, 0));

				Helpers.AddGem(living);
				Helpers.AddBattleGear(living);

				living.MoveTo(env, GetRandomSurfaceLocation(env));
			}
		}

		static void SetArea(EnvironmentObject env, IntGrid3 area, TileData data)
		{
			foreach (var p in area.Range())
				env.SetTileData(p, data);
		}

		static MaterialID GetRandomMaterial(MaterialCategory category)
		{
			var materials = Materials.GetMaterials(category).Select(mi => mi.ID).ToArray();
			return materials[Helpers.GetRandomInt(materials.Length)];
		}

		static GameColor GetRandomColor()
		{
			return (GameColor)Helpers.GetRandomInt(GameColorRGB.NUMCOLORS - 1) + 1;
		}

		static IntPoint3 GetRandomSurfaceLocation(EnvironmentObject env)
		{
			IntPoint3 p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				int x = Helpers.GetRandomInt(env.Width);
				int y = Helpers.GetRandomInt(env.Height);
				int z = env.GetDepth(new IntPoint2(x, y));

				p = new IntPoint3(x, y, z);
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}
	}
}
