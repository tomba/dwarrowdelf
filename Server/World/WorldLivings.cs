using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class World
	{
		[SaveGameProperty]
		ProcessableList<LivingObject> m_livings;

		LivingEnumerator m_livingEnumerator;

		internal void AddLiving(LivingObject living)
		{
			VerifyAccess();
			m_livings.Add(living);
		}

		internal void RemoveLiving(LivingObject living)
		{
			VerifyAccess();
			m_livings.Remove(living);
		}

		class LivingEnumerator
		{
			IList<LivingObject> m_list;
			int m_index;

			public LivingEnumerator(IList<LivingObject> list)
			{
				m_list = list;
				m_index = -1;
			}

			public LivingObject Current
			{
				get { return m_index == -1 ? null : m_list[m_index]; }
			}

			public bool MoveNext()
			{
				Debug.Assert(m_index < m_list.Count);
				++m_index;
				return m_index < m_list.Count;
			}

			public void Reset()
			{
				m_index = -1;
			}
		}

	}
}
