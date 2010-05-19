using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using MyGame;

namespace NetTest
{
	class Program
	{
		static void Main(string[] args)
		{
			MyDebug.WriteLine("**********************");

			var sender = new Sender();
			var receiver = new Receiver();

			sender.Connect();

			Console.WriteLine("waiting");
			Console.ReadLine();
		}
	}

	class Sender
	{
		Connection m_conn;

		public Sender()
		{
			m_conn = new Connection();
		}

		public void Connect()
		{
			m_conn.BeginConnect(ConnectCallback);
		}

		void ConnectCallback()
		{
			var msg = new MyGame.ClientMsgs.IronPythonCommand() { Text = "asdas" };
			m_conn.Send(msg);
		}
	}

	class Receiver
	{
		Connection m_conn;

		public Receiver()
		{
			Connection.NewConnectionEvent += new Action<Connection>(Connection_NewConnectionEvent);
			Connection.StartListening(9999);
		}

		void Connection_NewConnectionEvent(Connection obj)
		{
			m_conn = obj;
			m_conn.ReceiveEvent += new Action<MyGame.ClientMsgs.Message>(m_conn_ReceiveEvent);
		}

		void m_conn_ReceiveEvent(MyGame.ClientMsgs.Message obj)
		{
			Console.WriteLine("received {0}", obj);
		}
	}
}
