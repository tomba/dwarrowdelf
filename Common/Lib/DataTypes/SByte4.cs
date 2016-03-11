using System.Runtime.InteropServices;

namespace Dwarrowdelf
{
	[StructLayout(LayoutKind.Sequential, Size = 4)]
	public struct SByte4
	{
		public sbyte X;
		public sbyte Y;
		public sbyte Z;
		public sbyte W;

		public SByte4(sbyte x, sbyte y, sbyte z, sbyte w)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}

		public SByte4(int x, int y, int z, int w)
		{
			this.X = (sbyte)x;
			this.Y = (sbyte)y;
			this.Z = (sbyte)z;
			this.W = (sbyte)w;
		}
	}
}
