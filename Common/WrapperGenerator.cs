using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection.Emit;
using System.Reflection;

namespace MyGame
{
	public static class WrapperGenerator
	{
		/// <summary>
		/// Creates a wrapper delegate, that converts argument from T to argType,
		/// and calls a method that takes argType.
		/// </summary>
		public static Action<T> CreateHandlerWrapper<T>(string methodName, Type argType, object bindOb)
		{
			MethodInfo dummy;
			return CreateHandlerWrapper<T>(methodName, argType, bindOb, out dummy);
		}

		public static Action<T> CreateHandlerWrapper<T>(string methodName, Type argType, object bindOb,
			out MethodInfo method)
		{
			Type bindType = bindOb.GetType();
			method = bindType.GetMethod(methodName,
				BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding, null,
				new Type[] { argType }, null);

			if (method == null)
				return null;

			DynamicMethod dm = new DynamicMethod(methodName + "Wrapper", null,
				new Type[] { bindType, typeof(T) },
				bindType, true);
			var gen = dm.GetILGenerator();

			gen.Emit(OpCodes.Ldarg_0);
			gen.Emit(OpCodes.Ldarg_1);
			gen.Emit(OpCodes.Castclass, argType);
			gen.Emit(OpCodes.Call, method);
			gen.Emit(OpCodes.Ret);

			var del = dm.CreateDelegate(typeof(Action<T>), bindOb);

			return (Action<T>)del;
		}
	}
}
