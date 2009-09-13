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
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession,
		ConcurrencyMode = ConcurrencyMode.Single, MaxItemsInObjectGraph = 1147483647)]
	public class ServerService : IServerService
	{
		IClientCallback m_client;

		World m_world;
		Living m_player;
		InteractiveActor m_actor;

		// this user sees all
		bool m_seeAll = false;

		List<Living> m_friendlies = new List<Living>();

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
			{
				// When we log on, we need to send everything
				// XXX only this env
				Environment env = m_world.Map;
				var msg = env.Serialize();
				m_client.DeliverMessage(msg);
			}

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

		public void SetTiles(ObjectID mapID, IntCube cube, int type)
		{
			m_world.BeginInvokeInstant(_SetTiles, new object[] { mapID, cube, type });
		}

		void _SetTiles(object data)
		{
			object[] arr = (object[])data;
			ObjectID mapID = (ObjectID)arr[0];
			IntCube r = (IntCube)arr[1];
			int type = (int)arr[2];

			if (mapID != m_world.Map.ObjectID)
				throw new Exception();

			foreach (var p in r.Range())
			{
				if (!m_world.Map.Bounds.Contains(p))
					continue;

				m_world.Map.SetTerrain(p, type);
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

			m_friendlies.Add(m_player);
			m_client.LogOnCharReply(m_player.ObjectID);


			ItemObject item = new ItemObject(m_world);
			item.Name = "jalokivi1";
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID;
			item.MoveTo(m_player);

			item = new ItemObject(m_world);
			item.Name = "jalokivi2";
			item.SymbolID = obs.Single(o => o.Name == "Gem").SymbolID;
			item.Color = GameColors.Green;
			item.MoveTo(m_player);

			if (!m_player.MoveTo(m_world.Map, new IntPoint3D(0, 0, 0)))
				throw new Exception("Unable to move player");

			m_player.SendInventory();

			var pet = new Living(m_world);
			pet.SymbolID = obs.Single(o => o.Name == "Monster").SymbolID;
			pet.Name = "lemmikki";
			var petAI = new PetActor(pet, m_player);
			pet.Actor = petAI;
			m_friendlies.Add(pet);
			m_client.LogOnCharReply(pet.ObjectID);

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

			m_friendlies.Remove(m_player);

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
		Dictionary<Environment, HashSet<IntPoint3D>> m_knownLocations = new Dictionary<Environment, HashSet<IntPoint3D>>();
		HashSet<ServerGameObject> m_knownObjects = new HashSet<ServerGameObject>();

		void HandleChanges(IEnumerable<Change> changeArr)
		{
			IEnumerable<ClientMsgs.Message> msgs = new List<ClientMsgs.Message>();

			// if the user sees all, no need to send new terrains/objects
			if (!m_seeAll)
			{
				var m = SendNewTerrainsAndObjects(m_friendlies);
				msgs = msgs.Concat(m);
			}

			var changeMsgs = SendChanges(m_friendlies, changeArr);
			msgs = msgs.Concat(changeMsgs);

			m_client.DeliverMessages(msgs);
		}

		IEnumerable<ClientMsgs.Message> SendChanges(IEnumerable<Living> friendlies, IEnumerable<Change> changes)
		{
			IEnumerable<ClientMsgs.Message> msgs = new List<ClientMsgs.Message>();

			if (m_seeAll)
			{
				// If the user sees all, we don't collect newly visible objects. However,
				// we still need to tell about newly created objects.
				var newObjects = changes.
					OfType<ObjectMoveChange>().
					Where(c => c.SourceMapID == ObjectID.NullObjectID).
					Select(c => (ServerGameObject)c.Object);

				var newObMsgs = SendNewObjects(newObjects);
				msgs = msgs.Concat(newObMsgs);
			}
			else
			{
				// We don't collect newly visible terrains/objects on AllVisible maps.
				// However, we still need to tell about newly created objects that come
				// to AllVisible maps.
				var newObjects = changes.OfType<ObjectMoveChange>().
					Where(c => c.Source != c.Destination &&
						c.Destination is Environment &&
						((Environment)c.Destination).VisibilityMode == VisibilityMode.AllVisible).
					Select(c => (ServerGameObject)c.Object);

				var newObMsgs = SendNewObjects(newObjects);
				msgs = msgs.Concat(newObMsgs);

				// filter changes that friendlies see
				changes = changes.Where(c => friendlies.Any(l => l.ChangeFilter(c)));
			}

			var changeMsgs = changes.Select<Change, ClientMsgs.Message>(Living.ChangeToMessage);
			
			// NOTE: send changes last, so that object/map/tile information has already
			// been received by the client
			msgs = msgs.Concat(changeMsgs);

			return msgs;
		}

		IEnumerable<ClientMsgs.Message> SendNewTerrainsAndObjects(IEnumerable<Living> friendlies)
		{
			// Collect all locations that friendlies see
			var newKnownLocs = new Dictionary<Environment, HashSet<IntPoint3D>>();
			foreach (Living l in friendlies)
			{
				if (l.Environment == null)
					continue;

				// for AllVisible we already know all the objects and terrains
				if (l.Environment.VisibilityMode == VisibilityMode.AllVisible)
					continue;

				IEnumerable<IntPoint3D> locList = l.GetVisibleLocations().Select(p => new IntPoint3D(p.X, p.Y, l.Z));

				if (!newKnownLocs.ContainsKey(l.Environment))
					newKnownLocs[l.Environment] = new HashSet<IntPoint3D>();
				newKnownLocs[l.Environment].UnionWith(locList);
			}

			// Collect objects in visible locations
			var newKnownObs = new HashSet<ServerGameObject>();
			foreach (var kvp in newKnownLocs)
			{
				var env = kvp.Key;
				var newLocs = kvp.Value;

				foreach (var p in newLocs)
				{
					var obList = env.GetContents(p);
					if (obList == null)
						continue;
					newKnownObs.UnionWith(obList);
				}
			}

			// Collect locations that are newly visible
			var revealedLocs = new Dictionary<Environment, IEnumerable<IntPoint3D>>();
			foreach (var kvp in newKnownLocs)
			{
				if (m_knownLocations.ContainsKey(kvp.Key))
					revealedLocs[kvp.Key] = kvp.Value.Except(m_knownLocations[kvp.Key]);
				else
					revealedLocs[kvp.Key] = kvp.Value;
			}

			// Collect objects that are newly visible
			var revealedObs = newKnownObs.Except(m_knownObjects);

			m_knownLocations = newKnownLocs;
			m_knownObjects = newKnownObs;

			var terrainMsgs = SendNewTerrains(revealedLocs);
			var objectMsgs = SendNewObjects(revealedObs);

			return terrainMsgs.Concat(objectMsgs);
		}

		IEnumerable<ClientMsgs.Message> SendNewTerrains(Dictionary<Environment, IEnumerable<IntPoint3D>> revealedLocs)
		{
			var msgs = revealedLocs.Select(kvp => (ClientMsgs.Message)new ClientMsgs.TerrainData()
			{
				Environment = kvp.Key.ObjectID,
				MapDataList = kvp.Value.Select(l =>
					new ClientMsgs.MapTileData()
					{
						Location = l,
						TerrainID = kvp.Key.GetTerrainID(l),
					}).ToArray()
				// XXX there seems to be a problem serializing this.
				// evaluating it with ToArray() fixes it
			});

			return msgs;
		}

		IEnumerable<ClientMsgs.Message> SendNewObjects(IEnumerable<ServerGameObject> revealedObs)
		{
			var msgs = revealedObs.Select(o => o.Serialize());
			return msgs;
		}
	}
}
