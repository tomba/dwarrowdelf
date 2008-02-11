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
		Living m_player;
		InteractiveActor m_actor;

		public ServerService()
		{
			MyDebug.WriteLine("New ServerService");
		}

		void CleanUp()
		{
			m_player.Actor = null;

			m_world.AddChange(new ObjectEnvironmentChange(m_player, ObjectID.NullObjectID, new Location()));
			m_world.ProcessChanges();

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

				m_player = new Living(m_world);
				m_player.ClientCallback = m_client;
				m_actor = new InteractiveActor();
				m_player.Actor = m_actor;

				MyDebug.WriteLine("Player ob id {0}", m_player.ObjectID);

				m_client.LoginReply(m_player.ObjectID, m_player.VisionRange);

				if (!m_player.MoveTo(m_world.Map, new Location(0, 0)))
					throw new Exception("Unable to move player");

				m_world.ProcessChanges();
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
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

				m_world.ProcessChanges();
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		#endregion
	}
}
