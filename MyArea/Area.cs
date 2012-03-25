using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Server;
using Dwarrowdelf;

namespace MyArea
{
	public sealed class Area : IArea
	{
		public World World { get; private set; }

		EnvObserver m_envObserver;

		public Area()
		{
		}

		#region IArea Members

		public World CreateWorld()
		{
			this.World = new World(WorldTickMethod.Simultaneous);
			this.World.Initialize(delegate
				{
					WorldCreator.InitializeWorld(this.World);
				});

			// XXX
			var env = this.World.AllObjects.OfType<EnvironmentObject>().First();
			m_envObserver = new EnvObserver(env);

			return this.World;
		}

		public void SetupLivingAsControllable(LivingObject living)
		{
			living.SetAI(new DwarfAI(living, m_envObserver, 0));
		}

		public LivingObject[] SetupWorldForNewPlayer(Player player)
		{
			const int NUM_DWARVES = 7;

			// XXX entry location
			var env = this.World.AllObjects.OfType<EnvironmentObject>().First();

			var startRect = FindStartLocation(env);

			if (!startRect.HasValue)
				throw new Exception();

			var startLocs = startRect.Value.Range().ToArray();

			// clear trees
			foreach (var p in startLocs)
				env.SetInterior(p, InteriorID.Empty, MaterialID.Undefined);

			var list = new List<LivingObject>();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				var p = startLocs[Helpers.GetRandomInt(startLocs.Length - 1)];

				var l = CreateDwarf(i);

				if (!l.MoveTo(env, p))
					throw new Exception();

				list.Add(l);
			}

			return list.ToArray();
		}

		#endregion



		int FindSurface(EnvironmentObject env, IntPoint2 p2)
		{
			for (int z = env.Depth - 1; z > 0; --z)
			{
				var p = new IntPoint3(p2.X, p2.Y, z);

				var terrainID = env.GetTerrainID(p);
				var interiorID = env.GetInteriorID(p);

				if (terrainID != TerrainID.Empty || interiorID != InteriorID.Empty)
					return z;
			}

			throw new Exception();
		}

		bool TestStartArea(EnvironmentObject env, IntRectZ r)
		{
			foreach (var p in r.Range())
			{
				var terrainID = env.GetTerrainID(p);
				var interiorID = env.GetInteriorID(p);

				if (terrainID == TerrainID.NaturalFloor &&
					(interiorID == InteriorID.Empty || interiorID == InteriorID.Tree || interiorID == InteriorID.Sapling))
					continue;
				else
					return false;
			}

			return true;
		}

		IntRectZ? FindStartLocation(EnvironmentObject env)
		{
			const int size = 3;

			var center = env.Size.Plane.Center;

			foreach (var p in IntPoint2.SquareSpiral(center, env.Width))
			{
				var z = FindSurface(env, p);

				var r = new IntRectZ(p.X - size, p.Y - size, size * 2, size * 2, z);

				if (TestStartArea(env, r))
					return r;
			}

			return null;
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

			dwarf.SetAI(new DwarfAI(dwarf, m_envObserver, 0));

			Helpers.AddGem(dwarf);
			Helpers.AddBattleGear(dwarf);

			return dwarf;
		}

	}
}
