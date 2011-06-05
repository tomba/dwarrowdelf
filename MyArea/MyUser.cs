using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Server;
using Dwarrowdelf;

namespace MyArea
{
	[GameObject]
	class MyUser : Player
	{
		const int NUM_DWARVES = 5;

		// XXX for deserialize
		MyUser()
		{
		}

		public MyUser(int userID)
			: base(userID)
		{
		}

		protected override Living[] CreateControllables()
		{
			var env = this.World.Environments.First(); // XXX entry location

			var list = new List<Living>();

			var rand = new Random();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				IntPoint3D p;
				do
				{
					p = new IntPoint3D(rand.Next(env.Width), rand.Next(env.Height), 9);
				} while (env.GetInteriorID(p) != InteriorID.Empty);

				var player = new Living(String.Format("Dwarf{0}", i))
				{
					SymbolID = SymbolID.Player,
					Color = (GameColor)rand.Next((int)GameColor.NumColors - 1) + 1,
				};
				player.SetAI(new DwarfAI(player));
				player.Initialize(this.World);

				if (!player.MoveTo(env, p))
					throw new Exception();

				list.Add(player);
			}

			return list.ToArray();
		}
	}
}
