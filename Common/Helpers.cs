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
	}
}
