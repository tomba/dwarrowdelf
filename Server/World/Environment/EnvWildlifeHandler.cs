﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	sealed class EnvWildlifeHandler
	{
		EnvironmentObject m_env;
		List<AI.Group> m_herds;

		public EnvWildlifeHandler(EnvironmentObject env)
		{
			m_env = env;
			/*
						m_env.Contents
							.OfType<LivingObject>()
							.Where(o => o.LivingCategory == LivingCategory.Herbivore)
							.Count();
			 */

			//m_env.World.TickStarting += OnTickStarting;
		}

		void Destruct()
		{
			//m_env.World.TickStarting -= OnTickStarting;
		}
		/*
		void OnTickStarting()
		{
		}
		*/
		public void Init()
		{
			m_herds = new List<AI.Group>();

			m_herds.Add(CreateHerd(5, LivingID.Sheep));
			m_herds.Add(CreateHerd(3, LivingID.Sheep));
			m_herds.Add(CreateHerd(4, LivingID.Sheep));
		}

		AI.Group CreateHerd(int numAnimals, LivingID livingID)
		{
			var world = m_env.World;

			var group = new AI.Group();

			var center = m_env.GetRandomEnterableSurfaceLocation();

			using (var iter = IntVector2.SquareSpiral(center.ToIntVector2(), 20).GetEnumerator())
			{
				for (int i = 0; i < numAnimals; ++i)
				{
					bool ok = true;

					while (true)
					{
						if (iter.MoveNext() == false)
						{
							ok = false;
							break;
						}

						var p2 = iter.Current;

						if (m_env.Size.Plane.Contains(p2) == false)
							continue;

						var p = m_env.GetSurfaceLocation(p2);

						if (m_env.CanEnter(p) == false)
							continue;

						var livingBuilder = new LivingObjectBuilder(livingID);

						var living = livingBuilder.Create(world);
						var ai = new Dwarrowdelf.AI.HerbivoreAI(living, world.PlayerID);
						ai.Group = group;
						living.SetAI(ai);
						living.MoveToMustSucceed(m_env, p);

						break;
					}

					if (!ok)
						break;
				}
			}

			return group;
		}
	}
}
