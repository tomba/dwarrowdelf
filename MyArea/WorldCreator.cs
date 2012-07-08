using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;
using System.Threading;

namespace MyArea
{
	static class WorldCreator
	{
		const int MAP_SIZE = 8;	// 2^AREA_SIZE
		const int MAP_DEPTH = 20;

		const int NUM_SHEEP = 3;
		const int NUM_ORCS = 3;

		public static void InitializeWorld(World world)
		{
			int surfaceLevel;
			var environment = CreateEnv(world, out surfaceLevel);
			FinalizeEnv(environment, surfaceLevel);
		}

		static IntPoint3 GetRandomSurfaceLocation(EnvironmentObject env, int zLevel)
		{
			IntPoint3 p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3(Helpers.GetRandomInt(env.Width), Helpers.GetRandomInt(env.Height), zLevel);
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}

		static IntPoint3 GetRandomSubterraneanLocation(TileGrid grid)
		{
			IntPoint3 p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3(Helpers.GetRandomInt(grid.Width), Helpers.GetRandomInt(grid.Height), Helpers.GetRandomInt(grid.Depth));
			} while (grid.GetTerrainID(p) != TerrainID.NaturalWall);

			return p;
		}

		static EnvironmentObject CreateEnv(World world, out int surfaceLevel)
		{
			int sizeExp = MAP_SIZE;
			int s = (int)Math.Pow(2, sizeExp);

			var size = new IntSize3(s, s, MAP_DEPTH);

			var tg = new TerrainGenerator(size);

			var corners = new DiamondSquare.CornerData()
			{
				NE = 15,
				NW = 10,
				SW = 10,
				SE = 10,
			};

			tg.Generate(corners, 5, 0.75, 1, 2);

			var grid = tg.TileGrid;
			var heightMap = tg.HeightMap;

			CreateGrass(grid, heightMap);

			CreateTrees(grid, heightMap);

			surfaceLevel = FindSurfaceLevel(heightMap);

			var envBuilder = new EnvironmentObjectBuilder(grid, heightMap, VisibilityMode.GlobalFOV);

			return envBuilder.Create(world);
		}

