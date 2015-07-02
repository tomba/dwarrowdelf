using System;
using System.Linq;

namespace Dwarrowdelf.Client
{
	sealed class GameData : GameDataBase
	{
		public static GameData Data { get; private set; }

		public static void Create()
		{
			GameData.Data = new GameData();
		}

		public GameData()
		{
			this.TileSet = new TileSet("Content/TileSet.png");
		}

		public TileSet TileSet { get; private set; }
	}
}