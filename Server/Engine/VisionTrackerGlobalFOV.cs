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
		bool[, ,] m_visibilityArray;

		public VisionTrackerGlobalFOV(Player player, EnvironmentObject env)
		{
			Debug.Assert(env.VisibilityMode == VisibilityMode.GlobalFOV);

			m_player = player;
			m_environment = env;
		}

		public override void Start()
		{
			InitializeVisibilityArray();

			m_environment.TerrainOrInteriorChanged += OnTerrainOrInteriorChanged;

			m_environment.SendTo(m_player, ObjectVisibility.Public);
		}

		public override void Stop()
		{
			m_environment.TerrainOrInteriorChanged -= OnTerrainOrInteriorChanged;

			m_visibilityArray = null;
		}

		public override bool Sees(IntPoint3 p)
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

#if simple_version
			bounds.Range().AsParallel().ForAll(p =>
			{
				m_visibilityArray[p.Z, p.Y, p.X] = EnvironmentHelpers.CanSeeThrough(env, p) || EnvironmentHelpers.CanBeSeen(env, p);
			});
#else
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
						var p = new IntPoint3(x, y, z);

						var vis = env.GetTileData(p).IsSeeThrough || EnvironmentHelpers.CanBeSeen(env, p);

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
#endif

			sw.Stop();

			Trace.TraceInformation("Initialize visibilityarray took {0} ms", sw.ElapsedMilliseconds);
		}

		bool GetVisible(IntPoint3 p)
		{
			return m_visibilityArray[p.Z, p.Y, p.X];
		}

		void SetVisible(IntPoint3 p)
		{
			m_visibilityArray[p.Z, p.Y, p.X] = true;
		}

		void OnTerrainOrInteriorChanged(IntPoint3 location, TileData oldData, TileData newData)
		{
			if (GetVisible(location) == false)
				return;

			var env = m_environment;

			bool revealPlanar = oldData.IsSeeThrough == false && newData.IsSeeThrough == true;
			bool revealDown = oldData.IsSeeThroughDown == false && newData.IsSeeThroughDown == true;

			List<IntPoint3> revealed = new List<IntPoint3>();

			if (revealPlanar)
			{
				revealed.AddRange(DirectionExtensions.PlanarDirections
					.Select(dir => location + dir)
					.Where(env.Contains)
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
					TileDataList = revealed.Select(l => new Tuple<IntPoint3, TileData>(l, env.GetTileData(l))).ToArray(),
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
}
