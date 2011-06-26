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

			var gameDir = @"C:\Users\Tomba\Work\Dwarrowdelf\save";
			bool cleanSaves = true;

			SaveManager saveManager = new SaveManager(gameDir);

			Guid save = Guid.Empty;

			if (cleanSaves)
				saveManager.DeleteAll();
			else
				save = saveManager.GetLatestSaveFile();

			var gf = new GameFactory();
			// Typecast to Game to allow direct manipulation
			var game = (Game)gf.CreateGame("MyArea.dll", gameDir);

			if (save == Guid.Empty)
				game.CreateWorld();
			else
				game.LoadWorld(save);

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
			var game = (Game)_game;

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
						game.Engine.SignalWorld();
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