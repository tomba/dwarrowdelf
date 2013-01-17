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
		int[] m_randomArray;
		int m_currentIdx;
		int m_targetNumTrees;

		public EnvTreeHandler(EnvironmentObject env, int targetNumTrees)
		{
			m_env = env;
			m_targetNumTrees = targetNumTrees;

			m_randomArray = new int[env.Width * env.Height];
			for (int i = 0; i < m_randomArray.Length; ++i)
				m_randomArray[i] = (ushort)i;

			MyMath.ShuffleArray(m_randomArray, m_env.World.Random);

			m_numTrees = m_env.Size.Range().Count(p => m_env.GetTileData(p).InteriorID.IsTree());

			m_env.TerrainOrInteriorChanged += OnTerrainOrInteriorChanged;
		}

		void OnTerrainOrInteriorChanged(IntPoint3 p, TileData oldData, TileData newData)
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

		public void Tick()
		{
			// go through all tiles in one year
			int amount = m_env.Width * m_env.Height / World.YEAR_LENGTH;

			var woodMaterials = Materials.GetMaterials(MaterialCategory.Wood).ToArray();
			var grassMaterials = Materials.GetMaterials(MaterialCategory.Grass).ToArray();
			var r = m_env.World.Random;

			for (int i = 0; i < amount; ++i)
			{
				var idx = m_currentIdx++;

				if (m_currentIdx == m_randomArray.Length)
					m_currentIdx = 0;

				var tileIdx = m_randomArray[idx];

				int x, y;

				y = Math.DivRem(tileIdx, m_env.Width, out x);

				var p = m_env.GetSurface(x, y);

				var td = m_env.GetTileData(p);

				if (td.InteriorID == InteriorID.Sapling)
				{
					if (r.Next(100) < 80)
					{
						// A sapling grows to a tree
						td.InteriorID = InteriorID.Tree;
						m_env.SetTileData(p, td);
					}
				}
				else if (td.InteriorID == InteriorID.Tree)
				{
					if (r.Next(100) < 20)
					{
						// A tree dies
						td.InteriorID = InteriorID.DeadTree;
						m_env.SetTileData(p, td);
					}
				}
				else if (td.InteriorID == InteriorID.DeadTree)
				{
					if (r.Next(100) < 60)
					{
						// A dead tree disappears
						td.InteriorID = InteriorID.Grass;
						td.InteriorMaterialID = grassMaterials[r.Next(grassMaterials.Length)].ID;
						m_env.SetTileData(p, td);
					}
				}
				else if (td.IsClear && m_numTrees < m_targetNumTrees)
				{
					if (r.Next(100) < 60)
					{
						// A new sapling is planted
						td.InteriorID = InteriorID.Sapling;
						td.InteriorMaterialID = woodMaterials[r.Next(woodMaterials.Length)].ID;
						m_env.SetTileData(p, td);
					}
				}
			}
		}
	}
}
