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

		public override void SetupControllable(LivingObject living)
		{
			living.SetAI(new DwarfAI(living));
		}

		public override LivingObject[] CreateControllables(Player player)
		{
			const int NUM_DWARVES = 1;

			// XXX entry location
			var env = this.World.AllObjects.OfType<Dwarrowdelf.Server.EnvironmentObject>().First();

			var list = new List<LivingObject>();

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

		LivingObject CreateDwarf(int i)
		{
			var builder = new LivingObjectBuilder(LivingID.Dwarf)
			{
				Color = (GameColor)m_random.Next(GameColorRGB.NUMCOLORS - 1) + 1,
			};

			switch (i)
			{
				case 0:
					builder.Name = "Doc";
					builder.SetSkillLevel(SkillID.Mining, 100);
					break;

				case 1:
					builder.Name = "Grumpy";
					builder.SetSkillLevel(SkillID.Carpentry, 100);
					break;

				case 2:
					builder.Name = "Happy";
					builder.SetSkillLevel(SkillID.WoodCutting, 100);
					break;

				case 3:
					builder.Name = "Sleepy";
					builder.SetSkillLevel(SkillID.Masonry, 100);
					break;

				case 4:
					builder.Name = "Bashful";
					builder.SetSkillLevel(SkillID.Fighting, 100);
					break;

				case 5:
					builder.Name = "Sneezy";
					builder.SetSkillLevel(SkillID.Fighting, 100);
					break;

				case 6:
					builder.Name = "Dopey";
					builder.SetSkillLevel(SkillID.Fighting, 100);
					break;
			}

			var dwarf = builder.Create(this.World);

			dwarf.SetAI(new DwarfAI(dwarf));


			{
				var materials = Materials.GetMaterials(MaterialCategory.Gem).ToArray();
				var material = materials[m_random.Next(materials.Length)].ID;

				var itemBuilder = new ItemObjectBuilder(ItemID.Gem, material);
				var item = itemBuilder.Create(this.World);

				item.MoveTo(dwarf);
			}

			{
				var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
				var material = materials[m_random.Next(materials.Length)].ID;

				var itemBuilder = new ItemObjectBuilder(ItemID.ShortSword, material);
				var item = itemBuilder.Create(this.World);

				item.MoveTo(dwarf);

				dwarf.WieldWeapon(item);
			}

			{
				var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
				var material = materials[m_random.Next(materials.Length)].ID;

				var itemBuilder = new ItemObjectBuilder(ItemID.ChainMail, material);
				var item = itemBuilder.Create(this.World);

				item.MoveTo(dwarf);

				dwarf.WearArmor(item);
			}

			{
				var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
				var material = materials[m_random.Next(materials.Length)].ID;

				var itemBuilder = new ItemObjectBuilder(ItemID.Skullcap, material);
				var item = itemBuilder.Create(this.World);

				item.MoveTo(dwarf);

				dwarf.WearArmor(item);
			}

			return dwarf;
		}
	}
}
