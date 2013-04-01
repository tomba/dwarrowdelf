using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;

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

	class Map
	{
		public AStarMapTile[, ,] Grid { get; private set; }

		public IntGrid3 Bounds { get; private set; }

		public Map(int width, int height, int depth)
		{
			this.Grid = new AStarMapTile[depth, height, width];

			this.Bounds = new IntGrid3(0, 0, 0, width, height, depth);

			for (int y = 0; y < 350; ++y)
				SetBlocked(new IntPoint3(5, y, 0), true);

			for (int y = 2; y < 22; ++y)
				SetBlocked(new IntPoint3(14, y, 0), true);

			for (int y = 6; y < 11; ++y)
				SetWeight(new IntPoint3(10, y, 0), 40);


			for (int y = 6; y < 18; ++y)
				SetBlocked(new IntPoint3(3, y, 1), true);

			for (int y = 6; y < 11; ++y)
				SetWeight(new IntPoint3(5, y, 1), 40);


			SetStairs(new IntPoint3(10, 10, 0), Stairs.Up);
			SetStairs(new IntPoint3(10, 10, 1), Stairs.Down);

			SetStairs(new IntPoint3(15, 12, 0), Stairs.Up);
			SetStairs(new IntPoint3(15, 12, 1), Stairs.Down);

			var r = new Random(4);
			var bounds = new IntGrid2Z(16, 0, 30, 30, 0);
			foreach (var p in bounds.Range())
			{
				var v = r.Next(100);
				if (v < 30)
					SetBlocked(p, true);
				else if (v < 60)
					SetWeight(p, v);
			}

		}

		public int GetWeight(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].Weight;
		}

		public void SetWeight(IntPoint3 p, int weight)
		{
			this.Grid[p.Z, p.Y, p.X].Weight = weight;
		}

		public bool GetBlocked(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].Blocked;
		}

		public void SetBlocked(IntPoint3 p, bool blocked)
		{
			this.Grid[p.Z, p.Y, p.X].Blocked = blocked;
		}

		public Stairs GetStairs(IntPoint3 p)
		{
			return this.Grid[p.Z, p.Y, p.X].Stairs;
		}

		public void SetStairs(IntPoint3 p, Stairs stairs)
		{
			this.Grid[p.Z, p.Y, p.X].Stairs = stairs;
		}
	}
}
