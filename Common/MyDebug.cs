using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.CompilerServices;


namespace MyGame
{
	[Flags]
	public enum DebugFlag
	{
		None,
		Mark,
		Client,
		Server,
		Net,
	}

	public static class MyDebug
	{
		public static DebugFlag DefaultFlags { get; set; }

		public static MyDebugListener Listener { get; set; }

		[Conditional("DEBUG")]
		public static void WriteLine(string str)
		{
			if (Listener != null)
				Listener.Write(DefaultFlags, str);
		}

		[Conditional("DEBUG")]
		public static void WriteLine(string format, params object[] args)
		{
			WriteLine(String.Format(format, args));
		}
	}

	public abstract class MyDebugListener : MarshalByRefObject
	{
		public override object InitializeLifetimeService()
		{
			var lease = (System.Runtime.Remoting.Lifetime.ILease)base.InitializeLifetimeService();

			if (lease.CurrentState == System.Runtime.Remoting.Lifetime.LeaseState.Initial)
				lease.InitialLeaseTime = TimeSpan.Zero;

			return lease;
		}

		public abstract void Write(DebugFlag flags, string msg);
	}

	public class DefaultDebugListener : MyDebugListener
	{
		public override void Write(DebugFlag flags, string msg)
		{
			Debug.WriteLine(msg, flags.ToString());
		}
	}

	public class ConsoleDebugListener : MyDebugListener
	{
		public override void Write(DebugFlag flags, string msg)
		{
			Console.WriteLine("{0}: {1}", flags, msg);
		}
	}

}
