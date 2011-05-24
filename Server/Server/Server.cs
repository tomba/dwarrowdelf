//#define SAVE_EVERY_TURN

using System;
using System.Threading;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Server
{
	public class Server : MarshalByRefObject, IServer
	{
		static string s_gameDir = "save";
		static bool s_cleanSaves = true;

		Game m_game;

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

			if (!Directory.Exists(s_gameDir))
				Directory.CreateDirectory(s_gameDir);

			if (s_cleanSaves)
			{
				var files = Directory.EnumerateFiles(s_gameDir);
				foreach (var file in files)
					File.Delete(file);
			}

			if (!s_cleanSaves)
			{
				saveFile = GetLatestSaveFile();
			}

			IArea area = new MyArea.Area();

			if (saveFile == null)
			{
				m_game = Game.CreateNewGame(area, s_gameDir);
			}
			else
			{
				m_game = Game.LoadGame(area, s_gameDir, saveFile);
			}

			m_game.Start();

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

			m_game.Save();

			m_game.Stop();

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
						m_game.World.SignalWorld();
						break;

					case ConsoleKey.P:
						m_game.World.EnableSingleStep();
						break;

					case ConsoleKey.R:
						m_game.World.DisableSingleStep();
						break;

					case ConsoleKey.OemPeriod:
						m_game.World.SingleStep();
						break;

					default:
						Console.WriteLine("Unknown key");
						break;
				}
			}
		}

		void OnNewConnection(IConnection connection)
		{
			var serverConnection = new ServerConnection(connection);
			m_game.AddNewConnection(serverConnection);
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			Debug.Print("tuli exc");
		}

		static string GetLatestSaveFile()
		{
			var files = Directory.EnumerateFiles(s_gameDir);
			var list = new System.Collections.Generic.List<string>(files);
			list.Sort();
			var last = list[list.Count - 1];
			return Path.GetFileName(last);
		}
	}
}
