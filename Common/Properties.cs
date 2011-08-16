using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum PropertyID
	{
		None,

		// Base
		Name,
		MaterialID,
		Color,

		// Living
		HitPoints,
		MaxHitPoints,

		SpellPoints,
		MaxSpellPoints,

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

		Gender,

		// Item
		NutritionalValue,
		RefreshmentValue,
	}

	public enum PropertyVisibility
	{
		Public,
		Friendly,
	}

	public static class PropertyVisibilities
	{
		static PropertyVisibility[] s_visibilityArray;

		static PropertyVisibilities()
		{
			var map = new Dictionary<PropertyID, PropertyVisibility>();

			map[PropertyID.Name] = PropertyVisibility.Public;
			map[PropertyID.MaterialID] = PropertyVisibility.Public;
			map[PropertyID.Color] = PropertyVisibility.Public;

			// Living
			map[PropertyID.HitPoints] = PropertyVisibility.Friendly;
			map[PropertyID.SpellPoints] = PropertyVisibility.Friendly;

			map[PropertyID.Strength] = PropertyVisibility.Friendly;
			map[PropertyID.Dexterity] = PropertyVisibility.Friendly;
			map[PropertyID.Constitution] = PropertyVisibility.Friendly;
			map[PropertyID.Intelligence] = PropertyVisibility.Friendly;
			map[PropertyID.Wisdom] = PropertyVisibility.Friendly;
			map[PropertyID.Charisma] = PropertyVisibility.Friendly;

			map[PropertyID.ArmorClass] = PropertyVisibility.Friendly;

			map[PropertyID.VisionRange] = PropertyVisibility.Friendly;
			map[PropertyID.FoodFullness] = PropertyVisibility.Friendly;
			map[PropertyID.WaterFullness] = PropertyVisibility.Friendly;

			map[PropertyID.Assignment] = PropertyVisibility.Friendly;

			// Item
			map[PropertyID.NutritionalValue] = PropertyVisibility.Public;
			map[PropertyID.RefreshmentValue] = PropertyVisibility.Public;


			var max = map.Keys.Max(id => (int)id);
			s_visibilityArray = new PropertyVisibility[max + 1];

			foreach (var kvp in map)
				s_visibilityArray[(int)kvp.Key] = kvp.Value;
		}

		// XXX Server's living has own checks for visibility
		public static PropertyVisibility GetPropertyVisibility(PropertyID id)
		{
			return s_visibilityArray[(int)id];
		}
	}
}
