using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class World
	{
		public static World TheWorld { get; private set; }

		static World()
		{
			TheWorld = new World();
		}

		List<Environment> m_envList = new List<Environment>();

		public IAreaData AreaData { get; private set; }

		public World()
		{
			this.AreaData = new MyAreaData.AreaData(); // XXX
		}

		public void AddEnvironment(Environment env)
		{
			m_envList.Add(env);
		}
	}
}
