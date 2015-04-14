using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	/// <summary>
	/// All "open" tiles, and all tiles adjacent to those tiles, can be seen
	/// </summary>
	sealed class VisionTrackerGlobalFOV : VisionTrackerBase
	{
		Player m_player;
		EnvironmentObject m_environment;

		int m_livingCount;
		bool[, ,] m_visibilityArray;

		public VisionTrackerGlobalFOV(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.GlobalFOV);

			m_player = player;
			m_environment = env;
		}

		public override void AddLiving(LivingObject living)
		{
			if (m_livingCount == 0)
				Start();

			m_livingCount++;
		}

		public override void RemoveLiving(LivingObject living)
		{
			m_livingCount--;
			Debug.Assert(m_livingCount >= 0);

			if (m_livingCount == 0)
				Stop();
		}

		void Start()
		{
			InitializeVisibilityArray();

			m_environment.TerrainOrInteriorChanged += OnTerrainOrInteriorChanged;

			m_environment.SendTo(m_player, ObjectVisibility.Public);
		}

		void Stop()
		{
			m_environment.TerrainOrInteriorChanged -= OnTerrainOrInteriorChanged;

			m_visibilityArray = null;
		}

		public override bool Sees(IntVector3 p)
		{
			if (!m_environment.Contains(p))
				return false;

			return GetVisible(p);
		}

		void InitializeVisibilityArray()
		{
			var env = m_environment;

			var bounds = env.Size;

			m_visibilityArray = new bool[bounds.Depth, bounds.Height, bounds.Width];

			var sw = Stopwatch.StartNew();

			// XXX the optimization is not quite right. What if the player has dug a deep tunnel, and then blocked it.
			// but let's keep this for now to speed things up.
			// Perhaps the visibility array should be saved.
			for (int z = bounds.Depth - 1; z >= 0; --z)
			{
				bool lvlIsHidden = true;

				Parallel.For(0, bounds.Height, y =>
				{
					for (int x = 0; x < bounds.Width; ++x)
					{
						var p = new IntVector3(x, y, z);

						var vis = env.GetTileData(p).IsSeeThrough || env.CanBeSeen(p);

						if (vis)
						{
							lvlIsHidden = false;
							m_visibilityArray[p.Z, p.Y, p.X] = true;
						}
					}
				});

				// if the whole level is not visible, the levels below cannot be seen either
				if (lvlIsHidden)
					break;
			}

			sw.Stop();

			Trace.TraceInformation("Initialize visibilityarray took {0} ms", sw.ElapsedMilliseconds);
		}

		bool GetVisible(IntVector3 p)
		{
			if (m_visibilityArray == null)
				return false;

			return m_visibilityArray[p.Z, p.Y, p.X];
		}

		void SetVisible(IntVector3 p)
		{
			m_visibilityArray[p.Z, p.Y, p.X] = true;
		}

		class MyTarget : IBFSTarget
		{
			VisionTrackerGlobalFOV m_tracker;
			IEnvironmentObject m_env;

			public MyTarget(IEnvironmentObject env, VisionTrackerGlobalFOV tracker)
			{
				m_env = env;
				m_tracker = tracker;
			}

			public IEnumerable<Direction> GetValidDirs(IntVector3 p)
			{
				var td = m_env.GetTileData(p);

				if (!td.IsSeeThrough)
					yield break;

				foreach (var d in DirectionExtensions.PlanarUpDownDirections)
				{
					var pp = p + d;
					if (m_env.Contains(pp) && !m_tracker.GetVisible(pp))
						yield return d;
				}
			}

			public bool GetIsTarget(IntVector3 location)
			{
				return m_tracker.GetVisible(location) == false;
			}
		}

		void OnTerrainOrInteriorChanged(IntVector3 location, TileData oldData, TileData newData)
		{
			if (oldData.IsSeeThrough == false && newData.IsSeeThrough == true && GetVisible(location))
				CheckVisibility(location, oldData, newData);
		}

		void CheckVisibility(IntVector3 location, TileData oldData, TileData newData)
		{
			var env = m_environment;

			var initLocs = new IntVector3[] { location };
			var target = new MyTarget(env, this);

			var bfs = new BFS(initLocs, target);

			var revealed = bfs.Find().ToList();

			//Debug.Print("Revealed {0} tiles: {1}", revealed.Count, string.Join(", ", revealed.Select(p => p.ToString())));

			if (revealed.Count == 0)
				return;

			foreach (var p in revealed)
				SetVisible(p);

			// Send new tiles

			var msg = new Messages.MapDataTerrainsListMessage()
			{
				Environment = env.ObjectID,
				TileDataList = revealed.Select(l => new KeyValuePair<IntVector3, TileData>(l, env.GetTileData(l))).ToArray(),
			};

			m_player.Send(msg);

			// Send new objects

			foreach (var ob in revealed.SelectMany(env.GetContents))
			{
				var vis = m_player.GetObjectVisibility(ob);
				Debug.Assert(vis != ObjectVisibility.None);
				ob.SendTo(m_player, vis);
			}
		}
	}
}
