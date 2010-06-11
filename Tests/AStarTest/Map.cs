using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyGame;

namespace AStarTest
{
	enum Stairs
	{
		None,
		Up,
		Down,
		UpDown,
	}

	struct AStarMapTile
	{
		public int Weight;
		public bool Blocked;
		public Stairs Stairs;
	}

	class Map : ArrayGrid3D<AStarMapTile>
	{
		public Map(int width, int height, int depth)
			: base(width, height, depth)
		{
			for (int y = 0; y < 350; ++y)
				SetBlocked(new IntPoint3D(5, y, 0), true);

			for (int y = 2; y < 22; ++y)
				SetBlocked(new IntPoint3D(14, y, 0), true);

			for (int y = 6; y < 11; ++y)
				SetWeight(new IntPoint3D(10, y, 0), 40);


			for (int y = 6; y < 18; ++y)
				SetBlocked(new IntPoint3D(3, y, 1), true);

			for (int y = 6; y < 11; ++y)
				SetWeight(new IntPoint3D(5, y, 1), 40);


			SetStairs(new IntPoint3D(10, 10, 0), Stairs.Up);
			SetStairs(new IntPoint3D(10, 10, 1), Stairs.Down);

			SetStairs(new IntPoint3D(15, 12, 0), Stairs.Up);
			SetStairs(new IntPoint3D(15, 12, 1), Stairs.Down);

		}

		public int GetWeight(IntPoint3D p)
		{
			return base.Grid[p.Z, p.Y, p.X].Weight;
		}

		public void SetWeight(IntPoint3D p, int weight)
		{
			base.Grid[p.Z, p.Y, p.X].Weight = weight;
		}

		public bool GetBlocked(IntPoint3D p)
		{
			return base.Grid[p.Z, p.Y, p.X].Blocked;
		}

		public void SetBlocked(IntPoint3D p, bool blocked)
		{
			base.Grid[p.Z, p.Y, p.X].Blocked = blocked;
		}

		public Stairs GetStairs(IntPoint3D p)
		{
			return base.Grid[p.Z, p.Y, p.X].Stairs;
		}

		public void SetStairs(IntPoint3D p, Stairs stairs)
		{
			base.Grid[p.Z, p.Y, p.X].Stairs = stairs;
		}
	}
}
