using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class MyMath
	{
		/// <summary>
		/// Divide signed integer m with positive integer n, rounding up for positive m and and down for negative m
		/// </summary>
		/// <param name="m">Dividend</param>
		/// <param name="n">Divisor</param>
		/// <returns>Quotient</returns>
		public static int DivRound(int m, int n)
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
		public static int Clamp(int value, int max, int min)
		{
			return value > max ? max : (value < min ? min : value);
		}

		/// <summary>
		/// Clamp a double between two values
		/// </summary>
		/// <param name="value">Value to be clamped</param>
		/// <param name="max">Maximum value</param>
		/// <param name="min">Minimum value</param>
		/// <returns>Clamped value</returns>
		public static double Clamp(double value, double max, double min)
		{
			return value > max ? max : (value < min ? min : value);
		}

		/// <summary>
		/// 2^n
		/// </summary>
		public static int Pow2(int n)
		{
			return 1 << n;
		}

		/// <summary>
		/// n * n
		/// </summary>
		public static int Square(int n)
		{
			return n * n;
		}

		/// <summary>
		/// Return integer Log2 of an integer
		/// </summary>
		public static int Log2(int value)
		{
			int r = 0;

			while ((value >>= 1) != 0)
				r++;

			return r;
		}

		/// <summary>
		/// Is the number a power of two (2^n)
		/// </summary>
		public static bool IsPowerOfTwo(int x)
		{
			return x != 0 && (x & (x - 1)) == 0;
		}

		/// <summary>
		/// Shuffle the items in an array
		/// </summary>
		public static void ShuffleArray<T>(T[] array, Random random)
		{
			if (array.Length == 0)
				return;

			for (int i = array.Length - 1; i >= 1; i--)
			{
				int randomIndex = random.Next(i + 1);

				//Swap elements
				var tmp = array[i];
				array[i] = array[randomIndex];
				array[randomIndex] = tmp;
			}
		}
	}
}
