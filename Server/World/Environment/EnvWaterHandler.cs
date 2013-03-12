using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	sealed class EnvWaterHandler
	{
		EnvironmentObject m_env;

		HashSet<IntPoint3> m_waterTiles = new HashSet<IntPoint3>();

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
			Dictionary<IntPoint3, int> waterChangeMap = new Dictionary<IntPoint3, int>();

			foreach (var p in m_waterTiles)
			{
				if (m_env.GetWaterLevel(p) == 0)
					throw new Exception();

				HandleWaterAt(p, waterChangeMap);
			}

			foreach (var kvp in waterChangeMap)
			{
				var p = kvp.Key;
				int level = kvp.Value;

				m_env.SetWaterLevel(p, (byte)level);
			}
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

		void HandleWaterAt(IntPoint3 src, Dictionary<IntPoint3, int> waterChangeMap)
		{
			int srcLevel;

			if (!waterChangeMap.TryGetValue(src, out srcLevel))
				srcLevel = m_env.GetWaterLevel(src);

			var dirs = DirectionExtensions.CardinalUpDownDirections.ToArray();
			MyMath.ShuffleArray(dirs, m_env.World.Random);
			bool srcLevelChanged = false;

			foreach (var d in dirs)
			{
				var dst = src + d;

				if (!CanWaterFlow(src, dst))
					continue;

				int dstLevel;
				if (!waterChangeMap.TryGetValue(dst, out dstLevel))
					dstLevel = m_env.GetWaterLevel(dst);

				int flow;
				if (d == Direction.Up)
				{
					if (srcLevel > TileData.MaxWaterLevel)
					{
						flow = srcLevel - (dstLevel + TileData.MaxCompress) - 1;
						flow = MyMath.Clamp(flow, srcLevel - TileData.MaxWaterLevel, 0);
					}
					else
						flow = 0;

				}
				else if (d == Direction.Down)
				{
					if (dstLevel < TileData.MaxWaterLevel)
						flow = TileData.MaxWaterLevel - dstLevel;
					else if (srcLevel >= TileData.MaxWaterLevel)
						flow = srcLevel - dstLevel + TileData.MaxCompress;
					else
						flow = 0;

					flow = MyMath.Clamp(flow, srcLevel, 0);
				}
				else
				{
					if (srcLevel > TileData.MinWaterLevel && srcLevel > dstLevel)
					{
						int diff = srcLevel - dstLevel;
						flow = (diff + 5) / 6;
						Debug.Assert(flow < srcLevel);
						//flow = Math.Min(flow, curLevel - 1);
						//flow = IntClamp(flow, curLevel > 1 ? curLevel - 1 : 0, neighLevel > 1 ? -neighLevel + 1 : 0);
					}
					else
					{
						flow = 0;
					}
				}

				if (flow == 0)
					continue;

				srcLevel -= flow;
				dstLevel += flow;

				waterChangeMap[dst] = dstLevel;
				srcLevelChanged = true;
			}

			if (srcLevelChanged)
				waterChangeMap[src] = srcLevel;
		}
	}
}
