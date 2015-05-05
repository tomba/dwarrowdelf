using System;

namespace Dwarrowdelf.Client
{
	sealed class GameData : GameDataBase
	{
		public static GameData Data;

		public static void Create()
		{
			if (GameData.Data != null)
				throw new Exception();

			GameData.Data = new GameData();
		}
	}
}