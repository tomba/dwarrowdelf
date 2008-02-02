using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading;

namespace MyGame
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Single)]
	public class ServerService : IServerService
	{
		IClientCallback m_client;

		World m_world;
		ServerGameObject m_player;
		InteractiveActor m_actor;

		public ServerService()
		{
			MyDebug.WriteLine("New ServerService");
		}

		#region IServerService Members

		public void Login(string name)
		{
			MyDebug.WriteLine("Login {0}", name);

			m_client = OperationContext.Current.GetCallbackChannel<IClientCallback>();

			m_world = new World();
			World.CurrentWorld = m_world;


			// xxx
			var monster = new ServerGameObject();
			monster.MoveTo(m_world.Map, new Location(2, 5));
			var monsterAI = new MonsterActor(monster);
			m_world.AddActor(monsterAI);

			m_world.ChangesEvent += new HandleChanges(m_world_ChangesEvent);

			m_player = new ServerGameObject();
			m_actor = new InteractiveActor();
			m_player.SetActor(m_actor);
			m_world.AddActor(m_actor);

			m_client.LoginReply(m_player.ObjectID);

			if (!m_player.MoveTo(m_world.Map, new Location(0, 0)))
				throw new Exception("Unable to move player");

			SendMap();

			m_world.SendChanges();

			/*
			m_player.ObjectMoved += new ObjectMoved(m_player_ObjectMoved);
			m_map.MapChanged += new MapChanged(m_map_MapChanged);

			 */
		}

		void SendMap()
		{
			LocationGrid<int> map = m_world.Map.GetTerrain();

			Location ploc = m_player.Location;
			int viewRange = m_player.ViewRange;

			List<MapLocation> locations = new List<MapLocation>();

			for (int y = ploc.Y - viewRange; y < ploc.Y + viewRange; y++)
			{
				if (y < 0 || y >= m_world.Map.Height)
					continue;

				for (int x = ploc.X - viewRange; x < ploc.X + viewRange; x++)
				{
					if (x < 0 || x >= m_world.Map.Width)
						continue;

					if (m_player.Sees(new Location(x, y)))
					{
						locations.Add(new MapLocation(new Location(x, y), map[x, y]));
					}
				}
			}

			m_client.DeliverMapTerrains(locations.ToArray());
		}

		public void Logout()
		{
			MyDebug.WriteLine("Logout");
			World.CurrentWorld = m_world;
			m_world.ChangesEvent -= new HandleChanges(m_world_ChangesEvent);
			m_world.RemoveActor(m_actor);

			m_client = null;
		}

		public void DoAction(GameAction action)
		{
			World.CurrentWorld = m_world;
			if (action.ObjectID != m_player.ObjectID)
				throw new Exception("Illegal ob id");

			m_actor.EnqueueAction(action);
		}

		public void ToggleTile(Location l)
		{
			World.CurrentWorld = m_world;

			if (!m_world.Map.Bounds.Contains(l))
				return;

			if (m_world.Map.GetTerrain(l) == 1)
				m_world.Map.SetTerrain(l, 2);
			else
				m_world.Map.SetTerrain(l, 1);

			SendMap();

		}

		#endregion

		void m_world_ChangesEvent(Change[] changes)
		{
			m_client.DeliverChanges(changes);
			SendMap(); // xxx

		}
	}
}
