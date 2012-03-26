using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public static class DirectConnectionListener
	{
		static Action<DirectConnection> s_callback;

		public static void StartListening(Action<DirectConnection> callback)
		{
			s_callback = callback;
		}

		public static void StopListening()
		{
			s_callback = null;
		}

		public static void NewConnection(DirectConnection clientConnection)
		{
			var connection = new DirectConnection(clientConnection);
			s_callback.BeginInvoke(connection, null, null);
		}
	}
}
