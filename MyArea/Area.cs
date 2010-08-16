using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MyGame;
using MyGame.Server;
using Environment = MyGame.Server.Environment;

namespace MyArea
{
	public class Area : IArea
	{
		Environment m_map1;
		Environment m_map2;

		public void InitializeWorld(World world, IList<Environment> environments)
		{
			m_map1 = CreateMap1(world);
			environments.Add(m_map1);

			m_map2 = CreateMap2(world);
			environments.Add(m_map2);
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

		Random m_random = new Random(123);

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
			var terrainGen = new TerrainGen(sizeExp, 10, 5, 0.75);

			var env = new Environment(world, size, size, 20, VisibilityMode.LOS);

			Random r = new Random(123);

			/* create terrain */
			foreach (var p in env.Bounds.Range())
			{
				double d = terrainGen.Grid[p.ToIntPoint()];

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

			for (int i = 0; i < 1; ++i)
			{
				// Add a monster
				var monster = new Living(world, String.Format("monsu{0}", i))
				{
					SymbolID = SymbolID.Monster,
					Color = (GameColor)m_random.Next((int)GameColor.NumColors),
				};

				monster.Actor = new MonsterActor(monster);

				if (monster.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel)) == false)
					throw new Exception();
			}

			// Add items
			for (int i = 0; i < 10; ++i)
			{
				var item = new ItemObject(world)
				{
					SymbolID = SymbolID.Gem,
					Name = "gem" + i.ToString(),
					Color = (GameColor)m_random.Next((int)GameColor.NumColors),
					MaterialID = MaterialID.Diamond,
				};

				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			{
				// Add an item
				var item = new ItemObject(world)
				{
					SymbolID = SymbolID.Gem,
					Name = "red gem",
					Color = GameColor.Red,
					MaterialID = MaterialID.Diamond,
				};
				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));

				item = new ItemObject(world)
				{
					SymbolID = SymbolID.Gem,
					Name = "gem",
					MaterialID = MaterialID.Diamond,
				};
				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			var building = new BuildingObject(world, BuildingID.Smith) { Area = new IntRect(2, 6, 3, 3), Z = 9 };
			foreach (var p2d in building.Area.Range())
			{
				var p = new IntPoint3D(p2d, building.Z);
				env.SetFloor(p, FloorID.Floor, MaterialID.Granite);
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);
			}
			env.AddBuilding(building);

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
				var item = new ItemObject(world)
				{
					SymbolID = SymbolID.Key,
					Name = "water gen",
					Color = GameColor.Red,
					MaterialID = MaterialID.Diamond,
				};

				item.TickEvent += () => env.SetWaterLevel(item.Location, TileData.MaxWaterLevel);
				item.MoveTo(env, new IntPoint3D(3, 17, surfaceLevel));
			}

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
				env.SetFloor(loc.Location, loc.Direction.ToSlope(), env.GetFloorMaterialID(loc.Location));
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
			var env = new Environment(world, 20, 20, 1, VisibilityMode.SimpleFOV);

			foreach (var p in env.Bounds.Range())
			{
				env.SetInteriorID(p, InteriorID.Empty);
				env.SetFloor(p, FloorID.Floor, MaterialID.Granite);
			}

			env.SetInterior(m_portalLoc2, InteriorID.Portal, MaterialID.Steel);
			env.SetActionHandler(m_portalLoc2, ActionHandler);

			return env;
		}
	}
}
