using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;
using Environment = Dwarrowdelf.Server.EnvironmentObject;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;

namespace MyArea
{
	public sealed class Area
	{
		const int AREA_SIZE = 7;
		const int NUM_SHEEP = 3;
		const int NUM_ORCS = 3;

		Environment m_map1;

		public void InitializeWorld(World world)
		{
			m_map1 = CreateMap1(world);
		}

		IntPoint3D GetRandomSurfaceLocation(Environment env, int zLevel)
		{
			IntPoint3D p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3D(Helpers.MyRandom.Next(env.Width), Helpers.MyRandom.Next(env.Height), zLevel);
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}

		IntPoint3D GetRandomSubterraneanLocation(EnvironmentObjectBuilder env)
		{
			IntPoint3D p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3D(Helpers.MyRandom.Next(env.Width), Helpers.MyRandom.Next(env.Height), Helpers.MyRandom.Next(env.Depth));
			} while (env.GetTerrainID(p) != TerrainID.NaturalWall);

			return p;
		}

		Environment CreateMap1(World world)
		{
			int sizeExp = AREA_SIZE;
			int size = (int)Math.Pow(2, sizeExp);

			var grid = new ArrayGrid2D<double>(size + 1, size + 1);

			DiamondSquare.Render(grid, 10, 5, 0.75);
			Clamper.Clamp(grid, 10);

			var envBuilder = new EnvironmentObjectBuilder(new IntSize3D(size, size, 20), VisibilityMode.GlobalFOV);

			Random r = new Random(123);

			CreateTerrainFromHeightmap(grid, envBuilder);

			int surfaceLevel = FindSurfaceLevel(envBuilder);

			CreateSlopes(envBuilder);

			CreateTrees(envBuilder);

			int posx = envBuilder.Width / 10;
			int posy = 1;

			for (int x = posx; x < posx + 4; ++x)
			{
				int y = posy;

				IntPoint3D p;

				{
					p = new IntPoint3D(x, y++, surfaceLevel);
					envBuilder.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					envBuilder.SetInterior(p, InteriorID.Ore, MaterialID.NativeGold);
				}

				{
					p = new IntPoint3D(x, y++, surfaceLevel);
					envBuilder.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					envBuilder.SetInterior(p, InteriorID.Ore, MaterialID.Magnetite);
				}

				{
					p = new IntPoint3D(x, y++, surfaceLevel);
					envBuilder.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					envBuilder.SetInterior(p, InteriorID.Ore, MaterialID.Chrysoprase);
				}
			}

			{
				// create a wall and a hole (with a door created later)

				IntPoint3D p;

				for (int y = 4; y < 12; ++y)
				{
					int x = 17;

					p = new IntPoint3D(x, y, surfaceLevel);
					envBuilder.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					envBuilder.SetInterior(p, InteriorID.Undefined, MaterialID.Undefined);
				}

				p = new IntPoint3D(17, 7, surfaceLevel);
				envBuilder.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
				envBuilder.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
			}


			var oreMaterials = Materials.GetMaterials(MaterialCategory.Gem).Concat(Materials.GetMaterials(MaterialCategory.Mineral)).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 30; ++i)
			{
				var p = GetRandomSubterraneanLocation(envBuilder);
				var idx = Helpers.MyRandom.Next(oreMaterials.Length);
				CreateOreCluster(envBuilder, p, oreMaterials[idx]);
			}

			var env = envBuilder.Create(world);
			for (int i = 0; i < 200; ++i)
			{
				var p = new IntPoint3D(i, i, surfaceLevel);
				if (!EnvironmentHelpers.CanEnter(env, p))
					continue;

				for (i = i + 5; i < 200; ++i)
				{
					p = new IntPoint3D(i, i, surfaceLevel);
					if (!EnvironmentHelpers.CanEnter(env, p))
						continue;

					env.HomeLocation = new IntPoint3D(i, i, surfaceLevel);

					break;
				}

				break;
			}

			if (env.HomeLocation == new IntPoint3D())
				throw new Exception();




