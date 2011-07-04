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

		public override string ToString()
		{
			if (this == ObjectID.NullObjectID)
				return "OID(NULL)";
			else if (this == ObjectID.AnyObjectID)
				return "OID(ANY)";
			else
				return String.Format("OID({0}, {1})", this.ObjectType, this.Value);
		}

	}

}
