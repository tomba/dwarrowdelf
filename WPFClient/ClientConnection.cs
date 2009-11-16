
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MyGame.ClientMsgs;
using System.Runtime.Serialization;
using System.IO;

namespace MyGame
{
	class ClientConnection : Connection
	{
		int m_transactionNumber;
		ClientCallback m_cc;

		public ClientConnection() : base()
		{
		}

		public void BeginConnect(Action callback)
		{
			Client.BeginConnect(IPAddress.Loopback, 9999, ConnectCallback, callback);
		}

		void ConnectCallback(IAsyncResult ar)
		{
			var cb = (Action)ar.AsyncState;
			Client.EndConnect(ar);

			m_cc = new ClientCallback();

			cb.Invoke();

			BeginRead();
		}


		public void EnqueueAction(GameAction action)
		{
			int tid = GetNewTransactionID();
			action.TransactionID = tid;
			Send(new EnqueueActionMessage() { Action = action });
		}

		public void LogOn(string name)
		{
			var msg = new LogOnMessage() { Name = name };
			Send(msg);
		}

		public void LogOff()
		{
		}

		protected override void HandleMessage(Message msg)
		{
			var app = System.Windows.Application.Current;
			app.Dispatcher.BeginInvoke(new Action<Message>(m_cc.DeliverMessage), msg);
		}

		public int GetNewTransactionID()
		{
			return System.Threading.Interlocked.Increment(ref m_transactionNumber);
		}

	}
}
