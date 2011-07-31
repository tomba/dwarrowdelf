using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	// Stored in render tile data, needs to be short
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
		SlopeUpNorthEast,
		SlopeUpEast,
		SlopeUpSouthEast,
		SlopeUpSouth,
		SlopeUpSouthWest,
		SlopeUpWest,
		SlopeUpNorthWest,

		SlopeDownNorth,
		SlopeDownNorthEast,
		SlopeDownEast,
		SlopeDownSouthEast,
		SlopeDownSouth,
		SlopeDownSouthWest,
		SlopeDownWest,
		SlopeDownNorthWest,

		Sapling,
		Tree,
		Grass,

		/* Livings */
		Player,
		Sheep,
		Wolf,
		Dragon,

		/* items */
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
