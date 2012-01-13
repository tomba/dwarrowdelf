using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;
using Dwarrowdelf.AI;

namespace MyArea
{
	public sealed class MyEngine : GameEngine
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

		public override void SetupLivingAsControllable(LivingObject living)
		{
			living.SetAI(new DwarfAI(living));
		}

		public override LivingObject[] SetupWorldForNewPlayer(Player player)
		{
			const int NUM_DWARVES = 7;

			// XXX entry location
			var env = this.World.AllObjects.OfType<Dwarrowdelf.Server.EnvironmentObject>().First();

			var list = new List<LivingObject>();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				IntPoint3 p;
				do
				{
					p = new IntPoint3(Helpers.GetRandomInt(env.Width), Helpers.GetRandomInt(env.Height), 9);
				} while (!EnvironmentHelpers.CanEnter(env, p));

				var l = CreateDwarf(i);

				if (!l.MoveTo(env, p))
					throw new Exception();

				list.Add(l);
			}

			return list.ToArray();
		}

		LivingObject CreateDwarf(int i)
		{
			var builder = new LivingObjectBuilder(LivingID.Dwarf)
			{
				Color = (GameColor)Helpers.GetRandomInt(GameColorRGB.NUMCOLORS - 1) + 1,
				Gender = LivingGender.Male,
			};

			switch (i)
			{
				case 0:
					builder.Name = "Doc";
					builder.SetSkillLevel(SkillID.Mining, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;

				case 1:
					builder.Name = "Grumpy";
					builder.SetSkillLevel(SkillID.Carpentry, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;

				case 2:
					builder.Name = "Happy";
					builder.SetSkillLevel(SkillID.WoodCutting, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;

				case 3:
					builder.Name = "Sleepy";
					builder.SetSkillLevel(SkillID.Masonry, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;

				case 4:
					builder.Name = "Bashful";
					builder.SetSkillLevel(SkillID.BlackSmithing, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;

				case 5:
					builder.Name = "Sneezy";
					builder.SetSkillLevel(SkillID.GemCutting, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;

				case 6:
					builder.Name = "Dopey";
					builder.SetSkillLevel(SkillID.Smelting, 80);
					builder.SetSkillLevel(SkillID.Fighting, 40);
					break;
			}

			var dwarf = builder.Create(this.World);

			dwarf.SetAI(new DwarfAI(dwarf));

			Helpers.AddGem(dwarf);
			Helpers.AddBattleGear(dwarf);

			return dwarf;
		}
	}
}
