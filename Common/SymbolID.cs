using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum SymbolID : short
	{
		Undefined,

		/* floors */
		Floor,

		/* interiors */
		Wall,
		ValuableOre,
		GemOre,
		StairsUp,
		StairsDown,
		StairsUpDown,

		SlopeUpNorth,
		SlopeUpSouth,
		SlopeUpWest,
		SlopeUpEast,

		SlopeDownNorth,
		SlopeDownSouth,
		SlopeDownWest,
		SlopeDownEast,

		Sapling,
		Tree,
		Grass,

		/* objects */
		Player,
		Monster,
		UncutGem,
		Gem,
		Key,
		Rock,
		Log,
		Contraption,
		Consumable,
		Chair,
		Table,

		/* top */
		Water,
	}
}
