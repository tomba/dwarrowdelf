using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;
using Environment = Dwarrowdelf.Server.Environment;

using Dwarrowdelf.TerrainGen;
using System.Threading.Tasks;

namespace MyArea
{
	public class Area
	{
		const int AREA_SIZE = 7;
		const int NUM_SHEEP = 3;

		Environment m_map1;

		public void InitializeWorld(World world)
		{
			m_map1 = CreateMap1(world);
		}

		Random m_random = new Random(1234);

		IntPoint3D GetRandomSurfaceLocation(Environment env, int zLevel)
		{
			IntPoint3D p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3D(m_random.Next(env.Width), m_random.Next(env.Height), zLevel);
			} while (!EnvironmentHelpers.CanEnter(env, p));

			return p;
		}

		IntPoint3D GetRandomSubterraneanLocation(EnvironmentBuilder env)
		{
			IntPoint3D p;
			int iter = 0;

			do
			{
				if (iter++ > 10000)
					throw new Exception();

				p = new IntPoint3D(m_random.Next(env.Width), m_random.Next(env.Height), m_random.Next(env.Depth));
			} while (env.GetTerrainID(p) != TerrainID.NaturalWall);

			return p;
		}

		Environment CreateMap1(World world)
		{
			int sizeExp = AREA_SIZE;
			int size = (int)Math.Pow(2, sizeExp);

			Grid2D<double> grid = new Grid2D<double>(size + 1, size + 1);

			DiamondSquare.Render(grid, 10, 5, 0.75);
			Clamper.Clamp(grid, 10);

			var envBuilder = new EnvironmentBuilder(new IntSize3D(size, size, 20), VisibilityMode.GlobalFOV);

			Random r = new Random(123);

			CreateTerrainFromHeightmap(grid, envBuilder);

			int surfaceLevel = FindSurfaceLevel(envBuilder);

			CreateSlopes(envBuilder);

			CreateTrees(envBuilder);

			int posx = envBuilder.Bounds.Width / 10;
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

			var oreMaterials = Materials.GetMaterials(MaterialClass.Gem).Concat(Materials.GetMaterials(MaterialClass.Mineral)).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 30; ++i)
			{
				var p = GetRandomSubterraneanLocation(envBuilder);
				var idx = m_random.Next(oreMaterials.Length);
				CreateOreCluster(envBuilder, p, oreMaterials[idx]);
			}

			var env = envBuilder.Create(world);
			env.HomeLocation = new IntPoint3D(env.Bounds.Width / 10, env.Bounds.Height / 10, surfaceLevel);







