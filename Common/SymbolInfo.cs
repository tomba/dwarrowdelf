using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum SymbolID : short
	{
		Undefined,

		/* floors */
		Floor,

		/* interiors */
		Wall,
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
	}

	public class SymbolInfo
	{
		public SymbolID ID { get; set; }
		public string Name { get; set; }
		public char CharSymbol { get; set; }
		public string DrawingName { get; set; }
		public double X { get; set; }
		public double Y { get; set; }
		public double Width { get; set; }
		public double Height { get; set; }
		public double CharRotation { get; set; }
		public double DrawingRotation { get; set; }
	}
}
