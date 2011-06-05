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
		public MyEngine(string gameDir)
			: base(gameDir)
		{
		}

		protected override void InitializeWorld()
		{
			var area = new Area();
			area.InitializeWorld(this.World);
		}


		public override ServerUser CreateUser(int userID)
		{
			return new MyUser(userID, this, this.World);
		}
	}
}
