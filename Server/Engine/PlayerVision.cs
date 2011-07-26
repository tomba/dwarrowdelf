using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	abstract class VisionTrackerBase : IVisionTracker
	{
		public abstract void Start();
		public abstract void Stop();
		public abstract bool Sees(IntPoint3D p);
		public virtual void HandleNewControllable(Living living) { } // XXX update vision map
	}

	class AdminVisionTracker : VisionTrackerBase
	{
		public static AdminVisionTracker Tracker = new AdminVisionTracker();

		public override bool Sees(IntPoint3D p)
		{
			return true;
		}

		public override void Start()
		{
		}

		public override void Stop()
		{
		}
	}

	class AllVisibleVisionTracker : VisionTrackerBase
	{
		Player m_player;
		Environment m_environment;

		public AllVisibleVisionTracker(Player player, Environment env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.AllVisible);

			m_player = player;
			m_environment = env;
		}

		public override void Start()
		{
			m_environment.SendTo(m_player);
		}

		public override void Stop()
		{
		}

		public override bool Sees(IntPoint3D p)
		{
			return m_environment.Contains(p);
		}
	}

	class GlobalFOVVisionTracker : VisionTrackerBase
	{
		Player m_player;
		Environment m_environment;
		bool[, ,] m_visibilityArray;

		public GlobalFOVVisionTracker(Player player, Environment env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.GlobalFOV);

			m_player = player;
			m_environment = env;

			var bounds = env.Bounds;

			m_visibilityArray = new bool[bounds.Depth, bounds.Height, bounds.Width];

			foreach (var p in bounds.Range())
			{
				bool visible = true;

				if (env.GetTerrain(p).IsBlocker)
				{
					bool hidden = DirectionExtensions.PlanarDirections
						.Select(d => p + d)
						.Where(pp => env.Contains(pp))
						.All(pp => !env.GetTerrain(pp).IsSeeThrough);

					if (hidden && env.Contains(p + Direction.Up))
						hidden = !env.GetTerrain(p + Direction.Up).IsSeeThroughDown;

					visible = !hidden;
				}

				m_visibilityArray[p.Z, p.Y, p.X] = visible;
			}
		}

		public override void Start()
		{
			m_environment.TerrainChanged += OnTerrainChanged;

			m_environment.SendTo(m_player);
		}

		public override void Stop()
		{
			m_environment.TerrainChanged -= OnTerrainChanged;
		}

		public override bool Sees(IntPoint3D p)
		{
			if (!m_environment.Contains(p))
				return false;

			return GetVisible(p);
		}

		bool GetVisible(IntPoint3D p)
		{
			return m_visibilityArray[p.Z, p.Y, p.X];
		}

		void SetVisible(IntPoint3D p)
		{
			m_visibilityArray[p.Z, p.Y, p.X] = true;
		}

		void OnTerrainChanged(IntPoint3D p, TileData oldData, TileData newData)
		{
			var env = m_environment;

			var oldTerrain = Terrains.GetTerrain(oldData.TerrainID);
			var newTerrain = Terrains.GetTerrain(newData.TerrainID);

			if (oldTerrain.IsSeeThrough != newTerrain.IsSeeThrough)
			{
				var revealed = DirectionExtensions.PlanarDirections
					.Select(dir => p + dir)
					.Where(pp => env.Contains(pp))
					.Where(pp => GetVisible(pp) == false)
					.ToArray();

				foreach (var pp in revealed)
					SetVisible(pp);

				if (revealed.Length > 0)
				{
					// XXX this should also send the objects in that tile

					var msg = new Messages.MapDataTerrainsListMessage()
					{
						Environment = env.ObjectID,
						TileDataList = revealed.Select(l =>
							new Tuple<IntPoint3D, TileData>(l, env.GetTileData(l))
							).ToArray(),
					};

					m_player.Send(msg);
				}
			}

			if (oldTerrain.IsSeeThroughDown != newTerrain.IsSeeThroughDown)
			{
				var pp = p + Direction.Down;

				if (env.Contains(pp) && GetVisible(pp) == false)
				{
					SetVisible(pp);

					var msg = new Messages.MapDataTerrainsListMessage()
					{
						Environment = env.ObjectID,
						TileDataList = new Tuple<IntPoint3D, TileData>[] { new Tuple<IntPoint3D, TileData>(pp, env.GetTileData(pp)) },
					};

					m_player.Send(msg);
				}
			}
		}
	}

	class LOSVisionTracker : VisionTrackerBase
	{
		Player m_player;
		Environment m_environment;

		HashSet<IntPoint3D> m_oldKnownLocations = new HashSet<IntPoint3D>();
		HashSet<ServerGameObject> m_oldKnownObjects = new HashSet<ServerGameObject>();

		HashSet<IntPoint3D> m_newKnownLocations = new HashSet<IntPoint3D>();
		HashSet<ServerGameObject> m_newKnownObjects = new HashSet<ServerGameObject>();

		public LOSVisionTracker(Player player, Environment env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.LivingLOS);

			m_player = player;
			m_environment = env;
		}

		public override void Start()
		{
			m_environment.World.WorkEnded += HandleEndOfWork;

			var msg = new Dwarrowdelf.Messages.MapDataMessage()
			{
				Environment = m_environment.ObjectID,
				HomeLocation = m_environment.HomeLocation,
				VisibilityMode = m_environment.VisibilityMode,
			};

			m_player.Send(msg);
		}

		public override void Stop()
		{
			m_environment.World.WorkEnded -= HandleEndOfWork;
		}

		public override bool Sees(IntPoint3D p)
		{
			return m_player.Controllables.Any(l => l.Sees(m_environment, p));
		}

		// Called from the world at the end of work
		public void HandleEndOfWork()
		{
			HandleNewTerrainsAndObjects(m_player.Controllables);
		}

		void HandleNewTerrainsAndObjects(IList<Living> friendlies)
		{
			m_oldKnownLocations = m_newKnownLocations;
			m_newKnownLocations = CollectLocations(friendlies);

			m_oldKnownObjects = m_newKnownObjects;
			m_newKnownObjects = CollectObjects(m_newKnownLocations);

			var revealedLocations = CollectRevealedLocations(m_oldKnownLocations, m_newKnownLocations);
			var revealedObjects = CollectRevealedObjects(m_oldKnownObjects, m_newKnownObjects);
			//var revealedEnvironments = m_newKnownLocations.Keys.Except(m_knownEnvironments).ToArray();

			//m_knownEnvironments.UnionWith(revealedEnvironments);

			//SendNewEnvironments(revealedEnvironments);
			SendNewTerrains(revealedLocations);
			SendNewObjects(revealedObjects);
		}

		// Collect all locations that friendlies see
		HashSet<IntPoint3D> CollectLocations(IEnumerable<Living> friendlies)
		{
			var knownLocs = new HashSet<IntPoint3D>();

			foreach (var l in friendlies)
			{
				if (l.Environment != m_environment)
					continue;

				var locList = l.GetVisibleLocations().Select(p => new IntPoint3D(p.X, p.Y, l.Z));

				knownLocs.UnionWith(locList);
			}

			return knownLocs;
		}

		// Collect all objects in the given location map
		HashSet<ServerGameObject> CollectObjects(HashSet<IntPoint3D> knownLocs)
		{
			var knownObs = new HashSet<ServerGameObject>();

			foreach (var p in knownLocs)
			{
				// XXX
				var obList = ((Environment)m_environment).GetContents(p);
				if (obList != null)
					knownObs.UnionWith(obList);
			}

			return knownObs;
		}

		// Collect locations that are newly visible
		static HashSet<IntPoint3D> CollectRevealedLocations(HashSet<IntPoint3D> oldLocs, HashSet<IntPoint3D> newLocs)
		{
			return new HashSet<IntPoint3D>(newLocs.Except(oldLocs));
		}

		// Collect objects that are newly visible
		static IEnumerable<ServerGameObject> CollectRevealedObjects(HashSet<ServerGameObject> oldObjects, HashSet<ServerGameObject> newObjects)
		{
			return newObjects.Except(oldObjects);
		}

		private void SendNewEnvironments(IEnumerable<Environment> revealedEnvironments)
		{
			// send full data for AllVisible envs, and intro for other maps
			foreach (var env in revealedEnvironments)
			{
				if (env.VisibilityMode == VisibilityMode.AllVisible || env.VisibilityMode == VisibilityMode.GlobalFOV)
				{
					env.SendTo(m_player);
				}
				else
				{
					var msg = new Messages.MapDataMessage()
					{
						Environment = env.ObjectID,
						VisibilityMode = env.VisibilityMode,
					};
					m_player.Send(msg);
				}
			}
		}

		void SendNewTerrains(IEnumerable<IntPoint3D> revealedLocations)
		{
			var msg = new Messages.MapDataTerrainsListMessage()
			{
				Environment = m_environment.ObjectID,
				TileDataList = revealedLocations.Select(l => new Tuple<IntPoint3D, TileData>(l, m_environment.GetTileData(l))).ToArray(),
			};

			m_player.Send(msg);
		}

		void SendNewObjects(IEnumerable<ServerGameObject> revealedObjects)
		{
			var msgs = revealedObjects.Select(o => new Dwarrowdelf.Messages.ObjectDataMessage() { ObjectData = o.Serialize() });
			m_player.Send(msgs);
		}
	}
}
