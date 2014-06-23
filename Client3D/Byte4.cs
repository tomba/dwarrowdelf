using System.Runtime.InteropServices;

namespace Client3D
{
	[StructLayout(LayoutKind.Sequential, Size = 4)]
	struct Byte4
	{
		public byte X;
		public byte Y;
		public byte Z;
		public byte W;

		public Byte4(byte x, byte y, byte z, byte w)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
			this.W = w;
		}

		public Byte4(int x, int y, int z, int w)
		{
			this.X = (byte)x;
			this.Y = (byte)y;
			this.Z = (byte)z;
			this.W = (byte)w;
		}
	}
}
