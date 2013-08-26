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
		IntSize3 m_mapSize = new IntSize3(256, 256, 32);

		public override void DoTests()
		{
			//RunTest(new Map3DIterateM0Test(m_mapSize));
			//RunTest(new Map1DIterateM0Test(m_mapSize));

			RunTest(new Map3DIterateM1Test(m_mapSize));
			RunTest(new Map1DIterateM1Test(m_mapSize));

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
								tot += grid[m_map.GetIndex(x, y, z)].Raw;
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
								tot += grid[m_map.GetIndex(x, y, z)].Raw;

								if (x > 0)
									tot += grid[m_map.GetIndex(x - 1, y, z)].Raw;

								if (x < w - 1)
									tot += grid[m_map.GetIndex(x + 1, y, z)].Raw;

								if (y > 0)
									tot += grid[m_map.GetIndex(x, y - 1, z)].Raw;

								if (y < h - 1)
									tot += grid[m_map.GetIndex(x, y + 1, z)].Raw;

								if (z > 0)
									tot += grid[m_map.GetIndex(x, y, z - 1)].Raw;

								if (z < d - 1)
									tot += grid[m_map.GetIndex(x, y, z + 1)].Raw;
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

			protected class Map1D
			{
				public IntSize3 Size;
				public TileData[] Grid;

				public Map1D(IntSize3 size)
				{
					this.Size = size;
					this.Grid = new TileData[size.Depth * size.Height * size.Width];
				}

				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				public int GetIndex(int x, int y, int z)
				{
					return x + y * this.Size.Width + z * this.Size.Width * this.Size.Height;
				}
			}

			public abstract void DoTest(int loops);
		}
	}
}
