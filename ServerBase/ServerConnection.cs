using System;
using System.Collections.Generic;
using System.Net.Sockets;
using MyGame.ClientMsgs;

namespace MyGame
{

	public class ServerConnection : Connection
	{
		ServerService m_user;
		Dictionary<Type, Action<Message>> m_handlerMap = new Dictionary<Type, Action<Message>>();

		public ServerConnection(TcpClient client)
			: base(client)
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
			Action<Message> f;
			Type t = msg.GetType();
			if (!m_handlerMap.TryGetValue(t, out f))
			{
				f = WrapperGenerator.CreateHandlerWrapper<Message>("HandleMessage", t, this);

				if (f == null)
					throw new Exception();

				m_handlerMap[t] = f;
			}

			f(msg);
		}

		void HandleMessage(LogOnMessage msg)
		{
			m_user.LogOn(msg.Name);
		}

		void HandleMessage(LogOnCharMessage msg)
		{
			m_user.LogOnChar(msg.Name);
		}

		void HandleMessage(EnqueueActionMessage msg)
		{
			m_user.EnqueueAction(msg.Action);
		}

		void HandleMessage(ProceedTurnMessage msg)
		{
			m_user.ProceedTurn();
		}

		void HandleMessage(SetTilesMessage msg)
		{
			m_user.SetTiles(msg.MapID, msg.Cube, msg.TileID);
		}
	}
}
