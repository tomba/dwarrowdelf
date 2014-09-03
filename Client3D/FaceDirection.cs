using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	enum FaceDirection
	{
		PositiveX = 0,	// East
		NegativeX = 1,	// West
		PositiveY = 2,	// South
		NegativeY = 3,	// North
		PositiveZ = 4,	// Up
		NegativeZ = 5,	// Down
	}

	[Flags]
	enum FaceDirectionBits : byte
	{
		PositiveX = 1 << FaceDirection.PositiveX,	// East
		NegativeX = 1 << FaceDirection.NegativeX,	// West
		PositiveY = 1 << FaceDirection.PositiveY,	// South
		NegativeY = 1 << FaceDirection.NegativeY,	// North
		PositiveZ = 1 << FaceDirection.PositiveZ,	// Up
		NegativeZ = 1 << FaceDirection.NegativeZ,	// Down
		All = (1 << 6) - 1,
	}
}
