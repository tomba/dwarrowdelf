using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame.Server
{
	public partial class World
	{
		class InvokeInfo
		{
			public Delegate Action;
			public object[] Args;
		}

		List<InvokeInfo> m_preTickInvokeList = new List<InvokeInfo>();
		List<InvokeInfo> m_instantInvokeList = new List<InvokeInfo>();

		// thread safe
		public void BeginInvoke(Action<object> callback)
		{
			BeginInvoke(callback, null);
		}

		// thread safe
		public void BeginInvoke(Delegate callback, params object[] args)
		{
			lock (m_preTickInvokeList)
				m_preTickInvokeList.Add(new InvokeInfo() { Action = callback, Args = args });

			SignalWorld();
		}

		bool HasPreTickInvokeWork
		{
			get
			{
				lock (m_preTickInvokeList)
					return m_preTickInvokeList.Count > 0;
			}
		}

		void ProcessInvokeList()
		{
			VerifyAccess();

			lock (m_preTickInvokeList)
			{
				if (m_preTickInvokeList.Count > 0)
					MyDebug.WriteLine("Processing {0} invoke callbacks", m_preTickInvokeList.Count);
				foreach (InvokeInfo a in m_preTickInvokeList)
					a.Action.DynamicInvoke(a.Args); // XXX dynamicinvoke
				m_preTickInvokeList.Clear();
			}
		}


		// thread safe
		public void BeginInvokeInstant(Action<object> callback)
		{
			BeginInvokeInstant(callback, null);
		}

		// thread safe
		public void BeginInvokeInstant(Delegate callback, params object[] args)
		{
			lock (m_instantInvokeList)
				m_instantInvokeList.Add(new InvokeInfo() { Action = callback, Args = args });

			SignalWorld();
		}

		bool HasInstantInvokeWork
		{
			get
			{
				lock (m_instantInvokeList)
					return m_instantInvokeList.Count > 0;
			}
		}

		void ProcessInstantInvokeList()
		{
			VerifyAccess();

			lock (m_instantInvokeList)
			{
				if (m_instantInvokeList.Count > 0)
					MyDebug.WriteLine("Processing {0} instant invoke callbacks", m_instantInvokeList.Count);
				foreach (InvokeInfo a in m_instantInvokeList)
					a.Action.DynamicInvoke(a.Args); // XXX dynamicinvoke
				m_instantInvokeList.Clear();
			}
		}
	}
}
