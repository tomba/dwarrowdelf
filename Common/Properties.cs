using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum PropertyID : ushort
	{
		None,

		// Base
		Name,
		SymbolID,
		MaterialID,
		Color,

		// Living
		HitPoints,
		SpellPoints,

		Strength,
		Dexterity,
		Constitution,
		Intelligence,
		Wisdom,
		Charisma,

		ArmorClass,

		VisionRange,
		FoodFullness,
		WaterFullness,

		Assignment,

		// Item
		NutritionalValue,
		RefreshmentValue,
	}
}