		static void CreateGrass(TileGrid grid, ArrayGrid2D<int> intHeightMap)
		{
			int grassLimit = grid.Depth * 4 / 5;

			int w = grid.Width;
			int h = grid.Height;

			var materials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					int z = intHeightMap[x, y];

					var p = new IntPoint3(x, y, z);

					if (z < grassLimit)
					{
						var td = grid.GetTileData(p);

						if (Materials.GetMaterial(td.TerrainMaterialID).Category == MaterialCategory.Soil)
						{
							td.InteriorID = InteriorID.Grass;
							td.InteriorMaterialID = materials[Helpers.GetRandomInt(materials.Length)].ID;

							grid.SetTileData(p, td);
						}
					}
				}
			}
		}

		static void FinalizeEnv(EnvironmentObject env, int surfaceLevel)
		{
			var world = env.World;

			// Add items
			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Gem, GetRandomMaterial(MaterialCategory.Gem), GetRandomSurfaceLocation(env, surfaceLevel));

			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), GetRandomSurfaceLocation(env, surfaceLevel));

			//CreateWaterTest(env, surfaceLevel);

			CreateBuildings(env, surfaceLevel);

			{
				var p = new IntPoint3(env.Width / 2 - 1, env.Height / 2 - 2, surfaceLevel);

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

				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
				CreateItem(env, ItemID.Block, GetRandomMaterial(MaterialCategory.Rock), p);
			}

			{
				var gen = FoodGenerator.Create(env.World);
				gen.MoveTo(env, new IntPoint3(env.Width / 2 - 2, env.Height / 2 - 2, surfaceLevel));
			}

			AddMonsters(env, surfaceLevel);

			/*
			{
				// create a wall and a hole with door

				IntPoint3 p;

				for (int y = 4; y < 12; ++y)
				{
					int x = 17;

					p = new IntPoint3(x, y, surfaceLevel);
					env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Undefined, MaterialID.Undefined);
				}

				p = new IntPoint3(17, 7, surfaceLevel);
				env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);

				p = new IntPoint3(17, 7, surfaceLevel);
				var item = CreateItem(env, ItemID.Door, MaterialID.Birch, p);
				item.IsInstalled = true;
			}*/
		}

		static void CreateBuildings(EnvironmentObject env, int surfaceLevel)
		{
			var world = env.World;

			int posx = env.Width / 2 - 10;
			int posy = env.Height / 2 - 10;

			var floorTile = new TileData()
			{
				TerrainID = TerrainID.NaturalFloor,
				TerrainMaterialID = MaterialID.Granite,
				InteriorID = InteriorID.Empty,
				InteriorMaterialID = MaterialID.Undefined,
			};

			{
				var builder = new BuildingObjectBuilder(BuildingID.Smith, new IntRectZ(posx, posy, 3, 3, surfaceLevel));
				SetArea(env, builder.Area.ToCuboid(), floorTile);
				builder.Create(world, env);
			}

			posx += 4;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Carpenter, new IntRectZ(posx, posy, 3, 3, surfaceLevel));
				SetArea(env, builder.Area.ToCuboid(), floorTile);
				builder.Create(world, env);
			}

			posx += 4;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Mason, new IntRectZ(posx, posy, 3, 3, surfaceLevel));
				SetArea(env, builder.Area.ToCuboid(), floorTile);
				builder.Create(world, env);
			}

			posx = env.Width / 2 - 10;

			posy += 4;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Smelter, new IntRectZ(posx, posy, 3, 3, surfaceLevel));
				SetArea(env, builder.Area.ToCuboid(), floorTile);
				builder.Create(world, env);
			}

			posx += 4;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Gemcutter, new IntRectZ(posx, posy, 3, 3, surfaceLevel));
				SetArea(env, builder.Area.ToCuboid(), floorTile);
				builder.Create(world, env);
			}
		}

		static MaterialID GetRandomMaterial(MaterialCategory category)
		{
			var materials = Materials.GetMaterials(category).Select(mi => mi.ID).ToArray();
			return materials[Helpers.GetRandomInt(materials.Length)];
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

		static void AddMonsters(EnvironmentObject env, int surfaceLevel)
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

				living.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
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

				living.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}
		}

		/// <summary>
		/// return the z of the level with most ground
		/// </summary>
		/// <param name="heightMap"></param>
		static int FindSurfaceLevel(ArrayGrid2D<int> heightMap)
		{
			return heightMap
				.GroupBy(i => i)
				.Select(g => new { H = g.Key, Count = g.Count() })
				.OrderByDescending(c => c.Count)
				.First()
				.H;
		}

		static void CreateTrees(TileGrid grid, ArrayGrid2D<int> heightMap)
		{
			var materials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();

			int baseSeed = Helpers.GetRandomInt();
			if (baseSeed == 0)
				baseSeed = 1;

			grid.Size.Plane.Range().AsParallel().ForAll(p2d =>
			{
				int z = heightMap[p2d];

				var p = new IntPoint3(p2d, z);

				var td = grid.GetTileData(p);

				if (td.InteriorID == InteriorID.Grass)
				{
					var r = new MWCRandom(p, baseSeed);

					if (r.Next(8) == 0)
					{
						td.InteriorID = r.Next(2) == 0 ? InteriorID.Tree : InteriorID.Sapling;
						td.InteriorMaterialID = materials[r.Next(materials.Length)].ID;
						grid.SetTileData(p, td);
					}
				}
			});
		}


		static void CreateOreCluster(TileGrid grid, IntPoint3 p, MaterialID oreMaterialID)
		{
			CreateOreCluster(grid, p, oreMaterialID, Helpers.GetRandomInt(6) + 1);
		}

		static void CreateOreCluster(TileGrid grid, IntPoint3 p, MaterialID oreMaterialID, int count)
		{
			if (!grid.Contains(p))
				return;

			var td = grid.GetTileData(p);

			if (td.TerrainID != TerrainID.NaturalWall)
				return;

			if (td.InteriorID == InteriorID.Ore)
				return;

			td.InteriorID = InteriorID.Ore;
			td.InteriorMaterialID = oreMaterialID;
			grid.SetTileData(p, td);

			if (count > 0)
			{
				foreach (var d in DirectionExtensions.CardinalUpDownDirections)
					CreateOreCluster(grid, p + d, oreMaterialID, count - 1);
			}
		}

		static GameColor GetRandomColor()
		{
			return (GameColor)Helpers.GetRandomInt(GameColorRGB.NUMCOLORS - 1) + 1;
		}


		static void ClearTile(EnvironmentObject env, IntPoint3 p)
		{
			var td = env.GetTileData(p);

			td.TerrainID = TerrainID.Empty;
			td.TerrainMaterialID = MaterialID.Undefined;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			env.SetTileData(p, td);
		}

		static void ClearInside(EnvironmentObject env, IntPoint3 p)
		{
			var td = env.GetTileData(p);

			td.TerrainID = TerrainID.NaturalFloor;
			td.TerrainMaterialID = MaterialID.Granite;
			td.InteriorID = InteriorID.Empty;
			td.InteriorMaterialID = MaterialID.Undefined;

			env.SetTileData(p, td);
		}

		static void SetArea(EnvironmentObject env, IntCuboid area, TileData data)
		{
			foreach (var p in area.Range())
				env.SetTileData(p, data);
		}

		static void CreateWalls(EnvironmentObject env, IntRectZ area)
		{
			for (int x = area.X1; x < area.X2; ++x)
			{
				for (int y = area.Y1; y < area.Y2; ++y)
				{
					if ((y != area.Y1 && y != area.Y2 - 1) &&
						(x != area.X1 && x != area.X2 - 1))
						continue;

					var p = new IntPoint3(x, y, area.Z);

					var td = env.GetTileData(p);

					td.TerrainID = TerrainID.NaturalWall;
					td.TerrainMaterialID = MaterialID.Granite;
					td.InteriorID = InteriorID.Empty;
					td.InteriorMaterialID = MaterialID.Undefined;

					env.SetTileData(p, td);
				}
			}
		}

		static void CreateWater(EnvironmentObject env, IntRectZ area)
		{
			for (int x = area.X1; x < area.X2; ++x)
			{
				for (int y = area.Y1; y < area.Y2; ++y)
				{
					var p = new IntPoint3(x, y, area.Z);
					env.SetWaterLevel(p, TileData.MaxWaterLevel);
				}
			}
		}

		static void CreateWaterTest(EnvironmentObject env, int surfaceLevel)
		{
			var pos = new IntPoint3(10, 30, surfaceLevel);

			CreateWalls(env, new IntRectZ(pos.X, pos.Y, 3, 8, surfaceLevel));
			CreateWater(env, new IntRectZ(pos.X + 1, pos.Y + 1, 1, 6, surfaceLevel));

			int x = 15;
			int y = 30;

			ClearTile(env, new IntPoint3(x, y, surfaceLevel - 0));
			ClearTile(env, new IntPoint3(x, y, surfaceLevel - 1));
			ClearTile(env, new IntPoint3(x, y, surfaceLevel - 2));
			ClearTile(env, new IntPoint3(x, y, surfaceLevel - 3));
			ClearTile(env, new IntPoint3(x, y, surfaceLevel - 4));
			ClearInside(env, new IntPoint3(x + 0, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3(x + 1, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3(x + 2, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3(x + 3, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3(x + 4, y, surfaceLevel - 5));
			ClearTile(env, new IntPoint3(x + 4, y, surfaceLevel - 4));
			ClearTile(env, new IntPoint3(x + 4, y, surfaceLevel - 3));
			ClearTile(env, new IntPoint3(x + 4, y, surfaceLevel - 2));
			ClearTile(env, new IntPoint3(x + 4, y, surfaceLevel - 1));
			ClearTile(env, new IntPoint3(x + 4, y, surfaceLevel - 0));

			{
				// Add a water generator
				var item = WaterGenerator.Create(env.World);
				item.MoveTo(env, new IntPoint3(pos.X + 1, pos.Y + 2, surfaceLevel));
			}
		}
	}
}
