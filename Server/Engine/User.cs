using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Server
{
	public sealed class User
	{
		public int UserID { get; private set; }
		public Player Player { get; private set; }
		public string Name { get; private set; }

		IConnection m_connection;
		public bool IsConnected { get { return m_connection != null; } }

		MyTraceSource trace = new MyTraceSource("Server.User");

		public event Action<User> DisconnectEvent;

		IPRunner m_ipRunner;

		GameEngine m_engine;

		Task m_ipStartTask;

		public User(IConnection connection, int userID, string name, GameEngine engine, bool isIronPythonEnabled)
		{
			m_connection = connection;
			this.UserID = userID;
			this.Name = name;
			m_engine = engine;
			trace.Header = String.Format("User({0}/{1})", this.Name, this.UserID);

			if (isIronPythonEnabled)
			{
				// XXX creating IP engine takes some time. Do it in the background. Race condition with IP msg handlers
				m_ipStartTask = Task.Run(() =>
				{
					m_ipRunner = new IPRunner(this, m_engine);
					m_ipStartTask = null;
				});
			}
		}

		public override string ToString()
		{
			return String.Format("User({0}/{1})", this.Name, this.UserID);
		}

		public void SetPlayer(Player player)
		{
			trace.TraceInformation("{0} takes control of {1}", this, player);
			this.Player = player;
			player.ConnectUser(this);

			if (m_ipStartTask != null)
				m_ipStartTask.Wait();

			if (m_ipRunner != null)
				m_ipRunner.SetPlayer(player);
		}

		public void Disconnect()
		{
			if (this.Player != null)
			{
				if (m_ipRunner != null)
					m_ipRunner.SetPlayer(null);

				this.Player.DisconnectUser();
				this.Player = null;
			}

			m_connection.Disconnect();
		}

		void OnDisconnected()
		{
			trace.TraceInformation("OnDisconnected");

			DH.Dispose(ref m_connection);

			if (DisconnectEvent != null)
				DisconnectEvent(this);
		}

		public void Send(ClientMessage msg)
		{
			if (m_connection != null)
				m_connection.Send(msg);
		}

		public void PollNewMessages()
		{
			trace.TraceVerbose("PollNewMessages");

			if (this.Player != null)
			{
				Message msg;
				while (m_connection.TryGetMessage(out msg))
				{
					trace.TraceVerbose("OnReceiveMessage({0})", msg);

					bool handled = DispatchMessage(msg);

					if (!handled)
						this.Player.DispatchMessage(msg);
				}
			}

			if (!m_connection.IsConnected)
			{
				trace.TraceInformation("PollNewMessages, disconnected");

				OnDisconnected();
			}
		}

		bool DispatchMessage(Message msg)
		{
			Action<User, ServerMessage> method;

			if (s_handlerMap.TryGetValue(msg.GetType(), out method) == false)
				return false;

			method(this, (ServerMessage)msg);

			return true;
		}

		void ReceiveMessage(LogOutRequestMessage msg)
		{
			Send(new Messages.LogOutReplyMessage());

			Disconnect();
		}

		void ReceiveMessage(SetWorldConfigMessage msg)
		{
			if (msg.MinTickTime.HasValue)
				m_engine.SetMinTickTime(msg.MinTickTime.Value);
		}

		void ReceiveMessage(SaveRequestMessage msg)
		{
			m_engine.Save();
		}

		void ReceiveMessage(SaveClientDataReplyMessage msg)
		{
			m_engine.SaveClientData(this.Player.PlayerID, msg.ID, msg.Data);
		}

		void ReceiveMessage(IPExpressionMessage msg)
		{
			//trace.TraceInformation("IPExpressionMessage {0}", msg.Script);

			if (m_ipStartTask != null)
				m_ipStartTask.Wait();

			if (m_ipRunner != null)
				m_ipRunner.ExecExpr(msg.Script);
			else
				Send(new Messages.IPOutputMessage() { Text = "IronPython not enabled" });
		}

		void ReceiveMessage(IPScriptMessage msg)
		{
			//trace.TraceInformation("IPScriptMessage {0}", msg.Script);

			if (m_ipStartTask != null)
				m_ipStartTask.Wait();

			if (m_ipRunner != null)
				m_ipRunner.ExecScript(msg.Script, msg.Args);
			else
				Send(new Messages.IPOutputMessage() { Text = "IronPython not enabled" });
		}


		static Dictionary<Type, Action<User, ServerMessage>> s_handlerMap;

		static User()
		{
			var messageTypes = Helpers.GetNonabstractSubclasses(typeof(ServerMessage));

			s_handlerMap = new Dictionary<Type, Action<User, ServerMessage>>(messageTypes.Count());

			foreach (var type in messageTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<User, ServerMessage>("ReceiveMessage", type);
				if (method != null)
					s_handlerMap[type] = method;
			}
		}
	}
}
