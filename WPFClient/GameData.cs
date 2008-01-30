using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class GameData
	{
		public static readonly GameData Data = new GameData();

		public Connection Connection { get; set; }

		public GameObject Player { get; set; }

		public MyTraceListener MyTraceListener { get; set; }
	}
}
