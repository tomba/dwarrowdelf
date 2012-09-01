using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;


namespace Dwarrowdelf.Server
{
	static class ServerLauncher
	{
		static void Main(string[] args)
		{
			Thread.CurrentThread.Name = "SMain";

			var path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
			path = System.IO.Path.Combine(path, "Dwarrowdelf", "save");
			if (!System.IO.Directory.Exists(path))
				System.IO.Directory.CreateDirectory(path);

			var gameDir = path;

			bool cleanSaves = true;

			SaveManager saveManager = new SaveManager(gameDir);

			Guid save = Guid.Empty;

			if (cleanSaves)
				saveManager.DeleteAll();
			else
				save = saveManager.GetLatestSaveFile();

			var gf = new GameFactory();
			GameEngine game;

			if (save == Guid.Empty)
				game = (GameEngine)gf.CreateGame(gameDir, GameMode.Fortress, GameMap.Fortress);
			else
				game = (GameEngine)gf.LoadGame(gameDir, save);

			var keyThread = new Thread(KeyMain);
			keyThread.Start(game);

			game.Run(null);
		}

		static string GetLatestSaveFile(string gameDir)
		{
			var files = Directory.EnumerateFiles(gameDir);
			var list = new System.Collections.Generic.List<string>(files);
			list.Sort();
			var last = list[list.Count - 1];
			return Path.GetFileName(last);
		}

		static void KeyMain(object _game)
		{
			var game = (GameEngine)_game;

			Console.WriteLine("q - quit, s - signal, p - enable singlestep, r - disable singlestep, . - step");

			while (true)
			{
				var key = Console.ReadKey(true).Key;

				switch (key)
				{
					case ConsoleKey.Q:
						Console.WriteLine("Quit");
						game.Stop();
						return;

					case ConsoleKey.S:
						Console.WriteLine("Signal");
						game.SignalWorld();
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