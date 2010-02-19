using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace GameSerializer
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

		static Dictionary<Type, TypeData> m_map = new Dictionary<Type, TypeData>();

		delegate void SerializerSwitch(Stream stream, object ob);
		MethodInfo m_serializerSwitchMethodInfo;
		SerializerSwitch m_serializerSwitch;

		delegate void DeserializerSwitch(Stream stream, out object ob);
		MethodInfo m_deserializerSwitchMethodInfo;
		DeserializerSwitch m_deserializerSwitch;

		bool m_dynamic = true;

		public Serializer(Type[] rootTypes)
		{
			if (m_dynamic)
				GenerateDynamic(rootTypes);
			else
				GenerateStatic(rootTypes);
		}

		public void Serialize(Stream stream, object data)
		{
			if (!m_map.ContainsKey(data.GetType()))
				throw new ArgumentException("Type is not known");

			var typeID = m_map[data.GetType()].TypeID;

			D("Serializing {0}", data.GetType().Name);

			m_serializerSwitch(stream, data);
		}

		public object Deserialize(Stream stream)
		{
			D("Deserializing");

			object o;
			m_deserializerSwitch(stream, out o);
			return o;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string fmt, params object[] args)
		{
			//Console.WriteLine("S: " + String.Format(fmt, args));
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void D(ILGenerator ilGen, string fmt, params object[] args)
		{
			//ilGen.EmitWriteLine("E: " + String.Format(fmt, args));
		}

		Type[] AddTypes(Type[] rootTypes)
		{
			// add types needed by the serializer
			var typeSet = new HashSet<Type>(new Type[] { typeof(ushort), typeof(uint) });

			foreach (var type in rootTypes)
				AddTypes(type, typeSet);

			var types = typeSet.ToArray();

			ushort typeID = 0;
			foreach (var type in types)
			{
				if (type.IsInterface)
					throw new NotSupportedException();

				if (!type.IsSerializable)
					throw new NotSupportedException();

				var writer = Primitives.GetWritePrimitive(type);
				var reader = Primitives.GetReadPrimitive(type.MakeByRefType());

				if ((writer != null) != (reader != null))
					throw new Exception();

				var isStatic = writer != null;

				if (isStatic)
					m_map[type] = new TypeData(typeID++, writer, reader);
				else
					m_map[type] = new TypeData(typeID++);
			}

			return types;
		}

		void AddTypes(Type type, HashSet<Type> typeSet)
		{
			if (typeSet.Contains(type))
				return;

			typeSet.Add(type);

			if (type.IsArray)
			{
				AddTypes(type.GetElementType(), typeSet);
			}
			else
			{
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				foreach (var field in fields)
					AddTypes(field.FieldType, typeSet);
			}
		}

		void GenerateStatic(Type[] rootTypes)
		{
			AssemblyName aName = null;
			AssemblyBuilder ab = null;
			TypeBuilder tb = null;

			aName = new AssemblyName("Serializer");
			ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
			var modb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
			tb = modb.DefineType("Serializer", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

			AddTypes(rootTypes);

			var types = m_map.Where(kvp => kvp.Value.IsStatic == false).Select(kvp => kvp.Key);

			foreach (var type in types)
				GenerateStaticSerializerStub(type, tb);

			foreach (var type in types)
				GenerateStaticDeserializerStub(type, tb);

			foreach (var type in types)
				GenerateSerializerBody(type);

			foreach (var type in types)
				GenerateDeserializerBody(type);


			MethodBuilder mb = tb.DefineMethod("SerializerSwitch",
				MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object) });
			var ilGen = mb.GetILGenerator();
			GenerateSerializerSwitch(ilGen);

			mb = tb.DefineMethod("DeserializerSwitch",
				MethodAttributes.Public | MethodAttributes.Static, null, new Type[] { typeof(Stream), typeof(object).MakeByRefType() });
			ilGen = mb.GetILGenerator();
			GenerateDeserializerSwitch(ilGen);

			var t = tb.CreateType();
			ab.Save(aName + ".dll");

			var m1 = t.GetMethod("SerializerSwitch");
			m_serializerSwitchMethodInfo = m1;
			m_serializerSwitch = (SerializerSwitch)Delegate.CreateDelegate(typeof(SerializerSwitch), m1);

			var m2 = t.GetMethod("DeserializerSwitch");
			m_deserializerSwitchMethodInfo = m2;
			m_deserializerSwitch = (DeserializerSwitch)Delegate.CreateDelegate(typeof(DeserializerSwitch), m2);
		}


		void GenerateDynamic(Type[] rootTypes)
		{
			AddTypes(rootTypes);

			var types = m_map.Where(kvp => kvp.Value.IsStatic == false).Select(kvp => kvp.Key);

			/* generate stubs */
			foreach (var type in types)
				GenerateDynamicSerializerStub(type);

			foreach (var type in types)
				GenerateDynamicDeserializerStub(type);

			var serializerSwitchMethod = new DynamicMethod("SerializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object) },
				typeof(Serializer), true);
			m_serializerSwitchMethodInfo = serializerSwitchMethod;

			var deserializerSwitchMethod = new DynamicMethod("DeserializerSwitch", null,
				new Type[] { typeof(Stream), typeof(object).MakeByRefType() },
				typeof(Serializer), true);
			m_deserializerSwitchMethodInfo = deserializerSwitchMethod;


			/* generate bodies */
			foreach (var type in types)
				GenerateSerializerBody(type);

			foreach (var type in types)
				GenerateDeserializerBody(type);

			var ilGen = serializerSwitchMethod.GetILGenerator();
			GenerateSerializerSwitch(ilGen);
			m_serializerSwitch = (SerializerSwitch)serializerSwitchMethod.CreateDelegate(typeof(SerializerSwitch));

			ilGen = deserializerSwitchMethod.GetILGenerator();
			GenerateDeserializerSwitch(ilGen);
			m_deserializerSwitch = (DeserializerSwitch)deserializerSwitchMethod.CreateDelegate(typeof(DeserializerSwitch));
		}

		static ushort GetTypeID(Type type)
		{
			return m_map[type].TypeID;
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

		MethodInfo GetWriterMethodInfo(Type type)
		{
			if (!m_map.ContainsKey(type))
				throw new Exception(String.Format("Unknown type {0}", type));

			return m_map[type].WriterMethodInfo;
		}

		ILGenerator GetWriterILGen(Type type)
		{
			return m_map[type].WriterILGen;
		}

		MethodInfo GetReaderMethodInfo(Type type)
		{
			return m_map[type].ReaderMethodInfo;
		}

		ILGenerator GetReaderILGen(Type type)
		{
			return m_map[type].ReaderILGen;
		}
	}
}
