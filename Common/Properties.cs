using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public enum PropertyID : ushort
	{
		None,

		VisionRange,
		Color,

		HitPoints,
		SpellPoints,

		Strength,
		Dexterity,
		Constitution,
		Intelligence,
		Wisdom,
		Charisma,
	}

	public class PropertyDefinition
	{
		public PropertyDefinition(PropertyID id, PropertyVisibility visibility, object defaultValue)
		{
			this.PropertyID = id;
			this.Visibility = visibility;
			this.DefaultValue = defaultValue;
		}

		public PropertyID PropertyID { get; private set; }
		public PropertyVisibility Visibility { get; private set; }
		public object DefaultValue { get; private set; }
	}

	public enum PropertyVisibility
	{
		Public,		// everybody see
		Friendly,	// friendlies see
	}
}
