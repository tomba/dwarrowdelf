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
			if (str.Length == 0)
				return String.Empty;

			char[] arr = str.ToCharArray();
			arr[0] = Char.ToUpperInvariant(arr[0]);
			return new String(arr);
		}
	}
}
