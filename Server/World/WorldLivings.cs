using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public partial class World
	{
		[GameProperty]
		ProcessableList<Living> m_livings = new ProcessableList<Living>();

		internal void AddLiving(Living living)
		{
			VerifyAccess();
			m_livings.Add(living);
		}

		internal void RemoveLiving(Living living)
		{
			VerifyAccess();
			m_livings.Remove(living);
		}
	}
}
