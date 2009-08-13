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

		public World()
		{
		}

		public void AddEnvironment(Environment env)
		{
			m_envList.Add(env);
		}
	}
}
