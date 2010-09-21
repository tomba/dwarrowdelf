using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum PropertyID : ushort
	{
		None,

		Name,

		SymbolID,
		MaterialID,
		Color,

		HitPoints,
		SpellPoints,

		Strength,
		Dexterity,
		Constitution,
		Intelligence,
		Wisdom,
		Charisma,

		VisionRange,
		FoodFullness,
		WaterFullness,

		NutritionalValue,
		RefreshmentValue,

		Assignment,
	}

	public delegate void PropertyChangedCallback(PropertyDefinition property, object ob, object oldValue, object newValue);

	public class PropertyDefinition
	{
		public PropertyDefinition(PropertyID id, PropertyVisibility visibility, object defaultValue,
			PropertyChangedCallback propertyChangedCallback = null)
		{
			this.PropertyID = id;
			this.Visibility = visibility;
			this.DefaultValue = defaultValue;
			this.PropertyChangedCallback = propertyChangedCallback;
		}

		public PropertyID PropertyID { get; private set; }
		public PropertyVisibility Visibility { get; private set; }
		public object DefaultValue { get; private set; }
		public PropertyChangedCallback PropertyChangedCallback { get; private set; }
	}

	public enum PropertyVisibility
	{
		Public,		// everybody see
		Friendly,	// friendlies see
	}
}
