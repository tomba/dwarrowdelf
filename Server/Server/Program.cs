using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;


namespace Dwarrowdelf.Server
{
	class ServerLauncher
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.Name = "Main";

			string gameDir = "save";
			bool cleanSaves = true;
			string saveFile = null;

			if (!Directory.Exists(gameDir))
				Directory.CreateDirectory(gameDir);

			if (cleanSaves)
			{
				var files = Directory.EnumerateFiles(gameDir);
				foreach (var file in files)
					File.Delete(file);
			}

			if (!cleanSaves)
			{
				saveFile = GetLatestSaveFile(gameDir);
			}

			GameEngine game;

			if (saveFile == null)
			{
				game = new MyArea.MyEngine(gameDir);
			}
			else
			{
				game = new MyArea.MyEngine(gameDir, saveFile);
			}

			GameServer server;

			server = new GameServer(game);
			server.RunServer(null);

			KeyLoop(game);

		}

		static string GetLatestSaveFile(string gameDir)
		{
			var files = Directory.EnumerateFiles(gameDir);
			var list = new System.Collections.Generic.List<string>(files);
			list.Sort();
			var last = list[list.Count - 1];
			return Path.GetFileName(last);
		}

		static void KeyLoop(GameEngine game)
		{
			Console.WriteLine("q - quit, s - signal, p - enable singlestep, r - disable singlestep, . - step");

			while (true)
			{
				var key = Console.ReadKey(true).Key;

				switch (key)
				{
					case ConsoleKey.Q:
						//game.Stop();
						return;

					case ConsoleKey.S:
						//game.World.SignalWorld();
						break;

					case ConsoleKey.P:
						//game.World.EnableSingleStep();
						break;

					case ConsoleKey.R:
						//game.World.DisableSingleStep();
						break;

					case ConsoleKey.OemPeriod:
						//game.World.SingleStep();
						break;

					default:
						Console.WriteLine("Unknown key");
						break;
				}
			}
		}
	}
}