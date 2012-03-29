using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class Helpers
	{
		public static IEnumerable<Type> GetSubclasses(Type type)
		{
			return type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));
		}

		public static IEnumerable<Type> GetNonabstractSubclasses(Type type)
		{
			return type.Assembly.GetTypes().Where(t => !t.IsAbstract).Where(t => t.IsSubclassOf(type));
		}

		public static int Hash2D(int x, int y)
		{
			// X: 16 bits, from -32768 to 32767
			// Y: 16 bits, from -32768 to 32767
			return ((y & 0xffff) << 16) | ((x & 0xffff) << 0);
		}

		public static int Hash3D(int x, int y, int z)
		{
			// X: 12 bits, from -2048 to 2047
			// Y: 12 bits, from -2048 to 2047
			// Z: 8 bits, from -128 to 127
			return ((z & 0xff) << 24) | ((y & 0xfff) << 12) | ((x & 0xfff) << 0);
		}
	}
}
