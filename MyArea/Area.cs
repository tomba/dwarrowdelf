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

		Environment CreateMap1(World world)
		{
			int sizeExp = 7;
			int size = (int)Math.Pow(2, sizeExp);
			var terrainGen = new TerrainGen(sizeExp, 10, 5, 0.75);


			// XXX some size limit with the size in WCF
			var env = new Environment(world, size, size, 20, VisibilityMode.LOS);
			env.Name = "map1";

			Random r = new Random(123);

			var stone = Materials.Stone.ID;
			var steel = Materials.Steel.ID;
			var diamond = Materials.Diamond.ID;
			var wood = Materials.Wood.ID;

			/* create terrain */
			foreach (var p in env.Bounds.Range())
			{
				double d = terrainGen.Grid[p.ToIntPoint()];

				if (d > p.Z)
				{
					env.SetInterior(p, InteriorID.NaturalWall, stone);
					env.SetFloor(p, FloorID.NaturalFloor, stone);
				}
				else
				{
					env.SetInterior(p, InteriorID.Empty, Materials.Undefined.ID);
					if (env.GetInteriorID(p + Direction.Down) != InteriorID.Empty)
						env.SetFloor(p, FloorID.NaturalFloor, stone);
					else
						env.SetFloor(p, FloorID.Empty, Materials.Undefined.ID);
				}
			}

			/* create slopes */
			foreach (var p in env.Bounds.Range())
			{
				if (!env.Bounds.Contains(p + Direction.Up))
					continue;

				bool canHaveSlope = env.GetInteriorID(p) == InteriorID.Empty && env.GetFloorID(p) == FloorID.NaturalFloor &&
					env.GetInteriorID(p + Direction.Up) == InteriorID.Empty && env.GetFloorID(p + Direction.Up) == FloorID.Empty;

				if (!canHaveSlope)
					continue;

				foreach (var dir in DirectionExtensions.GetCardinalDirections())
				{
					if (!env.Bounds.Contains(p + dir))
						continue;

					canHaveSlope = env.GetInteriorID(p + dir) == InteriorID.NaturalWall && env.GetInteriorID(p + dir + Direction.Up) == InteriorID.Empty &&
						env.GetFloorID(p + dir + Direction.Up) == FloorID.NaturalFloor;

					if (canHaveSlope)
					{
						var slope = Interiors.GetSlopeFromDir(dir);
						env.SetInterior(p, slope, env.GetFloorMaterialID(p));
					}
				}
			}

			/* create trees */
			foreach (var p in env.Bounds.Range())
			{
				if (env.GetInteriorID(p) == InteriorID.Empty && env.GetFloorID(p) == FloorID.NaturalFloor)
				{
					if (m_random.Next() % 8 != 0)
						continue;

					env.SetInterior(p, m_random.Next() % 2 == 0 ? InteriorID.Tree : InteriorID.Sapling, MaterialID.Wood);
				}
			}

			/* create grass */
			foreach (var p in env.Bounds.Range())
			{
				if (env.GetInteriorID(p) == InteriorID.Empty && env.GetFloorID(p) == FloorID.NaturalFloor)
					env.SetInterior(p, InteriorID.Grass, MaterialID.Grass);
			}


			/* create long stairs */
			foreach (var p in env.Bounds.Range())
			{
				if (p.X == 1 && p.Y == 4)
				{
					env.SetInterior(p, InteriorID.Stairs, stone);
					env.SetFloor(p, FloorID.Hole, stone);
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
			env.SetInterior(m_portalLoc, InteriorID.Portal, steel);
			env.SetActionHandler(m_portalLoc, ActionHandler);

			for (int i = 0; i < 1; ++i)
			{
				// Add a monster
				var monster = new Living(world);
				monster.SymbolID = SymbolID.Monster;
				monster.Name = String.Format("monsu{0}", i);
				if (monster.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel)) == false)
					throw new Exception();
				var monsterAI = new MonsterActor(monster);
				monster.Actor = monsterAI;
				monster.Color = new GameColor((byte)m_random.Next(256), (byte)m_random.Next(256), (byte)m_random.Next(256));
			}

			// Add items
			for (int i = 0; i < 10; ++i)
			{
				var item = new ItemObject(world)
				{
					SymbolID = SymbolID.Gem,
					Name = "gem" + i.ToString(),
					Color = new GameColor((byte)m_random.Next(256), (byte)m_random.Next(256), (byte)m_random.Next(256)),
					MaterialID = diamond,
				};

				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			{
				// Add an item
				var item = new ItemObject(world)
				{
					SymbolID = SymbolID.Gem,
					Name = "red gem",
					Color = GameColors.Red,
					MaterialID = diamond,
				};
				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));

				item = new ItemObject(world)
				{
					SymbolID = SymbolID.Gem,
					Name = "gem",
					MaterialID = diamond,
				};
				item.MoveTo(env, GetRandomSurfaceLocation(env, surfaceLevel));
			}

			var building = new BuildingData(world, BuildingID.Smith) { Area = new IntRect(2, 4, 2, 2), Z = 9 };
			env.AddBuilding(building);

			return env;
		}

		Environment CreateMap2(World world)
		{
			var env = new Environment(world, 20, 20, 1, VisibilityMode.SimpleFOV);
			env.Name = "map2";

			var stone = Materials.Stone.ID;
			var steel = Materials.Steel.ID;

			foreach (var p in env.Bounds.Range())
			{
				env.SetInteriorID(p, InteriorID.Empty);
				env.SetFloor(p, FloorID.NaturalFloor, stone);
			}

			env.SetInterior(m_portalLoc2, InteriorID.Portal, steel);
			env.SetActionHandler(m_portalLoc2, ActionHandler);

			return env;
		}
	}
}
