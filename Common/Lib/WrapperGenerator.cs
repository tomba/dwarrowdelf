using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace Dwarrowdelf
{
	public static class WrapperGenerator
	{
		/// <summary>
		/// Creates a wrapper delegate, that converts argument from T1 to argType,
		/// and calls a method that takes argType.
		/// </summary>
		public static Action<TOb, T1> CreateActionWrapper<TOb, T1>(string methodName, Type argType)
		{
			var bindType = typeof(TOb);

			var method = bindType.GetMethod(methodName,
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding, null,
					new Type[] { argType }, null);

			if (method == null)
				return null;

			DynamicMethod dm = new DynamicMethod(methodName + "Wrapper", null,
				new Type[] { bindType, typeof(T1) },
				bindType, true);
			var gen = dm.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, argType);
			gen.Emit(OpCodes.Call, method);
			gen.Emit(OpCodes.Ret);

			var del = dm.CreateDelegate(typeof(Action<TOb, T1>));

			return (Action<TOb, T1>)del;
		}

		/// <summary>
		/// Creates a wrapper delegate, that converts argument from T1 to argType,
		/// and calls a method that takes argType.
		/// </summary>
		public static Func<TOb, T1, TResult> CreateFuncWrapper<TOb, T1, TResult>(string methodName, Type argType)
		{
			var bindType = typeof(TOb);

			MethodInfo method = null;

			while (bindType.BaseType != null)
			{
				method = bindType.GetMethod(methodName,
					BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding, null,
					new Type[] { argType }, null);

				if (method != null)
					break;

				bindType = bindType.BaseType;
			}

			if (method == null)
				return null;

			DynamicMethod dm = new DynamicMethod(methodName + "Wrapper", typeof(TResult),
				new Type[] { bindType, typeof(T1) },
				bindType, true);
			var gen = dm.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, argType);
			gen.Emit(OpCodes.Call, method);
			gen.Emit(OpCodes.Ret);

			var del = dm.CreateDelegate(typeof(Func<TOb, T1, TResult>));

			return (Func<TOb, T1, TResult>)del;
		}
	}
}
