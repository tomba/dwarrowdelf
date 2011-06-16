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
	public class SaveGameObjectAttribute : Attribute
	{
		public bool UseRef { get; set; }

		public SaveGameObjectAttribute()
		{
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class SaveGamePropertyAttribute : Attribute
	{
		public string Name { get; set; }
		public Type Converter { get; set; }
		public Type ReaderWriter { get; set; }

		public SaveGamePropertyAttribute()
		{
		}

		public SaveGamePropertyAttribute(string name)
		{
			this.Name = name;
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnSaveGameSerializingAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnSaveGameSerializedAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnSaveGameDeserializingAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnSaveGameDeserializedAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public class OnSaveGamePostDeserializationAttribute : Attribute
	{
	}
}
