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

		// this user sees all
		bool m_seeAll = true;

		public ServerService()
		{
			MyDebug.WriteLine("New ServerService");
			m_client = OperationContext.Current.GetCallbackChannel<IClientCallback>();
		}

		#region IServerService Members

		public void LogOn(string name)
		{
			m_world = World.TheWorld;

			m_world.BeginInvoke(_LogOn, name);
		}

		void _LogOn(object data)
		{
			string name = (string)data;

			MyDebug.WriteLine("LogOn {0}", name);

			m_client.LogOnReply();

			ClientMsgs.MapData md = new ClientMsgs.MapData()
			{
				ObjectID = m_world.Map.ObjectID,
				VisibilityMode = m_world.Map.VisibilityMode,
			};
			m_client.DeliverMessage(md);

			if (m_seeAll || m_world.Map.VisibilityMode == VisibilityMode.AllVisible)
				SendAllTerrainsAndObjects(m_world);

			m_world.HandleChangesEvent += HandleChanges;
		}

		public void LogOff()
		{
			m_world.BeginInvoke(_LogOff);
		}

		void _LogOff(object data)
		{
			MyDebug.WriteLine("Logout");

			if (m_player != null)
				_LogOffChar(null);

			m_world.HandleChangesEvent -= HandleChanges;

			m_client = null;
			m_world = null;
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

		public void ProceedTurn()
		{
			m_world.BeginInvoke(_ProceedTurn);
		}

		public void _ProceedTurn(object data)
		{
			MyDebug.WriteLine("ProceedTurn command");
			m_world.RequestTurn();
		}

		/* functions for livings */
		public void LogOnChar(string name)
		{
			m_world.BeginInvoke(_LogOnChar, name);
		}

		public void _LogOnChar(object data)
		{
			string name = (string)data;

			MyDebug.WriteLine("LogOnChar {0}", name);

			var obs = m_world.AreaData.Objects;

			m_player = new Living(m_world);
			m_player.SymbolID = obs.Single(o => o.Name == "Player").SymbolID; ;
			m_player.Name = "player";
			m_player.ClientCallback = m_client;
			m_actor = new InteractiveActor();
			m_player.Actor = m_actor;

			MyDebug.WriteLine("Player ob id {0}", m_player.ObjectID);

			m_client.LogOnCharReply(m_player.ObjectID);


			ItemObject item = new ItemObject(m_world);
			item.Name = "jalokivi1";
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID;
			item.MoveTo(m_player);

			item = new ItemObject(m_world);
			item.Name = "jalokivi2";
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID;
			item.Color = new GameColor(0, 255, 0);
			item.MoveTo(m_player);

			if (!m_player.MoveTo(m_world.Map, new IntPoint(0, 0)))
				throw new Exception("Unable to move player");

			m_player.SendInventory();

			var pet = new Living(m_world);
			pet.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
			pet.Name = "lemmikki";
			var petAI = new PetActor(pet, m_player);
			pet.Actor = petAI;
			pet.MoveTo(m_player.Environment, m_player.Location + new IntVector(1, 0));
			
		}

		public void LogOffChar()
		{
			m_world.BeginInvokeInstant(_LogOffChar);
		}

		void _LogOffChar(object data)
		{
			m_actor.EnqueueAction(new WaitAction(0, m_player, 1));
			m_world.BeginInvoke(__LogOffChar);
		}

		void __LogOffChar(object data)
		{
			MyDebug.WriteLine("LogOffChar");

			m_player.Actor = null;
			m_player.Cleanup();
			m_player = null;

			m_client.LogOffCharReply();
		}

		public void DoAction(GameAction action)
		{
			try
			{
				if (action.ActorObjectID != m_player.ObjectID)
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

		#endregion

		// These are used to determine new tiles and objects in sight
		Dictionary<Environment, HashSet<IntPoint>> m_knownLocations = new Dictionary<Environment, HashSet<IntPoint>>();
		HashSet<ServerGameObject> m_knownObjects = new HashSet<ServerGameObject>();

		void HandleChanges(Change[] changeArr)
		{
			// list of "friendly" livings that this player can observe
			Living[] livings;
			if (m_seeAll)
				livings = m_world.GetLivings();
			else if (m_player != null)
				livings = new Living[] { m_player };
			else
				livings = new Living[0];

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
			
			/* this sends info about objects that appeared from some other env.
			 * I don't like this here */
			List<ServerGameObject> newObjects = new List<ServerGameObject>();
			foreach (Change change in changes)
			{
				if (!(change is ObjectMoveChange))
					continue;
				ObjectMoveChange mc = (ObjectMoveChange)change;
				if (livings.Any(l => mc.SourceMapID != l.Environment.ObjectID))
					newObjects.Add((ServerGameObject)mc.Target);
			}
			SendNewObjects(newObjects);

			var msgs = changes.Select<Change, ClientMsgs.Message>(Living.ChangeToMessage);

			m_client.DeliverMessages(msgs);
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

				var mapDataList = newLocations.Select(l => new ClientMsgs.MapTileData() { Location = l, TerrainID = env.GetTerrainID(l) });
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
					msg = l.Serialize();
				}
				else if (ob is ItemObject)
				{
					ItemObject item = (ItemObject)ob;
					msg = item.Serialize();
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
					td.TerrainID = env.GetTerrainID(l);
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
