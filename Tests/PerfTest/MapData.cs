using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Dwarrowdelf;

namespace PerfTest
{
	class MapDataTestSuite : TestSuite
	{
		IntSize3 m_mapSize = new IntSize3(256, 256, 256);

		const int CHUNK_SHIFT_X = 5;
		const int CHUNK_SHIFT_Y = 5;
		const int CHUNK_SHIFT_Z = 5;

		const int CHUNK_WIDTH = 1 << CHUNK_SHIFT_X;
		const int CHUNK_HEIGHT = 1 << CHUNK_SHIFT_Y;
		const int CHUNK_DEPTH = 1 << CHUNK_SHIFT_Z;

		const int CHUNK_MASK_X = (1 << CHUNK_SHIFT_X) - 1;
		const int CHUNK_MASK_Y = (1 << CHUNK_SHIFT_Y) - 1;
		const int CHUNK_MASK_Z = (1 << CHUNK_SHIFT_Z) - 1;

		public override void DoTests()
		{
			RunTest(new MapChunkIterateM0Test(m_mapSize));
			//RunTest(new Map3DIterateM0Test(m_mapSize));
			RunTest(new Map1DIterateM0Test(m_mapSize));

			//RunTest(new Map3DIterateM1Test(m_mapSize));
			//RunTest(new Map1DIterateM1Test(m_mapSize));

		}

		class MapChunkIterateM0Test : MapChunkTestBase
		{
			public MapChunkIterateM0Test(IntSize3 size)
				: base(size)
			{
			}

			public override void DoTest(int loops)
			{
				//Console.Write("enter"); Console.ReadLine();

				int w = this.Size.Width;
				int h = this.Size.Height;
				int d = this.Size.Depth;

				ulong tot = 0;

#if asd
				for (int loop = 0; loop < loops; ++loop)
				{
					foreach (TileData td in GetRangeAll())
						tot += td.Raw;
				}
#else
				int cw = this.Size.Width >> CHUNK_SHIFT_X;
				int ch = this.Size.Height >> CHUNK_SHIFT_Y;
				int cd = this.Size.Depth >> CHUNK_SHIFT_Z;

				for (int loop = 0; loop < loops; ++loop)
				{
					for (int cz = 0; cz < cd; ++cz)
						for (int cy = 0; cy < ch; ++cy)
							for (int cx = 0; cx < cw; ++cx)
							{
								Chunk chunk = GetChunk(cx, cy, cz);

								if (chunk == null)
									continue;

#if !asd
								for (int z = 0; z < CHUNK_DEPTH; ++z)
									for (int y = 0; y < CHUNK_HEIGHT; ++y)
										for (int x = 0; x < CHUNK_WIDTH; ++x)
											tot += chunk.GetTileData(x, y, z).Raw;
#else
								for (int idx = 0; idx < CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH; ++idx)
									tot += chunk.GetTileData(idx).Raw;
#endif
							}
				}
#endif
				GC.KeepAlive(tot);
			}
		}

		class Map3DIterateM0Test : Map3DTestBase
		{
			public Map3DIterateM0Test(IntSize3 size)
				: base(size)
			{
			}

			public override void DoTest(int loops)
			{
				var grid = m_map.Grid;
				int w = m_map.Size.Width;
				int h = m_map.Size.Height;
				int d = m_map.Size.Depth;

				ulong tot = 0;

				for (int loop = 0; loop < loops; ++loop)
				{
					for (int z = 0; z < d; ++z)
						for (int y = 0; y < h; ++y)
							for (int x = 0; x < w; ++x)
								tot += grid[z, y, x].Raw;
				}

				GC.KeepAlive(tot);
			}
		}

		class Map1DIterateM0Test : Map1DTestBase
		{
			public Map1DIterateM0Test(IntSize3 size)
				: base(size)
			{
			}

			public override void DoTest(int loops)
			{
				int w = m_map.Size.Width;
				int h = m_map.Size.Height;
				int d = m_map.Size.Depth;

				ulong tot = 0;

				for (int loop = 0; loop < loops; ++loop)
				{
#if asd
					foreach (TileData td in GetRangeAll())
						tot += td.Raw;
#else
					for (int z = 0; z < d; ++z)
						for (int y = 0; y < h; ++y)
							for (int x = 0; x < w; ++x)
								tot += m_map.GetTileData(x, y, z).Raw;
#endif
				}

				GC.KeepAlive(tot);
			}
		}

