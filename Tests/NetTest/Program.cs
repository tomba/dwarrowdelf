using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

using Dwarrowdelf;
using System.Diagnostics;

/*
 * x64-release
 * 
 * Size 201326592
 * Sent 201369600 bytes
 * Received 2048 msgs, 201369600 bytes, in 2335 ms
 */

namespace NetTest
{
	static class Program
	{
		static public ManualResetEvent Event = new ManualResetEvent(false);
		public const int NUM_MSGS = 1024 * 2;

		static void Main(string[] args)
		{
			var sender = new Sender();
			var receiver = new Receiver();

			sender.Connect();

			Event.WaitOne();
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
			var msg = new Dwarrowdelf.Messages.MapDataTerrainsMessage()
			{
				Environment = new ObjectID(123),
				Bounds = new IntCuboid(),
				TerrainData = new TileData[0],
			};

			unsafe
			{
				Console.WriteLine("Size {0}", msg.TerrainData.Length * sizeof(TileData) * Program.NUM_MSGS);
			}

			for (int i = 0; i < Program.NUM_MSGS; ++i)
			{
				m_conn.Send(msg);
			}

			Console.WriteLine("Sent {0} bytes", m_conn.SentBytes);
		}
	}

	class Receiver
	{
		Connection m_conn;
		Stopwatch m_sw;

		public Receiver()
		{
			Connection.NewConnectionEvent += new Action<Connection>(Connection_NewConnectionEvent);
			Connection.StartListening();
		}

		void Connection_NewConnectionEvent(Connection obj)
		{
			m_conn = obj;
			m_conn.ReceiveEvent += new Action<Dwarrowdelf.Messages.Message>(m_conn_ReceiveEvent);
			m_sw = Stopwatch.StartNew();
		}

		int m_msgsReceived;

		void m_conn_ReceiveEvent(Dwarrowdelf.Messages.Message obj)
		{
			m_msgsReceived++;
			if (m_msgsReceived < Program.NUM_MSGS)
				return;

			m_sw.Stop();

			Console.WriteLine("Received {0} msgs, {1} bytes, in {2} ms",
				m_conn.ReceivedMessages, m_conn.ReceivedBytes,
				m_sw.ElapsedMilliseconds);

			Program.Event.Set();
		}
	}
}
