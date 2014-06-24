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
	enum FaceDirectionBits
	{
		PositiveX = 1 << FaceDirection.PositiveX,	// East
		NegativeX = 1 << FaceDirection.NegativeX,	// West
		PositiveY = 1 << FaceDirection.PositiveY,	// South
		NegativeY = 1 << FaceDirection.NegativeY,	// North
		PositiveZ = 1 << FaceDirection.PositiveZ,	// Up
		NegativeZ = 1 << FaceDirection.NegativeZ,	// Down
		All = (1 << 6) - 1,
	}

	struct Vertex3002
	{
		public Vertex3002(float x, float y, float z, float tu, float tv)
		{
			this.Pos = new Vector3(x, y, z);
			this.Tex = new Vector2(tu, tv);
		}

		public Vertex3002(Vector3 pos, Vector2 tex)
		{
			this.Pos = pos;
			this.Tex = tex;
		}

		public Vector3 Pos;
		public Vector2 Tex;
	}
}
