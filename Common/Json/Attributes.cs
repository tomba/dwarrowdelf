using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public struct SaveGameContext
	{
	}

	public abstract class SaveGameObjectBaseAttribute : Attribute
	{

	}

	[AttributeUsageAttribute(AttributeTargets.Class, Inherited = true)]
	public class SaveGameObjectByRefAttribute : SaveGameObjectBaseAttribute
	{
		public bool ClientObject { get; set; }

		public SaveGameObjectByRefAttribute()
		{
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Class, Inherited = true)]
	public class SaveGameObjectByValueAttribute : SaveGameObjectBaseAttribute
	{
		public SaveGameObjectByValueAttribute()
		{
		}
	}

	[AttributeUsageAttribute(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
	public class SaveGamePropertyAttribute : Attribute
	{
		public string Name { get; set; }
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
