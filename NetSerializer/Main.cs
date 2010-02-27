using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace NetSerializer
{
	public partial class Serializer
	{
		class TypeData
		{
			public TypeData(ushort typeID)
			{
				this.TypeID = typeID;
				this.IsStatic = false;
			}

			public TypeData(ushort typeID, MethodInfo writer, MethodInfo reader)
			{
				this.TypeID = typeID;
				this.WriterMethodInfo = writer;
				this.ReaderMethodInfo = reader;
				this.IsStatic = true;
			}

			public readonly bool IsStatic;
			public ushort TypeID;
			public MethodInfo WriterMethodInfo;
			public ILGenerator WriterILGen;
			public MethodInfo ReaderMethodInfo;
			public ILGenerator ReaderILGen;
		}

		static Dictionary<Type, TypeData> s_map;

		delegate void SerializerSwitch(Stream stream, object ob);
		static MethodInfo s_serializerSwitchMethodInfo;
		static SerializerSwitch s_serializerSwitch;

		delegate void DeserializerSwitch(Stream stream, out object ob);
		static MethodInfo s_deserializerSwitchMethodInfo;
		static DeserializerSwitch s_deserializerSwitch;

		public static void Initialize(Type[] rootTypes)
		{
			if (s_map != null)
				throw new Exception();

			s_map = new Dictionary<Type, TypeData>();
			GenerateDynamic(rootTypes);
		}

		public static void Serialize(Stream stream, object data)
		{
			if (!s_map.ContainsKey(data.GetType()))
				throw new ArgumentException("Type is not known");

			var typeID = s_map[data.GetType()].TypeID;

			D("Serializing {0}", data.GetType().Name);

			s_serializerSwitch(stream, data);
		}

		public static object Deserialize(Stream stream)
		{
			D("Deserializing");

			object o;
			s_deserializerSwitch(stream, out o);
			return o;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void D(string fmt, params object[] args)
		{
			//Console.WriteLine("S: " + String.Format(fmt, args));
		}

		[System.Diagnostics.Conditional("DEBUG")]
		static void D(ILGenerator ilGen, string fmt, params object[] args)
		{
			//ilGen.EmitWriteLine("E: " + String.Format(fmt, args));
		}

		static void AddTypes(Type[] rootTypes)
		{
			var primitives = new Type[] { typeof(bool), typeof(byte), typeof(char),
				typeof(ushort), typeof(short), typeof(uint), typeof(int), typeof(string) };

			var typeSet = new HashSet<Type>(primitives);

			foreach (var type in rootTypes)
				CollectTypes(type, typeSet);

			ushort typeID = 0;
			foreach (var type in typeSet)
			{
				if (type.IsInterface)
					throw new NotSupportedException("Interfaces not supported");

				if (!type.IsSerializable)
					throw new NotSupportedException(String.Format("Type {0} is not marked as Serializable", type.ToString()));

				var writer = Primitives.GetWritePrimitive(type);
				var reader = Primitives.GetReadPrimitive(type.MakeByRefType());

				if ((writer != null) != (reader != null))
					throw new Exception();

				var isStatic = writer != null;

				if (isStatic)
					s_map[type] = new TypeData(typeID++, writer, reader);
				else
					s_map[type] = new TypeData(typeID++);
			}
		}

		static void CollectTypes(Type type, HashSet<Type> typeSet)
		{
			if (typeSet.Contains(type))
				return;

			typeSet.Add(type);

			if (type.IsArray)
			{
				CollectTypes(type.GetElementType(), typeSet);
			}
			else
			{
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				foreach (var field in fields)
					CollectTypes(field.FieldType, typeSet);
			}
		}

		static void GenerateDynamic(Type[] rootTypes)
		{
			AddTypes(rootTypes);

			var types = s_map.Where(kvp => kvp.Value.IsStatic == false).Select(kvp => kvp.Key);

			/* generate stubs */
			foreach (var type in types)
			{
				var dm = GenerateDynamicSerializerStub(type);
				s_map[type].WriterMethodInfo = dm;
				s_map[type].WriterILGen = dm.GetILGenerator();
			}

			foreach (var type in types)
			{
				var dm = GenerateDynamicDeserializerStub(type);
				s_map[type].ReaderMethodInfo = dm;
				s_map[type].ReaderILGen = dm.GetILGenerator();
			}

			var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object) },
				typeof(Serializer), true);
			s_serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType() },
				typeof(Serializer), true);
			s_deserializerSwitchMethodInfo = deserializerSwitchMethod;


			/* generate bodies */
			foreach (var type in types)
				GenerateSerializerBody(type, s_map[type].WriterILGen);

			foreach (var type in types)
				GenerateDeserializerBody(type, s_map[type].ReaderILGen);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			GenerateSerializerSwitch(ilGen, s_map);
			s_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

			ilGen = deserializerSwitchMethod.GetILGenerator();
			GenerateDeserializerSwitch(ilGen, s_map);
			s_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));
		}

		static ushort GetTypeID(Type type)
		{
			return s_map[type].TypeID;
		}

		static ushort GetTypeID(object ob)
		{
			return GetTypeID(ob.GetType());
		}

		static FieldInfo[] GetFieldInfos(Type type)
		{
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

			if (type.BaseType == null)
			{
				return fields;
			}
			else
			{
				var baseFields = GetFieldInfos(type.BaseType);
				return baseFields.Concat(fields).ToArray();
			}
		}

		static MethodInfo GetWriterMethodInfo(Type type)
		{
			if (!s_map.ContainsKey(type))
				throw new Exception(String.Format("Unknown type {0}", type));

			return s_map[type].WriterMethodInfo;
		}

		static ILGenerator GetWriterILGen(Type type)
		{
			return s_map[type].WriterILGen;
		}

		static MethodInfo GetReaderMethodInfo(Type type)
		{
			return s_map[type].ReaderMethodInfo;
		}

		static ILGenerator GetReaderILGen(Type type)
		{
			return s_map[type].ReaderILGen;
		}
	}
}
