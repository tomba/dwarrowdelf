using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public enum TileID : byte
	{
		Undefined = 0,

		Empty,

		NaturalWall,
		Slope,

		Stairs,
		BuiltWall,
		Pavement,

		Sapling,
		Tree,
		DeadTree,
		Grass,
		Shrub,
	}
}
