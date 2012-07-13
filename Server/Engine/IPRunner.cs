using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Scripting.Hosting;

namespace Dwarrowdelf.Server
{
	sealed class IPRunner
	{
		ScriptEngine m_scriptEngine;
		ScriptScope m_exprScope;
		ScriptScope m_scriptScope;
		MyStream m_scriptOutputStream;

		Action<Messages.ClientMessage> m_sender;

		public IPRunner(World world, Action<Messages.ClientMessage> sender)
		{
			m_sender = sender;
			m_scriptOutputStream = new MyStream(sender);

			m_scriptEngine = IronPython.Hosting.Python.CreateEngine();

			InitRuntime(m_scriptEngine.Runtime);

			m_exprScope = m_scriptEngine.CreateScope();
			InitScope(m_exprScope, world);

			m_scriptScope = m_scriptEngine.CreateScope();
			InitScope(m_scriptScope, world);
		}

		void InitRuntime(ScriptRuntime runtime)
		{
			runtime.IO.SetOutput(m_scriptOutputStream, System.Text.Encoding.Unicode);
			runtime.IO.SetErrorOutput(m_scriptOutputStream, System.Text.Encoding.Unicode);

			foreach (var assemblyName in new string[] { "Dwarrowdelf.Common", "Dwarrowdelf.Server.World" })
			{
				var assembly = runtime.Host.PlatformAdaptationLayer.LoadAssembly(assemblyName);
				runtime.LoadAssembly(assembly);
			}
		}

		void InitScope(ScriptScope scope, World world)
		{
			var globals = new Dictionary<string, object>()
			{
				{ "world", world },
				{ "get", new Func<object, BaseObject>(world.IPGet) },
			};

			foreach (var kvp in globals)
				scope.SetVariable(kvp.Key, kvp.Value);

			// XXX perhaps this can also be done with C# somehow...
			m_scriptEngine.Execute("import Dwarrowdelf", scope);
		}

		public void ExecExpr(string script)
		{
			try
			{
				var r = m_scriptEngine.ExecuteAndWrap(script, m_exprScope);
				m_exprScope.SetVariable("ret", r);
				m_scriptEngine.Execute("print ret", m_exprScope);
			}
			catch (Exception e)
			{
				var str = "IP error:\n" + e.Message + "\n";
				m_sender(new Messages.IPOutputMessage() { Text = str });
			}
		}

		public void ExecScript(string script, Tuple<string, object>[] args)
		{
			try
			{
				if (args != null)
					foreach (var kvp in args)
						m_scriptScope.SetVariable(kvp.Item1, kvp.Item2);

				m_scriptEngine.Execute(script, m_scriptScope);
			}
			catch (Exception e)
			{
				var str = "IP error:\n" + e.Message + "\n";
				m_sender(new Messages.IPOutputMessage() { Text = str });
			}
		}
		sealed class MyStream : Stream
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

				if (text == "\r\n")
					return;

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
