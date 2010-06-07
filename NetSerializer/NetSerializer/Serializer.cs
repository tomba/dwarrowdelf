using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetSerializer
{
	public partial class Serializer
	{
		static DynamicMethod GenerateDynamicSerializerStub(Type type)
		{
			var dm = new DynamicMethod("Serialize", null,
				new Type[] { typeof(Stream), type },
				typeof(Serializer), true);

			return dm;
		}

		static void GenerateSerializerBody(Type type, ILGenerator il)
		{
			// arg0: Stream, arg1: value

			D(il, "ser {0}", type.Name);

			if (type.IsArray)
			{
				var elemType = type.GetElementType();

				// write array len
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldlen);
				il.EmitCall(OpCodes.Call, GetWriterMethodInfo(typeof(uint)), null);

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

				// write element at index i
				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldelem, elemType);
				// all classes go to switch method. unambiguous classes could skip that
				if (elemType.IsValueType)
					il.EmitCall(OpCodes.Call, GetWriterMethodInfo(elemType), null);
				else
					il.EmitCall(OpCodes.Call, s_serializerSwitchMethodInfo, null);

				// i = i + 1
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldc_I4_1);
				il.Emit(OpCodes.Add);
				il.Emit(OpCodes.Stloc, idxLocal);

				il.MarkLabel(loopCheckLabel);

				// loop condition
				il.Emit(OpCodes.Ldloc, idxLocal);
				il.Emit(OpCodes.Ldarg_1);
				il.Emit(OpCodes.Ldlen);
				il.Emit(OpCodes.Conv_I4);
				il.Emit(OpCodes.Clt);
				il.Emit(OpCodes.Brtrue, loopBodyLabel);
			}
			else
			{
				var fields = GetFieldInfos(type);
				// Sort the fields so that they are in the same order, regardless how Type.GetFields works
				Array.Sort(fields, (a, b) => String.Compare(a.Name, b.Name));

				foreach (var field in fields)
				{
					// Note: the user defined value type is not passed as reference. could cause perf problems with big structs

					il.Emit(OpCodes.Ldarg_0);
					if (type.IsValueType)
						il.Emit(OpCodes.Ldarga, 1);
					else
						il.Emit(OpCodes.Ldarg, 1);
					il.Emit(OpCodes.Ldfld, field);

					// all classes go to switch method. unambiguous classes could skip that
					if (field.FieldType.IsValueType)
						il.EmitCall(OpCodes.Call, GetWriterMethodInfo(field.FieldType), null);
					else
						il.EmitCall(OpCodes.Call, s_serializerSwitchMethodInfo, null);
				}
			}

			il.Emit(OpCodes.Ret);
		}


		static void GenerateSerializerSwitch(ILGenerator il, IDictionary<Type, TypeData> map)
		{
			// arg0: Stream, arg1: object

			var idLocal = il.DeclareLocal(typeof(ushort));
			var notNullLabel = il.DefineLabel();

			// object == null?
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Brtrue, notNullLabel);

			// if the object is null, write typeID 0xffff
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldc_I4_M1);
			il.EmitCall(OpCodes.Call, GetWriterMethodInfo(typeof(ushort)), null);
			il.Emit(OpCodes.Ret);

			// object was not null
			il.MarkLabel(notNullLabel);

			// get TypeID from object's Type
			var getTypeIDMethod = typeof(Serializer).GetMethod("GetTypeID", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { typeof(object) }, null);
			il.Emit(OpCodes.Ldarg_1);
			il.EmitCall(OpCodes.Call, getTypeIDMethod, null);
			il.Emit(OpCodes.Stloc, idLocal);

			// write typeID
			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldloc, idLocal);
			il.EmitCall(OpCodes.Call, GetWriterMethodInfo(typeof(ushort)), null);

			var jumpTable = new Label[map.Count];
			foreach (var kvp in map)
				jumpTable[kvp.Value.TypeID] = il.DefineLabel();

			il.Emit(OpCodes.Ldloc, idLocal);
			il.Emit(OpCodes.Switch, jumpTable);

			D(il, "eihx");
			il.ThrowException(typeof(Exception));

			foreach (var kvp in map)
			{
				var data = kvp.Value;

				il.MarkLabel(jumpTable[data.TypeID]);

				il.Emit(OpCodes.Ldarg_0);
				il.Emit(OpCodes.Ldarg_1);
				if (kvp.Key.IsValueType)
					il.Emit(OpCodes.Unbox_Any, kvp.Key);
				else
					il.Emit(OpCodes.Castclass, kvp.Key);
				il.EmitCall(OpCodes.Call, data.WriterMethodInfo, null);

				il.Emit(OpCodes.Ret);
			}
		}
	}
}
