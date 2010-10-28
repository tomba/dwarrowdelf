using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class Extensions
	{
		public static string Capitalize(this string str)
		{
			if (String.IsNullOrEmpty(str))
				return String.Empty;

			char[] arr = str.ToCharArray();
			arr[0] = Char.ToUpperInvariant(arr[0]);
			return new String(arr);
		}
	}
}
