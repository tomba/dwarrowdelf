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

				Debug.Assert(level >= 0 && level <= TileData.MaxWaterLevel);
				m_env.SetWaterLevel(p, (byte)level);
			}

			m_waterChangeMap.Clear();
		}

		bool CanWaterFlow(IntPoint3 from, IntPoint3 to)
		{
			Debug.Assert((to - from).IsNormal);

			if (!m_env.Contains(to))
				return false;

			var toTD = m_env.GetTileData(to);

			if (toTD.IsWaterPassable == false)
				return false;

			int zDiff = to.Z - from.Z;

			if (zDiff == 0)
				return true;

			if (zDiff > 0)
				return toTD.IsPermeable == true;

			if (zDiff < 0)
			{
				var fromTD = m_env.GetTileData(from);
				return fromTD.IsPermeable == true;
			}

			return false;
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

			HandleWaterFlowDown(src, ref srcLevel);

			HandleWaterFlowPlanar(src, ref srcLevel);

			if (srcLevel != origSrcLevel)
				m_waterChangeMap[src] = srcLevel;
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

		void HandleWaterFlowDown(IntPoint3 src, ref int srcLevel)
		{
			if (srcLevel == 0)
				return;

			var down = src.Down;

			if (CanWaterFlow(src, down) == false)
				return;

			int downLevel = GetCurrentWaterLevel(down);

			IntPoint3 dst;
			int dstLevel;
			int flow;

			if (downLevel < TileData.MaxWaterLevel)
			{
				dst = down;
				dstLevel = downLevel;
				flow = Math.Min(TileData.MaxWaterLevel - downLevel, srcLevel);
			}
			else
			{
				m_currentSrc = src;
				m_currentSrcLevel = srcLevel;

				var astar = new AStar(new IntPoint3[] { src.Down}, this);

				var status = astar.Find();

				if (status != AStarStatus.Found)
					return;

				dst = astar.LastNode.Loc;
				dstLevel = GetCurrentWaterLevel(dst);

				flow = Math.Min(TileData.MaxWaterLevel - dstLevel, srcLevel);
			}

			srcLevel -= flow;
			dstLevel += flow;

			m_waterChangeMap[dst] = dstLevel;
		}

		IntPoint3 m_currentSrc;
		int m_currentSrcLevel;

		bool IAStarTarget.GetIsTarget(IntPoint3 p)
		{
			int l = GetCurrentWaterLevel(p);
			return l < m_currentSrcLevel;
		}

		ushort IAStarTarget.GetHeuristic(IntPoint3 location)
		{
			return (ushort)location.Z;
		}

		ushort IAStarTarget.GetCostBetween(IntPoint3 src, IntPoint3 dst)
		{
			return 0;
		}

		IEnumerable<Direction> IAStarTarget.GetValidDirs(IntPoint3 from)
		{
			foreach (var dir in DirectionExtensions.CardinalUpDownDirections)
			{
				var to = from + dir;

				if (to.Z >= m_currentSrc.Z)
					continue;

				if (CanWaterFlow(from, to) == false)
					continue;

				yield return dir;
			}
		}
	}
}
