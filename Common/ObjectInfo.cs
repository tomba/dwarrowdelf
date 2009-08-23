using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class ObjectInfo
	{
		public int SymbolID { get; set; }
		public string Name { get; set; }
		public char CharSymbol { get; set; }
		public string DrawingName { get; set; }
	}

	public class Objects
	{
		ObjectInfo[] m_objects;

		public Objects(ObjectInfo[] objects)
		{
			m_objects = objects;
		}

		public int Count { get { return m_objects.Length; } }

		public ObjectInfo FindObjectByName(string name)
		{
			return m_objects.First(t => t.Name == name);
		}

		public ObjectInfo this[int id]
		{
			get { return m_objects[id]; }
		}
	}
}
