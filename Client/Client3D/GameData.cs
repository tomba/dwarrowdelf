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

		public ControlMode ControlMode = ControlMode.Fps;
		public bool AlignViewGridToCamera = true;

		Map m_map;
		public Map Map
		{
			get { return m_map; }
			set { m_map = value; if (this.MapChanged != null) this.MapChanged(); }
		}

		public event Action MapChanged;
	}
}
