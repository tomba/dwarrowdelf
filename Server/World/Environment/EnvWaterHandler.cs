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

		void ShuffleArray(Direction[] array)
		{
			if (array.Length == 0)
				return;

			var r = m_env.World.Random;

			for (int i = array.Length - 1; i >= 1; i--)
			{
				int randomIndex = r.Next(i + 1);

				//Swap elements
				var tmp = array[i];
				array[i] = array[randomIndex];
				array[randomIndex] = tmp;
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

		void HandleWaterAt(IntPoint3 p, Dictionary<IntPoint3, int> waterChangeMap)
		{
			int curLevel;

			if (!waterChangeMap.TryGetValue(p, out curLevel))
			{
				curLevel = m_env.GetWaterLevel(p);
				/* water evaporates */
				/*
				if (curLevel == 1 && s_waterRandom.Next(100) == 0)
				{
					waterChangeMap[p] = 0;
					return;
				}
				 */
			}

			var dirs = DirectionExtensions.CardinalUpDownDirections.ToArray();
			ShuffleArray(dirs);
			bool curLevelChanged = false;

			foreach (var d in dirs)
			{
				var pp = p + d;

				if (!CanWaterFlow(p, pp))
					continue;

				int neighLevel;
				if (!waterChangeMap.TryGetValue(pp, out neighLevel))
					neighLevel = m_env.GetWaterLevel(pp);

				int flow;
				if (d == Direction.Up)
				{
					if (curLevel > TileData.MaxWaterLevel)
					{
						flow = curLevel - (neighLevel + TileData.MaxCompress) - 1;
						flow = MyMath.Clamp(flow, curLevel - TileData.MaxWaterLevel, 0);
					}
					else
						flow = 0;

				}
				else if (d == Direction.Down)
				{
					if (neighLevel < TileData.MaxWaterLevel)
						flow = TileData.MaxWaterLevel - neighLevel;
					else if (curLevel >= TileData.MaxWaterLevel)
						flow = curLevel - neighLevel + TileData.MaxCompress;
					else
						flow = 0;

					flow = MyMath.Clamp(flow, curLevel, 0);
				}
				else
				{
					if (curLevel > TileData.MinWaterLevel && curLevel > neighLevel)
					{
						int diff = curLevel - neighLevel;
						flow = (diff + 5) / 6;
						Debug.Assert(flow < curLevel);
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

				curLevel -= flow;
				neighLevel += flow;

				waterChangeMap[pp] = neighLevel;
				curLevelChanged = true;
			}

			if (curLevelChanged)
				waterChangeMap[p] = curLevel;
		}

		public void HandleWater()
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
	}
}
