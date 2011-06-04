using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;


namespace MyArea
{
	public class MyGame : Game
	{
		Area m_area;

		public MyGame(string gameDir)
			: base(gameDir)
		{
			m_area = new Area();

			this.World.BeginInitialize();

			m_area.InitializeWorld(this.World);

			this.World.EndInitialize();
		}

		public MyGame(string gameDir, string saveFile)
			: base(gameDir, saveFile)
		{
		}

		public override ServerUser CreateUser(int userID)
		{
			return new MyUser(userID, this.World);
		}
	}
}