		class Map3DIterateM1Test : Map3DTestBase
		{
			public Map3DIterateM1Test(IntSize3 size)
				: base(size)
			{
			}

			public override void DoTest(int loops)
			{
				var grid = m_map.Grid;
				int w = m_map.Size.Width;
				int h = m_map.Size.Height;
				int d = m_map.Size.Depth;

				ulong tot = 0;

				for (int loop = 0; loop < loops; ++loop)
				{
					for (int z = 0; z < d; ++z)
						for (int y = 0; y < h; ++y)
							for (int x = 0; x < w; ++x)
							{
								tot += grid[z, y, x].Raw;

								if (x > 0)
									tot += grid[z, y, x - 1].Raw;

								if (x < w - 1)
									tot += grid[z, y, x + 1].Raw;

								if (y > 0)
									tot += grid[z, y - 1, x].Raw;

								if (y < h - 1)
									tot += grid[z, y + 1, x].Raw;

								if (z > 0)
									tot += grid[z - 1, y, x].Raw;

								if (z < d - 1)
									tot += grid[z + 1, y, x].Raw;
							}
				}

				GC.KeepAlive(tot);
			}
		}

		class Map1DIterateM1Test : Map1DTestBase
		{
			public Map1DIterateM1Test(IntSize3 size)
				: base(size)
			{
			}

			public override void DoTest(int loops)
			{
				int w = m_map.Size.Width;
				int h = m_map.Size.Height;
				int d = m_map.Size.Depth;

				ulong tot = 0;

				for (int loop = 0; loop < loops; ++loop)
				{
					for (int z = 0; z < d; ++z)
						for (int y = 0; y < h; ++y)
							for (int x = 0; x < w; ++x)
							{
								tot += m_map.GetTileData(m_map.GetIndex(x, y, z)).Raw;

								if (x > 0)
									tot += m_map.GetTileData(m_map.GetIndex(x - 1, y, z)).Raw;

								if (x < w - 1)
									tot += m_map.GetTileData(m_map.GetIndex(x + 1, y, z)).Raw;

								if (y > 0)
									tot += m_map.GetTileData(m_map.GetIndex(x, y - 1, z)).Raw;

								if (y < h - 1)
									tot += m_map.GetTileData(m_map.GetIndex(x, y + 1, z)).Raw;

								if (z > 0)
									tot += m_map.GetTileData(m_map.GetIndex(x, y, z - 1)).Raw;

								if (z < d - 1)
									tot += m_map.GetTileData(m_map.GetIndex(x, y, z + 1)).Raw;
							}
				}

				GC.KeepAlive(tot);
			}
		}

		abstract class Map3DTestBase : ITest
		{
			protected Map3D m_map;

			protected Map3DTestBase(IntSize3 size)
			{
				m_map = new Map3D(size);

			}

			protected class Map3D
			{
				public IntSize3 Size;
				public TileData[, ,] Grid;

				public Map3D(IntSize3 size)
				{
					this.Size = size;
					this.Grid = new TileData[size.Depth, size.Height, size.Width];
				}

			}

			public abstract void DoTest(int loops);
		}

		abstract class Map1DTestBase : ITest
		{
			protected Map1D m_map;

			protected Map1DTestBase(IntSize3 size)
			{
				m_map = new Map1D(size);

			}

			protected IEnumerable<TileData> GetRangeAll()
			{
				int w = m_map.Size.Width;
				int h = m_map.Size.Height;
				int d = m_map.Size.Depth;

				for (int z = 0; z < d; ++z)
					for (int y = 0; y < h; ++y)
						for (int x = 0; x < w; ++x)
							yield return m_map.GetTileData(x, y, z);
			}

			protected class Map1D
			{
				public IntSize3 Size;
				TileData[] Grid;

