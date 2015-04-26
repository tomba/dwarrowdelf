using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class MovableManager
	{
		List<MovableObject> m_movables = new List<MovableObject>();

		public IReadOnlyList<MovableObject> Movables { get { return m_movables.AsReadOnly(); } }

		public void AddMovable(MovableObject o)
		{
			m_movables.Add(o);
		}
	}
}
