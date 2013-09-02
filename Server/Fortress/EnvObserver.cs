using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace Dwarrowdelf.Server.Fortress
{
	/// <summary>
	/// Track interesting areas, and dwarves can idle around those
	/// </summary>
	[SaveGameObject]
	class EnvObserver
	{
		[SaveGameProperty]
		Region m_region;

		public EnvObserver(EnvironmentObject env)
		{
			m_region = new Region();
		}

		EnvObserver(SaveGameContext ctx)
		{
		}

		public bool Contains(IntPoint3 p)
		{
			return m_region.Contains(p);
		}

		public void Add(IntGrid2Z rect)
		{
			m_region.Add(rect);
		}

		public IntPoint3? Center { get { return m_region.Center; } }
	}
}
