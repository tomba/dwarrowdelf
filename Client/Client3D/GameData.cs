using System;

namespace Dwarrowdelf.Client
{
	enum ControlMode
	{
		Fps,
		Rts,
	}

	sealed class GameData : GameDataBase
	{
		public static GameData Data;

		public static void Create()
		{
			if (GameData.Data != null)
				throw new Exception();

			GameData.Data = new GameData();
		}

		// XXX not used at the moment
		public ControlMode ControlMode = ControlMode.Fps;
		// XXX not used at the moment
		public bool AlignViewGridToCamera = true;
	}
}
