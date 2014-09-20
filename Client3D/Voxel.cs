using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	enum VoxelType : byte
	{
		Undefined = 0,
		Empty,
		Rock,
		Water,
	}

	enum VoxelFlags : byte
	{
		None = 0,
		Grass = 1 << 0,
		Tree = 1 << 1,
		Tree2 = 1 << 2,
	}

	[StructLayout(LayoutKind.Explicit, Size = 4)]
	struct Voxel
	{
		[FieldOffset(0)]
		public uint Raw;

		[FieldOffset(0)]
		public VoxelType Type;
		[FieldOffset(1)]
		public FaceDirectionBits VisibleFaces;
		[FieldOffset(2)]
		public VoxelFlags Flags;

		public bool IsOpaque { get { return !(this.Type == VoxelType.Empty || this.Type == VoxelType.Undefined); } }

		public bool IsUndefined { get { return this.Type == VoxelType.Undefined; } }
		public bool IsEmpty { get { return this.Type == VoxelType.Empty; } }

		public readonly static Voxel Undefined = new Voxel() { Type = VoxelType.Undefined };
		public readonly static Voxel Empty = new Voxel() { Type = VoxelType.Empty };
		public readonly static Voxel Rock = new Voxel() { Type = VoxelType.Rock };
		public readonly static Voxel Water = new Voxel() { Type = VoxelType.Water };
	}
}
