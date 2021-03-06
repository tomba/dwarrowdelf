﻿using System;
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
	/*
	 * Fails to serialize:
	 * 
	 * class foo { ILuokka[] m_array; }
	 * 
	 * new foo() { m_array = new Luokka[] };
	 * 
	 * I.e. if the array/list object is different type than the field type
	 * 
	 */

	public sealed class SaveGameSerializer : IDisposable
	{
		sealed class SerializerRefResolver
		{
			Dictionary<object, int> m_refMap = new Dictionary<object, int>();
			int m_refNum;

			public int Create(object ob)
			{
				if (m_refMap.ContainsKey(ob))
					throw new Exception();

				int id = m_refNum++;
				m_refMap[ob] = id;
				return id;
			}

			public int Get(object ob)
			{
				int id;
				if (m_refMap.TryGetValue(ob, out id) == false)
					return -1;
				return id;
			}
		}

		JsonTextWriter m_writer;
		SerializerRefResolver m_referenceResolver;
		SaveGameConverterCache m_globalConverters;
		SaveGameRefResolverCache m_globalResolvers;

		public SaveGameSerializer(Stream stream)
		{
			m_writer = new JsonTextWriter(new StreamWriter(stream));
			m_writer.Formatting = Formatting.Indented;
			m_referenceResolver = new SerializerRefResolver();
		}

		public SaveGameSerializer(Stream stream, IEnumerable<ISaveGameConverter> globalConverters)
			: this(stream)
		{
			m_globalConverters = new SaveGameConverterCache(globalConverters);
		}

		public SaveGameSerializer(Stream stream, IEnumerable<ISaveGameRefResolver> globalResolvers)
			: this(stream)
		{
			m_globalResolvers = new SaveGameRefResolverCache(globalResolvers);
		}


		#region IDisposable Members

		bool m_disposed;

		~SaveGameSerializer()
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
				m_writer.Close();
				m_writer = null;
			}
			//TODO: Unmanaged cleanup code here

			m_disposed = true;
		}

		#endregion IDisposable Members

		public void Serialize<T>(T ob)
		{
			SerializeObject(ob, typeof(T));

			m_writer.Flush();
		}

		void SerializeObject(object ob, Type containerType)
		{
			if (ob == null)
			{
				m_writer.WriteNull();
				return;
			}

			var type = ob.GetType();

			if (m_globalConverters != null)
			{
				var globalConverter = m_globalConverters.GetGlobalConverter(type);
				if (globalConverter != null)
				{
					ob = globalConverter.ConvertToSerializable(ob);
					SerializeObject(ob, containerType);
					return;
				}
			}

			bool writeType = !type.IsValueType && type != containerType;

			var typeInfo = TypeInfo.GetTypeInfo(type);

			switch (typeInfo.TypeClass)
			{
				case TypeClass.Undefined:
					throw new Exception();

				case TypeClass.Basic:
					m_writer.WriteValue(ob);
					break;

				case TypeClass.Enum:
					if (writeType)
						throw new Exception();

					var value = typeInfo.TypeConverter.ConvertToInvariantString(ob);
					m_writer.WriteValue(value);

					break;

				case TypeClass.Convertable:
					if (writeType)
						throw new Exception();

					WriteConvertable(ob, typeInfo);
					break;

				case TypeClass.Array:
				case TypeClass.List:
				case TypeClass.GenericList:
					if (writeType)
						throw new Exception();

					WriteIEnumerable(ob, typeInfo);
					break;

				case TypeClass.GenericDictionary:
					WriteGenericDictionary(ob, typeInfo, writeType);
					break;

				case TypeClass.Dictionary:
					throw new NotImplementedException();

				case TypeClass.GameObject:
					WriteGameObject(ob, typeInfo, writeType);
					break;

				case TypeClass.Serializable:
					WriteSerializable(ob, typeInfo, writeType);
					break;

				default:
					throw new NotImplementedException();
			}
		}

		void WriteConvertable(object ob, TypeInfo typeInfo)
		{
			var str = typeInfo.TypeConverter.ConvertToInvariantString(ob);
			m_writer.WriteValue(str);
		}

		void WriteGameObject(object ob, TypeInfo typeInfo, bool writeType)
		{
			m_writer.WriteStartObject();

			m_writer.WriteWhitespace(" ");
			m_writer.WriteComment(ob.ToString());

			bool canRef = typeInfo.UseRef;
			int id = canRef == false ? -1 : m_referenceResolver.Get(ob);

			if (canRef && id != -1)
			{
				m_writer.WritePropertyName("$ref");
				m_writer.WriteValue(id);
			}
			else
			{
				if (canRef)
				{
					id = m_referenceResolver.Create(ob);
					m_writer.WritePropertyName("$id");
					m_writer.WriteValue(id);
				}

				if (writeType)
				{
					m_writer.WritePropertyName("$type");
					// XXX fully qualified name is rather long...
					m_writer.WriteValue(typeInfo.Type.AssemblyQualifiedName);
				}

				WriteGameObjectData(ob, typeInfo);
			}

			m_writer.WriteEndObject();
		}

		void WriteGameObjectData(object ob, TypeInfo typeInfo)
		{
			var type = typeInfo.Type;

			if (m_globalResolvers != null)
			{
				var globalResolver = m_globalResolvers.GetGlobalResolver(type);
				if (globalResolver != null)
				{
					var id = globalResolver.ToRefID(ob);
					m_writer.WritePropertyName("$sid");
					m_writer.WriteValue(id);
				}
			}

			var serializingMethods = typeInfo.OnSerializingMethods;
			foreach (var method in serializingMethods)
				method.Invoke(ob, null);

			var entries = typeInfo.GameMemberEntries;

			for (int i = 0; i < entries.Length; ++i)
			{
				var entry = entries[i];
				var value = entry.GetValue(ob);

				m_writer.WritePropertyName(entry.Name);

				if (entry.ReaderWriter != null)
				{
					entry.ReaderWriter.Write(m_writer, value);
				}
				else
				{
					var memberType = entry.MemberType;

					if (entry.Converter != null)
						value = entry.Converter.ConvertToSerializable(value);

					SerializeObject(value, memberType);
				}
			}

			if (typeInfo.HasDelegate)
			{
				var data = ((ISaveGameDelegate)ob).GetSaveData();

				if (data != null)
				{
					m_writer.WritePropertyName("$del");
					SerializeObject(data, typeof(object));
				}
			}

			var serializedMethods = typeInfo.OnSerializedMethods;
			foreach (var method in serializedMethods)
				method.Invoke(ob, null);
		}

		void WriteSerializable(object ob, TypeInfo typeInfo, bool writeType)
		{
			m_writer.WriteStartObject();

			m_writer.WriteWhitespace(" ");
			m_writer.WriteComment(ob.ToString());

			if (writeType)
			{
				m_writer.WritePropertyName("$type");
				// XXX fully qualified name is rather long...
				m_writer.WriteValue(typeInfo.Type.AssemblyQualifiedName);
			}

			var members = typeInfo.SerializableMembers;

			var values = FormatterServices.GetObjectData(ob, members);

			for (int i = 0; i < members.Length; ++i)
			{
				var member = (FieldInfo)members[i];
				var value = values[i];

				m_writer.WritePropertyName(member.Name);
				SerializeObject(value, member.FieldType);
			}

			m_writer.WriteEndObject();
		}

		void WriteGenericDictionary(object ob, TypeInfo typeInfo, bool writeType)
		{
			m_writer.WriteStartObject();

			if (writeType)
			{
				m_writer.WritePropertyName("$type");
				m_writer.WriteValue(typeInfo.Type.FullName);
			}

			var dict = (IDictionary)ob;

			var keyType = typeInfo.ElementType1;
			var valueType = typeInfo.ElementType2;

			var enumerator = dict.GetEnumerator();

			var keyTypeInfo = TypeInfo.GetTypeInfo(keyType);
			if (keyTypeInfo.TypeConverter == null)
				throw new Exception();

			while (enumerator.MoveNext())
			{
				var key = enumerator.Key;
				var value = enumerator.Value;

				var keyStr = keyTypeInfo.TypeConverter.ConvertToInvariantString(key);

				m_writer.WritePropertyName(keyStr);
				SerializeObject(value, valueType);
			}

			m_writer.WriteEndObject();
		}

		void WriteIEnumerable(object ob, TypeInfo typeInfo)
		{
			m_writer.WriteStartArray();

			var ienum = (IEnumerable)ob;
			var elemType = typeInfo.ElementType1;

			foreach (var o in ienum)
				SerializeObject(o, elemType);

			m_writer.WriteEndArray();
		}
	}
}
