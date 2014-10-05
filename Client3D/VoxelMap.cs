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
		public event Action<IntVector3> VoxelChanged;

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

		public VoxelMap(IntSize3 size, Voxel init)
			: this(size)
		{
			Clear(init);
		}

		public void Clear(Voxel init)
		{
			Parallel.For(0, this.Depth, z =>
			{
				for (int y = 0; y < this.Height; ++y)
					for (int x = 0; x < this.Width; ++x)
					{
						this.Grid[z, y, x] = init;
					}
			});
		}

		public void SetVoxel(IntVector3 p, Voxel voxel)
		{
			this.Grid[p.Z, p.Y, p.X] = voxel;

			CheckVisibleFaces(p);

			if (this.VoxelChanged != null)
				this.VoxelChanged(p);

			// TODO: optimize, we only need to check the faces towards the voxel that is set

			foreach (var v in IntVector3.CardinalUpDownDirections)
			{
				var n = p + v;

				if (!this.Size.Contains(n))
					continue;

				if (this.Grid[n.Z, n.Y, n.X].Type == VoxelType.Empty)
					continue;

				CheckVisibleFaces(n);

				if (this.VoxelChanged != null)
					this.VoxelChanged(n);
			}
		}

		public Voxel GetVoxel(IntVector3 p)
		{
			return this.Grid[p.Z, p.Y, p.X];
		}

		public void CheckVisibleFaces(bool undefineHidden)
		{
			var grid = this.Grid;

			Parallel.For(0, this.Depth, z =>
			{
				for (int y = 0; y < this.Height; ++y)
					for (int x = 0; x < this.Width; ++x)
					{
						var p = new IntVector3(x, y, z);

						CheckVisibleFaces(p);
					}
			});
		}

		void CheckVisibleFaces(IntVector3 p)
		{
			this.Grid[p.Z, p.Y, p.X].VisibleFaces = 0;

			foreach (var dir in DirectionExtensions.CardinalUpDownDirections)
			{
				var n = p + dir;

				if (this.Size.Contains(n) == false)
					continue;

				if (this.Grid[n.Z, n.Y, n.X].IsOpaque)
					continue;

				this.Grid[p.Z, p.Y, p.X].VisibleFaces |= dir;
			}
		}

		public void FillFromNoiseMap(SharpNoise.NoiseMap map)
		{
			var max = map.Data.Max();
			var min = map.Data.Min();

			Parallel.For(0, map.Data.Length, i =>
			{
				var v = map.Data[i];	// [-1 .. 1]

				v -= min;
				v /= (max - min);		// [0 .. 1]

				v *= this.Depth * 8 / 10;
				v += this.Depth * 2 / 10;

				map.Data[i] = v;
			});

			var grid = this.Grid;

			int waterLimit = this.Depth * 3 / 10;
			int grassLimit = this.Depth * 8 / 10;

			Parallel.For(0, this.Height, y =>
			{
				for (int x = 0; x < this.Width; ++x)
				{
					var v = map[x, y];

					int iv = (int)v;

					for (int z = this.Depth - 1; z >= 0; --z)
					{
						/* above ground */
						if (z > iv)
						{
							if (z < waterLimit)
								grid[z, y, x] = Voxel.Water;
							else
								grid[z, y, x] = Voxel.Empty;
						}
						/* surface */
						else if (z == iv)
						{
							grid[z, y, x] = Voxel.Rock;

							if (z >= waterLimit && z < grassLimit)
							{
								grid[z, y, x].Flags = VoxelFlags.Grass;

								Dwarrowdelf.MWCRandom r = new MWCRandom(new IntVector3(x, y, z), 0);
								if (r.Next(100) < 30)
								{
									grid[z + 1, y, x].Flags |= VoxelFlags.Tree;
									//grid[z, y, x].Flags |= VoxelFlags.Tree2;
								}
							}
						}
						/* underground */
						else if (z < iv)
						{
							grid[z, y, x] = Voxel.Rock;
						}
						else
						{
							throw new Exception();
						}
					}
				}
			});
		}

		public static VoxelMap CreateBallMap(int side, int innerSide = 0)
		{
			var map = new VoxelMap(new IntSize3(side, side, side));

			var grid = map.Grid;

			int r = side / 2 - 1;
			int ir = innerSide / 2 - 1;

			Parallel.For(0, side, z =>
			{
				for (int y = 0; y < side; ++y)
					for (int x = 0; x < side; ++x)
					{
						var pr = Math.Sqrt((x - r) * (x - r) + (y - r) * (y - r) + (z - r) * (z - r));

						if (pr < r && pr >= ir)
							grid[z, y, x].Type = VoxelType.Rock;
						else
							grid[z, y, x].Type = VoxelType.Empty;
					}
			});

			return map;
		}

		public static VoxelMap CreateCubeMap(int side, int offset)
		{
			var map = new VoxelMap(new IntSize3(side, side, side));

			var grid = map.Grid;

			map.Clear(Voxel.Empty);

			for (int z = offset; z < side - offset; ++z)
				for (int y = offset; y < side - offset; ++y)
					for (int x = offset; x < side - offset; ++x)
					{
						grid[z, y, x].Type = VoxelType.Rock;
					}

			return map;
		}

		public unsafe static VoxelMap Load(string path)
		{
			using (var stream = File.OpenRead(path))
			{
				VoxelMap map;

				using (var br = new BinaryReader(stream, Encoding.Default, true))
				{
					int w = br.ReadInt32();
					int h = br.ReadInt32();
					int d = br.ReadInt32();

					var size = new IntSize3(w, h, d);

					map = new VoxelMap(size);
				}

				fixed (Voxel* v = map.Grid)
				{
					byte* p = (byte*)v;
					using (var memStream = new UnmanagedMemoryStream(p, 0, map.Size.Volume * sizeof(Voxel), FileAccess.Write))
						stream.CopyTo(memStream);
				}

				return map;
			}
		}

		public unsafe void Save(string path)
		{
			using (var stream = File.Create(path))
			{
				using (var bw = new BinaryWriter(stream, Encoding.Default, true))
				{
					bw.Write(this.Size.Width);
					bw.Write(this.Size.Height);
					bw.Write(this.Size.Depth);
				}

				fixed (Voxel* v = this.Grid)
				{
					byte* p = (byte*)v;
					using (var memStream = new UnmanagedMemoryStream(p, this.Size.Volume * sizeof(Voxel)))
						memStream.CopyTo(stream);
				}
			}
		}
	}
}