				public Map1D(IntSize3 size)
				{
					this.Size = size;
					this.Grid = new TileData[size.Depth * size.Height * size.Width];
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public TileData GetTileData(int x, int y, int z)
				{
					int idx = GetIndex(x, y, z);
					return GetTileData(idx);
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public TileData GetTileData(int idx)
				{
					return this.Grid[idx];
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public int GetIndex(int x, int y, int z)
				{
					return x + y * this.Size.Width + z * this.Size.Width * this.Size.Height;
				}
			}

			public abstract void DoTest(int loops);
		}

		abstract class MapChunkTestBase : ITest
		{
			//protected Dictionary<uint, Map1D> m_dict;
			protected Chunk[] m_arr;
			public IntSize3 Size;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			uint expand(int _x)
			{
				uint x = (uint)_x;
				x &= 0x3FF;
				x = (x | (x << 16)) & 4278190335;
				x = (x | (x << 8)) & 251719695;
				x = (x | (x << 4)) & 3272356035;
				x = (x | (x << 2)) & 1227133513;
				return x;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			protected uint hashCode(int i, int j, int k)
			{
				//int cw = this.Size.Width >> CHUNK_SHIFT_X;
				//int ch = this.Size.Height >> CHUNK_SHIFT_Y;
				//return (uint)(i + j * cw + k * cw * ch);
				return expand(i) + (expand(j) << 1) + (expand(k) << 2);
			}

			protected MapChunkTestBase(IntSize3 size)
			{
				this.Size = size;
				//m_dict = new Dictionary<uint, Map1D>();

				int cw = size.Width >> CHUNK_SHIFT_X;
				int ch = size.Height >> CHUNK_SHIFT_Y;
				int cd = size.Depth >> CHUNK_SHIFT_Z;

				uint max = 0;

				m_arr = new Chunk[512];

				for (int cz = 0; cz < cd; ++cz)
					for (int cy = 0; cy < ch; ++cy)
						for (int cx = 0; cx < cw; ++cx)
						{
							uint hash = hashCode(cx, cy, cz);
							max = Math.Max(max, hash);

							if (cx % 2 == 0)
								//if (cz > cd / 2)
								m_arr[hash] = new Chunk();
						}

				Console.WriteLine("max {0}", max);
			}

			protected Chunk GetChunk(int cx, int cy, int cz)
			{
				uint hash = hashCode(cx, cy, cz);
				return m_arr[hash];
			}

			protected IEnumerable<TileData> GetRangeAll()
			{
				int cw = this.Size.Width >> CHUNK_SHIFT_X;
				int ch = this.Size.Height >> CHUNK_SHIFT_Y;
				int cd = this.Size.Depth >> CHUNK_SHIFT_Z;

				for (int cz = 0; cz < cd; ++cz)
					for (int cy = 0; cy < ch; ++cy)
						for (int cx = 0; cx < cw; ++cx)
						{
							Chunk chunk = GetChunk(cx, cy, cz);

							if (chunk == null)
								continue;

#if asd
							for (int z = 0; z < CHUNK_DEPTH; ++z)
								for (int y = 0; y < CHUNK_HEIGHT; ++y)
									for (int x = 0; x < CHUNK_WIDTH; ++x)
									{
										//if (m == null)
										//{
										//	yield return TileData.EmptyTileData;
										//}
										//else
										{
											yield return chunk.GetTileData(x, y, z);
										}
									}
#else
							for (int idx = 0; idx < CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH; ++idx)
								yield return chunk.GetTileData(idx);
#endif
						}
			}
			/*
			protected TileData GetTileData(int x, int y, int z)
			{
				int cx = x >> CHUNK_SHIFT_X;
				int cy = y >> CHUNK_SHIFT_Y;
				int cz = z >> CHUNK_SHIFT_Z;

				int hash = cx | (cy << 10) | (cz << 10);

				Map1D m;

				if (m_dict.TryGetValue(hash, out m) == false)
					return TileData.EmptyTileData;

				int ix = x & CHUNK_MASK_X;
				int iy = y & CHUNK_MASK_Y;
				int iz = z & CHUNK_MASK_Z;

				int idx = m.GetIndex(ix, iy, iz);

				return m.Grid[idx];
			}*/

			protected class Chunk
			{
				TileData[] m_grid;

				public Chunk()
				{
					m_grid = new TileData[CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH];
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public TileData GetTileData(int x, int y, int z)
				{
					int idx = GetIndex(x, y, z);
					return GetTileData(idx);
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public TileData GetTileData(int idx)
				{
					return m_grid[idx];
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public int GetIndex(int x, int y, int z)
				{
					return x + y * CHUNK_WIDTH + z * CHUNK_WIDTH * CHUNK_HEIGHT;
				}
			}

			public abstract void DoTest(int loops);
		}

	}
}
