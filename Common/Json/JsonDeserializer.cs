using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Dwarrowdelf
{
	public interface ISaveGameDeserializerRefResolver
	{
		object Get(int id);
		void Add(int id, object ob);
	}

	public sealed class SaveGameDeserializer : IDisposable
	{
		sealed class DefaultDeserializerRefResolver : ISaveGameDeserializerRefResolver
		{
			Dictionary<int, object> m_refMap = new Dictionary<int, object>();

			public object Get(int id)
			{
				object ob;
				return m_refMap.TryGetValue(id, out ob) ? ob : null;
			}

			public void Add(int id, object ob)
			{
				if (m_refMap.ContainsKey(id))
					throw new Exception();

				m_refMap[id] = ob;
			}
		}

		JsonTextReader m_reader;
		ISaveGameDeserializerRefResolver ReferenceResolver;
		SaveGameConverterCache m_globalConverters;
		SaveGameRefResolverCache m_globalResolvers;

		List<PostDeserializationDelegate> m_postDeserMethods = new List<PostDeserializationDelegate>();
		delegate void PostDeserializationDelegate();

		public SaveGameDeserializer(Stream stream)
			: this(new StreamReader(stream))
		{
		}

		public SaveGameDeserializer(TextReader reader)
		{
			m_reader = new JsonTextReader(reader);
			this.ReferenceResolver = new DefaultDeserializerRefResolver();
		}

		public SaveGameDeserializer(TextReader reader, IEnumerable<ISaveGameConverter> globalConverters)
			: this(reader)
		{
			m_globalConverters = new SaveGameConverterCache(globalConverters);
		}


		public SaveGameDeserializer(TextReader reader, IEnumerable<ISaveGameRefResolver> globalRefResolers)
			: this(reader)
		{
			m_globalResolvers = new SaveGameRefResolverCache(globalRefResolers);
		}

		#region IDisposable Members

		bool m_disposed;

		~SaveGameDeserializer()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			this.Dispose(true);
			GC.SuppressFinalize(this);
		}

		void Dispose(bool disposing)
		{
			if (m_disposed)
				return;

			if (disposing)
			{
				//TODO: Managed cleanup code here, while managed refs still valid
				m_reader.Close();
				m_reader = null;
			}
			//TODO: Unmanaged cleanup code here

			m_disposed = true;
		}

		#endregion IDisposable Members

		public object Deserialize()
		{
			if (Read() == false)
				return null;

			var ob = DeserializeObject(null);

			CallPostDeser();

			return ob;
		}

		public object Deserialize(Type type)
		{
			if (Read() == false)
				return null;

			var ob = DeserializeObject(type);

			CallPostDeser();

			return ob;
		}

		public T Deserialize<T>()
		{
			if (Read() == false)
				return default(T);

			var ob = (T)DeserializeObject(typeof(T));

			CallPostDeser();

			return ob;
		}

		void CallPostDeser()
		{
			foreach (var dele in m_postDeserMethods)
				dele();

			m_postDeserMethods = null;
		}

		bool Read()
		{
			do
			{
				if (m_reader.Read() == false)
					throw new Exception();
			} while (m_reader.TokenType == JsonToken.Comment);

			return true;
		}

		object DeserializeObject(Type expectedType)
		{
			if (expectedType != null && m_globalConverters != null)
			{
				var conv = m_globalConverters.GetGlobalConverter(expectedType);

				if (conv != null)
				{
					var ob = _DeserializeObject(conv.OutputType);

					if (ob == null)
						return null;

					return conv.ConvertFromSerializable(ob);
				}
			}

			return _DeserializeObject(expectedType);
		}

		object _DeserializeObject(Type expectedType)
		{
			switch (m_reader.TokenType)
			{
				case JsonToken.Null:
					return null;

				case JsonToken.StartObject:
					return DeserializeFullObject(expectedType);

				case JsonToken.StartArray:
					return DeserializeArray(expectedType);

				case JsonToken.Boolean:
					return m_reader.Value;

				case JsonToken.Integer:
					return DeserializeInteger(expectedType);

				case JsonToken.Float:
					return DeserializeFloat(expectedType);

				case JsonToken.String:
					return DeserializeString(expectedType);

				default:
					throw new Exception();
			}
		}

		object DeserializeFullObject(Type expectedType)
		{
			Trace.Assert(m_reader.TokenType == JsonToken.StartObject);

			Read();

			object ob;

			if (m_reader.TokenType == JsonToken.PropertyName && (string)m_reader.Value == "$ref")
			{
				Read();

				Trace.Assert(m_reader.TokenType == JsonToken.Integer);

				var id = (int)(long)m_reader.Value;

				ob = this.ReferenceResolver.Get(id);

				if (ob == null)
					throw new Exception();

				Read();

				Trace.Assert(m_reader.TokenType == JsonToken.EndObject);
			}
			else
			{
				var id = ReadID();

				var type = ReadType() ?? expectedType;

				if (type == null)
					throw new SerializationException("No type for object");

				var typeInfo = TypeInfo.GetTypeInfo(type);

				switch (typeInfo.TypeClass)
				{
					case TypeClass.Dictionary:
					case TypeClass.GenericDictionary:
						ob = DeserializeDictionary(typeInfo);
						break;

					case TypeClass.GameObject:
						ob = DeserializeGameObject(typeInfo, id);
						break;

					case TypeClass.Serializable:
						ob = DeserializeSerializable(typeInfo, id);
						break;

					default:
						throw new Exception();
				}
			}

			return ob;
		}

		int ReadID()
		{
			if (m_reader.TokenType == JsonToken.PropertyName && (string)m_reader.Value == "$id")
			{
				Read();

				Trace.Assert(m_reader.TokenType == JsonToken.Integer);

				var id = (int)(long)m_reader.Value;

				Read();

				return id;
			}
			else
			{
				return -1;
			}
		}

		Type ReadType()
		{
			if (m_reader.TokenType == JsonToken.PropertyName && (string)m_reader.Value == "$type")
			{
				Read();

				Trace.Assert(m_reader.TokenType == JsonToken.String);

				string typeName = (string)m_reader.Value;

				var type = Type.GetType(typeName);
				if (type == null)
					throw new Exception("Unable to find type " + typeName);

				Read();

				return type;
			}
			else
			{
				return null;
			}
		}

		object DeserializeArray(Type expectedType)
		{
			Trace.Assert(m_reader.TokenType == JsonToken.StartArray);

			Read();

			var list = new List<object>();

			Type elementType = null;
			TypeInfo typeInfo = null;

			if (expectedType != null)
			{
				typeInfo = TypeInfo.GetTypeInfo(expectedType);
				elementType = typeInfo.ElementType1;
			}

			while (m_reader.TokenType != JsonToken.EndArray)
			{
				var ob = DeserializeObject(elementType);
				list.Add(ob);
				Read();
			}

			if (expectedType == null || expectedType == typeof(object))
				return list;

			switch (typeInfo.TypeClass)
			{
				case TypeClass.Array:
					{
						var arr = Array.CreateInstance(expectedType.GetElementType(), list.Count);
						Array.Copy(list.ToArray(), arr, list.Count);
						return arr;
					}

				case TypeClass.GenericList:
					{
						var ilist = (IList)Activator.CreateInstance(expectedType);
						foreach (var ob in list)
							ilist.Add(ob);
						return ilist;
					}

				default:
					throw new Exception();
			}
		}

		object DeserializeString(Type expectedType)
		{
			Trace.Assert(m_reader.TokenType == JsonToken.String);

			if (expectedType == null || expectedType == typeof(object))
				return m_reader.Value;

			string v = (string)m_reader.Value;

			if (expectedType == typeof(string))
				return v;

			var typeConverter = TypeDescriptor.GetConverter(expectedType);
			return typeConverter.ConvertFromInvariantString(v);
		}

		object DeserializeInteger(Type expectedType)
		{
			Trace.Assert(m_reader.TokenType == JsonToken.Integer);

			if (expectedType == null || expectedType == typeof(object))
				return m_reader.Value;

			long v = (long)m_reader.Value;

			var typeCode = Type.GetTypeCode(expectedType);
			switch (typeCode)
			{
				case TypeCode.Byte:
					return (byte)v;

				case TypeCode.Int16:
					return (short)v;

				case TypeCode.Int32:
					return (int)v;

				case TypeCode.Int64:
					return v;

				case TypeCode.UInt16:
					return (ushort)v;

				case TypeCode.UInt32:
					return (uint)v;

				case TypeCode.UInt64:
					return (ulong)v;

				default:
					throw new Exception();
			}
		}

		object DeserializeFloat(Type expectedType)
		{
			Trace.Assert(m_reader.TokenType == JsonToken.Float);

			if (expectedType == null || expectedType == typeof(object))
				return m_reader.Value;

			double v = (double)m_reader.Value;

			switch (Type.GetTypeCode(expectedType))
			{
				case TypeCode.Single:
					return (float)v;

				case TypeCode.Double:
					return v;

				default:
					throw new Exception();
			}
		}

		object DeserializeGameObject(TypeInfo typeInfo, int id)
		{
			var type = typeInfo.Type;

			object ob;
			bool created;

			if (m_reader.TokenType == JsonToken.PropertyName && (string)m_reader.Value == "$sid")
			{
				Read();

				Trace.Assert(m_reader.TokenType == JsonToken.Integer);

				var sid = (int)(long)m_reader.Value;

				Read();

				var resolver = m_globalResolvers.GetGlobalResolver(type);

				ob = resolver.FromRef(sid);

				created = false;
			}
			else
			{
				ob = FormatterServices.GetUninitializedObject(type);

				created = true;
			}

			var deserializingMethods = typeInfo.OnDeserializingMethods;
			foreach (var method in deserializingMethods)
				method.Invoke(ob, null);

			if (id != -1)
				this.ReferenceResolver.Add(id, ob);

			var entries = typeInfo.GameMemberEntries;
			var values = new object[entries.Length];

			while (true)
			{
				if (m_reader.TokenType == JsonToken.EndObject)
					break;

				Trace.Assert(m_reader.TokenType == JsonToken.PropertyName);

				string propName = (string)m_reader.Value;

				var idx = Array.FindIndex(entries, fi => fi.Name == propName);
				var entry = entries[idx];

				Read();

				object value;

				if (entry.ReaderWriter != null)
				{
					value = entry.ReaderWriter.Read(m_reader);
				}
				else
				{
					var memberType = entry.MemberType;

					value = DeserializeObject(memberType);

					if (entry.Converter != null)
						value = entry.Converter.ConvertFromSerializable(value);
				}

				values[idx] = value;

				Read();
			}

			typeInfo.PopulateObjectMembers(ob, values);

			if (created)
				typeInfo.DeserializeConstructor.Invoke(ob, new object[] { new SaveGameContext() });

			var deserializedMethods = typeInfo.OnDeserializedMethods;
			foreach (var method in deserializedMethods)
				method.Invoke(ob, null);

			var postDeserMethods = typeInfo.OnGamePostDeserializationMethods;
			foreach (var method in postDeserMethods)
			{
				var dele = (PostDeserializationDelegate)Delegate.CreateDelegate(typeof(PostDeserializationDelegate), ob, method);
				m_postDeserMethods.Add(dele);
			}

			return ob;
		}

		object DeserializeSerializable(TypeInfo typeInfo, int id)
		{
			var type = typeInfo.Type;

			var defConstructor = type.GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[0], null);

			object ob;

			if (defConstructor != null)
				ob = Activator.CreateInstance(type, true);
			else
				ob = FormatterServices.GetUninitializedObject(type);

			if (id != -1)
				this.ReferenceResolver.Add(id, ob);

			var members = typeInfo.SerializableMembers;

			var values = new object[members.Length];

			while (true)
			{
				if (m_reader.TokenType == JsonToken.EndObject)
					break;

				Trace.Assert(m_reader.TokenType == JsonToken.PropertyName);

				string fieldName = (string)m_reader.Value;

				var idx = Array.FindIndex(members, fi => fi.Name == fieldName);
				var field = (FieldInfo)members[idx];

				Read();

				object value = DeserializeObject(field.FieldType);

				values[idx] = value;

				Read();
			}

			FormatterServices.PopulateObjectMembers(ob, members, values);

			return ob;
		}

		object DeserializeDictionary(TypeInfo typeInfo)
		{
			var type = typeInfo.Type;

			var dict = (IDictionary)Activator.CreateInstance(type);

			var keyType = typeInfo.ElementType1;
			var valueType = typeInfo.ElementType2;

			var typeConverter = TypeDescriptor.GetConverter(keyType);
			if (typeConverter.CanConvertFrom(typeof(string)) == false)
				throw new Exception();

			while (true)
			{
				if (m_reader.TokenType == JsonToken.EndObject)
					break;

				Trace.Assert(m_reader.TokenType == JsonToken.PropertyName);

				object key = typeConverter.ConvertFromInvariantString((string)m_reader.Value);

				Read();

				object value = DeserializeObject(valueType);

				dict.Add(key, value);

				Read();
			}

			return dict;
		}

	}
}
