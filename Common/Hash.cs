using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class Hash
	{
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

		// http://stackoverflow.com/questions/682438/hash-function-providing-unique-uint-from-an-integer-coordinate-pair
		public static uint HashUInt32(uint a)
		{
			a = (a ^ 61) ^ (a >> 16);
			a = a + (a << 3);
			a = a ^ (a >> 4);
			a = a * 0x27d4eb2d;
			a = a ^ (a >> 15);
			return a;
		}

		// http://www.concentric.net/~ttwang/tech/inthash.htm
		public static int HashInt32(int v)
		{
			v = ~v + (v << 15);
			v = v ^ (v >> 12);
			v = v + (v << 2);
			v = v ^ (v >> 4);
			v = v * 2057;
			v = v ^ (v >> 16);
			return v;
		}
	}
}
