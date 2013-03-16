using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	sealed class EnvWaterHandler : IAStarTarget
	{
		EnvironmentObject m_env;

		HashSet<IntPoint3> m_waterTiles = new HashSet<IntPoint3>();
		Dictionary<IntPoint3, int> m_waterChangeMap = new Dictionary<IntPoint3, int>();

		public EnvWaterHandler(EnvironmentObject env)
		{
			m_env = env;

			ScanWaterTiles();

			m_env.World.TickEnding += OnTick;
		}

		public void Destruct()
		{
			m_env.World.TickEnding -= OnTick;
		}

		public void AddWater(IntPoint3 p)
		{
			m_waterTiles.Add(p);
		}

		public void RemoveWater(IntPoint3 p)
		{
			m_waterTiles.Remove(p);
		}

		public void Rescan()
		{
			ScanWaterTiles();
		}

		void ScanWaterTiles()
		{
			foreach (var p in m_env.Size.Range())
			{
				if (m_env.GetWaterLevel(p) > 0)
					m_waterTiles.Add(p);
				else
					m_waterTiles.Remove(p);
			}
		}

		void OnTick()
		{
			foreach (var p in m_waterTiles)
			{
				if (m_env.GetWaterLevel(p) == 0)
					throw new Exception();

				HandleWaterAt(p);
			}

			foreach (var kvp in m_waterChangeMap)
			{
				var p = kvp.Key;
				int level = kvp.Value;

				m_env.SetWaterLevel(p, (byte)level);
			}

			m_waterChangeMap.Clear();
		}

		bool CanWaterFlow(IntPoint3 from, IntPoint3 to)
		{
			if (!m_env.Contains(to))
				return false;

			IntVector3 v = to - from;

			Debug.Assert(v.IsNormal);

			var dstTerrain = m_env.GetTerrain(to);
			var dstInter = m_env.GetInterior(to);

			if (dstTerrain.IsBlocker || dstInter.IsBlocker)
				return false;

			if (v.Z == 0)
				return true;

			Direction dir = v.ToDirection();

			if (dir == Direction.Up)
				return dstTerrain.IsPermeable == true;

			var srcTerrain = m_env.GetTerrain(from);

			if (dir == Direction.Down)
				return srcTerrain.IsPermeable == true;

			throw new Exception();
		}

		int GetCurrentWaterLevel(IntPoint3 p)
		{
			int l;
			if (!m_waterChangeMap.TryGetValue(p, out l))
				l = m_env.GetWaterLevel(p);
			return l;
		}

		void HandleWaterAt(IntPoint3 src)
		{
			int srcLevel = GetCurrentWaterLevel(src);

			if (srcLevel == 0)
				return;

			int origSrcLevel = srcLevel;

			bool teleportDownNotPossible;

			teleportDownNotPossible = HandleWaterFlowDown(src, ref srcLevel);

			HandleWaterFlowPlanar(src, ref srcLevel);

			if (teleportDownNotPossible == false)
				HandleWaterFlowDownTeleport(src, ref srcLevel);

			if (srcLevel != origSrcLevel)
				m_waterChangeMap[src] = srcLevel;
		}

		/* returns if teleport down is not possible */
		bool HandleWaterFlowDown(IntPoint3 src, ref int srcLevel)
		{
			if (srcLevel == 0)
				return true;

			var dst = src + Direction.Down;

			if (CanWaterFlow(src, dst) == false)
				return true;

			int dstLevel = GetCurrentWaterLevel(dst);

			if (dstLevel == TileData.MaxWaterLevel)
				return false;

			int flow = Math.Min(TileData.MaxWaterLevel - dstLevel, srcLevel);

			srcLevel -= flow;
			dstLevel += flow;

			m_waterChangeMap[dst] = dstLevel;

			return true;
		}

		void HandleWaterFlowPlanar(IntPoint3 src, ref int srcLevel)
		{
			if (srcLevel <= 1)
				return;

			var dirs = DirectionExtensions.CardinalDirections.ToArray();
			MyMath.ShuffleArray(dirs, m_env.World.Random);
			foreach (var d in dirs)
			{
				var dst = src + d;

				if (!CanWaterFlow(src, dst))
					continue;

				int dstLevel = GetCurrentWaterLevel(dst);

				int flow;

				if (srcLevel <= dstLevel)
					continue;

				int diff = srcLevel - dstLevel;
				flow = (diff + 5) / 6;
				Debug.Assert(flow < srcLevel);
				//flow = Math.Min(flow, curLevel - 1);
				//flow = IntClamp(flow, curLevel > 1 ? curLevel - 1 : 0, neighLevel > 1 ? -neighLevel + 1 : 0);

				if (flow == 0)
					continue;

				srcLevel -= flow;
				dstLevel += flow;

				m_waterChangeMap[dst] = dstLevel;

				if (srcLevel <= 1)
					return;
			}
		}

		void HandleWaterFlowDownTeleport(IntPoint3 src, ref int srcLevel)
		{
			if (srcLevel == 0)
				return;

			var down = src + Direction.Down;

			if (CanWaterFlow(src, down) == false)
				return;

			// this shouldn't happen, as HandleWaterFlowDown should've handled it
			if (GetCurrentWaterLevel(down) < TileData.MaxWaterLevel)
				return;

			m_currentSrc = src;

			var astar = new AStar(new IntPoint3[] { src + Direction.Down }, this, GetWaterDirs, null);

			var status = astar.Find();

			if (status != AStarStatus.Found)
				return;

			var dst = astar.LastNode.Loc;
			int dstLevel = GetCurrentWaterLevel(dst);

			int flow;

			if (dst.Z < src.Z)
			{
				flow = Math.Min(TileData.MaxWaterLevel - dstLevel, srcLevel);
			}
			else
			{
				int diff = srcLevel - dstLevel;
				flow = (diff + 5) / 6;
				Debug.Assert(flow < srcLevel);
			}

			srcLevel -= flow;
			dstLevel += flow;

			m_waterChangeMap[dst] = dstLevel;
		}

		IntPoint3 m_currentSrc;
		IEnumerable<Direction> GetWaterDirs(IntPoint3 p)
		{
			foreach (var dir in DirectionExtensions.CardinalUpDownDirections)
			{
				var pp = p + dir;
				if (pp == m_currentSrc)
					continue;

				if (pp.Z > m_currentSrc.Z)
					continue;

				if (CanWaterFlow(p, pp) == false)
					continue;

				yield return dir;
			}
		}

		bool IAStarTarget.GetIsTarget(IntPoint3 p)
		{
			int l = GetCurrentWaterLevel(p);
			return l < TileData.MaxWaterLevel;
		}

		ushort IAStarTarget.GetHeuristic(IntPoint3 location)
		{
			return 0;
		}
	}
}
