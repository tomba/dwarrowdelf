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

		public override Living[] CreateControllables(Player player)
		{
			const int NUM_DWARVES = 5;

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

				var l = new Living(String.Format("Dwarf{0}", i))
				{
					SymbolID = SymbolID.Player,
					Color = (GameColor)rand.Next((int)GameColor.NumColors - 1) + 1,
				};
				l.SetAI(new DwarfAI(l));
				l.Initialize(this.World);

				if (!l.MoveTo(env, p))
					throw new Exception();

				list.Add(l);
			}

			return list.ToArray();
		}
	}
}
