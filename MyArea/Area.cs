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
	public sealed class Area
	{
		const int AREA_SIZE = 7;
		const int NUM_SHEEP = 3;
		const int NUM_ORCS = 3;

		EnvironmentObject m_environment;

		public void InitializeWorld(World world)
		{
			int surfaceLevel;
			m_environment = CreateEnv(world, out surfaceLevel);
			FinalizeEnv(m_environment, surfaceLevel);
		}

		IntPoint3 GetRandomSurfaceLocation(EnvironmentObject env, int zLevel)
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

		IntPoint3 GetRandomSubterraneanLocation(EnvironmentObjectBuilder env)
		{
			IntPoint3 p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3(Helpers.GetRandomInt(env.Width), Helpers.GetRandomInt(env.Height), Helpers.GetRandomInt(env.Depth));
			} while (env.GetTerrainID(p) != TerrainID.NaturalWall);

			return p;
		}

		EnvironmentObject CreateEnv(World world, out int surfaceLevel)
		{
			int sizeExp = AREA_SIZE;
			int size = (int)Math.Pow(2, sizeExp);

			// size + 1 for the DiamondSquare algorithm
			var doubleHeightMap = new ArrayGrid2D<double>(size + 1, size + 1);

			DiamondSquare.Render(doubleHeightMap, 10, 5, 0.75);
			Clamper.Clamp(doubleHeightMap, 10);

			// integer heightmap. the number tells the z level where the floor is.
			var intHeightMap = new ArrayGrid2D<int>(size, size);
			foreach (var p in IntPoint2.Range(size, size))
				intHeightMap[p] = (int)Math.Truncate(doubleHeightMap[p]); // XXX perhaps Round is better

			var envBuilder = new EnvironmentObjectBuilder(new IntSize3(size, size, 20), VisibilityMode.GlobalFOV);

			CreateTerrainFromHeightmap(intHeightMap, envBuilder);

			CreateSlopes(envBuilder, intHeightMap);

			CreateTrees(envBuilder, intHeightMap);

			var oreMaterials = Materials.GetMaterials(MaterialCategory.Gem).Concat(Materials.GetMaterials(MaterialCategory.Mineral)).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 30; ++i)
			{
				var p = GetRandomSubterraneanLocation(envBuilder);
				var idx = Helpers.GetRandomInt(oreMaterials.Length);
				CreateOreCluster(envBuilder, p, oreMaterials[idx]);
			}

			surfaceLevel = FindSurfaceLevel(intHeightMap);

			var env = envBuilder.Create(world);

			for (int i = 0; i < 200; ++i)
			{
				var p = new IntPoint3(i, i, surfaceLevel);
				if (!EnvironmentHelpers.CanEnter(env, p))
					continue;

				for (i = i + 5; i < 200; ++i)
				{
					p = new IntPoint3(i, i, surfaceLevel);
					if (!EnvironmentHelpers.CanEnter(env, p))
						continue;

					env.HomeLocation = new IntPoint3(i, i, surfaceLevel);

					break;
				}

				break;
			}

			if (env.HomeLocation == new IntPoint3())
				throw new Exception();

			return env;
		}

		void FinalizeEnv(EnvironmentObject env, int surfaceLevel)
		{
			var world = env.World;

			// Add items
			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Gem, GetRandomMaterial(MaterialCategory.Gem), GetRandomSurfaceLocation(env, surfaceLevel));

			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), GetRandomSurfaceLocation(env, surfaceLevel));

			CreateWaterTest(env, surfaceLevel);

			CreateBuildings(env, surfaceLevel);

			{
				var p = new IntPoint3(env.Width / 10 - 1, env.Height / 10 - 2, surfaceLevel);

				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);

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
			}

			{
				var gen = FoodGenerator.Create(env.World);
				gen.MoveTo(env, new IntPoint3(env.Width / 10 - 2, env.Height / 10 - 2, surfaceLevel));
			}

			AddMonsters(env, surfaceLevel);


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
			}
		}

		void CreateBuildings(EnvironmentObject env, int surfaceLevel)
		{
			var world = env.World;

			int posx = env.Width / 10;
			int posy = env.Height / 10;

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

			posx = env.Width / 10;
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

		MaterialID GetRandomMaterial(MaterialCategory category)
		{
			var materials = Materials.GetMaterials(category).Select(mi => mi.ID).ToArray();
			return materials[Helpers.GetRandomInt(materials.Length)];
		}

		ItemObject CreateItem(EnvironmentObject env, ItemID itemID, MaterialID materialID, IntPoint3 p)
		{
			var builder = new ItemObjectBuilder(itemID, materialID);
			var item = builder.Create(env.World);
			var ok = item.MoveTo(env, p);
			if (!ok)
				throw new Exception();

			return item;
		}

		void AddMonsters(EnvironmentObject env, int surfaceLevel)
		{
			var world = env.World;

			for (int i = 0; i < NUM_SHEEP; ++i)
			{
				var livingBuilder = new LivingObjectBuilder(LivingID.Sheep)
				{
					Color = this.GetRandomColor(),
				};

				var living = livingBuilder.Create(world);
				living.SetAI(new Dwarrowdelf.AI.HerbivoreAI(living));

				living.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			for (int i = 0; i < NUM_ORCS; ++i)
			{
				var livingBuilder = new LivingObjectBuilder(LivingID.Orc)
				{
					Color = this.GetRandomColor(),
				};

				var living = livingBuilder.Create(world);
				living.SetAI(new Dwarrowdelf.AI.HerbivoreAI(living));

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

		static void CreateTerrainFromHeightmap(ArrayGrid2D<int> heightMap, EnvironmentObjectBuilder env)
		{
			Parallel.For(0, env.Height, y =>
			{
				for (int x = 0; x < env.Width; ++x)
				{
					int surface = heightMap[x, y];

					for (int z = 0; z < env.Depth; ++z)
					{
						var p = new IntPoint3(x, y, z);
						var td = new TileData();

						td.InteriorID = InteriorID.Empty;
						td.InteriorMaterialID = MaterialID.Undefined;

						if (z < surface)
						{
							td.TerrainID = TerrainID.NaturalWall;
							td.TerrainMaterialID = MaterialID.Granite;
						}
						else if (z == surface)
						{
							td.TerrainID = TerrainID.NaturalFloor;
							td.TerrainMaterialID = MaterialID.Granite;
							td.Flags = TileFlags.Grass;
						}
						else
						{
							td.TerrainID = TerrainID.Empty;
							td.TerrainMaterialID = MaterialID.Undefined;
						}

						env.SetTileData(p, td);
					}
				}
			});
		}

		void CreateTrees(EnvironmentObjectBuilder env, ArrayGrid2D<int> heightMap)
		{
			var materials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();

			env.Bounds.Plane.Range().AsParallel().ForAll(p2d =>
			{
				int z = heightMap[p2d];

				var p = new IntPoint3(p2d, z);

				var terrainID = env.GetTerrainID(p);

				if (terrainID == TerrainID.NaturalFloor || terrainID.IsSlope())
				{
					var interiorID = env.GetInteriorID(p);

					if (interiorID == InteriorID.Empty)
					{
						if (Helpers.GetRandomInt(8) == 0)
						{
							var material = materials[Helpers.GetRandomInt(materials.Length)].ID;
							var interior = Helpers.GetRandomInt() % 2 == 0 ? InteriorID.Tree : InteriorID.Sapling;
							env.SetInterior(p, interior, material);
						}
					}
				}
			});
		}

		static void CreateSlopes(EnvironmentObjectBuilder env, ArrayGrid2D<int> heightMap)
		{
			var arr = new ThreadLocal<Direction[]>(() => new Direction[8]);

			var plane = env.Bounds.Plane;

			plane.Range().AsParallel().ForAll(p =>
			{
				int z = heightMap[p];

				int count = 0;
				Direction dir = Direction.None;

				int offset = Helpers.GetRandomInt(8);

				// Count the tiles around this tile which are higher. Create slope to a random direction, but skip
				// the slope if all 8 tiles are higher.
				for (int i = 0; i < 8; ++i)
				{
					var d = DirectionExtensions.PlanarDirections[(i + offset) % 8];

					var t = p + d;

					if (plane.Contains(t) && heightMap[t] > z)
					{
						dir = d;
						count++;
					}
				}

				if (count > 0 && count < 8)
				{
					var p3d = new IntPoint3(p, z);
					env.SetTerrain(p3d, dir.ToSlope(), env.GetTerrainMaterialID(p3d));
				}
			});
		}

		void CreateOreCluster(EnvironmentObjectBuilder env, IntPoint3 p, MaterialID oreMaterialID)
		{
			CreateOreCluster(env, p, oreMaterialID, Helpers.GetRandomInt(6) + 1);
		}

		static void CreateOreCluster(EnvironmentObjectBuilder env, IntPoint3 p, MaterialID oreMaterialID, int count)
		{
			if (!env.Contains(p))
				return;

			if (env.GetTerrainID(p) != TerrainID.NaturalWall)
				return;

			if (env.GetInteriorID(p) == InteriorID.Ore)
				return;

			env.SetInterior(p, InteriorID.Ore, oreMaterialID);

			if (count > 0)
			{
				foreach (var d in DirectionExtensions.CardinalUpDownDirections)
					CreateOreCluster(env, p + d, oreMaterialID, count - 1);
			}
		}

		GameColor GetRandomColor()
		{
			return (GameColor)Helpers.GetRandomInt(GameColorRGB.NUMCOLORS - 1) + 1;
		}


		static void ClearTile(EnvironmentObject env, IntPoint3 p)
		{
			env.SetTerrain(p, TerrainID.Empty, MaterialID.Undefined);
			env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
		}

		static void ClearInside(EnvironmentObject env, IntPoint3 p)
		{
			env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
			env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
		}

		static void SetArea(EnvironmentObject env, IntCuboid area, TileData data)
		{
			foreach (var p in area.Range())
				env.SetTileData(p, data);
		}

		void CreateWalls(EnvironmentObject env, IntRectZ area)
		{
			for (int x = area.X1; x < area.X2; ++x)
			{
				for (int y = area.Y1; y < area.Y2; ++y)
				{
					if ((y != area.Y1 && y != area.Y2 - 1) &&
						(x != area.X1 && x != area.X2 - 1))
						continue;

					var p = new IntPoint3(x, y, area.Z);
					env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				}
			}
		}

		void CreateWater(EnvironmentObject env, IntRectZ area)
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

		void CreateWaterTest(EnvironmentObject env, int surfaceLevel)
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

			env.ScanWaterTiles();

			{
				// Add a water generator
				var item = WaterGenerator.Create(env.World);
				item.MoveTo(env, new IntPoint3(pos.X + 1, pos.Y + 2, surfaceLevel));
			}
		}
	}
}
