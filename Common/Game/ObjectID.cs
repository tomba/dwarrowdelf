using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Globalization;

namespace Dwarrowdelf
{
	/// <summary>
	/// 24 bit ID with 8 bit Object type
	/// </summary>
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(ObjectIDConverter))]
	public struct ObjectID : IEquatable<ObjectID>
	{
		readonly uint m_value;

		public static readonly ObjectID NullObjectID = new ObjectID(ObjectType.None, 0x0);
		public static readonly ObjectID AnyObjectID = new ObjectID(ObjectType.None, 0x1);

		public ObjectID(uint rawValue)
		{
			m_value = rawValue;
		}

		public ObjectID(ObjectType objectType, uint value)
		{
			if ((value & ~((1 << 24) - 1)) != 0)
				throw new Exception();

			m_value = ((uint)objectType << 24) | value;
		}

		public uint RawValue { get { return m_value; } }

		public uint Value { get { return m_value & ((1 << 24) - 1); } }
		public ObjectType ObjectType { get { return (ObjectType)(m_value >> 24); } }

		public bool Equals(ObjectID objectID)
		{
			return objectID.m_value == m_value;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is ObjectID))
				return false;

			ObjectID objectID = (ObjectID)obj;
			return objectID.m_value == m_value;
		}

		public static bool operator ==(ObjectID left, ObjectID right)
		{
			return left.m_value == right.m_value;
		}

		public static bool operator !=(ObjectID left, ObjectID right)
		{
			return !(left == right);
		}

		public override int GetHashCode()
		{
			return (int)m_value;
		}

		public static ObjectID GetID(IIdentifiable ob)
		{
			if (ob == null)
				return ObjectID.NullObjectID;
			else
				return ob.ObjectID;
		}

		public static bool TryParse(string str, out ObjectID oid)
		{
			oid = ObjectID.NullObjectID;

			if (str.Length < 2)
				return false;

			if (string.Compare(str, "NULL", true) == 0)
			{
				oid = ObjectID.NullObjectID;
				return true;
			}

			if (string.Compare(str, "ANY", true) == 0)
			{
				oid = ObjectID.AnyObjectID;
				return true;
			}

			char c = str[0];
			c = char.ToUpper(c);

			uint value;
			if (uint.TryParse(str.Substring(1), out value) == false)
				return false;

			ObjectType ot = Dwarrowdelf.ObjectType.None;

			switch (c)
			{
				case 'E': ot = Dwarrowdelf.ObjectType.Environment; break;
				case 'I': ot = Dwarrowdelf.ObjectType.Item; break;
				case 'L': ot = Dwarrowdelf.ObjectType.Living; break;
				default: return false;
			}

			oid = new ObjectID(ot, value);
			return true;
		}

		public override string ToString()
		{
			if (this == ObjectID.NullObjectID)
				return "NULL";
			else if (this == ObjectID.AnyObjectID)
				return "ANY";
			else
			{
				char c;
				switch (this.ObjectType)
				{
					case Dwarrowdelf.ObjectType.Environment: c = 'E'; break;
					case Dwarrowdelf.ObjectType.Item: c = 'I'; break;
					case Dwarrowdelf.ObjectType.Living: c = 'L'; break;
					default: c = '?'; break;
				}

				return String.Format("{0}{1}", c, this.Value);
			}
		}
	}
	public sealed class ObjectIDConverter : TypeConverter
	{
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return ((destinationType == typeof(string)) || base.CanConvertTo(context, destinationType));
		}

		public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
		{
			return ((sourceType == typeof(string)) || base.CanConvertFrom(context, sourceType));
		}

		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			if ((destinationType == null) || !(value is ObjectID) || destinationType != typeof(string))
				return base.ConvertTo(context, culture, value, destinationType);

			var oid = (ObjectID)value;
			return oid.RawValue.ToString(System.Globalization.NumberFormatInfo.InvariantInfo);
		}

		public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
		{
			if (value == null)
				throw base.GetConvertFromException(value);

			string source = value as string;

			if (source == null)
				return base.ConvertFrom(context, culture, value);

			return new ObjectID(Convert.ToUInt32(source, System.Globalization.NumberFormatInfo.InvariantInfo));
		}
	}

}
