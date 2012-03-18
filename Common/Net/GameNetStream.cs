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

		int m_totalRead;
		int m_totalSent;

		BufferedStream m_recvStream;
		BufferedStream m_sendStream;

		public GameNetStream(Socket socket)
		{
			m_socket = socket;

			var netStream = new NetworkStream(socket, false);
			m_recvStream = new BufferedStream(netStream);
			m_sendStream = new BufferedStream(netStream);
		}

		public override bool CanRead { get { return true; } }
		public override bool CanWrite { get { return true; } }
		public override bool CanSeek { get { return false; } }

		public int ReadBytes { get { return m_totalRead; } }
		public int SentBytes { get { return m_totalSent; } }

		public override int ReadByte()
		{
			int b = m_recvStream.ReadByte();
			if (b == -1)
				throw new SocketException((int)SocketError.Success);
			m_totalRead++;
			return b;
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			var len = m_recvStream.Read(buffer, offset, count);
			if (len == 0)
				throw new SocketException((int)SocketError.Success);
			m_totalRead += len;
			return len;
		}


		public override void Flush()
		{
			m_sendStream.Flush();
		}

		public override void WriteByte(byte value)
		{
			m_totalSent++;
			m_sendStream.WriteByte(value);
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			m_totalSent += count;
			m_sendStream.Write(buffer, offset, count);
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
