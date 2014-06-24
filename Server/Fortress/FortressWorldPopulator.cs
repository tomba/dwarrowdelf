using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace Dwarrowdelf.Server.Fortress
{
	static class FortressWorldPopulator
	{
		const int NUM_ORCS = 3;

		public static void FinalizeEnv(EnvironmentObject env)
		{
			// Add gems to random locs
			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Gem, GetRandomMaterial(MaterialCategory.Gem), env.GetRandomEnterableSurfaceLocation());

			// Add rocks to random locs
			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), env.GetRandomEnterableSurfaceLocation());

			CreateWorkbenches(env);

			CreateStartItems(env);
			CreateDebugItems(env);

			{
				var gen = FoodGenerator.Create(env.World);
				gen.MoveTo(env, env.GetSurfaceLocation(env.Width / 2 - 2, env.Height / 2 - 2));
			}

			AddMonsters(env);
		}

		static void ClearFloor(EnvironmentObject env, IntVector3 p)
		{
			var td = env.GetTileData(p);

			if (td.TerrainID.IsFloor() == false)
				throw new Exception();

			if (td.IsGreen)
			{
				td.InteriorID = InteriorID.Grass;
				td.InteriorMaterialID = GetRandomMaterial(MaterialCategory.Grass);
			}

			if (!td.IsClearFloor)
				throw new Exception();

			env.SetTileData(p, td);
		}

		private static void CreateStartItems(EnvironmentObject env)
		{
			var p = env.GetSurfaceLocation(env.Width / 2 - 1, env.Height / 2 - 4);

			ClearFloor(env, p);

			CreateItem(env, ItemID.Log, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Log, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Log, GetRandomMaterial(MaterialCategory.Wood), p);

			CreateItem(env, ItemID.Bar, GetRandomMaterial(MaterialCategory.Metal), p);
			CreateItem(env, ItemID.Bar, GetRandomMaterial(MaterialCategory.Metal), p);
			CreateItem(env, ItemID.Bar, GetRandomMaterial(MaterialCategory.Metal), p);

			CreateItem(env, ItemID.CarpentersWorkbench, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.MasonsWorkbench, GetRandomMaterial(MaterialCategory.Wood), p);
		}

		private static void CreateDebugItems(EnvironmentObject env)
		{
			var p = env.GetSurfaceLocation(env.Width / 2 - 1, env.Height / 2 - 2);

			ClearFloor(env, p);

			CreateItem(env, ItemID.Ore, MaterialID.Tin, p);
			CreateItem(env, ItemID.Ore, MaterialID.Tin, p);
			CreateItem(env, ItemID.Ore, MaterialID.Lead, p);
			CreateItem(env, ItemID.Ore, MaterialID.Lead, p);
			CreateItem(env, ItemID.Ore, MaterialID.Iron, p);
			CreateItem(env, ItemID.Ore, MaterialID.Iron, p);

			CreateItem(env, ItemID.Door, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Door, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Table, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Bed, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Chair, GetRandomMaterial(MaterialCategory.Wood), p);

			CreateItem(env, ItemID.Barrel, GetRandomMaterial(MaterialCategory.Wood), p);
			CreateItem(env, ItemID.Bin, GetRandomMaterial(MaterialCategory.Wood), p);

			CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
			CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
			CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
			CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
			CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);

			CreateItem(env, ItemID.UncutGem, GetRandomMaterial(MaterialCategory.Gem), p);
			CreateItem(env, ItemID.UncutGem, GetRandomMaterial(MaterialCategory.Gem), p);
			CreateItem(env, ItemID.UncutGem, GetRandomMaterial(MaterialCategory.Gem), p);

			CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), p);
			CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), p);
			CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), p);

			p = p + new IntVector3(8, 0, 0);

			ClearFloor(env, p);

			var bed = CreateItem(env, ItemID.Bed, GetRandomMaterial(MaterialCategory.Wood), p);
			bed.IsInstalled = true;
		}

		static void CreateWorkbenches(EnvironmentObject env)
		{
			int posx = env.Width / 2 - 10;
			int posy = env.Height / 2 - 10;

			var surface = env.GetSurfaceLevel(new IntVector2(posx, posy));

			{
				var p = new IntVector3(posx, posy, surface);
				ClearFloor(env, p);
				var item = CreateItem(env, ItemID.SmithsWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx += 4;

			{
				var p = new IntVector3(posx, posy, surface);
				ClearFloor(env, p);
				var item = CreateItem(env, ItemID.CarpentersWorkbench, MaterialID.Oak, p);
				item.IsInstalled = true;
			}

			posx += 4;

			{
				var p = new IntVector3(posx, posy, surface);
				ClearFloor(env, p);
				var item = CreateItem(env, ItemID.MasonsWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx = env.Width / 2 - 10;

			posy += 4;

			{
				var p = new IntVector3(posx, posy, surface);
				ClearFloor(env, p);
				var item = CreateItem(env, ItemID.SmelterWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}

			posx += 4;

			{
				var p = new IntVector3(posx, posy, surface);
				ClearFloor(env, p);
				var item = CreateItem(env, ItemID.GemcuttersWorkbench, MaterialID.Iron, p);
				item.IsInstalled = true;
			}
		}

		static ItemObject CreateItem(EnvironmentObject env, ItemID itemID, MaterialID materialID, IntVector3 p)
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

			for (int i = 0; i < NUM_ORCS; ++i)
			{
				var livingBuilder = new LivingObjectBuilder(LivingID.Orc)
				{
					Color = GetRandomColor(),
				};

				var living = livingBuilder.Create(world);
				living.SetAI(new Dwarrowdelf.AI.MonsterAI(living, world.PlayerID));

				Helpers.AddGem(living);
				Helpers.AddBattleGear(living);

				living.MoveTo(env, env.GetRandomEnterableSurfaceLocation());
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
	}
}
