using System;

namespace Dwarrowdelf
{
	public static class DH
	{
		public static void Dispose<T>(ref T v) where T : class, IDisposable
		{
			if (v != null)
			{
				v.Dispose();
				v = null;
			}
		}
	}
}
