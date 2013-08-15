using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Pipes;

namespace Dwarrowdelf
{
	public static class PipeConnectionListener
	{
		static Action<PipeConnection> s_callback;

		public static void StartListening(Action<PipeConnection> callback)
		{
			s_callback = callback;

			NewAccept();
		}

		public static void StopListening()
		{
		}

		static void NewAccept()
		{
			var stream = new NamedPipeServerStream("Dwarrowdelf.Pipe", PipeDirection.InOut, 4, PipeTransmissionMode.Byte,
				PipeOptions.Asynchronous);

			stream.BeginWaitForConnection(AcceptCallback, stream);
		}

		static void AcceptCallback(IAsyncResult ar)
		{
			var stream = (NamedPipeServerStream)ar.AsyncState;

			stream.EndWaitForConnection(ar);

			var conn = new PipeConnection(stream);

			s_callback(conn);

			NewAccept();
		}
	}
}
