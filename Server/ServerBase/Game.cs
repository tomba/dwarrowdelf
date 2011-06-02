using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Server
{
	public abstract class Game
	{
		string m_gameDir;
		World m_world;

		public World World { get { return m_world; } }

		protected Game(string gameDir)
		{
			m_gameDir = gameDir;
			m_world = new World();
		}

		protected Game(string gameDir, string saveFile)
		{
			m_gameDir = gameDir;
			m_world = LoadWorld(Path.Combine(m_gameDir, saveFile));
		}

		public void Start()
		{
			m_world.Start();
		}

		public void Stop()
		{
			m_world.Stop();
		}

		public void SignalWorld()
		{
			m_world.SignalWorld();
		}

		public abstract ServerUser CreateUser(int userID);

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
