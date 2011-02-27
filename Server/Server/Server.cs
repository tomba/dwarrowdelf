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

		public void RunServer(bool isEmbedded, EventWaitHandle serverStartWaitHandle, EventWaitHandle serverStopWaitHandle)
		{
			Debug.Print("Start");

			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			/* Load area */

			int magic = 0;
			GameAction.MagicNumberGenerator = () => -Math.Abs(Interlocked.Increment(ref magic));

			IArea area = new MyArea.Area();

			m_world = new World();
			m_world.Initialize(area);
			Save(m_world);

			// XXX test the serialization and deserialization
			m_world = Load();

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
				Console.WriteLine("Press enter to exit");
				while (Console.ReadKey().Key != ConsoleKey.Enter)
					m_world.SignalWorld();
			}

			m_world.Stop();

			Debug.Print("Server exiting");

			Connection.StopListening();

			Debug.Print("Server exit");
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

		void Save(World world)
		{
			Trace.TraceInformation("Saving world");
			var watch = Stopwatch.StartNew();



			var stream = new System.IO.MemoryStream();

			var serializer = new Dwarrowdelf.Json.JsonSerializer(stream);
			serializer.Serialize(world);

			stream.Position = 0;
			//stream.CopyTo(Console.OpenStandardOutput());

			stream.Position = 0;
			using (var file = File.Create("json.txt"))
				stream.WriteTo(file);

			watch.Stop();
			Trace.TraceInformation("Saving world took {0}", watch.Elapsed);
		}

		World Load()
		{
			Trace.TraceInformation("Loading world");
			var watch = Stopwatch.StartNew();

			World world;

			var stream = File.OpenRead("json.txt");
			var deserializer = new Dwarrowdelf.Json.JsonDeserializer(stream);
			world = (World)deserializer.Deserialize<World>();

			watch.Stop();
			Trace.TraceInformation("Loading world took {0}", watch.Elapsed);

			return world;
		}
	}
}
