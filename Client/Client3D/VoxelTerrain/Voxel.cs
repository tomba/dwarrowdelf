using Dwarrowdelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	[StructLayout(LayoutKind.Explicit, Size = 4)]
	struct Voxel
	{
		[FieldOffset(0)]
		public uint Raw;

		[FieldOffset(0)]
		public byte Unused1;
		[FieldOffset(1)]
		public Direction VisibleFaces;
		[FieldOffset(2)]
		public byte Unused2;
	}
}
