using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading;
using System.Diagnostics;

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

		void CleanUp()
		{
			m_world.ChangesEvent -= new HandleChanges(m_world_ChangesEvent);
			m_world.RemoveActor(m_actor);

			m_world.AddChange(new EnvironmentChange(m_player, ObjectID.NullObjectID, new Location()));
			m_world.SendChanges();

			m_client = null;
			m_player = null;
			m_world = null;
		}


		#region IServerService Members

		public void Login(string name)
		{
			try
			{
				MyDebug.WriteLine("Login {0}", name);

				m_client = OperationContext.Current.GetCallbackChannel<IClientCallback>();

				m_world = World.TheWorld;

				m_world.ChangesEvent += new HandleChanges(m_world_ChangesEvent);

				m_player = new ServerGameObject(m_world);
				m_actor = new InteractiveActor();
				m_actor.ActionDequeuedEvent += m_actor_ActionDequeuedEvent;
				m_player.SetActor(m_actor);
				m_world.AddActor(m_actor);

				MyDebug.WriteLine("Player ob id {0}", m_player.ObjectID);

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
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		void m_actor_ActionDequeuedEvent(int transactionID)
		{
			m_client.TransactionDone(transactionID);
		}


		public void Logout()
		{
			try
			{
				MyDebug.WriteLine("Logout");
				CleanUp();
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void DoAction(GameAction action)
		{
			try
			{
				if (action.ObjectID != m_player.ObjectID)
					throw new Exception("Illegal ob id");

				m_actor.EnqueueAction(action);
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void ToggleTile(Location l)
		{
			try
			{
				if (!m_world.Map.Bounds.Contains(l))
					return;

				if (m_world.Map.GetTerrain(l) == 1)
					m_world.Map.SetTerrain(l, 2);
				else
					m_world.Map.SetTerrain(l, 1);

				SendMap();
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		#endregion

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

			try
			{
				m_client.DeliverMapTerrains(locations.ToArray());
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Failed to send map to client");
				MyDebug.WriteLine(e.ToString());
				CleanUp();
			}
		}

		Change ChangeSelector(Change change)
		{
			if (change is EnvironmentChange)
			{
				EnvironmentChange ec = (EnvironmentChange)change;
				if (ec.MapID == m_player.Environment.ObjectID)
					change = new LocationChange(m_world.FindObject(ec.ObjectID), ec.Location, ec.Location);
				else
					return null;
			}

			if (change is LocationChange)
			{
				LocationChange lc = (LocationChange)change;
				if (!m_player.Sees(lc.SourceLocation) && !m_player.Sees(lc.TargetLocation))
				{
					MyDebug.WriteLine("plr doesn't see ob at {0}, skipping change", lc.SourceLocation);
					return null;
				}
			}
			// send only changes that the player sees and needs to know

			return change;
		}

		void m_world_ChangesEvent(Change[] changes)
		{
			MyDebug.WriteLine("ChangesEvent plr id {0}", m_player.ObjectID);
			Debug.Indent();
			foreach(Change c  in changes)
				MyDebug.WriteLine(c.ToString());
			Debug.Unindent();

			IEnumerable<Change> arr = changes.Select<Change, Change>(ChangeSelector).Where(c => { return c != null; });

			try
			{
				m_client.DeliverChanges(arr.ToArray());
				SendMap(); // xxx
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Failed to send changes to client");
				MyDebug.WriteLine(e.ToString());
				CleanUp();
			}
		}
	}
}
