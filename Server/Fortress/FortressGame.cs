using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Server;
using Dwarrowdelf;

namespace Dwarrowdelf.Server.Fortress
{
	[SaveGameObject]
	public sealed class FortressGame : GameEngine
	{
		public FortressGame(string gameDir, GameOptions options)
			: base(gameDir, options)
		{
			EnvironmentObject env;

			switch (options.Map)
			{
				case GameMap.Fortress:
					env = FortressWorldCreator.InitializeWorld(this.World, options.MapSize);
					break;

				case GameMap.Ball:
				case GameMap.Cube:
					{
						var creator = new ArtificialWorldCreator(this.World, options.Map);
						creator.InitializeWorld(options.MapSize);
						env = creator.MainEnv;
					}
					break;

				case GameMap.NoiseTerrain:
					{
						var creator = new NoiseWorldCreator(this.World, options.Map);
						creator.InitializeWorld(options.MapSize);
						env = creator.MainEnv;
					}
					break;

				default:
					throw new NotImplementedException();
			}

			int numPlayers = 1;

			for (int playerNum = 0; playerNum < numPlayers; ++playerNum)
			{
				var player = CreatePlayer(playerNum, env);

				this.AddPlayer(player);
			}
		}

		FortressGame(SaveGameContext ctx)
			: base(ctx)
		{
		}

		Player CreatePlayer(int playerNum, EnvironmentObject env)
		{
			const int NUM_DWARVES = 7;

			var player = new FortressPlayer(2 + playerNum, this, env);

			IntVector3 pos;

			switch (playerNum)
			{
				case 0:
					pos = env.StartLocation;
					break;

				case 1:
					pos = env.GetSurfaceLocation(env.Width / 4, env.Height / 4);
					break;

				case 2:
					pos = env.GetSurfaceLocation(env.Width / 4 * 3, env.Height / 4 * 3);
					break;

				default:
					throw new Exception();
			}

			var startRect = FindStartLocation(env, pos);

			if (!startRect.HasValue)
				throw new Exception();

			player.EnvObserver.Add(startRect.Value);

			var startLocs = startRect.Value.Range().ToArray();

			// clear trees
			foreach (var p in startLocs)
			{
				var td = env.GetTileData(p);
				if (td.HasTree)
				{
					td.ID = TileID.Grass;
					td.MaterialID = MaterialID.RyeGrass;
					env.SetTileData(p, td);
				}
			}

			var list = new List<LivingObject>();

			for (int i = 0; i < NUM_DWARVES; ++i)
			{
				var p = startLocs[Helpers.GetRandomInt(startLocs.Length - 1)];

				var l = CreateDwarf(i);

				l.SetAI(new DwarfAI(l, player.EnvObserver, this.World.PlayerID));

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
				var td = env.GetTileData(p);

				if (td.IsWalkable)
					continue;
				else
					return false;
			}

			return true;
		}

		IntGrid2Z? FindStartLocation(EnvironmentObject env, IntVector3 pos)
		{
			const int size = 3;

			var center = pos;

			foreach (var p in IntVector2.SquareSpiral(center.ToIntVector2(), env.Width / 2))
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

			return dwarf;
		}

	}
}
