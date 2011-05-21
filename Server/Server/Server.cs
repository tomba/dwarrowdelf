using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Server
{
	public class Server : MarshalByRefObject, IServer
	{
		World m_world;

		public Server()
		{
		}

		public void RunServer(bool isEmbedded, EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle, string saveFile)
		{
			Debug.Print("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			int magic = 0;
			GameAction.MagicNumberGenerator = () => -Math.Abs(Interlocked.Increment(ref magic));

			IArea area = new MyArea.Area();

			if (saveFile != null)
			{
				m_world = Load(saveFile);
			}
			else
			{
				if (Directory.Exists("save"))
				{
					var files = Directory.EnumerateFiles("save");
					foreach (var file in files)
						File.Delete(file);
				}

				m_world = new World();
				m_world.Initialize(area);
				Save(m_world, "save-0.json");

				//m_world = Load("save-0.json");
			}

			m_world.TickEnded += OnWorldTickEnded;

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

			m_world.Stop();

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
#if asd
			Console.WriteLine("Tick {0}", m_world.TickNumber);

			int tick = m_world.TickNumber;
			string name = String.Format("save-{0}.json", tick);
			Save(m_world, name);

			var w = Load(name);
#endif
		}

		void Save(World world, string name)
		{
			Trace.TraceInformation("Saving world {0}", name);
			var watch = Stopwatch.StartNew();


			var stream = new System.IO.MemoryStream();

			var serializer = new Dwarrowdelf.Json.JsonSerializer(stream);
			serializer.Serialize(world);

			stream.Position = 0;
			//stream.CopyTo(Console.OpenStandardOutput());

			if (!Directory.Exists("save"))
				Directory.CreateDirectory("save");

			stream.Position = 0;
			using (var file = File.Create("save" + "\\" + name))
				stream.WriteTo(file);

			watch.Stop();
			Trace.TraceInformation("Saving world took {0}", watch.Elapsed);
		}

		World Load(string name)
		{
			Trace.TraceInformation("Loading world {0}", name);
			var watch = Stopwatch.StartNew();

			World world;

			var stream = File.OpenRead("save" + "\\" + name);
			var deserializer = new Dwarrowdelf.Json.JsonDeserializer(stream);
			world = (World)deserializer.Deserialize<World>();

			watch.Stop();
			Trace.TraceInformation("Loading world took {0}", watch.Elapsed);

			return world;
		}
	}
}
