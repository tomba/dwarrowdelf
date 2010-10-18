using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dwarrowdelf;
using Dwarrowdelf.Server;
using Environment = Dwarrowdelf.Server.Environment;
using System.IO;

using Dwarrowdelf.TerrainGen;

namespace MyArea
{
	public class Area : IArea
	{
		Environment m_map1;
		Environment m_map2;

		public void InitializeWorld(World world)
		{
			m_map1 = CreateMap1(world);
			m_map2 = CreateMap2(world);

			//SerializeMap(m_map1);
		}

		void SerializeMap(Environment env)
		{
			using (var txtFile = File.CreateText("map.txt"))
			{
				txtFile.WriteLine("{0}x{1}x{2}", env.Width, env.Height, env.Depth);

				SerializeEnum<FloorID>(txtFile);
				SerializeEnum<InteriorID>(txtFile);
				SerializeEnum<MaterialID>(txtFile);

				txtFile.WriteLine("first: {0}", env.Bounds.Range().First());
				txtFile.WriteLine("last: {0}", env.Bounds.Range().Last());

				txtFile.Close();
			}

			using (var binFile = File.Create("map.bin"))
			using (var bw = new BinaryWriter(binFile))
			{
				foreach (var p in env.Bounds.Range())
				{
					var t = env.GetTileData(p);

					bw.Write((int)t.FloorID);
					bw.Write((int)t.FloorMaterialID);
					bw.Write((int)t.InteriorID);
					bw.Write((int)t.InteriorMaterialID);
					bw.Write((int)(t.Grass ? 1 : 0));
					bw.Write((int)t.WaterLevel);
				}
			}
		}

		void SerializeEnum<T>(StreamWriter writer)
		{
			writer.WriteLine(typeof(T).Name);
			var values = Enum.GetValues(typeof(T));
			foreach (T val in values)
				writer.WriteLine("\t{0}: {1}", val.ToString(), Enum.Format(typeof(T), val, "d"));
		}

		IntPoint3D m_portalLoc = new IntPoint3D(2, 4, 9);
		IntPoint3D m_portalLoc2 = new IntPoint3D(1, 1, 0);

		bool ActionHandler(ServerGameObject ob, GameAction action)
		{
			if (ob.Environment == m_map1 && ob.Location == m_portalLoc)
			{
				var a = action as MoveAction;

				if (a == null)
					return false;

				if (a.Direction != Direction.Up && a.Direction != Direction.Down)
					return false;

				var dst = m_map2;
				var dstPos = new IntPoint3D(0, 0, 0);

				var ok = ob.MoveTo(dst, dstPos);

				return ok;
			}
			else if (ob.Environment == m_map2 && ob.Location == m_portalLoc2)
			{
				var a = action as MoveAction;

				if (a == null)
					return false;

				if (a.Direction != Direction.Up && a.Direction != Direction.Down)
					return false;

				var dst = m_map1;
				var dstPos = new IntPoint3D(0, 0, 0);

				var ok = ob.MoveTo(dst, dstPos);

				return ok;
			}
			else
			{
				throw new Exception();
			}
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
			} while (!env.CanEnter(p));

			return p;
		}

		void FillTile(Environment env, IntPoint3D p, MaterialID material)
		{
			env.SetInterior(p, InteriorID.Wall, material);
			env.SetFloor(p, FloorID.Floor, material);
		}

