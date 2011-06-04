using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;


namespace MyArea
{
	public class MyEngine : GameEngine
	{
		Area m_area;

		public MyEngine(string gameDir)
			: base(gameDir)
		{
			m_area = new Area();

			this.World.Initialize(delegate
			{
				m_area.InitializeWorld(this.World);
			});
		}

		public MyEngine(string gameDir, string saveFile)
			: base(gameDir, saveFile)
		{
		}

		public override ServerUser CreateUser(int userID)
		{
			return new MyUser(userID, this, this.World);
		}
	}
}
