using Dwarrowdelf;
using SharpDX;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class VoxelMap
	{
		public Voxel[, ,] Grid { get; private set; }
		public IntSize3 Size { get; private set; }

		public int Width { get { return this.Size.Width; } }
		public int Height { get { return this.Size.Height; } }
		public int Depth { get { return this.Size.Depth; } }

		public VoxelMap(IntSize3 size)
		{
			System.Diagnostics.Trace.Assert(Marshal.SizeOf<Voxel>() == 4);

			this.Size = size;
			this.Grid = new Voxel[size.Depth, size.Height, size.Width];
		}

		public void SetVoxel(IntVector3 p, Voxel voxel)
		{
			this.Grid[p.Z, p.Y, p.X] = voxel;

			this.Grid[p.Z, p.Y, p.X].VisibleFaces = GetVisibleFaces(p);

			// TODO: optimize, we only need to check the faces towards the voxel that is set

			foreach (var v in IntVector3.CardinalUpDownDirections)
			{
				var n = p + v;

				if (!this.Size.Contains(n))
					continue;

				if (this.Grid[n.Z, n.Y, n.X].Type == VoxelType.Empty)
					continue;

				this.Grid[n.Z, n.Y, n.X].VisibleFaces = GetVisibleFaces(n);
			}
		}

		public void SetVoxelDirect(IntVector3 p, Voxel voxel)
		{
			this.Grid[p.Z, p.Y, p.X] = voxel;
		}

		public Voxel GetVoxel(IntVector3 p)
		{
			return this.Grid[p.Z, p.Y, p.X];
		}

		public void CheckVisibleFaces()
		{
			var grid = this.Grid;

			Parallel.For(0, this.Depth, z =>
			{
				for (int y = 0; y < this.Height; ++y)
					for (int x = 0; x < this.Width; ++x)
					{
						var p = new IntVector3(x, y, z);

						this.Grid[p.Z, p.Y, p.X].VisibleFaces = GetVisibleFaces(p);
					}
			});
		}

		Direction GetVisibleFaces(IntVector3 p)
		{
			Direction visibleFaces = 0;

			foreach (var dir in DirectionExtensions.CardinalUpDownDirections)
			{
				var n = p + dir;

				if (this.Size.Contains(n) == false)
					continue;

				if (this.Grid[n.Z, n.Y, n.X].IsOpaque)
					continue;

				visibleFaces |= dir;
			}

			return visibleFaces;
		}
	}
}
