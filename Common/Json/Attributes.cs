using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public struct SaveGameContext
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Class, Inherited = true)]
	public sealed class SaveGameObjectAttribute : Attribute
	{
		public bool ClientObject { get; set; }
		public bool ByValue { get; set; }

		public SaveGameObjectAttribute()
		{
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public sealed class SaveGamePropertyAttribute : Attribute
	{
		public string Name { get; private set; }
		public Type Converter { get; set; }
		public Type ReaderWriter { get; set; }
		public bool UseOldList { get; set; }

		public SaveGamePropertyAttribute()
		{
		}

		public SaveGamePropertyAttribute(string name)
		{
			this.Name = name;
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public sealed class OnSaveGameSerializingAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public sealed class OnSaveGameSerializedAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public sealed class OnSaveGameDeserializingAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public sealed class OnSaveGameDeserializedAttribute : Attribute
	{
	}

	[AttributeUsageAttribute(AttributeTargets.Method)]
	public sealed class OnSaveGamePostDeserializationAttribute : Attribute
	{
	}
}
