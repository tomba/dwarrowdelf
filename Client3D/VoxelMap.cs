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

		public bool IsUndefined { get { return this.Type == VoxelType.Undefined; } }
		public bool IsEmpty { get { return this.Type == VoxelType.Empty; } }

		public readonly static Voxel Undefined = new Voxel() { Type = VoxelType.Undefined };
		public readonly static Voxel Empty = new Voxel() { Type = VoxelType.Empty };
		public readonly static Voxel Rock = new Voxel() { Type = VoxelType.Rock };
		public readonly static Voxel Water = new Voxel() { Type = VoxelType.Water };
	}

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

						var neighbors = DirectionSet.CardinalUpDown.ToSurroundingPoints(p);

						foreach (var n in neighbors)
						{
							if (this.Size.Contains(n) == true && grid[n.Z, n.Y, n.X].IsEmpty == false)
								continue;

							var dir = (n - p).ToDirection();

							grid[z, y, x].VisibleFaces |= dir.ToFaceDirectionBits();
						}
					}
			});
		}

		public void UndefineHiddenVoxels()
		{
			var size = this.Size;

			var visibilityArray = new bool[size.Depth, size.Height, size.Width];

			for (int z = size.Depth - 1; z >= 0; --z)
			{
				bool lvlIsHidden = true;

				Parallel.For(0, size.Height, y =>
				{
					for (int x = 0; x < size.Width; ++x)
					{
						bool visible;

						// Air tiles are always visible
						if (this.Grid[z, y, x].IsEmpty)
						{
							visible = true;
						}
						else
						{
							var p = new IntVector3(x, y, z);
							visible = DirectionSet.All.ToSurroundingPoints(p)
								.Where(this.Size.Contains)
								.Any(n => GetVoxel(n).IsEmpty);
						}

						if (visible)
						{
							lvlIsHidden = false;
							visibilityArray[z, y, x] = true;
						}
					}
				});

				// if the whole level is not visible, the levels below cannot be seen either
				if (lvlIsHidden)
					break;
			}

			for (int z = this.Depth - 1; z >= 0; --z)
			{
				Parallel.For(0, this.Height, y =>
				{
					for (int x = 0; x < this.Width; ++x)
					{
						if (visibilityArray[z, y, x] == false)
						{
							this.Grid[z, y, x] = Voxel.Undefined;
						}
					}
				});
			}
		}

		public void FillFromNoiseMap(SharpNoise.NoiseMap map)
		{
			var max = map.Data.Max();
			var min = map.Data.Min();

			var grid = this.Grid;

			int waterLimit = this.Depth * 3 / 10;
			int grassLimit = this.Depth * 8 / 10;

			Parallel.For(0, this.Height, y =>
			{
				for (int x = 0; x < this.Width; ++x)
				{
					var v = map[x, y];	// [-1 .. 1]

					v -= min;
					v /= (max - min);	// [0 .. 1]

					v *= this.Depth * 8 / 10;
					v += this.Depth * 2 / 10;

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

		public static VoxelMap CreateFromTileData(TileData[, ,] tileData)
		{
			int d = tileData.GetLength(0);
			int h = tileData.GetLength(1);
			int w = tileData.GetLength(2);

			var size = new IntSize3(w, h, d);

			var map = new VoxelMap(size);

			for (int z = 0; z < d; ++z)
				for (int y = 0; y < h; ++y)
					for (int x = 0; x < w; ++x)
					{
						ConvertTile(x, y, z, map.Grid, tileData);
					}

			return map;
		}

		static void ConvertTile(int x, int y, int z, Voxel[, ,] grid, TileData[, ,] tileData)
		{
			var td = tileData[z, y, x];

			if (td.IsUndefined)
			{
				grid[z, y, x].Type = VoxelType.Undefined;
				return;
			}

			if (td.IsEmpty)
			{
				grid[z, y, x].Type = VoxelType.Empty;
				return;
			}

			if (td.InteriorID == InteriorID.NaturalWall)
			{
				grid[z, y, x].Type = VoxelType.Rock;
				return;
			}

			if (td.WaterLevel > 0)
			{
				grid[z, y, x].Type = VoxelType.Water;
				return;
			}

			if (td.IsGreen)
			{
				grid[z - 1, y, x].Flags |= VoxelFlags.Grass;

				Dwarrowdelf.MWCRandom r = new MWCRandom(new IntVector3(x, y, z), 0);

				if (r.Next(100) < 30)
				{
					grid[z, y, x].Flags |= VoxelFlags.Tree;
					grid[z - 1, y, x].Flags |= VoxelFlags.Tree2;
				}
			}

			grid[z, y, x].Type = VoxelType.Empty;
		}

		public static VoxelMap Load(string path)
		{
			using (var stream = File.OpenRead(path))
			using (var br = new BinaryReader(stream))
			{
				int w = br.ReadInt32();
				int h = br.ReadInt32();
				int d = br.ReadInt32();

				var size = new IntSize3(w, h, d);

				var map = new VoxelMap(size);

				var grid = map.Grid;

				for (int z = 0; z < d; ++z)
					for (int y = 0; y < h; ++y)
						for (int x = 0; x < w; ++x)
						{
							grid[z, y, x].Raw = br.ReadUInt32();
						}

				return map;
			}
		}

		public void Save(string path)
		{
			using (var stream = File.Create(path))
			using (var bw = new BinaryWriter(stream))
			{
				bw.Write(this.Size.Width);
				bw.Write(this.Size.Height);
				bw.Write(this.Size.Depth);

				int w = this.Width;
				int h = this.Height;
				int d = this.Depth;

				for (int z = 0; z < d; ++z)
					for (int y = 0; y < h; ++y)
						for (int x = 0; x < w; ++x)
							bw.Write(this.Grid[z, y, x].Raw);
			}
		}
	}
}
