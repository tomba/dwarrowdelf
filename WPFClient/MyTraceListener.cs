using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Threading;


namespace MyGame
{
	class ClientDebugListener : MyDebugListener
	{
		List<DebugEntry> m_entryList = new List<DebugEntry>();

		void AppendText()
		{
			List<DebugEntry> list;

			lock (m_entryList)
			{
				list = m_entryList;
				m_entryList = new List<DebugEntry>();
			}

			App.DebugWindow.AddRange(list);
		}

		public override void Write(DebugFlag flags, string msg)
		{
			bool call;

			lock (m_entryList)
			{
				call = m_entryList.Count == 0;
				m_entryList.Add(new DebugEntry(msg, flags));
			}

			if (call)
				App.DebugWindow.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(AppendText));

		}
	}
}
