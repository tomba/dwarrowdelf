using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace MyGame
{
	class World
	{
		public static World TheWorld { get; private set; }

		static World()
		{
			TheWorld = new World();
		}

		public ObservableCollection<Environment> Environments { get; private set; }

		public IAreaData AreaData { get; private set; }

		public World()
		{
			this.AreaData = new MyAreaData.AreaData(); // XXX
			this.Environments = new ObservableCollection<Environment>();
		}

		public void AddEnvironment(Environment env)
		{
			this.Environments.Add(env);
		}
	}
}
