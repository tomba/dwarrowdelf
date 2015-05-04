using Dwarrowdelf;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class VoxelRayCast
	{
		/**
		 * Call the callback with (x,y,z,face) of all blocks along the line
		 * segment from point 'origin' in vector direction 'direction' of length
		 * 'radius'. 'radius' may be infinite.
		 * 
		 * 'face' is the normal vector of the face of that block that was entered.
		 * It should not be used after the callback returns.
		 * 
		 * If the callback returns a true value, the traversal will be stopped.
		 */

		public delegate bool RayCastDelegate(int x, int y, int z, Direction dir);

		public static void RunRayCast(Vector3 origin, Vector3 direction, float radius, RayCastDelegate callback)
		{
			int wx = GlobalData.Map.Width;
			int wy = GlobalData.Map.Height;
			int wz = GlobalData.Map.Depth;

			// From "A Fast Voxel Traversal Algorithm for Ray Tracing"
			// by John Amanatides and Andrew Woo, 1987
			// <http://www.cse.yorku.ca/~amana/research/grid.pdf>
			// <http://citeseer.ist.psu.edu/viewdoc/summary?doi=10.1.1.42.3443>
			// Extensions to the described algorithm:
			//   • Imposed a distance limit.
			//   • The face passed through to reach the current cube is provided to
			//     the callback.

			// The foundation of this algorithm is a parameterized representation of
			// the provided ray,
			//                    origin + t * direction,
			// except that t is not actually stored; rather, at any given point in the
			// traversal, we keep track of the *greater* t values which we would have
			// if we took a step sufficient to cross a cube boundary along that axis
			// (i.e. change the integer part of the coordinate) in the variables
			// tMaxX, tMaxY, and tMaxZ.

			// Cube containing origin point.
			int x = MyMath.Floor(origin[0]);
			int y = MyMath.Floor(origin[1]);
			int z = MyMath.Floor(origin[2]);
			// Break out direction vector.
			float dx = direction.X;
			float dy = direction.Y;
			float dz = direction.Z;
			// Direction to increment x,y,z when stepping.
			int stepX = Math.Sign(dx);
			int stepY = Math.Sign(dy);
			int stepZ = Math.Sign(dz);
			// See description above. The initial values depend on the fractional
			// part of the origin.
			float tMaxX = IntBound(origin.X, dx);
			float tMaxY = IntBound(origin.Y, dy);
			float tMaxZ = IntBound(origin.Z, dz);
			// The change in t when taking a step (always positive).
			float tDeltaX = stepX / dx;
			float tDeltaY = stepY / dy;
			float tDeltaZ = stepZ / dz;
			// Buffer for reporting faces to the callback.
			Direction face = Direction.None;

			// Avoids an infinite loop.
			if (dx == 0 && dy == 0 && dz == 0)
				throw new Exception("Raycast in zero direction!");

			// Rescale from units of 1 cube-edge to units of 'direction' so we can
			// compare with 't'.
			radius /= (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

			while (/* ray has not gone past bounds of world */
				   (stepX > 0 ? x < wx : x >= 0) &&
				   (stepY > 0 ? y < wy : y >= 0) &&
				   (stepZ > 0 ? z < wz : z >= 0))
			{

				// Invoke the callback, unless we are not *yet* within the bounds of the
				// world.
				if (!(x < 0 || y < 0 || z < 0 || x >= wx || y >= wy || z >= wz))
				{
					if (callback(x, y, z, face))
						break;
				}

				// tMaxX stores the t-value at which we cross a cube boundary along the
				// X axis, and similarly for Y and Z. Therefore, choosing the least tMax
				// chooses the closest cube boundary. Only the first case of the four
				// has been commented in detail.
				if (tMaxX < tMaxY)
				{
					if (tMaxX < tMaxZ)
					{
						if (tMaxX > radius)
							break;
						// Update which cube we are now in.
						x += stepX;
						// Adjust tMaxX to the next X-oriented boundary crossing.
						tMaxX += tDeltaX;
						// Record the normal vector of the cube face we entered.
						face = -stepX > 0 ? Direction.East : Direction.West;
					}
					else
					{
						if (tMaxZ > radius)
							break;
						z += stepZ;
						tMaxZ += tDeltaZ;
						face = -stepZ > 0 ? Direction.Up : Direction.Down;
					}
				}
				else
				{
					if (tMaxY < tMaxZ)
					{
						if (tMaxY > radius)
							break;
						y += stepY;
						tMaxY += tDeltaY;
						face = -stepY > 0 ? Direction.South : Direction.North;
					}
					else
					{
						// Identical to the second case, repeated for simplicity in
						// the conditionals.
						if (tMaxZ > radius)
							break;
						z += stepZ;
						tMaxZ += tDeltaZ;
						face = -stepZ > 0 ? Direction.Up : Direction.Down;
					}
				}
			}
		}

		// Find the smallest positive t such that s+t*ds is an integer.
		static float IntBound(float s, float ds)
		{
			if (ds < 0)
				return IntBound(-s, -ds);

			s = Mod(s, 1);
			// problem is now s+t*ds = 1
			return (1 - s) / ds;
		}

		static float Mod(float value, float modulus)
		{
			return (value % modulus + modulus) % modulus;
		}
	}
}
