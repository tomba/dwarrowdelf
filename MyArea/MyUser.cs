using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf.Server;
using Dwarrowdelf;

namespace MyArea
{
	class MyUser : ServerUser
	{
		const int NUM_DWARVES = 5;

		public MyUser(int userID)
			: base(userID)
		{
		}

		protected override Living[] CreateControllables()
		{
			var env = this.World.Environments.First(); // XXX entry location

#if asd
			var player = new Living(m_world, "player")
			{
				SymbolID = SymbolID.Player,
			};
			player.AI = new InteractiveActor(player);

			Debug.Print("Player ob id {0}", player.ObjectID);

			m_controllables.Add(player);

			ItemObject item = new ItemObject(m_world)
			{
				Name = "jalokivi1",
				SymbolID = SymbolID.Gem,
				MaterialID = MaterialID.Diamond,
			};
			item.MoveTo(player);

			item = new ItemObject(m_world)
			{
				Name = "jalokivi2",
				SymbolID = SymbolID.Gem,
				Color = GameColor.Green,
				MaterialID = MaterialID.Diamond,
			};
			item.MoveTo(player);

			/*
			var pp = GetRandomSurfaceLocation(env, 9);
			if (!player.MoveTo(env, pp))
				throw new Exception("Unable to move player");
			*/

			if (!player.MoveTo(env, new IntPoint3D(10, 10, 9)))
				throw new Exception("Unable to move player");

#if qwe
			var pet = new Living(m_world);
			pet.SymbolID = SymbolID.Monster;
			pet.Name = "lemmikki";
			pet.Actor = new InteractiveActor();
			m_controllables.Add(pet);

			pet.MoveTo(player.Environment, player.Location + new IntVector(1, 0));
#endif

#else
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
#endif
			return list.ToArray();
		}
	}
}
