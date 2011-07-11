using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;
using Environment = Dwarrowdelf.Server.Environment;

using Dwarrowdelf.TerrainGen;

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

		IntPoint3D GetRandomSubterraneanLocation(Environment env)
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

			var env = new Environment(size, size, 20, VisibilityMode.GlobalFOV);

			Random r = new Random(123);

			CreateTerrainFromHeightmap(grid, env);

			int surfaceLevel = FindSurfaceLevel(env);

			env.HomeLocation = new IntPoint3D(env.Bounds.Width / 10, env.Bounds.Height / 10, surfaceLevel);

			CreateSlopes(env);

			CreateTrees(env);

			int posx = env.Bounds.Width / 10;
			int posy = 1;

			for (int x = posx; x < posx + 4; ++x)
			{
				int y = posy;

				IntPoint3D p;

				{
					p = new IntPoint3D(x, y++, surfaceLevel);
					env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Ore, MaterialID.NativeGold);
				}

				{
					p = new IntPoint3D(x, y++, surfaceLevel);
					env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Ore, MaterialID.Magnetite);
				}

				{
					p = new IntPoint3D(x, y++, surfaceLevel);
					env.SetTerrain(p, TerrainID.NaturalWall, MaterialID.Granite);
					env.SetInterior(p, InteriorID.Ore, MaterialID.Chrysoprase);
				}
			}

			var oreMaterials = Materials.GetMaterials(MaterialClass.Gem).Concat(Materials.GetMaterials(MaterialClass.Mineral)).Select(mi => mi.ID).ToArray();
			for (int i = 0; i < 30; ++i)
			{
				var p = GetRandomSubterraneanLocation(env);
				var idx = m_random.Next(oreMaterials.Length);
				CreateOreCluster(env, p, oreMaterials[idx]);
			}

			env.Initialize(world);







			for (int i = 0; i < 0; ++i)
			{
				// Add a monster
				var monster = new Living(String.Format("monsu{0}", i))
				{
					SymbolID = SymbolID.Monster,
					Color = GetRandomColor(),
				};
				//monster.SetAI(new MonsterActor(monster));
				monster.Initialize(world);

				if (monster.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel)) == false)
					throw new Exception();
			}

			// Add items
			var gemMaterials = Materials.GetMaterials(MaterialClass.Gem).ToArray();
			for (int i = 0; i < 6; ++i)
			{
				var material = gemMaterials[m_random.Next(gemMaterials.Length)].ID;
				var item = new ItemObject(ItemID.Gem, material);
				item.Initialize(world);

				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			var rockMaterials = Materials.GetMaterials(MaterialClass.Rock).ToArray();
			for (int i = 0; i < 6; ++i)
			{
				var material = rockMaterials[m_random.Next(rockMaterials.Length)].ID;
				var item = new ItemObject(ItemID.Rock, material);
				item.Initialize(world);

				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

#if asd
			for (int x = 3; x < 4; ++x)
			{
				for (int y = 12; y < 14; ++y)
				{
					var p = new IntPoint3D(x, y, surfaceLevel);
					var td = env.GetTileData(p);
					td.WaterLevel = TileData.MaxWaterLevel;
					env.SetTileData(p, td);
				}
			}

			for (int x = 2; x <= 4; ++x)
			{
				for (int y = 15; y < 21; ++y)
				{
					var p = new IntPoint3D(x, y, surfaceLevel);
					FillTile(env, p, MaterialID.Granite);
				}
			}

			for (int y = 16; y < 20; ++y)
			{
				var p = new IntPoint3D(3, y, surfaceLevel);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
			}

			for (int y = 16; y < 18; ++y)
			{
				var p = new IntPoint3D(3, y, surfaceLevel);
				var td = env.GetTileData(p);
				td.WaterLevel = TileData.MaxWaterLevel;
				env.SetTileData(p, td);
			}

			ClearTile(env, new IntPoint3D(3, 16, surfaceLevel - 0));
			ClearTile(env, new IntPoint3D(3, 16, surfaceLevel - 1));
			ClearTile(env, new IntPoint3D(3, 16, surfaceLevel - 2));
			ClearTile(env, new IntPoint3D(3, 16, surfaceLevel - 3));
			ClearTile(env, new IntPoint3D(3, 16, surfaceLevel - 4));
			ClearInside(env, new IntPoint3D(3, 16, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(4, 16, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(5, 16, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(6, 16, surfaceLevel - 5));
			ClearInside(env, new IntPoint3D(7, 16, surfaceLevel - 5));
			ClearTile(env, new IntPoint3D(7, 16, surfaceLevel - 4));
			ClearTile(env, new IntPoint3D(7, 16, surfaceLevel - 3));
			ClearTile(env, new IntPoint3D(7, 16, surfaceLevel - 2));
			ClearTile(env, new IntPoint3D(7, 16, surfaceLevel - 1));
			ClearTile(env, new IntPoint3D(7, 16, surfaceLevel - 0));

			env.ScanWaterTiles();

			{
				// Add a water generator
				var item = new Dwarrowdelf.Server.Items.WaterGenerator();
				item.Initialize(world);
				item.MoveTo(env, new IntPoint3D(3, 17, surfaceLevel));
			}
#endif

			posx = env.Bounds.Width / 10;
			posy = env.Bounds.Height / 10;

			var building = new BuildingObject(BuildingID.Smith, new IntRectZ(posx, posy, 3, 3, 9));
			foreach (var p in building.Area.Range())
			{
				env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				env.SetGrass(p, false);
			}
			building.Initialize(world, env);

			posx += 4;

			building = new BuildingObject(BuildingID.Carpenter, new IntRectZ(posx, posy, 3, 3, 9));
			foreach (var p in building.Area.Range())
			{
				env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				env.SetGrass(p, false);
			}
			building.Initialize(world, env);

			posx += 4;

			building = new BuildingObject(BuildingID.Mason, new IntRectZ(posx, posy, 3, 3, 9));
			foreach (var p in building.Area.Range())
			{
				env.SetTerrain(p, TerrainID.NaturalFloor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				env.SetGrass(p, false);
			}
			building.Initialize(world, env);


			{
				var gen = new FoodGenerator();
				gen.Initialize(env.World);
				gen.MoveTo(env, new IntPoint3D(env.Bounds.Width / 10 - 2, env.Bounds.Height / 10 - 2, 9));
			}


			/* Add Monsters */

			for (int i = 0; i < NUM_SHEEP; ++i)
			{
				var sheep = new Living(String.Format("Sheep{0}", i))
				{
					SymbolID = SymbolID.Monster,
					Color = this.GetRandomColor(),
				};
				sheep.SetAI(new AnimalAI(sheep));
				sheep.Initialize(env.World);

				for (int j = 0; j < i; ++j)
				{
					var material = rockMaterials[m_random.Next(rockMaterials.Length)].ID;
					var item = new ItemObject(ItemID.Rock, material);
					item.Initialize(world);

					for (int t = 0; t < j; ++t)
					{
						var material2 = rockMaterials[m_random.Next(rockMaterials.Length)].ID;
						var item2 = new ItemObject(ItemID.Rock, material2);
						item2.Initialize(world);
						item2.MoveTo(item);
					}

					item.MoveTo(sheep);
				}

				sheep.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			return env;
		}

		static int FindSurfaceLevel(Environment env)
		{
			int surfaceLevel = 0;
			int numSurfaces = 0;

			/* find the z level with most surface */
			for (int z = env.Bounds.Z1; z < env.Bounds.Z2; ++z)
			{
				int n = env.Bounds.Plane.Range()
					.Where(p => EnvironmentHelpers.CanEnter(env, new IntPoint3D(p, z)))
					.Count();

				if (n > numSurfaces)
				{
					surfaceLevel = z;
					numSurfaces = n;
				}
			}

			return surfaceLevel;
		}

		static void CreateTerrainFromHeightmap(Grid2D<double> heightMap, Environment env)
		{
			foreach (var p in env.Bounds.Range())
			{
				double d = heightMap[p.ToIntPoint()];

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

		void CreateTrees(Environment env)
		{
			var materials = Materials.GetMaterials(MaterialClass.Wood).ToArray();

			var locations = env.Bounds.Range()
				.Where(p => env.GetInteriorID(p) == InteriorID.Empty)
				.Where(p => env.GetTerrainID(p) == TerrainID.NaturalFloor || env.GetTerrainID(p).IsSlope())
				.Where(p => m_random.Next() % 8 == 0);

			foreach (var p in locations)
			{
				var material = materials[m_random.Next(materials.Length)].ID;
				env.SetInterior(p, m_random.Next() % 2 == 0 ? InteriorID.Tree : InteriorID.Sapling, material);
			}
		}

		static void CreateSlopes(Environment env)
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

		void CreateOreCluster(Environment env, IntPoint3D p, MaterialID oreMaterialID)
		{
			CreateOreCluster(env, p, oreMaterialID, m_random.Next(6) + 1);
		}

		static void CreateOreCluster(Environment env, IntPoint3D p, MaterialID oreMaterialID, int count)
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
	}
}
