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
		Size,

		NaturalArmorClass,

		VisionRange,
		Hunger,
		Thirst,

		Assignment,
		ReservedByStr,

		Gender,

		// Item
		Quality,

		NutritionalValue,
		RefreshmentValue,

		IsInstalled,
		IsClosed,
	}

	[Flags]
	public enum ObjectVisibility
	{
		None = 0,
		Private = 1 << 0,
		Public = 1 << 1,
		Debug = 2 << 1,
		All = Private | Public | Debug,
	}

	public static class PropertyVisibilities
	{
		static ObjectVisibility[] s_visibilityArray;

		static PropertyVisibilities()
		{
			var map = new Dictionary<PropertyID, ObjectVisibility>();

			// Common
			map[PropertyID.Name] = ObjectVisibility.Public;
			map[PropertyID.MaterialID] = ObjectVisibility.Public;
			map[PropertyID.Color] = ObjectVisibility.Public;

			// Living
			map[PropertyID.HitPoints] = ObjectVisibility.Private;
			map[PropertyID.MaxHitPoints] = ObjectVisibility.Private;
			map[PropertyID.SpellPoints] = ObjectVisibility.Private;
			map[PropertyID.MaxSpellPoints] = ObjectVisibility.Private;

			map[PropertyID.Strength] = ObjectVisibility.Private;
			map[PropertyID.Dexterity] = ObjectVisibility.Private;
			map[PropertyID.Constitution] = ObjectVisibility.Private;
			map[PropertyID.Intelligence] = ObjectVisibility.Private;
			map[PropertyID.Wisdom] = ObjectVisibility.Private;
			map[PropertyID.Charisma] = ObjectVisibility.Private;
			map[PropertyID.Size] = ObjectVisibility.Public;

			map[PropertyID.NaturalArmorClass] = ObjectVisibility.Private;

			map[PropertyID.VisionRange] = ObjectVisibility.Private;
			map[PropertyID.Hunger] = ObjectVisibility.Private;
			map[PropertyID.Thirst] = ObjectVisibility.Private;

			map[PropertyID.Gender] = ObjectVisibility.Public;

			map[PropertyID.Assignment] = ObjectVisibility.Debug;

			// Item
			map[PropertyID.Quality] = ObjectVisibility.Public;
			map[PropertyID.NutritionalValue] = ObjectVisibility.Public;
			map[PropertyID.RefreshmentValue] = ObjectVisibility.Public;
			map[PropertyID.ReservedByStr] = ObjectVisibility.Debug;
			map[PropertyID.IsInstalled] = ObjectVisibility.Public;
			map[PropertyID.IsClosed] = ObjectVisibility.Public;

			s_visibilityArray = new ObjectVisibility[EnumHelpers.GetEnumMax<PropertyID>() + 1];

			foreach (var kvp in map)
				s_visibilityArray[(int)kvp.Key] = kvp.Value;

			for (int i = 1; i < s_visibilityArray.Length; ++i)
				if (s_visibilityArray[i] == ObjectVisibility.None)
					throw new Exception("missing visibility for " + (PropertyID)i);
		}

		public static ObjectVisibility GetPropertyVisibility(PropertyID id)
		{
			return s_visibilityArray[(int)id];
		}
	}
}
