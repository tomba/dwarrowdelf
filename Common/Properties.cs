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

		ArmorClass,

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
		public PropertyDefinition(PropertyID id, Type propertyType, PropertyVisibility visibility, object defaultValue,
			PropertyChangedCallback propertyChangedCallback = null)
		{
			this.PropertyID = id;
			this.PropertyType = propertyType;
			this.Visibility = visibility;
			this.DefaultValue = defaultValue;
			this.PropertyChangedCallback = propertyChangedCallback;
		}

		public PropertyID PropertyID { get; private set; }
		public Type PropertyType { get; private set; }
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
