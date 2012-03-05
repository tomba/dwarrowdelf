using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace MyArea
{
	[SaveGameObjectByRef]
	class EnvObserver
	{
		[SaveGameProperty]
		Region m_region;

		public EnvObserver(EnvironmentObject env)
		{
			m_region = new Region();

			foreach (var ob in env.GetLargeObjects())
				AddLargeObject(ob);

			env.LargeObjectAdded += OnLargeObjectAdded;
			env.LargeObjectRemoved += OnLargeObjectRemoved;
		}

		EnvObserver(SaveGameContext ctx)
		{
		}

		public bool Contains(IntPoint3 p)
		{
			return m_region.Contains(p);
		}

		public IntPoint3? Center { get { return m_region.Center; } }

		IntCuboid LargeObjectToCuboid(AreaObject ob)
		{
			var area = ob.Area;

			int d = 2;
			return new IntCuboid(new IntPoint3(area.X1 - d, area.Y1 - d, area.Z), new IntPoint3(area.X2 + d, area.Y2 + d, area.Z + 1));
		}

		void AddLargeObject(AreaObject ob)
		{
			var c = LargeObjectToCuboid(ob);
			m_region.Add(c);
		}

		void RemoveLargeObject(AreaObject ob)
		{
			var c = LargeObjectToCuboid(ob);
			m_region.Remove(c);
		}

		void OnLargeObjectAdded(AreaObject ob)
		{
			AddLargeObject(ob);
		}

		void OnLargeObjectRemoved(AreaObject ob)
		{
			RemoveLargeObject(ob);
		}
	}
}
