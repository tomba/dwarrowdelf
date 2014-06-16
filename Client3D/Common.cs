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

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct Vertex3003
	{
		[VertexElement("POSITION")]
		public Vector3 Position;
		[VertexElement("TEXCOORD0")]
		public Vector3 Tex;

		public Vertex3003(Vector3 pos, Vector3 tex)
		{
			this.Position = pos;
			this.Tex = tex;
		}
	}

	public struct Vertex3302
	{
		public Vector3 Position;
		public Vector3 Normal;
		public Vector2 TexC;
	}

	struct Vertex3332
	{
		public Vertex3332(float px, float py, float pz,
			float nx, float ny, float nz,
			float tx, float ty, float tz,
			float u, float v)
		{
			this.Position = new Vector3(px, py, pz);
			this.Normal = new Vector3(nx, ny, nz);
			this.TangentU = new Vector3(tx, ty, tz);
			this.TexC = new Vector2(u, v);
		}

		public Vector3 Position;
		public Vector3 Normal;
		public Vector3 TangentU;
		public Vector2 TexC;
	}

}
