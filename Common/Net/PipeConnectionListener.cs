using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;

namespace Dwarrowdelf
{
	public static class PipeConnectionListener
	{
		static NamedPipeServerStream s_stream;
		static Action<PipeConnection> s_callback;

		public static void StartListening(Action<PipeConnection> callback)
		{
			s_callback = callback;

			s_stream = new NamedPipeServerStream("Dwarrowdelf.Pipe", PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

			s_stream.BeginWaitForConnection(AcceptCallback, null);
		}

		public static void StopListening()
		{
		}

		static void AcceptCallback(IAsyncResult ar)
		{
			s_stream.EndWaitForConnection(ar);

			var conn = new PipeConnection(s_stream);

			s_callback(conn);
		}
	}
}