		Environment CreateMap1(World world)
		{
			int sizeExp = 6;
			int size = (int)Math.Pow(2, sizeExp);

			Grid2D<double> grid = new Grid2D<double>(size + 1, size + 1);

			DiamondSquare.Render(grid, 10, 5, 0.75);
			Clamper.Clamp(grid, 10);

			var env = new Environment(size, size, 20, VisibilityMode.LOS);
			env.Initialize(world);

			Random r = new Random(123);

			/* create terrain */
			foreach (var p in env.Bounds.Range())
			{
				double d = grid[p.ToIntPoint()];

				if (d > p.Z)
				{
					FillTile(env, p, MaterialID.Granite);
				}
				else
				{
					env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
					if (env.GetInteriorID(p + Direction.Down) != InteriorID.Empty)
					{
						env.SetFloor(p, FloorID.Floor, MaterialID.Granite);
						env.SetGrass(p, true);
					}
					else
					{
						env.SetFloor(p, FloorID.Empty, MaterialID.Undefined);
					}
				}
			}

			/* create long stairs */
			foreach (var p in env.Bounds.Range())
			{
				if (p.X == 1 && p.Y == 4)
				{
					env.SetInterior(p, InteriorID.Stairs, MaterialID.Granite);
					env.SetFloor(p, FloorID.Hole, MaterialID.Granite);
				}
			}

			int surfaceLevel = 0;
			int numSurfaces = 0;
			/* find the z level with most surface */
			for (int z = env.Bounds.Z1; z < env.Bounds.Z2; ++z)
			{
				var n = 0;
				foreach (var p in env.Bounds.Plane.Range())
				{
					if (env.GetInterior(new IntPoint3D(p, z)).Blocker == false && env.GetFloor(new IntPoint3D(p, z)).IsBlocking == true)
						n++;
				}

				if (n > numSurfaces)
				{
					surfaceLevel = z;
					numSurfaces = n;
				}
			}

			/* create the portal */
			m_portalLoc = GetRandomSurfaceLocation(env, surfaceLevel);
			env.SetInterior(m_portalLoc, InteriorID.Portal, MaterialID.Steel);
			env.SetActionHandler(m_portalLoc, ActionHandler);

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
			for (int i = 0; i < 10; ++i)
			{
				var item = new ItemObject(ItemClass.Gem)
				{
					SymbolID = SymbolID.Gem,
					Name = "gem" + i.ToString(),
					Color = GetRandomColor(),
					MaterialID = MaterialID.Diamond,
				};
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

			CreateSlopes(env);

			CreateTrees(env);

			for (int x = 24; x < 27; ++x)
			{
				for (int y = 12; y < 15; ++y)
				{
					var p = new IntPoint3D(x, y, surfaceLevel);
					env.SetInterior(p, InteriorID.Ore, MaterialID.Gold);
				}
			}


			var building = new BuildingObject( BuildingID.Smith) { Area = new IntRect(2, 6, 3, 3), Z = 9 };
			foreach (var p2d in building.Area.Range())
			{
				var p = new IntPoint3D(p2d, building.Z);
				env.SetFloor(p, FloorID.Floor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				env.SetGrass(p, false);
			}
			building.Initialize(world, env);

			building = new BuildingObject(BuildingID.Carpenter) { Area = new IntRect(6, 6, 3, 3), Z = 9 };
			foreach (var p2d in building.Area.Range())
			{
				var p = new IntPoint3D(p2d, building.Z);
				env.SetFloor(p, FloorID.Floor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
				env.SetGrass(p, false);
			}
			building.Initialize(world, env);

			{
				var gen = new Dwarrowdelf.Server.Items.FoodGenerator();
				gen.Initialize(env.World);
				gen.MoveTo(env, new IntPoint3D(10, 10, 9));
			}

			return env;
		}

		private void CreateTrees(Environment env)
		{
			var locations = env.Bounds.Range()
				.Where(p => env.GetInteriorID(p) == InteriorID.Empty)
				.Where(p => env.GetFloorID(p) == FloorID.Floor || env.GetFloorID(p).IsSlope())
				.Where(p => m_random.Next() % 8 == 0);

			foreach (var p in locations)
				env.SetInterior(p, m_random.Next() % 2 == 0 ? InteriorID.Tree : InteriorID.Sapling, MaterialID.Wood);
		}

		private static void CreateSlopes(Environment env)
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

			var locs = from s in env.Bounds.Range()
					   let su = s + Direction.Up
					   where env.Bounds.Contains(su)
					   where env.GetInteriorID(s) == InteriorID.Empty && env.GetFloorID(s) != FloorID.Empty
					   where env.GetInteriorID(su) == InteriorID.Empty && env.GetFloorID(su) == FloorID.Empty
					   from d in DirectionExtensions.CardinalDirections
					   let td = s + d
					   let t = s + d + Direction.Up
					   where env.Bounds.Contains(t)
					   where env.GetInteriorID(td) == InteriorID.Wall
					   where env.GetInteriorID(t) == InteriorID.Empty && env.GetFloorID(t) != FloorID.Empty
					   select new { Location = s, Direction = d };

			foreach (var loc in locs)
			{
				if (DirectionExtensions.CardinalDirections
					.Where(d => env.Bounds.Contains(loc.Location + d))
					.All(d => env.GetInteriorID(loc.Location + d) != InteriorID.Empty))
					continue;

				env.SetFloor(loc.Location, loc.Direction.ToSlope(), env.GetFloorMaterialID(loc.Location));
			}
		}

		void ClearTile(Environment env, IntPoint3D p)
		{
			env.SetFloor(p, FloorID.Empty, MaterialID.Undefined);
			env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
		}

		void ClearInside(Environment env, IntPoint3D p)
		{
			env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
		}

		Environment CreateMap2(World world)
		{
			var env = new Environment(20, 20, 1, VisibilityMode.SimpleFOV);
			env.Initialize(world);

			foreach (var p in env.Bounds.Range())
			{
				env.SetInteriorID(p, InteriorID.Empty);
				env.SetFloor(p, FloorID.Floor, MaterialID.Granite);
			}

			env.SetInterior(m_portalLoc2, InteriorID.Portal, MaterialID.Steel);
			env.SetActionHandler(m_portalLoc2, ActionHandler);

			return env;
		}

		GameColor GetRandomColor()
		{
			return (GameColor)m_random.Next((int)GameColor.NumColors - 1) + 1;
		}
	}
}
