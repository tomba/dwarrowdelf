﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Threading;
using System.Diagnostics;
using System.ServiceModel.Description;

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
			m_client = OperationContext.Current.GetCallbackChannel<IClientCallback>();
		}

		void CleanUp()
		{
			m_player.Cleanup();

			m_player.Actor = null;

			m_world.AddChange(new ObjectEnvironmentChange(m_player, m_player.Environment.ObjectID, m_player.Location,
				ObjectID.NullObjectID, new Location()));
			m_world.ProcessChanges();

			m_world.HandleChangesEvent -= HandleChanges;

			m_client = null;
			m_player = null;
			m_world = null;

			World.TheMonster.ClientCallback = null;
		}


		#region IServerService Members

		public void Login(string name)
		{
			try
			{
				MyDebug.WriteLine("Login {0}", name);

				m_world = World.TheWorld;

				m_player = new Living(m_world);
				m_player.SymbolID = 3;
				m_player.ClientCallback = m_client;
				m_actor = new InteractiveActor();
				m_player.Actor = m_actor;

				ItemObject item = new ItemObject(m_world);
				item.Name = "itemi1";
				item.SymbolID = 4;
				m_player.Inventory.Add(item);

				item = new ItemObject(m_world);
				item.Name = "itemi2";
				item.SymbolID = 5;
				m_player.Inventory.Add(item);

				MyDebug.WriteLine("Player ob id {0}", m_player.ObjectID);

				m_client.LoginReply(m_player.ObjectID);
				m_client.DeliverMessage(new ClientMsgs.LivingData()
				{
					ObjectID = m_player.ObjectID,
					SymbolID = 3,
					Name = "player",
					VisionRange = m_player.VisionRange
				}
				);

				if (!m_player.MoveTo(m_world.Map, new Location(0, 0)))
					throw new Exception("Unable to move player");

				m_player.SendInventory();

				//World.TheMonster.ClientCallback = m_client;
				m_world.HandleChangesEvent += HandleChanges;
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

		Dictionary<MapLevel, HashSet<Location>> m_knownLocations = new Dictionary<MapLevel, HashSet<Location>>();

		void HandleChanges(Change[] changes)
		{
			ClientMsgs.Message[] arr = changes.
				Select<Change, ClientMsgs.Message>(Living.ChangeToMessage).
				ToArray();

			m_client.DeliverMessages(arr);

			Living[] livings = m_world.GetLivings();
			var newLocs = new Dictionary<MapLevel, HashSet<Location>>();
			
			foreach (Living l in livings)
			{
				if (l.Environment == null)
					continue;

				if (!newLocs.ContainsKey(l.Environment))
					newLocs[l.Environment] = new HashSet<Location>();

				newLocs[l.Environment].UnionWith(l.GetNewLocations());
			}

			foreach (var kvp in newLocs)
				SendNewTerrains(kvp.Key, kvp.Value.ToArray());
		}

		void SendNewTerrains(MapLevel env, Location[] newLocations)
		{
			if (newLocations.Length == 0)
				return;

			var terrains = new ClientMsgs.MapData[newLocations.Length];
			int i = 0;
			foreach (Location l in newLocations)
			{
				ObjectID[] obs = null;
				List<ServerGameObject> items = env.GetContents(l);
				if (items != null)
					obs = items.Select(o => o.ObjectID).ToArray();

				terrains[i++] = new ClientMsgs.MapData()
				{
					Location = l,
					Terrain = env.GetTerrain(l),
					Objects = obs 
				};
			}

			var msgs = new ClientMsgs.Message[] { new ClientMsgs.TerrainData() { MapDataList = terrains } };
			m_client.DeliverMessages(msgs);
		}
	}
}
