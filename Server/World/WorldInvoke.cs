using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class World
	{
		public void BeginInvoke(Delegate callback, params object[] args)
		{
			m_preTickInvokeList.BeginInvoke(callback, args);
			//SignalWorld(); // XXX
		}

		public void BeginInvokeInstant(Delegate callback, params object[] args)
		{
			m_instantInvokeList.BeginInvoke(callback, args);
			//SignalWorld(); // XXX
		}

		class InvokeList
		{
			class InvokeInfo
			{
				public Delegate Action;
				public object[] Args;
			}

			List<InvokeInfo> m_invokeList = new List<InvokeInfo>();

			World m_world;

			public InvokeList(World world)
			{
				m_world = world;
			}

			// thread safe
			public void BeginInvoke(Delegate callback, params object[] args)
			{
				lock (m_invokeList)
					m_invokeList.Add(new InvokeInfo() { Action = callback, Args = args });

				//m_world.SignalWorld(); // XXX
			}

			public bool HasInvokeWork
			{
				get
				{
					lock (m_invokeList)
						return m_invokeList.Count > 0;
				}
			}

			public void ProcessInvokeList()
			{
				m_world.VerifyAccess();

				lock (m_invokeList)
				{
					if (m_invokeList.Count > 0)
						Debug.Print("Processing {0} invoke callbacks", m_invokeList.Count);
					foreach (InvokeInfo a in m_invokeList)
						a.Action.DynamicInvoke(a.Args); // XXX DynamicInvoke
					m_invokeList.Clear();
				}
			}
		}
	}
}
