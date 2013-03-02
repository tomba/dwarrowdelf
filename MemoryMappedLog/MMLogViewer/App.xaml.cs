//#define SPAM

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MemoryMappedLog
{
	public partial class App : Application
	{
		Thread m_spamThread;
		static volatile bool s_exit;

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

#if SPAM
			m_spamThread = new Thread(SpamMain);
			m_spamThread.Start();
#endif
		}

		protected override void OnExit(ExitEventArgs e)
		{
			s_exit = true;

			base.OnExit(e);
		}

		static void SpamMain()
		{
			int i = 0;

			while (s_exit == false)
			{
				MMLog.Append(123, "asd", "kala", "hdr", String.Format("qwe {0}", i++));
				System.Threading.Thread.Sleep(1000);
			}
		}
	}
}
