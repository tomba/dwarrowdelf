using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Dwarrowdelf
{
	public interface IGameConverter
	{
		object ConvertToSerializable(object parent, object value);
		object ConvertFromSerializable(object parent, object value);
		Type OutputType { get; }
	}

	class GameMemberEntry
	{
		public string Name { get; private set; }
		public MemberInfo Member { get; private set; }
		public Type MemberType { get; private set; }
		public IGameConverter Converter { get; private set; }

		public GameMemberEntry(MemberInfo member)
		{
			var attrs = member.GetCustomAttributes(typeof(GamePropertyAttribute), false);
			Trace.Assert(attrs.Length == 1);
			var attr = (GamePropertyAttribute)attrs[0];

			string name = attr.Name ?? member.Name;

			Type memberType;
			if (member.MemberType == MemberTypes.Field)
			{
				var field = (FieldInfo)member;
				memberType = field.FieldType;
			}
			else if (member.MemberType == MemberTypes.Property)
			{
				var prop = (PropertyInfo)member;
				memberType = prop.PropertyType;
			}
			else
				throw new Exception();

			IGameConverter converter = null;
			if (attr.Converter != null)
			{
				converter = GetConverter(attr.Converter);
				memberType = converter.OutputType;
			}

			this.Member = member;
			this.Name = name;
			this.MemberType = memberType;
			this.Converter = converter;
		}

		static Dictionary<Type, IGameConverter> s_converterMap = new Dictionary<Type, IGameConverter>();

		static IGameConverter GetConverter(Type converterType)
		{
			IGameConverter converter;

			lock (s_converterMap)
			{
				if (!s_converterMap.TryGetValue(converterType, out converter))
				{
					converter = (IGameConverter)Activator.CreateInstance(converterType);
					s_converterMap[converterType] = converter;
				}
			}

			return converter;
		}
	}

	enum TypeClass
	{
		Undefined = 0,
		Basic,
		Enum,
		Convertable,
		Array,
		List,
		GenericList,
		Dictionary,
		GenericDictionary,
		GameObject,
		Serializable,
	}

	class TypeInfo
	{
		static Dictionary<Type, TypeInfo> s_typeInfoMap = new Dictionary<Type, TypeInfo>();

		public static TypeInfo GetTypeInfo(Type type)
		{
			TypeInfo ti;

			lock (s_typeInfoMap)
			{
				if (s_typeInfoMap.TryGetValue(type, out ti) == false)
				{
					ti = new TypeInfo(type);
					s_typeInfoMap[type] = ti;
				}
			}

			return ti;
		}


		public Type Type { get; private set; }

		public TypeClass TypeClass { get; private set; }
		public TypeConverter TypeConverter { get; private set; }

		public ConstructorInfo DeserializeConstructor { get; private set; }
		public GameMemberEntry[] GameMemberEntries { get; private set; }
		public MethodInfo[] OnSerializingMethods { get; private set; }
		public MethodInfo[] OnSerializedMethods { get; private set; }
		public MethodInfo[] OnDeserializingMethods { get; private set; }
		public MethodInfo[] OnDeserializedMethods { get; private set; }
		public MethodInfo[] OnGamePostDeserializationMethods { get; private set; }
		public bool UseRef { get; private set; }

		public MemberInfo[] SerializableMembers { get; private set; }

		public Type ElementType1 { get; private set; }
		public Type ElementType2 { get; private set; }

		public TypeInfo(Type type)
		{
			this.Type = type;
			this.TypeConverter = GetConverter(type);

			var gameObjAttrs = type.GetCustomAttributes(typeof(GameObjectAttribute), false);

			Type iface;

			if (type.IsEnum)
			{
				this.TypeClass = TypeClass.Enum;
			}
			else if (IsBasicType(type))
			{
				this.TypeClass = TypeClass.Basic;
			}
			else if (this.TypeConverter != null)
			{
				this.TypeClass = TypeClass.Convertable;
			}
			else if (type.IsArray)
			{
				this.TypeClass = TypeClass.Array;
				this.ElementType1 = type.GetElementType();
			}
			else if ((iface = FindGenericInterface(typeof(IDictionary<object, object>).GetGenericTypeDefinition(), type)) != null)
			{
				this.TypeClass = TypeClass.GenericDictionary;
				this.ElementType1 = iface.GetGenericArguments()[0];
				this.ElementType2 = iface.GetGenericArguments()[1];
			}
			else if (typeof(IDictionary).IsAssignableFrom(type))
			{
				this.TypeClass = TypeClass.Dictionary;
			}
			else if ((iface = FindGenericInterface(typeof(IList<object>).GetGenericTypeDefinition(), type)) != null)
			{
				this.TypeClass = TypeClass.GenericList;
				this.ElementType1 = iface.GetGenericArguments()[0];
			}
			else if (typeof(IList).IsAssignableFrom(type))
			{
				this.TypeClass = TypeClass.List;
			}
			else if (gameObjAttrs.Length > 0)
			{
				var attr = (GameObjectAttribute)gameObjAttrs[0];

				this.TypeClass = TypeClass.GameObject;
				this.GameMemberEntries = GetMemberEntries(type);
				this.OnSerializingMethods = GetSerializationMethods(type, typeof(OnGameSerializingAttribute));
				this.OnSerializedMethods = GetSerializationMethods(type, typeof(OnGameSerializedAttribute));
				this.OnDeserializingMethods = GetSerializationMethods(type, typeof(OnGameDeserializingAttribute));
				this.OnDeserializedMethods = GetSerializationMethods(type, typeof(OnGameDeserializedAttribute));
				this.OnGamePostDeserializationMethods = GetSerializationMethods(type, typeof(OnGamePostDeserializationAttribute));
				this.DeserializeConstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(GameSerializationContext) }, null);
				if (this.DeserializeConstructor == null)
					throw new Exception(String.Format("Need Deserialize constructor for type {0}", type.Name));
				this.UseRef = attr.UseRef;
			}
			else if (type.Attributes.HasFlag(TypeAttributes.Serializable))
			{
				this.TypeClass = TypeClass.Serializable;
				this.SerializableMembers = FormatterServices.GetSerializableMembers(type);
			}
			else
			{
				throw new Exception(String.Format("Type {0} not serializable", type.Name));
			}
		}

		static Type FindGenericInterface(Type genericIface, Type type)
		{
			var ifaces = type.GetInterfaces();

			foreach (var iface in ifaces)
			{
				if (!iface.IsGenericType)
					continue;

				var geniface = iface.GetGenericTypeDefinition();

				if (geniface == genericIface)
					return iface;
			}

			return null;
		}

		public object[] GetObjectData(object ob)
		{
			var entries = this.GameMemberEntries;

			object[] values = new object[entries.Length];

			for (int i = 0; i < entries.Length; ++i)
			{
				var entry = entries[i];
				object value;

				if (entry.Member.MemberType == MemberTypes.Field)
				{
					var field = (FieldInfo)entry.Member;
					value = field.GetValue(ob);
				}
				else if (entry.Member.MemberType == MemberTypes.Property)
				{
					var prop = (PropertyInfo)entry.Member;
					value = prop.GetValue(ob, null);
				}
				else
					throw new Exception();

				values[i] = value;
			}

			return values;
		}

		public void PopulateObjectMembers(object ob, object[] values)
		{
			var entries = this.GameMemberEntries;

			for (int i = 0; i < entries.Length; ++i)
			{
				var entry = entries[i];
				var value = values[i];
				var member = entry.Member;

				if (member.MemberType == MemberTypes.Field)
					((FieldInfo)member).SetValue(ob, value);
				else if (member.MemberType == MemberTypes.Property)
					((PropertyInfo)member).SetValue(ob, value, null);
				else
					throw new Exception();
			}
		}



		static GameMemberEntry[] GetMemberEntries(Type type)
		{
			var members = GetMembers(type);
			var entries = new List<GameMemberEntry>();
			var nameSet = new HashSet<string>();

			foreach (var member in members)
			{
				var attrs = member.GetCustomAttributes(typeof(GamePropertyAttribute), false);
				if (attrs.Length == 0)
					continue;

				var entry = new GameMemberEntry(member);

				if (nameSet.Add(entry.Name) == false)
					throw new Exception("duplicate name");

				entries.Add(entry);
			}

			entries.Sort(CompareEntries);

			return entries.ToArray();
		}

		static int CompareEntries(GameMemberEntry a, GameMemberEntry b)
		{
			bool simpleA = TypeIsSimple(a.MemberType);
			bool simpleB = TypeIsSimple(b.MemberType);

			if ((simpleA && simpleB) || (!simpleA && !simpleB))
				return string.Compare(a.Name, b.Name);
			else if (simpleA)
				return -1;
			else
				return 1;
		}

		static bool TypeIsSimple(Type type)
		{
			var typeConverter = GetConverter(type);

			return type.IsEnum ||
				IsBasicType(type) ||
				typeConverter != null;
		}

		static MethodInfo[] GetSerializationMethods(Type type, Type attributeType)
		{
			var members = GetMembers(type);

			var methods = members.OfType<MethodInfo>().Where(mi => mi.GetCustomAttributes(attributeType, false).Length > 0);

			return methods.ToArray();
		}

		static IEnumerable<MemberInfo> GetMembers(Type type)
		{
			var members = type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

			if (type.BaseType == null)
			{
				return members;
			}
			else
			{
				var baseFields = GetMembers(type.BaseType);
				return baseFields.Concat(members);
			}
		}

		static bool IsBasicType(Type type)
		{
			var code = Type.GetTypeCode(type);

			switch (code)
			{
				case TypeCode.Boolean:

				case TypeCode.Byte:
				case TypeCode.SByte:

				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:

				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:

				case TypeCode.Single:
				case TypeCode.Double:

				case TypeCode.Char:
				case TypeCode.String:
					return true;

				default:
					return false;
			}
		}

		static TypeConverter GetConverter(Type type)
		{
			if (typeof(IDictionary).IsAssignableFrom(type))
				return null;

			if (typeof(IList).IsAssignableFrom(type))
				return null;

			var typeConverter = TypeDescriptor.GetConverter(type);
			if (typeConverter != null &&
				typeConverter.GetType() != typeof(TypeConverter) &&
				typeConverter.CanConvertTo(typeof(string)))
				return typeConverter;

			return null;
		}

		public override string ToString()
		{
			return String.Format("TypeInfo({0})", this.Type.Name);
		}
	}
}
