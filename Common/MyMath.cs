using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public class MyMath
	{
		/// <summary>
		/// Divide signed integer m with positive integer n, rounding up for positive m and and down for negative m
		/// </summary>
		/// <param name="m">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Quotient</returns>
		public static int IntDivRound(int m, int n)
		{
			return (m + (m >= 0 ? (n - 1) : -(n - 1))) / n;
		}

		/// <summary>
		/// Clamp an integer between two values
		/// </summary>
		/// <param name="value">Value to be clamped</param>
		/// <param name="max">Maximum value</param>
		/// <param name="min">Minimum value</param>
		/// <returns>Clamped value</returns>
		public static int IntClamp(int value, int max, int min)
		{
			return value > max ? max : (value < min ? min : value);
		}
	}
}
