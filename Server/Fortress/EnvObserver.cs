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

			foreach (var ob in env.Inventory.OfType<ItemObject>().Where(item => item.ItemCategory == ItemCategory.Workbench))
				m_region.Add(new IntGrid2Z(ob.Location.ToIntPoint() - new IntVector2(2, 2), new IntSize2(5, 5), ob.Location.Z));
		}

		EnvObserver(SaveGameContext ctx)
		{
		}

		public bool Contains(IntPoint3 p)
		{
			return m_region.Contains(p);
		}

		public IntPoint3? Center { get { return m_region.Center; } }
	}
}
