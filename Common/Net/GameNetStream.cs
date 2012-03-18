using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	sealed class GameNetStream : Stream
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

		public override bool CanRead { get { return true; } }
		public override bool CanSeek { get { return false; } }
		public override bool CanWrite { get { return true; } }

		public int ReadBytes { get { return m_totalRead; } }
		public int SentBytes { get { return m_totalSent; } }

		public override int ReadByte()
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

		public override int Read(byte[] buffer, int offset, int count)
		{
			int len;

			if (m_received == m_read)
			{
				len = m_socket.Receive(buffer, offset, count, SocketFlags.None);
			}
			else
			{
				len = Math.Min(count, m_received - m_read);

				Buffer.BlockCopy(m_receiveBuffer, m_read, buffer, offset, len);

				m_read += len;
			}

			m_totalRead += len;
			return len;
		}


		public override void Flush()
		{
			int len = m_socket.Send(m_sendBuffer, m_used, SocketFlags.None);
			if (len != m_used)
				throw new Exception("short write");

			m_totalSent += len;

			m_used = 0;
		}

		public override void WriteByte(byte value)
		{
			if (m_used == m_sendBuffer.Length)
				Flush();

			m_sendBuffer[m_used++] = value;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			if (m_used > 0)
				Flush();

			int len = m_socket.Send(buffer, 0, count, SocketFlags.None);
			if (len != count)
				throw new Exception("short write");

			m_totalSent += len;
		}


		public override long Length
		{
			get { throw new NotImplementedException(); }
		}

		public override long Position
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotImplementedException();
		}

		public override void SetLength(long value)
		{
			throw new NotImplementedException();
		}
	}
}
