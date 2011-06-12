using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Scripting.Hosting;

namespace Dwarrowdelf.Server
{
	class IPRunner
	{
		ScriptEngine m_scriptEngine;
		ScriptScope m_scriptScope;
		MyStream m_scriptOutputStream;

		Action<Messages.ClientMessage> m_sender;

		public IPRunner(World world, Action<Messages.ClientMessage> sender)
		{
			m_sender = sender;
			m_scriptOutputStream = new MyStream(sender);

			m_scriptEngine = IronPython.Hosting.Python.CreateEngine();
			m_scriptEngine.Runtime.IO.SetOutput(m_scriptOutputStream, System.Text.Encoding.Unicode);
			m_scriptEngine.Runtime.IO.SetErrorOutput(m_scriptOutputStream, System.Text.Encoding.Unicode);

			m_scriptScope = m_scriptEngine.CreateScope();
			m_scriptScope.SetVariable("world", world);
			m_scriptScope.SetVariable("get", new Func<object, IBaseGameObject>(world.IPGet));

			m_scriptEngine.Execute("import clr", m_scriptScope);
			m_scriptEngine.Execute("clr.AddReference('Dwarrowdelf.Common')", m_scriptScope);
			m_scriptEngine.Execute("import Dwarrowdelf", m_scriptScope);
		}

		public void Exec(string script)
		{
			try
			{
				var r = m_scriptEngine.ExecuteAndWrap(script, m_scriptScope);
				m_scriptScope.SetVariable("ret", r);
				m_scriptEngine.Execute("print ret", m_scriptScope);
			}
			catch (Exception e)
			{
				var str = "IP error:\n" + e.Message + "\n";
				m_sender(new Messages.IPOutputMessage() { Text = str });
			}
		}

		class MyStream : Stream
		{
			Action<Messages.ClientMessage> m_sender;
			MemoryStream m_stream = new MemoryStream();

			public MyStream(Action<Messages.ClientMessage> sender)
			{
				m_sender = sender;
			}

			public override bool CanRead { get { return false; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }

			public override void Flush()
			{
				if (m_stream.Position == 0)
					return;

				var text = System.Text.Encoding.Unicode.GetString(m_stream.GetBuffer(), 0, (int)m_stream.Position);
				m_stream.Position = 0;
				m_stream.SetLength(0);
				var msg = new Messages.IPOutputMessage() { Text = text };
				m_sender(msg);
			}

			public override long Length { get { throw new NotImplementedException(); } }

			public override long Position
			{
				get { throw new NotImplementedException(); }
				set { throw new NotImplementedException(); }
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				throw new NotImplementedException();
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				throw new NotImplementedException();
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (count == 0)
					return;

				m_stream.Write(buffer, offset, count);
			}
		}



	}
}
