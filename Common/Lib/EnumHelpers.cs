using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class EnumHelpers
	{
		public static int GetEnumMax<TEnum>()
		{
			return EnumConv.ToInt32(GetEnumValues<TEnum>().Max());
		}

		public static TEnum[] GetEnumValues<TEnum>()
		{
			return (TEnum[])Enum.GetValues(typeof(TEnum));
		}
	}
}
