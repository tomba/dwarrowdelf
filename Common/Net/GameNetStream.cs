using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	sealed class GameNetStream
	{
		Socket m_socket;

		byte[] m_receiveBuffer;
		int m_received;
		int m_read;
		int m_totalRead;

		byte[] m_sendBuffer;
		int m_used;
		int m_totalSent;

		public GameNetStream(Socket socket, int receiveBufferLen, int sendBufferLen)
		{
			m_socket = socket;

			m_receiveBuffer = new byte[receiveBufferLen];
			m_sendBuffer = new byte[sendBufferLen];
		}

		public int ReadBytes { get { return m_totalRead; } }
		public int SentBytes { get { return m_totalSent; } }

		public int ReadByte()
		{
			if (m_received == m_read)
			{
				int len = m_socket.Receive(m_receiveBuffer);

				if (len == 0)
				{
					throw new SocketException();
				}

				m_received = len;
				m_read = 0;
			}

			m_totalRead++;
			return m_receiveBuffer[m_read++];
		}


		public void Flush()
		{
			int len = m_socket.Send(m_sendBuffer, m_used, SocketFlags.None);
			if (len != m_used)
				throw new Exception("short write");

			m_totalSent += len;

			m_used = 0;
		}

		public void WriteByte(byte value)
		{
			if (m_used == m_sendBuffer.Length)
				Flush();

			m_sendBuffer[m_used++] = value;
		}

	}
}
