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

		EnvironmentObject m_map;
		public EnvironmentObject Map
		{
			get { return m_map; }
			set { var old = m_map; m_map = value; if (this.MapChanged != null) this.MapChanged(old, m_map); }
		}

		public event Action<EnvironmentObject, EnvironmentObject> MapChanged;
	}
}