			for (int i = 0; i < 0; ++i)
			{
				// Add a monster
				var builder = new LivingBuilder(String.Format("monsu{0}", i))
				{
					SymbolID = SymbolID.Monster,
					Color = GetRandomColor(),
				};
				//monster.SetAI(new MonsterActor(monster));
				var monster = builder.Create(world);

				if (monster.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel)) == false)
					throw new Exception();
			}

			// Add items
			var gemMaterials = Materials.GetMaterials(MaterialClass.Gem).ToArray();
			for (int i = 0; i < 6; ++i)
			{
				var material = gemMaterials[m_random.Next(gemMaterials.Length)].ID;

				var builder = new ItemObjectBuilder(ItemID.Gem, material);
				var item = builder.Create(world);

				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			var rockMaterials = Materials.GetMaterials(MaterialClass.Rock).ToArray();
			for (int i = 0; i < 6; ++i)
			{
				var material = rockMaterials[m_random.Next(rockMaterials.Length)].ID;
				var builder = new ItemObjectBuilder(ItemID.Rock, material);
				var item = builder.Create(world);

				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			CreateWaterTest(env, surfaceLevel);

			posx = env.Bounds.Width / 10;
			posy = env.Bounds.Height / 10;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Smith, new IntRectZ(posx, posy, 3, 3, 9));
				foreach (var p in builder.Area.Range())
				{
					env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					env.SetGrass(p, false);
				}
				builder.Create(world, env);
			}

			posx += 4;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Carpenter, new IntRectZ(posx, posy, 3, 3, 9));
				foreach (var p in builder.Area.Range())
				{
					env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					env.SetGrass(p, false);
				}
				builder.Create(world, env);
			}

			posx += 4;

			{
				var builder = new BuildingObjectBuilder(BuildingID.Mason, new IntRectZ(posx, posy, 3, 3, 9));
				foreach (var p in builder.Area.Range())
				{
					env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					env.SetGrass(p, false);
				}
				builder.Create(world, env);
			}

			{
				var gen = FoodGenerator.Create(env.World);
				gen.MoveTo(env, new IntPoint3D(env.Bounds.Width / 10 - 2, env.Bounds.Height / 10 - 2, 9));
			}


			/* Add Monsters */

			for (int i = 0; i < NUM_SHEEP; ++i)
			{
				var sheepBuilder = new LivingBuilder(String.Format("Sheep{0}", i))
				{
					SymbolID = SymbolID.Monster,
					Color = this.GetRandomColor(),
				};
				var sheep = sheepBuilder.Create(world);
				sheep.SetAI(new AnimalAI(sheep));

				for (int j = 0; j < i; ++j)
				{
					var material = rockMaterials[m_random.Next(rockMaterials.Length)].ID;
					var builder = new ItemObjectBuilder(ItemID.Rock, material);
					var item = builder.Create(world);

					for (int t = 0; t < j; ++t)
					{
						var material2 = rockMaterials[m_random.Next(rockMaterials.Length)].ID;
						builder = new ItemObjectBuilder(ItemID.Rock, material2);
						var item2 = builder.Create(world);
						item2.MoveTo(item);
					}

					item.MoveTo(sheep);
				}

				sheep.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			return env;
		}

		static int FindSurfaceLevel(EnvironmentBuilder env)
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

		static void CreateTerrainFromHeightmap(Grid2D<double> heightMap, EnvironmentBuilder env)
		{
			var plane = env.Bounds.Plane;

			Parallel.For(0, env.Height, y =>
			{
				for (int x = 0; x < env.Width; ++x)
				{
					double d = heightMap[x, y];

					for (int z = 0; z < env.Depth; ++z)
					{
						var p = new IntPoint3D(x, y, z);

						env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);

						if (d > p.Z)
						{
							env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
						}
						else
						{
							if (env.GetTerrainID(p + Direction.Down) == TerrainID.NaturalWall)
							{
								env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
								env.SetGrass(p, true);
							}
							else
							{
								env.SetTerrain(p, TerrainID.Empty, MaterialID.Undefined);
							}
						}
					}
				}
			});
		}

		void CreateTrees(EnvironmentBuilder env)
		{
			var materials = Materials.GetMaterials(MaterialClass.Wood).ToArray();

			var locations = env.Bounds.Range()
				.Where(p => env.GetTerrainID(p) == TerrainID.NaturalFloor || env.GetTerrainID(p).IsSlope())
				.Where(p => env.GetInteriorID(p) == InteriorID.Empty)
				.Where(p => m_random.Next() % 8 == 0);

			foreach (var p in locations)
			{
				var material = materials[m_random.Next(materials.Length)].ID;
				env.SetInterior(p, m_random.Next() % 2 == 0 ? InteriorID.Tree : InteriorID.Sapling, material);
			}
		}

		static void CreateSlopes(EnvironmentBuilder env)
		{
			/*
			 * su t
			 * s  td
			 *
			 *    ___
			 *    |
			 * ___|
			 *
			 */

			var bounds = env.Bounds;

			var locs = from s in bounds.Range()
					   let su = s + Direction.Up
					   where bounds.Contains(su)
					   where env.GetTerrainID(s) == TerrainID.NaturalFloor && env.GetTerrainID(su) == TerrainID.Empty
					   from d in DirectionExtensions.PlanarDirections
					   let td = s + d
					   let t = s + d + Direction.Up
					   where bounds.Contains(t)
					   where env.GetTerrainID(td) == TerrainID.NaturalWall && env.GetTerrainID(t) == TerrainID.NaturalFloor
					   select new { Location = s, Direction = d };

			foreach (var loc in locs)
			{
				// skip places surrounded by walls
				if (DirectionExtensions.PlanarDirections
					.Where(d => bounds.Contains(loc.Location + d))
					.All(d => env.GetTerrainID(loc.Location + d) != TerrainID.NaturalWall))
					continue;

				env.SetTerrain(loc.Location, loc.Direction.ToSlope(), env.GetTerrainMaterialID(loc.Location));
			}
		}

		void CreateOreCluster(EnvironmentBuilder env, IntPoint3D p, MaterialID oreMaterialID)
		{
			CreateOreCluster(env, p, oreMaterialID, m_random.Next(6) + 1);
		}

		static void CreateOreCluster(EnvironmentBuilder env, IntPoint3D p, MaterialID oreMaterialID, int count)
		{
			if (!env.Bounds.Contains(p))
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
			return (GameColor)m_random.Next((int)GameColor.NumColors - 1) + 1;
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
