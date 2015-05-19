using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Server;
using Dwarrowdelf;

namespace Dwarrowdelf.Server.Fortress
{
	public sealed class DungeonGame : GameEngine
	{
		public DungeonGame(string gameDir, GameOptions options)
			: base(gameDir, options)
		{
			EnvironmentObject env;

			switch (options.Map)
			{
				case GameMap.Fortress:
					env = FortressWorldCreator.InitializeWorld(this.World, options.MapSize);
					break;

				case GameMap.Adventure:
					var dwc = new DungeonWorldCreator(this.World);
					dwc.InitializeWorld(this.World, options.MapSize);
					env = dwc.MainEnv;
					break;

				default:
					throw new Exception();
			}

			var player = CreatePlayer(env);
			this.AddPlayer(player);
		}

		DungeonGame(SaveGameContext ctx)
			: base(ctx)
		{
		}

		Player CreatePlayer(EnvironmentObject env)
		{
			const int NUM_DWARVES = 1;

			var player = new DungeonPlayer(2, this);

			var startRect = FindStartLocation(env);

			if (!startRect.HasValue)
				throw new Exception();

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
				l.Strength = 100;

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

		IntGrid2Z? FindStartLocation(EnvironmentObject env)
		{
			const int size = 2;

			var center = env.StartLocation;

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
