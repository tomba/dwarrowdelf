using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Client3D
{
	class GameMap
	{
		public TileData[, ,] Grid { get; private set; }
		public IntSize3 Size { get; private set; }

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		byte[,] m_levelMap;

		public IntGrid2Z StartLoc { get; private set; }

		public GameMap()
		{
			this.Size = new IntSize3(128, 128, 32);

			string file = Path.Combine(Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath), "map.dat");

			CreateTerrain();
		}

		void CreateTerrain()
		{
			var terrainData = new TerrainData(this.Size);

			//ClearTerrain(terrainData);
			//CreateSlopeTest1(terrainData);
			//CreateSlopeTest2(terrainData);
			CreateRealTerrain(terrainData);
			//CreateBallTerrain(terrainData);

			TileData[, ,] grid;
			terrainData.GetData(out grid, out m_levelMap);
			this.Grid = grid;

			CreateExtraStuff();

			//UndefineNonvisible(terrainData);
		}

		void CreateExtraStuff()
		{
			var sl = FindStartLocation();
			if (sl.HasValue == false)
				throw new Exception();

			this.StartLoc = sl.Value;

			for (int z = 0; z < 10; ++z)
			{
				var p = this.StartLoc.Center + new IntVector3(0, 0, -z);
				SetTerrain(p, new TileData()
				{
					TerrainID = TerrainID.Empty,
					InteriorID = InteriorID.Empty,
				});

				if (z % 2 != 0)
				{
					for (int x = 0; x < 10; ++x)
					{
						var p2 = p + new IntVector3(1, 0, 0) * x;

						SetTerrain(p2, new TileData()
						{
							TerrainID = TerrainID.NaturalFloor,
							InteriorID = InteriorID.Empty,
						});
					}
				}
			}

			for (int y = 0; y < 10; ++y)
			{
				var p = this.StartLoc.Center + new IntVector3(0, y, 0);
				SetTerrain(p, new TileData()
				{
					TerrainID = TerrainID.NaturalFloor,
					InteriorID = InteriorID.Empty,
				});
			}

			for (int x = 0; x < 10; ++x)
			{
				var p = this.StartLoc.Center + new IntVector3(-x, 8, 0);
				SetTerrain(p, new TileData()
				{
					TerrainID = TerrainID.NaturalFloor,
					InteriorID = InteriorID.Empty,
				});
			}

			{
				var p = this.StartLoc.Center + new IntVector3(0, 9, 0);
				SetTerrain(p, new TileData()
				{
					TerrainID = TerrainID.Empty,
					InteriorID = InteriorID.Empty,
				});
			}

			for (int y = 0; y < 5; ++y)
			{
				var p = this.StartLoc.Center + new IntVector3(0, 9 + y, -1);
				SetTerrain(p, new TileData()
				{
					TerrainID = TerrainID.NaturalFloor,
					InteriorID = InteriorID.Empty,
				});
			}
		}

		void SetTerrain(IntVector3 p, TileData td)
		{
			this.Grid[p.Z, p.Y, p.X] = td;

			foreach (var np in DirectionSet.Planar.ToSurroundingPoints(p))
			{
				if (GetTerrainID(np) == TerrainID.Slope)
				{
					this.Grid[np.Z, np.Y, np.X] = new TileData()
					{
						TerrainID = TerrainID.NaturalFloor,
						InteriorID = InteriorID.Empty,
					};
				}
			}
		}

		static void ClearTerrain(TerrainData terrainData)
		{
			TileData[, ,] grid;
			byte[,] levelMap;
			terrainData.GetData(out grid, out levelMap);

			for (int z = 0; z < terrainData.Depth; ++z)
				for (int y = 0; y < terrainData.Height; ++y)
					for (int x = 0; x < terrainData.Width; ++x)
						grid[z, y, x] = TileData.EmptyTileData;
		}

		static void CreateSlopeTest1(TerrainData terrainData)
		{
			TileData[, ,] grid;
			byte[,] levelMap;
			terrainData.GetData(out grid, out levelMap);

			grid[10, 10, 10].InteriorID = InteriorID.NaturalWall;
			grid[10, 10, 9].TerrainID = TerrainID.Slope;
			grid[10, 10, 11].TerrainID = TerrainID.Slope;
			grid[10, 9, 10].TerrainID = TerrainID.Slope;
			grid[10, 11, 10].TerrainID = TerrainID.Slope;

			grid[10, 9, 9].TerrainID = TerrainID.Slope;
			grid[10, 11, 11].TerrainID = TerrainID.Slope;
			grid[10, 9, 11].TerrainID = TerrainID.Slope;
			grid[10, 11, 9].TerrainID = TerrainID.Slope;
		}

		static void CreateSlopeTest2(TerrainData terrainData)
		{
			TileData[, ,] grid;
			byte[,] levelMap;
			terrainData.GetData(out grid, out levelMap);

			grid[10, 10, 10].InteriorID = InteriorID.NaturalWall;
			grid[10, 10, 11].InteriorID = InteriorID.NaturalWall;
			grid[10, 11, 11].InteriorID = InteriorID.NaturalWall;
			grid[10, 12, 11].InteriorID = InteriorID.NaturalWall;
			grid[10, 11, 10].TerrainID = TerrainID.Slope;
			grid[10, 12, 10].TerrainID = TerrainID.Slope;
		}

		static void CreateRealTerrain(TerrainData terrainData)
		{
			var tg = new Dwarrowdelf.TerrainGen.TerrainGenerator(terrainData, new Random(1));

			var corners = new Dwarrowdelf.TerrainGen.DiamondSquare.CornerData()
			{
				NE = 15,
				NW = 10,
				SW = 10,
				SE = 10,
			};

			tg.Generate(corners, 5, 0.75, 2);

			int grassLimit = terrainData.Depth * 4 / 5;
			TerrainHelpers.CreateVegetation(terrainData, new Random(1), grassLimit);
		}

		static void CreateBallTerrain(TerrainData terrainData)
		{
			TileData[, ,] grid;
			byte[,] levelMap;
			terrainData.GetData(out grid, out levelMap);

			for (int z = 0; z < terrainData.Depth; ++z)
				for (int y = 0; y < terrainData.Height; ++y)
					for (int x = 0; x < terrainData.Width; ++x)
					{
						int r = terrainData.Width / 2 - 1;
						if (Math.Sqrt((x - r) * (x - r) + (y - r) * (y - r) + (z - r) * (z - r)) < r)
						{
							grid[z, y, x].TerrainID = TerrainID.NaturalFloor;
							grid[z, y, x].InteriorID = InteriorID.NaturalWall;
						}
						else
						{
							grid[z, y, x].TerrainID = TerrainID.Empty;
							grid[z, y, x].InteriorID = InteriorID.Empty;
						}
					}
		}

		void UndefineNonvisible(TerrainData terrainData)
		{
			var bounds = terrainData.Size;

			var visibilityArray = new bool[bounds.Depth, bounds.Height, bounds.Width];

			for (int z = bounds.Depth - 1; z >= 0; --z)
			{
				bool lvlIsHidden = true;

				Parallel.For(0, bounds.Height, y =>
				{
					for (int x = 0; x < bounds.Width; ++x)
					{
						var p = new IntVector3(x, y, z);

						var vis = terrainData.GetTileData(p).IsSeeThrough || CanBeSeen(terrainData, p);

						if (vis)
						{
							lvlIsHidden = false;
							visibilityArray[p.Z, p.Y, p.X] = true;
						}
					}
				});

				// if the whole level is not visible, the levels below cannot be seen either
				if (lvlIsHidden)
					break;
			}

			for (int z = bounds.Depth - 1; z >= 0; --z)
			{
				Parallel.For(0, bounds.Height, y =>
				{
					for (int x = 0; x < bounds.Width; ++x)
					{
						if (visibilityArray[z, y, x] == false)
						{
							var p = new IntVector3(x, y, z);
							terrainData.SetTileData(p, TileData.UndefinedTileData);
						}
					}
				});
			}
		}

		static bool CanBeSeen(TerrainData terrainData, IntVector3 location)
		{
			foreach (var d in DirectionExtensions.PlanarDirections)
			{
				var p = location + d;
				if (terrainData.Contains(p) && terrainData.GetTileData(p).IsSeeThrough)
					return true;
			}

			var pu = location.Up;
			if (terrainData.Contains(pu) && terrainData.GetTileData(pu).IsSeeThroughDown)
				return true;

			return false;
		}

		public int GetSurfaceLevel(IntVector2 p)
		{
			return m_levelMap[p.Y, p.X];
		}

		IntGrid2Z? FindStartLocation()
		{
			const int size = 1;

			var center = this.Size.Plane.Center;

			foreach (var p in IntVector2.SquareSpiral(center, this.Width / 2))
			{
				if (this.Size.Plane.Contains(p) == false)
					continue;

				var z = this.GetSurfaceLevel(p);

				var r = new IntGrid2Z(p.X - size, p.Y - size, size * 2, size * 2, z);

				if (TestStartArea(r))
					return r;
			}

			return null;
		}

		bool TestStartArea(IntGrid2Z r)
		{
			foreach (var p in r.Range())
			{
				var terrainID = this.GetTerrainID(p);
				var interiorID = this.GetInteriorID(p);

				if ((terrainID == TerrainID.NaturalFloor || terrainID == TerrainID.Slope) &&
					interiorID.IsClear())
					continue;
				else
					return false;
			}

			return true;
		}

		public TerrainID GetTerrainID(IntVector3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].TerrainID;
		}

		public InteriorID GetInteriorID(IntVector3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].InteriorID;
		}
	}
}
