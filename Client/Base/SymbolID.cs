using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	// Stored in render tile data, needs to be short
	public enum SymbolID : short
	{
		Undefined,

		Unknown,

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
		Block,
		Gem,
		Key,
		Rock,
		Log,
		Contraption,
		Consumable,
		Chair,
		Table,
		Door,
		Bed,
		Barrel,
		Bucket,

		/* top */
		Water,

		/* designations */
		DesignationMine,
	}
}
