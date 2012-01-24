using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class EnumHelpers
	{
		public static int GetEnumMax<T>() where T : IConvertible
		{
			return GetEnumValues<T>().Max().ToInt32(null);
		}

		public static T[] GetEnumValues<T>()
		{
			return (T[])Enum.GetValues(typeof(T));
		}
	}
}
