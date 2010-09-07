using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGame;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace PerfTest
{
	interface ITest
	{
		void DoTest(int loops);
	}

	class Program
	{
		public const int LOOPS = 100;
		public const int WIDTH = 512;
		public const int HEIGHT = 512;
		public const int DEPTH = 16;

		// best 464
		// normal 7000

		static void Main(string[] args)
		{
			Console.WriteLine("Array size {0}", WIDTH * HEIGHT * DEPTH * Marshal.SizeOf(typeof(TileData)));


			//Run(new ArrayTestSingle());
			//Run(new ArrayTestMulti());

			Run(new Test1()); // 8
			Run(new Test2()); // 12
			Run(new Test3());
			//Run(new ShortTest4());



			//Console.WriteLine("Done. Press enter to quit.");
			//Console.ReadLine();
		}

		static void Run(ITest test)
		{
			var sw = new Stopwatch();

			GC.Collect();
			GC.WaitForPendingFinalizers();

			test.DoTest(1);

			var c1 = GC.CollectionCount(0);
			var c2 = GC.CollectionCount(1);
			var c3 = GC.CollectionCount(2);

			sw.Start();
			test.DoTest(Program.LOOPS);
			sw.Stop();

			c1 = GC.CollectionCount(0) - c1;
			c2 = GC.CollectionCount(1) - c2;
			c3 = GC.CollectionCount(2) - c3;

			GC.Collect();

			Console.WriteLine("{0}: {1} ms, {2} ticks, {3}/{4}/{5}",
				test.GetType().Name, sw.ElapsedMilliseconds, sw.ElapsedTicks,
				c1, c2, c3);
		}
	}

	class ArrayTestSingle : ITest
	{
		TileData[] m_grid;
		int m_width;
		int m_height;
		int m_depth;

		public ArrayTestSingle()
		{
			m_width = Program.WIDTH;
			m_height = Program.HEIGHT;
			m_depth = Program.DEPTH;
			m_grid = new TileData[m_width * m_height * m_depth];
		}

		public void DoTest(int loops)
		{
			int w, h, d;
			w = m_width;
			h = m_height;
			d = m_depth;

			while (loops-- > 0)
			{
					for (int z = 0; z < d; ++z)
				{
					for (int y = 0; y < h; ++y)
					{
						for (int x = 0; x < w; ++x)
						{
							int idx = x + y * m_width + z * m_width * m_height;
							var wl = m_grid[idx].WaterLevel;
							wl += 10;
							m_grid[idx].WaterLevel = wl;
						}
					}
				}
			}
		}
	}

	class ArrayTestMulti : ITest
	{
		TileData[,,] m_grid;
		int m_width;
		int m_height;
		int m_depth;

		public ArrayTestMulti()
		{
			m_width = Program.WIDTH;
			m_height = Program.HEIGHT;
			m_depth = Program.DEPTH;
			m_grid = new TileData[m_width, m_height, m_depth];
		}

		public void DoTest(int loops)
		{
			while (loops-- > 0)
			{
				for (int x = 0; x < m_width; ++x)
				{
					for (int y = 0; y < m_height; ++y)
					{
						for (int z = 0; z < m_depth; ++z)
						{
							var wl = m_grid[x, y, z].WaterLevel;
							wl += 10;
							m_grid[x, y, z].WaterLevel = wl;
						}
					}
				}
			}
		}
	}


	class GridTestBase : ITest
	{
		protected TileGrid m_grid;

		public GridTestBase()
		{
			m_grid = new TileGrid(Program.WIDTH, Program.HEIGHT, Program.DEPTH);
		}

		public virtual void DoTest(int loops)
		{
		}
	}

	class ShortGridTestBase : ITest
	{
		protected ShortTileGrid m_grid;

		public ShortGridTestBase()
		{
			m_grid = new ShortTileGrid(Program.WIDTH, Program.HEIGHT, Program.DEPTH);
		}

		public virtual void DoTest(int loops)
		{
		}
	}

	class Test1 : GridTestBase
	{
		public override void DoTest(int loops)
		{
			while (loops-- > 0)
			{
				for (int z = 0; z < m_grid.Depth; ++z)
				{
					for (int y = 0; y < m_grid.Height; ++y)
					{
						for (int x = 0; x < m_grid.Width; ++x)
						{
							var p = new IntPoint3D(x, y, z);
							var wl = m_grid.GetWaterLevel(p);
							wl += 10;
							m_grid.SetWaterLevel(p, (byte)wl);
						}
					}
				}
			}
		}
	}

	class Test2 : GridTestBase
	{
		public override void DoTest(int loops)
		{
			while (loops-- > 0)
			{
				foreach (var p in m_grid.Bounds.Range())
				{
					var wl = m_grid.GetWaterLevel(p);
					wl += 10;
					m_grid.SetWaterLevel(p, wl);
				}
			}
		}
	}
	
	class Test3 : GridTestBase
	{
		public override void DoTest(int loops)
		{
			int w = m_grid.Width;
			int h = m_grid.Height;
			int d = m_grid.Depth;

			var grid = m_grid.Grid;

			while (loops-- > 0)
			{
				for (int z = 0; z < d; ++z)
				{
					for (int y = 0; y < h; ++y)
					{
						for (int x = 0; x < w; ++x)
						{
							var wl = grid[z, y, x].WaterLevel;
							wl += 10;
							grid[z, y, x].WaterLevel = wl;
						}
					}
				}
			}
		}
	}

	class ShortTest4 : ShortGridTestBase
	{
		public override void DoTest(int loops)
		{
			while (loops-- > 0)
			{
				for (short x = 0; x < m_grid.Width; ++x)
				{
					for (short y = 0; y < m_grid.Height; ++y)
					{
						for (short z = 0; z < m_grid.Depth; ++z)
						{	
							//var p = new ShortPoint3D(x, y, z);
							//int idx = x + y * w + z * w * h; //m_grid.GetIndex2(p);
							var wl = m_grid.GetWaterLevel(x, y, z);
							wl += 10;
							m_grid.SetWaterLevel(x, y, z, wl);
						}
					}
				}
			}
		}
	}


	class TileGrid
	{
		public readonly int Width;
		public readonly int Height;
		public readonly int Depth;
		public readonly TileData[, ,] Grid;

		public TileGrid(int width, int height, int depth)
		{
			this.Width = width;
			this.Height = height;
			this.Depth = depth;
			Grid = new TileData[depth, height, width];
		}

		public void SetWaterLevel(IntPoint3D p, byte waterLevel)
		{
			Grid[p.Z, p.Y, p.X].WaterLevel = waterLevel;
		}

		public byte GetWaterLevel(IntPoint3D p)
		{
			return Grid[p.Z, p.Y, p.X].WaterLevel;
		}

		public IntCuboid Bounds { get { return new IntCuboid(0, 0, 0, Width, Height, Depth); } }
	}

	class ShortTileGrid : ShortGrid3DBase<TileData>
	{
		public ShortTileGrid(int width, int height, int depth)
			: base(width, height, depth)
		{
		}

		public void SetWaterLevel(ShortPoint3D p, byte waterLevel)
		{
			base.Grid[p.X, p.Y, p.Z].WaterLevel = waterLevel;
		}

		public byte GetWaterLevel(ShortPoint3D p)
		{
			return base.Grid[p.X, p.Y, p.Z].WaterLevel;
		}

		public void SetWaterLevel(short x, short y, short z, byte waterLevel)
		{
			base.Grid[x, y, z].WaterLevel = waterLevel;
		}

		public byte GetWaterLevel(short x, short y, short z)
		{
			return base.Grid[x, y, z].WaterLevel;
		}
	}
}
