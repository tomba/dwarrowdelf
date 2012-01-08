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
		public virtual void HandleNewControllable(LivingObject living) { } // XXX update vision map
	}

	sealed class AdminVisionTracker : VisionTrackerBase
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

	sealed class AllVisibleVisionTracker : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;

		public AllVisibleVisionTracker(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.AllVisible);

			m_player = player;
			m_environment = env;
		}

		public override void Start()
		{
			m_environment.SendTo(m_player, ObjectVisibility.Public);
		}

		public override void Stop()
		{
		}

		public override bool Sees(IntPoint3D p)
		{
			return m_environment.Contains(p);
		}
	}

	/// <summary>
	/// All "open" tiles, and all tiles adjacent to those tiles, can be seen
	/// </summary>
	sealed class GlobalFOVVisionTracker : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;
		bool[, ,] m_visibilityArray;

		public GlobalFOVVisionTracker(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.GlobalFOV);

			m_player = player;
			m_environment = env;

			var bounds = env.Bounds;

			m_visibilityArray = new bool[bounds.Depth, bounds.Height, bounds.Width];

			foreach (var p in bounds.Range())
				m_visibilityArray[p.Z, p.Y, p.X] = EnvironmentHelpers.CanSeeThrough(env, p) || EnvironmentHelpers.CanBeSeen(env, p);
		}

		public override void Start()
		{
			m_environment.TerrainOrInteriorChanged += OnTerrainOrInteriorChanged;

			m_environment.SendTo(m_player, ObjectVisibility.Public);
		}

		public override void Stop()
		{
			m_environment.TerrainOrInteriorChanged -= OnTerrainOrInteriorChanged;
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

		// XXX move to EnvironmentHelpers?
		bool IsSeeThrough(TileData data)
		{
			var terrain = Terrains.GetTerrain(data.TerrainID);
			var interior = Interiors.GetInterior(data.InteriorID);

			return terrain.IsSeeThrough && interior.IsSeeThrough;
		}

		// XXX move to EnvironmentHelpers?
		bool IsSeeThroughDown(TileData data)
		{
			var terrain = Terrains.GetTerrain(data.TerrainID);

			return terrain.IsSeeThroughDown;
		}

		void OnTerrainOrInteriorChanged(IntPoint3D location, TileData oldData, TileData newData)
		{
			var env = m_environment;

			bool revealPlanar = IsSeeThrough(oldData) == false && IsSeeThrough(newData) == true;
			bool revealDown = IsSeeThroughDown(oldData) == false && IsSeeThroughDown(newData) == true;

			List<IntPoint3D> revealed = new List<IntPoint3D>();

			if (revealPlanar)
			{
				revealed.AddRange(DirectionExtensions.PlanarDirections
					.Select(dir => location + dir)
					.Where(p => env.Contains(p))
					.Where(p => GetVisible(p) == false));
			}

			if (revealDown)
			{
				var p = location + Direction.Down;

				if (env.Contains(p) && GetVisible(p) == false)
					revealed.Add(p);
			}

			if (revealed.Count > 0)
			{
				foreach (var p in revealed)
					SetVisible(p);

				// Send new tiles

				var msg = new Messages.MapDataTerrainsListMessage()
				{
					Environment = env.ObjectID,
					TileDataList = revealed.Select(l => new Tuple<IntPoint3D, TileData>(l, env.GetTileData(l))).ToArray(),
				};

				m_player.Send(msg);

				// Send new objects

				foreach (var ob in revealed.SelectMany(p => env.GetContents(p)))
				{
					var vis = m_player.GetObjectVisibility(ob);
					Debug.Assert(vis != ObjectVisibility.None);
					ob.SendTo(m_player, vis);
				}
			}
		}
	}

	sealed class LOSVisionTracker : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;

		HashSet<IntPoint3D> m_oldKnownLocations = new HashSet<IntPoint3D>();
		HashSet<MovableObject> m_oldKnownObjects = new HashSet<MovableObject>();

		HashSet<IntPoint3D> m_newKnownLocations = new HashSet<IntPoint3D>();
		HashSet<MovableObject> m_newKnownObjects = new HashSet<MovableObject>();

		public LOSVisionTracker(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.LivingLOS);

			m_player = player;
			m_environment = env;
		}

		public override void Start()
		{
			m_environment.World.WorkEnded += HandleEndOfWork;

			m_player.Send(new Messages.ObjectDataMessage(new MapData()
			{
				ObjectID = m_environment.ObjectID,
				VisibilityMode = m_environment.VisibilityMode,
				HomeLocation = m_environment.HomeLocation,
			}));
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

		void HandleNewTerrainsAndObjects(IList<LivingObject> friendlies)
		{
			m_oldKnownLocations = m_newKnownLocations;
			m_newKnownLocations = CollectLocations(friendlies);

			m_oldKnownObjects = m_newKnownObjects;
			m_newKnownObjects = CollectObjects(m_newKnownLocations);

			var revealedLocations = CollectRevealedLocations(m_oldKnownLocations, m_newKnownLocations);
			var revealedObjects = CollectRevealedObjects(m_oldKnownObjects, m_newKnownObjects);

			SendNewTerrains(revealedLocations);
			SendNewObjects(revealedObjects);
		}

		// Collect all locations that friendlies see
		HashSet<IntPoint3D> CollectLocations(IEnumerable<LivingObject> friendlies)
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
		HashSet<MovableObject> CollectObjects(HashSet<IntPoint3D> knownLocs)
		{
			var knownObs = new HashSet<MovableObject>();

			foreach (var p in knownLocs)
			{
				// XXX
				var obList = ((EnvironmentObject)m_environment).GetContents(p);
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
		static IEnumerable<MovableObject> CollectRevealedObjects(HashSet<MovableObject> oldObjects, HashSet<MovableObject> newObjects)
		{
			return newObjects.Except(oldObjects);
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
