using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	// XXX tree growth is not so good. If the player clears an area, it's likely that the trees
	// have grown elsewhere and the area will stay clear.
	sealed class EnvTreeHandler
	{
		EnvironmentObject m_env;
		int m_numTrees;
		int m_currentIdx;
		int m_targetNumTrees;

		public EnvTreeHandler(EnvironmentObject env, int targetNumTrees)
		{
			m_env = env;
			m_targetNumTrees = targetNumTrees;

			m_numTrees = ParallelEnumerable.Range(0, m_env.Size.Depth).Sum(z =>
			{
				int sum = 0;
				for (int y = 0; y < m_env.Size.Height; ++y)
					for (int x = 0; x < m_env.Size.Width; ++x)
						if (m_env.GetTileData(x, y, z).HasTree)
							sum++;

				return sum;
			});

			m_env.TerrainOrInteriorChanged += OnTerrainOrInteriorChanged;

			m_env.World.TickEnding += OnTick;
		}

		public void Destruct()
		{
			m_env.TerrainOrInteriorChanged -= OnTerrainOrInteriorChanged;

			m_env.World.TickEnding -= OnTick;
		}

		void OnTerrainOrInteriorChanged(IntVector3 p, TileData oldData, TileData newData)
		{
			if (oldData.HasTree != newData.HasTree)
			{
				if (newData.HasTree)
					AddTree();
				else
					RemoveTree();
			}
		}

		void AddTree()
		{
			m_numTrees++;
			//Debug.Print(m_numTrees.ToString());
		}

		void RemoveTree()
		{
			m_numTrees--;
			Debug.Assert(m_numTrees >= 0);

			//Debug.Print(m_numTrees.ToString());
		}

		void OnTick()
		{
			// go through all tiles in one year
			int amount = m_env.Width * m_env.Height / World.YEAR_LENGTH;

			var woodMaterials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();
			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			var r = m_env.World.Random;

			for (int i = 0; i < amount; ++i)
			{
				var idx = m_currentIdx++;

				if (m_currentIdx == m_env.Width * m_env.Height)
					m_currentIdx = 0;

				var p = m_env.GetRandomSurfaceLocation(idx);

				var td = m_env.GetTileData(p);

				if (td.ID == TileID.Sapling)
				{
					if (r.Next(100) < 80)
					{
						// any object prevents sapling from growing to tree
						if (m_env.HasContents(p) == false)
						{
							// A sapling grows to a tree
							td.ID = TileID.Tree;
							m_env.SetTileData(p, td);
						}
					}
				}
				else if (td.ID == TileID.Tree)
				{
					if (r.Next(100) < 20)
					{
						// A tree dies
						td.ID = TileID.DeadTree;
						m_env.SetTileData(p, td);
					}
				}
				else if (td.ID == TileID.DeadTree)
				{
					if (r.Next(100) < 60)
					{
						// A dead tree disappears
						td.ID = TileID.Grass;
						td.MaterialID = grassMaterials[r.Next(grassMaterials.Length)].ID;
						m_env.SetTileData(p, td);
					}
				}
				else if (m_numTrees < m_targetNumTrees && td.WaterLevel == 0 && td.IsClearFloor)
				{
					if (r.Next(100) < 60)
					{
						// A new sapling is planted
						td.ID = TileID.Sapling;
						td.MaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
						m_env.SetTileData(p, td);
					}
				}
			}
		}
	}
}
