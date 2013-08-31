using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Server;
using Dwarrowdelf;

namespace Dwarrowdelf.Server.Fortress
{
	[SaveGameObject]
	public sealed class FortressGameManager : GameEngine
	{
		[SaveGameProperty]
		EnvObserver m_envObserver;

		public FortressGameManager(string gameDir, GameMode gameMode, WorldTickMethod tickMethod)
			: base(gameDir, gameMode, tickMethod)
		{
			FortressWorldCreator.InitializeWorld(this.World);

			// XXX
			var env = this.World.HackGetFirstEnv();
			m_envObserver = new EnvObserver(env);

			int numPlayers = 1;

			for (int playerNum = 0; playerNum < numPlayers; ++playerNum)
			{
				var player = CreatePlayer(playerNum, env);

				this.AddPlayer(player);
			}
		}

		FortressGameManager(SaveGameContext ctx)
			: base(ctx)
		{
		}

		Player CreatePlayer(int playerNum, EnvironmentObject env)
		{
			const int NUM_DWARVES = 7;

			var player = new Player(2 + playerNum, this);

			IntPoint3 pos;

			switch (playerNum)
			{
				case 0:
					pos = env.StartLocation;
					break;

				case 1:
					pos = env.GetSurfaceLocation(env.Width / 4, env.Height / 4);
					break;

				default:
					throw new Exception();
			}

			var startRect = FindStartLocation(env, pos);

			if (!startRect.HasValue)
				throw new Exception();

			var startLocs = startRect.Value.Range().ToArray();

			// clear trees
			foreach (var p in startLocs)
			{
				var td = env.GetTileData(p);
				if (td.InteriorID == InteriorID.Tree)
				{
					td.InteriorID = InteriorID.Grass;
					td.InteriorMaterialID = MaterialID.RyeGrass;
					env.SetTileData(p, td);
				}
			}

			var list = new List<LivingObject>();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				var p = startLocs[Helpers.GetRandomInt(startLocs.Length - 1)];

				var l = CreateDwarf(i);

				if (!l.MoveTo(env, p))
					throw new Exception();

				list.Add(l);
			}

			player.AddControllables(list);

			return player;
		}

		bool TestStartArea(EnvironmentObject env, IntGrid2Z r)
		{
			foreach (var p in r.Range())
			{
				var terrainID = env.GetTerrainID(p);
				var interiorID = env.GetInteriorID(p);

				if (terrainID == TerrainID.NaturalFloor)
					continue;
				else
					return false;
			}

			return true;
		}

		IntGrid2Z? FindStartLocation(EnvironmentObject env, IntPoint3 pos)
		{
			const int size = 3;

			var center = pos;

			foreach (var p in IntPoint2.SquareSpiral(center.ToIntPoint(), env.Width / 2))
			{
				if (env.Size.Plane.Contains(p) == false)
					continue;

				var z = env.GetSurfaceLevel(p);

				var r = new IntGrid2Z(p.X - size, p.Y - size, size * 2, size * 2, z);

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

			Helpers.AddGem(dwarf);
			Helpers.AddBattleGear(dwarf);

			dwarf.SetAI(new DwarfAI(dwarf, m_envObserver, this.World.PlayerID));

			return dwarf;
		}

	}
}
