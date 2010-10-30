using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Dwarrowdelf
{
	public interface IServer
	{
		void RunServer(bool isEmbedded, EventWaitHandle serverStartWaitHandle,
			EventWaitHandle serverStopWaitHandle);
	}
}
