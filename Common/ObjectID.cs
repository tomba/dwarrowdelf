using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	[Serializable]
	[System.ComponentModel.TypeConverter(typeof(ObjectIDConverter))]
	public struct ObjectID : IEquatable<ObjectID>
	{
		readonly int m_value;

		public static readonly ObjectID NullObjectID = new ObjectID(0);

		public ObjectID(int rawValue)
		{
			m_value = rawValue;
		}

		public ObjectID(ObjectType objectType, int value)
		{
			if ((value & ~((1 << 24) - 1)) != 0)
				throw new Exception();

			m_value = ((int)objectType << 24) | value;
		}

		public int RawValue { get { return m_value; } }

		public int Value { get { return m_value & ((1 << 24) - 1); } }
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
			return m_value;
		}

		public override string ToString()
		{
			return String.Format("OID({0:x})", m_value);
		}

	}

}
