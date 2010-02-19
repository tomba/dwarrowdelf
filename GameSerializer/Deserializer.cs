using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace GameSerializer
{
	public partial class Serializer
	{
		void GenerateDynamicDeserializerStub(Type type)
		{
			if (!m_map.ContainsKey(type))
				throw new Exception();

			var dm = new DynamicMethod("Deserialize", null,
				new Type[] { typeof(Stream), type.MakeByRefType() },
				typeof(Serializer), true);
			dm.DefineParameter(2, ParameterAttributes.Out, "value");

			m_map[type].ReaderMethodInfo = dm;
			m_map[type].ReaderILGen = dm.GetILGenerator();
		}

		void GenerateStaticDeserializerStub(Type type, TypeBuilder tb)
		{
			if (!m_map.ContainsKey(type))
				throw new Exception("asd");

			MethodBuilder mb = tb.DefineMethod("Deserialize",
				MethodAttributes.Public | MethodAttributes.Static,
				null,
				new Type[] { typeof(Stream), type.MakeByRefType() });
			mb.DefineParameter(2, ParameterAttributes.Out, "value");

			m_map[type].ReaderMethodInfo = mb;
			m_map[type].ReaderILGen = mb.GetILGenerator();
		}


		void GenerateDeserializerBody(Type type)
		{
			// arg0: stream, arg1: out value

			var dm = GetReaderMethodInfo(type);
			var il = GetReaderILGen(type);

			D(il, "deser {0}", type.Name);

			if (type.IsArray)
			{
				var elemType = type.GetElementType();

				var lenLocal = il.DeclareLocal(typeof(uint));

				// read array len
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca, lenLocal);
				il.EmitCall(OpCodes.Call, GetReaderMethodInfo(typeof(uint)), null);

				var arrLocal = il.DeclareLocal(type);

				// create new array
				il.Emit(OpCodes.Ldloc, lenLocal);
				il.Emit(OpCodes.Newarr, elemType);
				il.Emit(OpCodes.Stloc, arrLocal);

				// declare i
				var idxLocal = il.DeclareLocal(typeof(int));

				// i = 0
				il.Emit(OpCodes.Ldnull);
				il.Emit(OpCodes.Stloc, idxLocal);

				var loopBodyLabel = il.DefineLabel();
				var loopCheckLabel = il.DefineLabel();

				il.Emit(OpCodes.Br, loopCheckLabel);

				// loop body
				il.MarkLabel(loopBodyLabel);

				// read element to arr[i]
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloc, arrLocal);
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldelema, elemType);
				if (elemType.IsValueType)
					il.EmitCall(OpCodes.Call, GetReaderMethodInfo(elemType), null);
				else
					il.EmitCall(OpCodes.Call, m_deserializerSwitchMethodInfo, null);

				// i = i + 1
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Add);
				il.Emit(OpCodes.Stloc, idxLocal);

				il.MarkLabel(loopCheckLabel);

				// loop condition
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldloc, arrLocal);
				il.Emit(OpCodes.Ldlen);
				il.Emit(OpCodes.Conv_I4);
				il.Emit(OpCodes.Clt);
				il.Emit(OpCodes.Brtrue, loopBodyLabel);


				// store new array to the out value
				il.Emit(OpCodes.Ldarg, 1);
				il.Emit(OpCodes.Ldloc, arrLocal);
				il.Emit(OpCodes.Stind_Ref);
			}
			else
			{
				if (type.IsClass)
				{
					// instantiate empty class
					il.Emit(OpCodes.Ldarg_1);

					var gtfh = typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static);
					var guo = typeof(System.Runtime.Serialization.FormatterServices).GetMethod("GetUninitializedObject", BindingFlags.Public | BindingFlags.Static);
					il.Emit(OpCodes.Ldtoken, type);
					il.Emit(OpCodes.Call, gtfh);
					il.Emit(OpCodes.Call, guo);
					il.Emit(OpCodes.Castclass, type);

					il.Emit(OpCodes.Stind_Ref);
				}

				var fields = GetFieldInfos(type);

				foreach (var field in fields)
				{
					il.Emit(OpCodes.Ldarg_0);
					il.Emit(OpCodes.Ldarg, 1);
					if (type.IsClass)
						il.Emit(OpCodes.Ldind_Ref);
					il.Emit(OpCodes.Ldflda, field);

					if (field.FieldType.IsValueType)
						il.EmitCall(OpCodes.Call, GetReaderMethodInfo(field.FieldType), null);
					else
						il.EmitCall(OpCodes.Call, m_deserializerSwitchMethodInfo, null);
				}
			}

			D(il, "deser done");
			il.Emit(OpCodes.Ret);
		}



		void GenerateDeserializerSwitch(ILGenerator il)
		{
			// arg0: stream, arg1: out object

			D(il, "deser switch");

			var idLocal = il.DeclareLocal(typeof(ushort));

			// read typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloca, idLocal);
			il.EmitCall(OpCodes.Call, GetReaderMethodInfo(typeof(ushort)), null);

			var notNullLabel = il.DefineLabel();

			// if typeID == 0xffff
			il.Emit(OpCodes.Ldloc, idLocal);
			il.Emit(OpCodes.Ldc_I4, 0xffff);
			il.Emit(OpCodes.Conv_I4);
			il.Emit(OpCodes.Bne_Un, notNullLabel);

			// write null to out object
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Ldnull);
			il.Emit(OpCodes.Stind_Ref);
			D(il, "deser done");
			il.Emit(OpCodes.Ret);

			// if typeID != 0xffff
			il.MarkLabel(notNullLabel);

			var jumpTable = new Label[m_map.Count];
			foreach (var kvp in m_map)
				jumpTable[kvp.Value.TypeID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc, idLocal);
			il.Emit(OpCodes.Switch, jumpTable);

			D(il, "eihx");
			il.ThrowException(typeof(Exception));

			foreach (var kvp in m_map)
			{
				var data = kvp.Value;

				il.MarkLabel(jumpTable[data.TypeID]);

				var local = il.DeclareLocal(kvp.Key);

				// call deserializer for this typeID
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldloca, local);
				il.EmitCall(OpCodes.Call, data.ReaderMethodInfo, null);

				// write result object to out object
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldloc, local);
				if (kvp.Key.IsValueType)
					il.Emit(OpCodes.Box, kvp.Key);
				il.Emit(OpCodes.Stind_Ref);

				D(il, "deser switch done");

				il.Emit(OpCodes.Ret);
			}
		}
	}
}
