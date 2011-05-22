//#define SAVE_EVERY_TURN

using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Server
{
	public class Server : MarshalByRefObject, IServer
	{
		static string s_saveDir = "save";

		World m_world;
		WorldLogger m_logger;

		public Server()
		{
		}

		public void RunServer(bool isEmbedded, EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle, string saveFile)
		{
			bool cleanSaves = false;

			Debug.Print("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			int magic = 0;
			GameAction.MagicNumberGenerator = () => -Math.Abs(Interlocked.Increment(ref magic));

			IArea area = new MyArea.Area();

			if (!Directory.Exists(s_saveDir))
				Directory.CreateDirectory(s_saveDir);

			if (cleanSaves)
			{
				var files = Directory.EnumerateFiles(s_saveDir);
				foreach (var file in files)
					File.Delete(file);
			}

			if (!cleanSaves)
			{
				saveFile = GetLatestSaveFile();
			}

			if (saveFile != null)
			{
				m_world = Load(saveFile);
			}
			else
			{
				m_world = new World();
				m_world.Initialize(area);

				//Save();

				//m_world = Load("save-0.json");
			}

			m_world.TickEnded += OnWorldTickEnded;

			m_logger = new WorldLogger();
			m_logger.Start(m_world, Path.Combine(s_saveDir, "changes.log"));

			m_world.Start();

			Connection.NewConnectionEvent += OnNewConnection;
			Connection.StartListening();

			Debug.Print("The service is ready.");

			if (isEmbedded)
			{
				Debug.Print("Server signaling client for start.");
				if (serverStartWaitHandle != null)
				{
					serverStartWaitHandle.Set();
					serverStopWaitHandle.WaitOne();
				}
			}
			else
			{
				KeyLoop();
			}

			//Save();

			m_world.Stop();

			m_logger.Stop();
			m_world.TickEnded -= OnWorldTickEnded;

			Debug.Print("Server exiting");

			Connection.StopListening();

			Debug.Print("Server exit");
		}

		void KeyLoop()
		{
			bool exit = false;

			Console.WriteLine("q - quit, s - signal, p - enable singlestep, r - disable singlestep, . - step");

			while (exit == false)
			{
				var key = Console.ReadKey(true).Key;

				switch (key)
				{
					case ConsoleKey.Q:
						exit = true;
						break;

					case ConsoleKey.S:
						m_world.SignalWorld();
						break;

					case ConsoleKey.P:
						m_world.EnableSingleStep();
						break;

					case ConsoleKey.R:
						m_world.DisableSingleStep();
						break;

					case ConsoleKey.OemPeriod:
						m_world.SingleStep();
						break;

					default:
						Console.WriteLine("Unknown key");
						break;
				}
			}
		}

		void OnNewConnection(IConnection connection)
		{
			var sconn = new ServerConnection(connection);
			sconn.Init(m_world);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Debug.Print("tuli exc");
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

		void Save()
		{
			int tick = m_world.TickNumber;
			string name = String.Format("save-{0}.json", tick);
			Save(m_world, name);
		}

		static void Save(World world, string name)
		{
			Trace.TraceInformation("Saving world {0}", name);
			var watch = Stopwatch.StartNew();


			var stream = new System.IO.MemoryStream();

			var serializer = new Dwarrowdelf.JsonSerializer(stream);
			serializer.Serialize(world);

			stream.Position = 0;
			//stream.CopyTo(Console.OpenStandardOutput());

			stream.Position = 0;
			using (var file = File.Create(Path.Combine(s_saveDir, name)))
				stream.WriteTo(file);

			watch.Stop();
			Trace.TraceInformation("Saving world took {0}", watch.Elapsed);
		}

		static string GetLatestSaveFile()
		{
			var files = Directory.EnumerateFiles(s_saveDir);
			var list = new System.Collections.Generic.List<string>(files);
			list.Sort();
			var last = list[list.Count - 1];
			return Path.GetFileName(last);
		}

		static World Load(string name)
		{
			Trace.TraceInformation("Loading world {0}", name);
			var watch = Stopwatch.StartNew();

			World world;

			var stream = File.OpenRead(Path.Combine(s_saveDir, name));
			var deserializer = new Dwarrowdelf.JsonDeserializer(stream);
			world = (World)deserializer.Deserialize<World>();

			watch.Stop();
			Trace.TraceInformation("Loading world took {0}", watch.Elapsed);

			return world;
		}
	}
}
