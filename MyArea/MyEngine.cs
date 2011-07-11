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

		Random m_random = new Random();

		public override Living[] CreateControllables(Player player)
		{
			const int NUM_DWARVES = 5;

			var env = this.World.Environments.First(); // XXX entry location

			var list = new List<Living>();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				IntPoint3D p;
				do
				{
					p = new IntPoint3D(m_random.Next(env.Width), m_random.Next(env.Height), 9);
				} while (!EnvironmentHelpers.CanEnter(env, p));

				var l = CreateDwarf(i);

				if (!l.MoveTo(env, p))
					throw new Exception();

				list.Add(l);
			}

			return list.ToArray();
		}

		Living CreateDwarf(int i)
		{
			var builder = new LivingBuilder(String.Format("Dwarf{0}", i))
			{
				SymbolID = SymbolID.Player,
				Color = (GameColor)m_random.Next((int)GameColor.NumColors - 1) + 1,
			};

			switch (i)
			{
				case 0:
					builder.Name = "Miner";
					builder.SetSkillLevel(SkillID.Mining, 100);
					break;

				case 1:
					builder.Name = "Carpenter";
					builder.SetSkillLevel(SkillID.Carpentry, 100);
					break;

				case 2:
					builder.Name = "Wood Cutter";
					builder.SetSkillLevel(SkillID.WoodCutting, 100);
					break;

				case 3:
					builder.Name = "Mason";
					builder.SetSkillLevel(SkillID.Masonry, 100);
					break;

				case 4:
					builder.Name = "Fighter";
					builder.SetSkillLevel(SkillID.Fighting, 100);
					break;
			}

			var l = builder.Create(this.World);

			l.SetAI(new DwarfAI(l));

			return l;
		}
	}
}