			// Add items
			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Gem, GetRandomMaterial(MaterialCategory.Gem), GetRandomSurfaceLocation(env, surfaceLevel));

			for (int i = 0; i < 6; ++i)
				CreateItem(env, ItemID.Rock, GetRandomMaterial(MaterialCategory.Rock), GetRandomSurfaceLocation(env, surfaceLevel));


			CreateWaterTest(env, surfaceLevel);

			CreateBuildings(env, surfaceLevel);

			{
				var p = new IntPoint3D(env.Width / 10 - 1, env.Height / 10 - 2, surfaceLevel);
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
				gen.MoveTo(env, new IntPoint3D(env.Width / 10 - 2, env.Height / 10 - 2, surfaceLevel));
			}

			AddMonsters(env, surfaceLevel);

			{
				var p = new IntPoint3D(17, 7, surfaceLevel);
				var item = CreateItem(env, ItemID.Door, MaterialID.Birch, p);
				item.IsInstalled = true;
			}

			return env;
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
			return materials[Helpers.MyRandom.Next(materials.Length)];
		}

		ItemObject CreateItem(Environment env, ItemID itemID, MaterialID materialID, IntPoint3D p)
		{
			var builder = new ItemObjectBuilder(itemID, materialID);
			var item = builder.Create(env.World);
			var ok = item.MoveTo(env, p);
			if (!ok)
				throw new Exception();

			return item;
		}

		void AddMonsters(Environment env, int surfaceLevel)
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

		static int FindSurfaceLevel(EnvironmentObjectBuilder env)
		{
			int surfaceLevel = 0;
			int numSurfaces = 0;

			/* find the z level with most surface */
			for (int z = 0; z < env.Depth; ++z)
			{
				int n = env.Bounds.Plane.Range()
					.Select(p => new IntPoint3D(p, z))
					.Where(p => env.GetTerrain(p).IsSupporting && !env.GetTerrain(p).IsBlocker && !env.GetInterior(p).IsBlocker)
					.Count();

				if (n > numSurfaces)
				{
					surfaceLevel = z;
					numSurfaces = n;
				}
			}

			return surfaceLevel;
		}

		static void CreateTerrainFromHeightmap(ArrayGrid2D<double> heightMap, EnvironmentObjectBuilder env)
		{
			Parallel.For(0, env.Height, y =>
			{
				for (int x = 0; x < env.Width; ++x)
				{
					double d = heightMap[x, y];

					for (int z = 0; z < env.Depth; ++z)
					{
						var p = new IntPoint3D(x, y, z);
						var td = new TileData();

						td.InteriorID = InteriorID.Empty;
						td.InteriorMaterialID = MaterialID.Undefined;

						if (d > p.Z)
						{
							td.TerrainID = TerrainID.NaturalWall;
							td.TerrainMaterialID = MaterialID.Granite;
						}
						else
						{
							if (env.GetTerrainID(p + Direction.Down) == TerrainID.NaturalWall)
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
						}

						env.SetTileData(p, td);
					}
				}
			});
		}

		void CreateTrees(EnvironmentObjectBuilder env)
		{
			var materials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();

			var bounds = env.Bounds;

			for (int y = 0; y < bounds.Height; ++y)
			{
				for (int x = 0; x < bounds.Width; ++x)
				{
					int z = GetSurfaceZ(env, x, y);

					if (z == -1)
						continue;

					var p = new IntPoint3D(x, y, z);

					var terrainID = env.GetTerrainID(p);

					if (terrainID == TerrainID.NaturalFloor || terrainID.IsSlope())
					{
						var interiorID = env.GetInteriorID(p);

						if (interiorID == InteriorID.Empty)
						{
							if (Helpers.MyRandom.Next(8) == 0)
							{
								var material = materials[Helpers.MyRandom.Next(materials.Length)].ID;
								env.SetInterior(p, Helpers.MyRandom.Next() % 2 == 0 ? InteriorID.Tree : InteriorID.Sapling, material);
							}
						}
					}
				}
			}
		}

		static int GetSurfaceZ(EnvironmentObjectBuilder env, int x, int y)
		{
			for (int z = env.Bounds.Z2 - 1; z >= 0; --z)
			{
				var p = new IntPoint3D(x, y, z);

				var terrainID = env.GetTerrainID(p);

				if (terrainID != TerrainID.Empty)
				{
					if (terrainID != TerrainID.NaturalWall)
						return z;

					break;
				}
			}

			return -1;
		}

		static void CreateSlopes(EnvironmentObjectBuilder env)
		{
			var bounds = env.Bounds;

			Parallel.For(0, bounds.Height, y =>
			{
				Direction[] arr = new Direction[8];

				for (int x = 0; x < bounds.Width; ++x)
				{
					int z = GetSurfaceZ(env, x, y);

					if (z == -1)
						continue;

					var p = new IntPoint3D(x, y, z);

					int count = 0;

					foreach (var dir in DirectionExtensions.PlanarDirections)
					{
						var t = p + dir;

						if (bounds.Contains(t) && env.GetTerrainID(t) == TerrainID.NaturalWall)
							arr[count++] = dir;
					}

					// skip places surrounded by walls
					if (count > 0 && count < 8)
					{
						// If there are multiple possible directions for the slope, use "random"
						var idx = (x + y) % count;
						env.SetTerrain(p, arr[idx].ToSlope(), env.GetTerrainMaterialID(p));
					}
				}
			});
		}

		void CreateOreCluster(EnvironmentObjectBuilder env, IntPoint3D p, MaterialID oreMaterialID)
		{
			CreateOreCluster(env, p, oreMaterialID, Helpers.MyRandom.Next(6) + 1);
		}

		static void CreateOreCluster(EnvironmentObjectBuilder env, IntPoint3D p, MaterialID oreMaterialID, int count)
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
			return (GameColor)Helpers.MyRandom.Next(GameColorRGB.NUMCOLORS - 1) + 1;
		}


		static void ClearTile(Environment env, IntPoint3D p)
		{
			env.SetTerrain(p, TerrainID.Empty, MaterialID.Undefined);
			env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
		}

		static void ClearInside(Environment env, IntPoint3D p)
		{
			env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
			env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
		}

		static void SetArea(Environment env, IntCuboid area, TileData data)
		{
			foreach (var p in area.Range())
				env.SetTileData(p, data);
		}

		void CreateWalls(Environment env, IntRectZ area)
		{
			for (int x = area.X1; x < area.X2; ++x)
			{
				for (int y = area.Y1; y < area.Y2; ++y)
				{
					if ((y != area.Y1 && y != area.Y2 - 1) &&
						(x != area.X1 && x != area.X2 - 1))
						continue;

					var p = new IntPoint3D(x, y, area.Z);
					env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				}
			}
		}

		void CreateWater(Environment env, IntRectZ area)
		{
			for (int x = area.X1; x < area.X2; ++x)
			{
				for (int y = area.Y1; y < area.Y2; ++y)
				{
					var p = new IntPoint3D(x, y, area.Z);
					env.SetWaterLevel(p, TileData.MaxWaterLevel);
				}
			}
		}

		void CreateWaterTest(Environment env, int surfaceLevel)
		{
			var pos = new IntPoint3D(10, 30, surfaceLevel);

			CreateWalls(env, new IntRectZ(pos.X, pos.Y, 3, 8, surfaceLevel));
			CreateWater(env, new IntRectZ(pos.X + 1, pos.Y + 1, 1, 6, surfaceLevel));

			int x = 15;
			int y = 30;

			ClearTile(env, new IntPoint3D(x, y, surfaceLevel - 0));
			ClearTile(env, new IntPoint3D(x, y, surfaceLevel - 1));
			ClearTile(env, new IntPoint3D(x, y, surfaceLevel - 2));
			ClearTile(env, new IntPoint3D(x, y, surfaceLevel - 3));
			ClearTile(env, new IntPoint3D(x, y, surfaceLevel - 4));
			ClearInside(env, new IntPoint3D(x + 0, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(x + 1, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(x + 2, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(x + 3, y, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(x + 4, y, surfaceLevel - 5));
			ClearTile(env, new IntPoint3D(x + 4, y, surfaceLevel - 4));
			ClearTile(env, new IntPoint3D(x + 4, y, surfaceLevel - 3));
			ClearTile(env, new IntPoint3D(x + 4, y, surfaceLevel - 2));
			ClearTile(env, new IntPoint3D(x + 4, y, surfaceLevel - 1));
			ClearTile(env, new IntPoint3D(x + 4, y, surfaceLevel - 0));

			env.ScanWaterTiles();

			{
				// Add a water generator
				var item = WaterGenerator.Create(env.World);
				item.MoveTo(env, new IntPoint3D(pos.X + 1, pos.Y + 2, surfaceLevel));
			}
		}
	}
}
