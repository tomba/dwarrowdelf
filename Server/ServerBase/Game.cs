using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Server
{
	public class Game
	{
		public static Game CreateNewGame(IArea area, string gameDir)
		{
			var game = new Game(area, gameDir);
			return game;
		}

		public static Game LoadGame(IArea area, string gameDir, string saveFile)
		{
			var game = new Game(area, gameDir, saveFile);
			return game;
		}


		string m_gameDir;
		World m_world;
		WorldLogger m_logger;

		public World World { get { return m_world; } }

		Game(IArea area, string gameDir)
		{
			m_gameDir = gameDir;

			m_world = new World();

			m_world.BeginInitialize();

			area.InitializeWorld(m_world);

			m_world.EndInitialize();
		}

		Game(IArea area, string gameDir, string saveFile)
		{
			m_gameDir = gameDir;

			m_world = LoadWorld(Path.Combine(m_gameDir, saveFile));
		}

		public void Start()
		{
			m_world.TickEnded += OnWorldTickEnded;

			m_logger = new WorldLogger();
			m_logger.Start(m_world, Path.Combine(m_gameDir, "changes.log"));

			m_world.Start();
		}

		public void Stop()
		{
			m_world.Stop();

			m_logger.Stop();

			m_world.TickEnded -= OnWorldTickEnded;
		}

		public void AddNewConnection(ServerConnection sConn)
		{
			sConn.Init(m_world);
		}

		void OnWorldTickEnded()
		{
#if SAVE_EVERY_TURN
			Console.WriteLine("Tick {0}", m_world.TickNumber);

			int tick = m_world.TickNumber;
			string name = String.Format("save-{0}.json", tick);
			Save(m_world, name);

			var w = Load(name);
#endif
		}

		public void Save()
		{
			int tick = m_world.TickNumber;
			string name = String.Format("save-{0}.json", tick);
			Save(name);
		}

		public void Save(string saveFile)
		{
			SaveWorld(m_world, Path.Combine(m_gameDir, saveFile));
		}

		static void SaveWorld(World world, string savePath)
		{
			Trace.TraceInformation("Saving game {0}", savePath);
			var watch = Stopwatch.StartNew();


			var stream = new System.IO.MemoryStream();

			var serializer = new Dwarrowdelf.JsonSerializer(stream);
			serializer.Serialize(world);

			stream.Position = 0;
			//stream.CopyTo(Console.OpenStandardOutput());

			stream.Position = 0;
			using (var file = File.Create(savePath))
				stream.WriteTo(file);

			watch.Stop();
			Trace.TraceInformation("Saving game took {0}", watch.Elapsed);
		}

		static World LoadWorld(string savePath)
		{
			Trace.TraceInformation("Loading game {0}", savePath);
			var watch = Stopwatch.StartNew();

			World world;

			var stream = File.OpenRead(savePath);
			var deserializer = new Dwarrowdelf.JsonDeserializer(stream);
			world = (World)deserializer.Deserialize<World>();

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);

			return world;
		}
	}
}
