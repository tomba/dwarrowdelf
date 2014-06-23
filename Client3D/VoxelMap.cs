using Dwarrowdelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	enum VoxelType
	{
		Undefined = 0,
		Empty,
		Rock,
	}

	struct Voxel
	{
		public VoxelType Type;
		public bool IsGrass;

		public bool IsUndefined { get { return this.Type == VoxelType.Undefined; } }
		public bool IsEmpty { get { return this.Type == VoxelType.Empty; } }

		public readonly static Voxel Undefined = new Voxel() { Type = VoxelType.Undefined };
		public readonly static Voxel Empty = new Voxel() { Type = VoxelType.Empty };
		public readonly static Voxel Rock = new Voxel() { Type = VoxelType.Rock };
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
			this.Size = size;
			this.Grid = new Voxel[size.Depth, size.Height, size.Width];
		}

		public Voxel GetVoxel(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X];
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
							var p = new IntPoint3(x, y, z);
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

		public static VoxelMap CreateSimplexMap(int side, float limit)
		{
			var map = new VoxelMap(new IntSize3(side, side, side));

			var grid = map.Grid;

			var n = new SharpNoise.Modules.Simplex()
			{
				OctaveCount = 3,
			};

			Parallel.For(0, side, z =>
			{
				for (int y = 0; y < side; ++y)
					for (int x = 0; x < side; ++x)
					{
						var v = new SharpDX.Vector3(x, y, z) / side;

						var val = n.GetValue(v.X, v.Y, v.Z);

						if (val < limit)
							grid[z, y, x] = Voxel.Empty;
						else
							grid[z, y, x] = Voxel.Rock;
					}
			});

			return map;
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

			if (td.IsGreen)
				grid[z - 1, y, x].IsGrass = true;

			grid[z, y, x].Type = VoxelType.Empty;
		}
	}
}
