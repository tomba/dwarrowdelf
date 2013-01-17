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
			if (str == null)
				return null;

			if (str.Length == 0)
				return String.Empty;

			char[] arr = str.ToCharArray();
			arr[0] = Char.ToUpperInvariant(arr[0]);
			return new String(arr);
		}

		public static byte Min(this byte[,] arr)
		{
			byte v = byte.MaxValue;

			for (int y = 0; y < arr.GetLength(0); ++y)
				for (int x = 0; x < arr.GetLength(1); ++x)
					if (arr[y, x] < v)
						v = arr[y, x];

			return v;
		}

		public static byte Max(this byte[,] arr)
		{
			byte v = byte.MinValue;

			for (int y = 0; y < arr.GetLength(0); ++y)
				for (int x = 0; x < arr.GetLength(1); ++x)
					if (arr[y, x] > v)
						v = arr[y, x];

			return v;
		}
	}
}
