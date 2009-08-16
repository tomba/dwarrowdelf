using System;
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

		#region IServerService Members

		public void Login(string name)
		{
			MyDebug.WriteLine("BeginLogin {0}", name);

			m_world = World.TheWorld;

			m_world.BeginInvoke(_Login, name);
		}

		void _Login(object data)
		{
			string name = (string)data;

			MyDebug.WriteLine("Login {0}", name);

			m_player = new Living(m_world);
			m_player.SymbolID = 3;
			m_player.Name = "player";
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

			ClientMsgs.MapData md = new ClientMsgs.MapData()
			{
				ObjectID = m_world.Map.ObjectID,
				VisibilityMode = m_world.Map.VisibilityMode,
			};
			m_client.DeliverMessage(md);

			if (m_seeAll || m_world.Map.VisibilityMode == VisibilityMode.AllVisible)
				SendAllTerrainsAndObjects(m_world);

			if (!m_player.MoveTo(m_world.Map, new IntPoint(0, 0)))
				throw new Exception("Unable to move player");

			m_player.SendInventory();

			m_world.HandleChangesEvent += HandleChanges;
		}

		public void Logout()
		{
			MyDebug.WriteLine("BeginLogout");

			m_world.BeginInvoke(_Logout);
		}

		void _Logout(object data)
		{
			MyDebug.WriteLine("Logout");

			m_player.Actor = null;

			m_world.HandleChangesEvent -= HandleChanges;

			m_player.Cleanup();

			m_client = null;
			m_player = null;
			m_world = null;
		}

		public void DoAction(GameAction action)
		{
			try
			{
				if (action.ObjectID != m_player.ObjectID)
					throw new Exception("Illegal ob id");

				// this is safe to call out of world thread (is it? =)
				m_actor.EnqueueAction(action);
			}
			catch (Exception e)
			{
				MyDebug.WriteLine("Uncaught exception");
				MyDebug.WriteLine(e.ToString());
			}
		}

		public void SetTiles(IntRect r, int type)
		{
			m_world.BeginInvokeInstant(_SetTiles, new object[] { r, type });
		}

		void _SetTiles(object data)
		{
			object[] arr = (object[])data;
			IntRect r = (IntRect)arr[0];
			int type = (int)arr[1];

			for (int y = r.Top; y < r.Bottom; ++y)
			{
				for (int x = r.Left; x < r.Right; ++x)
				{
					IntPoint p = new IntPoint(x, y);

					if (!m_world.Map.Bounds.Contains(p))
						continue;

					m_world.Map.SetTerrain(p, type);
				}
			}
		}

		#endregion

		// These are used to determine new tiles and objects in sight
		Dictionary<Environment, HashSet<IntPoint>> m_knownLocations = new Dictionary<Environment, HashSet<IntPoint>>();
		HashSet<ServerGameObject> m_knownObjects = new HashSet<ServerGameObject>();

		// this user sees all
		bool m_seeAll = false;

		void HandleChanges(Change[] changeArr)
		{
			// list of "friendly" livings that this player can observe
			Living[] livings = m_world.GetLivings();
			livings = new Living[] { m_player };

			SendChanges(livings, changeArr);

			// if the user sees all, no need to send new terrains/objects
			if (!m_seeAll && m_player.Environment.VisibilityMode != VisibilityMode.AllVisible)
				SendNewTerrainsAndObjects(livings);
		}

		void SendChanges(Living[] livings, Change[] changeArr)
		{
			// filter changes that livings see
			IEnumerable<Change> changes = changeArr;

			if (!m_seeAll)
				changes = changes.Where(c => livings.Any(l => l.ChangeFilter(c)));

			ClientMsgs.Message[] msgArr = changes.
				Select<Change, ClientMsgs.Message>(Living.ChangeToMessage).
				ToArray();

			m_client.DeliverMessages(msgArr);
		}

		void SendNewTerrainsAndObjects(Living[] livings)
		{
			var newKnownLocs = new Dictionary<Environment, HashSet<IntPoint>>();
			var newKnownObs = new HashSet<ServerGameObject>();
			
			foreach (Living l in livings)
			{
				if (l.Environment == null)
					continue;

				IEnumerable<IntPoint> locList = l.GetVisibleLocations();

				if (!newKnownLocs.ContainsKey(l.Environment))
					newKnownLocs[l.Environment] = new HashSet<IntPoint>();
				newKnownLocs[l.Environment].UnionWith(locList);

				foreach(IntPoint loc in locList)
				{
					var obList = l.Environment.GetContents(loc);
					if (obList == null)
						continue;
					newKnownObs.UnionWith(obList);
				}
			}

			var revealedLocs = new Dictionary<Environment, IEnumerable<IntPoint>>();

			foreach (var kvp in newKnownLocs)
			{
				if (m_knownLocations.ContainsKey(kvp.Key))
					revealedLocs[kvp.Key] = kvp.Value.Except(m_knownLocations[kvp.Key]);
				else
					revealedLocs[kvp.Key] = kvp.Value;
			}

			var revealedObs = newKnownObs.Except(m_knownObjects);

			m_knownLocations = newKnownLocs;
			m_knownObjects = newKnownObs;

			SendNewTerrains(revealedLocs);

			SendNewObjects(revealedObs);
		}

		void SendNewTerrains(Dictionary<Environment, IEnumerable<IntPoint>> revealedLocs)
		{
			foreach (var kvp in revealedLocs)
			{
				var env = kvp.Key;
				var newLocations = kvp.Value;

				var mapDataList = newLocations.Select(l => new ClientMsgs.MapTileData() { Location = l, Terrain = env.GetTerrainID(l) });
				var mapDataArr = mapDataList.ToArray();
				if (mapDataArr.Length == 0)
					continue;
				var msg = new ClientMsgs.TerrainData() { Environment = env.ObjectID, MapDataList = mapDataArr };

				m_client.DeliverMessage(msg);
			}
		}

		void SendNewObjects(IEnumerable<ServerGameObject> revealedObs)
		{
			foreach (var ob in revealedObs)
			{
				ClientMsgs.Message msg;

				if (ob is Living)
				{
					Living l = (Living)ob;
					msg = new ClientMsgs.LivingData()
					{
						ObjectID = l.ObjectID,
						SymbolID = l.SymbolID,
						Name = l.Name,
						VisionRange = l.VisionRange,
						Environment = l.Environment != null ? l.Environment.ObjectID : ObjectID.NullObjectID,
						Location = l.Location,
					};
				}
				else if (ob is ItemObject)
				{
					ItemObject item = (ItemObject)ob;
					msg = new ClientMsgs.ItemData()
					{
						ObjectID = item.ObjectID,
						SymbolID = item.SymbolID,
						Name = item.Name,
						Environment = item.Environment != null ? item.Environment.ObjectID : ObjectID.NullObjectID,
						Location = item.Location,
					};
				}
				else
					continue;

				m_client.DeliverMessage(msg);
			}
		}

		void SendAllTerrainsAndObjects(World world)
		{
			Environment env = world.Map;

			ClientMsgs.MapTileData[] mapDataArr = new ClientMsgs.MapTileData[env.Width * env.Height];

			for (int y = 0; y < env.Height; ++y)
			{
				for (int x = 0; x < env.Width; ++x)
				{
					IntPoint l = new IntPoint(x, y);
					ClientMsgs.MapTileData td = new ClientMsgs.MapTileData();
					td.Location = l;
					td.Terrain = env.GetTerrainID(l);
					mapDataArr[x + y * env.Width] = td;
				}
			}

			var msg = new ClientMsgs.TerrainData() { Environment = env.ObjectID, MapDataList = mapDataArr };

			m_client.DeliverMessage(msg);

			// XXX
			m_world.ForEachObject(o => SendNewObjects(new ServerGameObject[] { o }));
		}
	}
}
