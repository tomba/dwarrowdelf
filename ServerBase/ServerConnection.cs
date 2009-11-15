using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using MyGame.ClientMsgs;

namespace MyGame
{
	public class ServerConnection : Connection
	{
		ServerService m_user;

		public ServerConnection(TcpClient client) : base(client)
		{
			m_user = new ServerService(this);
		}

		public void Start()
		{
			BeginRead();
		}

		public void Send(IEnumerable<Message> msgs)
		{
			foreach (var msg in msgs)
				Send(msg);
		}

		protected override void HandleMessage(Message msg)
		{
			if (msg is LogOnMessage)
			{
				var m = (LogOnMessage)msg;
				m_user.LogOn(m.Name);
			}
			else if (msg is LogOnCharMessage)
			{
				var m = (LogOnCharMessage)msg;
				m_user.LogOnChar(m.Name);
			}
			else if (msg is EnqueueActionMessage)
			{
				var m = (EnqueueActionMessage)msg;
				m_user.EnqueueAction(m.Action);
			}
			else if (msg is ProceedTurnMessage)
			{
				var m = (ProceedTurnMessage)msg;
				m_user.ProceedTurn();
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
