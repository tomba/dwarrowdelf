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
		Ore,
		StairsUp,
		StairsDown,
		StairsUpDown,
		Portal,

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
		Gem,
		Key,
		Rock,
		Log,
		Contraption,
		Consumable,

		/* top */
		Water,
	}
}
