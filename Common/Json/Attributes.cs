using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public struct GameSerializationContext
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Class, Inherited = false)]
	public class GameObjectAttribute : Attribute
	{
		public bool UseRef { get; set; }

		public GameObjectAttribute()
		{
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class GamePropertyAttribute : Attribute
	{
		public string Name { get; set; }
		public Type Converter { get; set; }

		public GamePropertyAttribute()
		{
		}

		public GamePropertyAttribute(string name)
		{
			this.Name = name;
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnGameSerializingAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnGameSerializedAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnGameDeserializingAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnGameDeserializedAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnGamePostDeserializationAttribute : Attribute
	{
	}
}
