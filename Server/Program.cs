using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.IO;
using System.Diagnostics;


namespace MyGame
{
	class ServerLauncher
	{
		struct TileData
		{
			public int m_terrainID;
			public List<ServerGameObject> m_contentList;
		}

		class Grid2DBase<T>
		{
			public int Width { get; private set; }
			public int Height { get; private set; }

			public Grid2DBase(int width, int height)
			{
				this.Width = width;
				this.Height = height;
				this.Grid = new T[width * height];
			}

			protected T[] Grid { get; private set; }

			protected int GetIndex(IntPoint p)
			{
				return p.Y * this.Width + p.X;
			}
		}

		class MyGrid : Grid2DBase<TileData>
		{
			public MyGrid(int width, int height)
				: base(width, height)
			{
			}

			public void SetTerrainType(IntPoint l, int terrainType)
			{
				base.Grid[GetIndex(l)].m_terrainID = terrainType;
			}
		}

		class TileGrid
		{
			TileData[] m_tileGrid;

			public int Width { get; private set; }
			public int Height { get; private set; }

			int GetIndex(IntPoint p)
			{
				return p.Y * this.Width + p.X;
			}

			public TileGrid(int width, int height)
			{
				this.Width = width;
				this.Height = height;
				m_tileGrid = new TileData[width * height];
			}

			public void SetTerrainType(IntPoint l, int terrainType)
			{
				m_tileGrid[GetIndex(l)].m_terrainID = terrainType;
			}

			public int GetTerrainID(IntPoint l)
			{
				return m_tileGrid[GetIndex(l)].m_terrainID;
			}

			public List<ServerGameObject> GetContentList(IntPoint l)
			{
				return m_tileGrid[GetIndex(l)].m_contentList;
			}

			public void SetContentList(IntPoint l, List<ServerGameObject> list)
			{
				m_tileGrid[GetIndex(l)].m_contentList = list;
			}

		}

		static void Test()
		{
			Stopwatch sw = new Stopwatch();

			var grid = new MyGrid(4096*4, 4096*2);

			sw.Start();
			for (int y = 0; y < grid.Height; ++y)
			{
				for (int x = 0; x < grid.Width; ++x)
				{
					grid.SetTerrainType(new IntPoint(x, y), 123);
				}
			}

			sw.Stop();
			Console.WriteLine(sw.ElapsedTicks);
		}

		static void Main(string[] args)
		{
			Test();
			return;
			/*
			long t = LOSShadowCast1.PerfTest();
			Console.WriteLine(t);
			return;
			*/

			bool debugServer = Properties.Settings.Default.DebugServer;

			Server server = new Server();

			if (debugServer)
			{
				TraceListener listener = new ConsoleTraceListener();
				server.TraceListener = listener;
			}

			server.RunServer(false, null, null);
		}
	}
}