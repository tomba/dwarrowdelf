using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	sealed class VisionTrackerLOS : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;

		List<LivingObject> m_livings = new List<LivingObject>();

		HashSet<IntPoint3> m_oldKnownLocations = new HashSet<IntPoint3>();
		HashSet<MovableObject> m_oldKnownObjects = new HashSet<MovableObject>();

		HashSet<IntPoint3> m_newKnownLocations = new HashSet<IntPoint3>();
		HashSet<MovableObject> m_newKnownObjects = new HashSet<MovableObject>();

		public VisionTrackerLOS(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.LivingLOS);

			m_player = player;
			m_environment = env;
		}

		public override void AddLiving(LivingObject living)
		{
			m_livings.Add(living);

			if (m_livings.Count == 1)
				Start();
		}

		public override void RemoveLiving(LivingObject living)
		{
			var ok = m_livings.Remove(living);
			Debug.Assert(ok);

			if (m_livings.Count == 0)
				Stop();
		}

		void Start()
		{
			m_environment.World.WorldChanged += OnWorldChanged;

			m_environment.SendIntroTo(m_player);

			HandleNewTerrainsAndObjects();
		}

		void Stop()
		{
			m_environment.World.WorldChanged -= OnWorldChanged;
		}

		void OnWorldChanged(Change change)
		{
			if (change is ObjectMoveChange)
			{
				var c = (ObjectMoveChange)change;
				var mo = (MovableObject)c.Object;

				if ((c.Source == m_environment && EventIsNear(c.SourceLocation)) ||
					(c.Destination == m_environment && EventIsNear(c.DestinationLocation)))
					HandleNewTerrainsAndObjects();
			}
			else if (change is ObjectMoveLocationChange)
			{
				var c = (ObjectMoveLocationChange)change;
				var mo = (MovableObject)c.Object;

				if (mo.Environment == m_environment && (EventIsNear(c.SourceLocation) || EventIsNear(c.DestinationLocation)))
					HandleNewTerrainsAndObjects();
			}
			else if (change is MapChange)
			{
				var c = (MapChange)change;

				if (c.Environment == m_environment && EventIsNear(c.Location))
					HandleNewTerrainsAndObjects();
			}
		}

		bool EventIsNear(IntPoint3 p)
		{
			return m_livings.Any(l => (l.Location - p).ManhattanLength <= l.VisionRange);
		}

		public override bool Sees(IntPoint3 p)
		{
			return m_livings.Any(l => l.Sees(m_environment, p));
		}

		void HandleNewTerrainsAndObjects()
		{
			m_oldKnownLocations = m_newKnownLocations;
			m_newKnownLocations = CollectLocations();

			m_oldKnownObjects = m_newKnownObjects;
			m_newKnownObjects = CollectObjects(m_newKnownLocations);

			var revealedLocations = m_newKnownLocations.Except(m_oldKnownLocations);
			var revealedObjects = m_newKnownObjects.Except(m_oldKnownObjects);

			SendNewTerrains(revealedLocations);
			SendNewObjects(revealedObjects);
		}

		// Collect all locations that friendlies see
		HashSet<IntPoint3> CollectLocations()
		{
			var knownLocs = new HashSet<IntPoint3>();

			foreach (var l in m_livings)
			{
				var locList = l.GetVisibleLocations().Select(p => new IntPoint3(p.X, p.Y, l.Z));

				knownLocs.UnionWith(locList);
			}

			return knownLocs;
		}

		// Collect all objects in the given location map
		HashSet<MovableObject> CollectObjects(HashSet<IntPoint3> knownLocs)
		{
			var knownObs = new HashSet<MovableObject>();

			foreach (var p in knownLocs)
			{
				var obList = m_environment.GetContents(p);
				if (obList != null)
					knownObs.UnionWith(obList);
			}

			return knownObs;
		}

		void SendNewTerrains(IEnumerable<IntPoint3> revealedLocations)
		{
			if (revealedLocations.Any() == false)
				return;

			var msg = new Messages.MapDataTerrainsListMessage()
			{
				Environment = m_environment.ObjectID,
				TileDataList = revealedLocations.Select(l => new KeyValuePair<IntPoint3, TileData>(l, m_environment.GetTileData(l))).ToArray(),
			};

			m_player.Send(msg);
		}

		void SendNewObjects(IEnumerable<MovableObject> revealedObjects)
		{
			foreach (var ob in revealedObjects)
			{
				var vis = m_player.GetObjectVisibility(ob);
				Debug.Assert(vis != ObjectVisibility.None);
				ob.SendTo(m_player, vis);
			}
		}
	}
}
