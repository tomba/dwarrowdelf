using Dwarrowdelf;
using Dwarrowdelf.TerrainGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class Map : IEnvironmentObject
	{
		TerrainData m_terrainData;

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public event Action<IntVector3> TileChanged;

		public Map(IntSize3 size)
		{
			this.Size = size;

			//m_terrainData = CreateTerrain(size);
			//m_terrainData = CreateNoiseTerrain(size);
			m_terrainData = CreateBallMap(size, 8);
			//m_terrainData = CreateCubeMap(size, 4);
		}

		public void SetTileData(IntVector3 p, TileData td)
		{
			m_terrainData.SetTileDataNoHeight(p, td);

			if (this.TileChanged != null)
				TileChanged(p);

			// XXX send TileChanged for neighbors, so that their VisibleFaces can be updated.
			// This could be done inside Chunk, but to update the edges of a chunk we need to touch multiple Chunks.
			// But as we don't change the content of the neighbors, and only the face towards the changed tile
			// needs to be changed, there's room for optimization.
			foreach (var v in IntVector3.CardinalUpDownDirections)
			{
				var n = p + v;

				if (!this.Size.Contains(n))
					continue;

				if (this.TileChanged != null)
					TileChanged(n);
			}
		}

		public Direction GetVisibleFaces(IntVector3 p)
		{
			Direction visibleFaces = 0;

			foreach (var dir in DirectionExtensions.CardinalUpDownDirections)
			{
				var n = p + dir;

				if (this.Size.Contains(n) == false)
					continue;

				var td = m_terrainData.GetTileData(n);

				if (td.IsUndefined || td.IsSeeThrough == false)
					continue;

				visibleFaces |= dir;
			}

			return visibleFaces;
		}

		static TerrainData CreateTerrain(IntSize3 size)
		{
			//var random = Helpers.Random;
			var random = new Random(1);

			var terrain = new TerrainData(size);

			var tg = new TerrainGenerator(terrain, random);

			var corners = new DiamondSquare.CornerData()
			{
				NE = 15,
				NW = 10,
				SW = 10,
				SE = 10,
			};

			tg.Generate(corners, 5, 0.75, 2);

			int grassLimit = terrain.Depth * 4 / 5;
			TerrainHelpers.CreateVegetation(terrain, random, grassLimit);

			return terrain;
		}

		static TerrainData CreateNoiseTerrain(IntSize3 size)
		{
			var terrainData = new TerrainData(size);

			var noise = VoxelMapGen.CreateTerrainNoise();
			var noisemap = VoxelMapGen.CreateTerrainNoiseMap(noise, new IntSize2(size.Width, size.Height));

			FillFromNoiseMap(terrainData, noisemap);

			return terrainData;
		}

		static void FillFromNoiseMap(TerrainData terrainData, SharpNoise.NoiseMap noiseMap)
		{
			var max = noiseMap.Data.Max();
			var min = noiseMap.Data.Min();

			Parallel.For(0, noiseMap.Data.Length, i =>
			{
				var v = noiseMap.Data[i];	// [-1 .. 1]

				v -= min;
				v /= (max - min);		// [0 .. 1]

				v *= terrainData.Depth * 8 / 10;
				v += terrainData.Depth * 2 / 10;

				noiseMap.Data[i] = v;
			});

			int waterLimit = terrainData.Depth * 3 / 10;
			int grassLimit = terrainData.Depth * 8 / 10;

			Parallel.For(0, terrainData.Height, y =>
			{
				for (int x = 0; x < terrainData.Width; ++x)
				{
					var v = noiseMap[x, y];

					int iv = (int)v;

					for (int z = terrainData.Depth - 1; z >= 0; --z)
					{
						var p = new IntVector3(x, y, z);

						/* above ground */
						if (z > iv)
						{
							if (z < waterLimit)
								terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData); // XXX water
							else
								terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData);
						}
						/* surface */
						else if (z == iv)
						{
							terrainData.SetTileDataNoHeight(p, TileData.EmptyTileData);

							if (z >= waterLimit && z < grassLimit)
							{
								Dwarrowdelf.MWCRandom r = new MWCRandom(new IntVector3(x, y, z), 0);
								if (r.Next(100) < 30)
								{
									terrainData.SetTileDataNoHeight(p, new TileData()
									{
										ID = TileID.Tree,
										MaterialID = MaterialID.Fir,
									});
								}
								else
								{
									terrainData.SetTileDataNoHeight(p, new TileData()
									{
										ID = TileID.Grass,
										MaterialID = MaterialID.HairGrass,
									});
								}
							}
						}
						/* underground */
						else if (z < iv)
						{
							terrainData.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Granite));
						}
						else
						{
							throw new Exception();
						}
					}
				}
			});
		}

		static TerrainData CreateBallMap(IntSize3 size, int innerSide = 0)
		{
			var map = new TerrainData(size);

			int side = MyMath.Min(size.Width, size.Height, size.Depth);

			int r = side / 2 - 1;
			int ir = innerSide / 2 - 1;

			Parallel.For(0, size.Depth, z =>
			{
				for (int y = 0; y < size.Height; ++y)
					for (int x = 0; x < size.Width; ++x)
					{
						var pr = Math.Sqrt((x - r) * (x - r) + (y - r) * (y - r) + (z - r) * (z - r));

						var p = new IntVector3(x, y, z);

						if (pr < r && pr >= ir)
							map.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Granite));
						else
							map.SetTileDataNoHeight(p, TileData.EmptyTileData);
					}
			});

			return map;
		}

		static TerrainData CreateCubeMap(IntSize3 size, int margin)
		{
			var map = new TerrainData(size);

			Parallel.For(0, size.Depth, z =>
			{
				for (int y = 0; y < size.Height; ++y)
					for (int x = 0; x < size.Width; ++x)
					{
						var p = new IntVector3(x, y, z);

						if (x < margin || y < margin || z < margin ||
							x >= size.Width - margin || y >= size.Height - margin || z >= size.Depth - margin)
							map.SetTileDataNoHeight(p, TileData.EmptyTileData);
						else
							map.SetTileDataNoHeight(p, TileData.GetNaturalWall(MaterialID.Granite));
					}
			});

			return map;
		}


		public VisibilityMode VisibilityMode
		{
			get { throw new NotImplementedException(); }
		}

		public IntSize3 Size
		{
			get;
			private set;
		}

		public bool Contains(IntVector3 p)
		{
			return this.Size.Contains(p);
		}

		public TileID GetTileID(IntVector3 l)
		{
			return m_terrainData.GetTileID(l);
		}

		public MaterialID GetMaterialID(IntVector3 l)
		{
			throw new NotImplementedException();
		}

		public MaterialInfo GetMaterial(IntVector3 l)
		{
			throw new NotImplementedException();
		}

		public TileData GetTileData(IntVector3 l)
		{
			return m_terrainData.GetTileData(l);
		}

		public bool GetTileFlags(IntVector3 l, TileFlags flags)
		{
			throw new NotImplementedException();
		}

		public bool HasContents(IntVector3 pos)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IMovableObject> GetContents(IntVector3 pos)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IMovableObject> GetContents(IntGrid2Z rect)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IMovableObject> Contents
		{
			get { throw new NotImplementedException(); }
		}

		public IWorld World
		{
			get { throw new NotImplementedException(); }
		}

		public bool IsDestructed
		{
			get { throw new NotImplementedException(); }
		}

		public event Action<IBaseObject> Destructed;

		public ObjectID ObjectID
		{
			get { throw new NotImplementedException(); }
		}

		public ObjectType ObjectType
		{
			get { throw new NotImplementedException(); }
		}
	}
}
