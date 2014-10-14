using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Microsoft.Scripting.Hosting;
using IronPython.Runtime;

namespace Dwarrowdelf.Server
{
	sealed class IPRunner
	{
		ScriptEngine m_scriptEngine;
		ScriptScope m_exprScope;
		MyStream m_scriptOutputStream;
		User m_user;
		Dictionary<string, object> m_scopeVars;

		public IPRunner(User user, GameEngine engine)
		{
			m_user = user;
			m_scriptOutputStream = new MyStream(user.Send);

			m_scriptEngine = IronPython.Hosting.Python.CreateEngine();

			InitRuntime(m_scriptEngine.Runtime);

			m_scopeVars = new Dictionary<string, object>()
			{
				{ "engine", engine },
				{ "world", engine.World },
				{ "get", new Func<object, BaseObject>(engine.World.IPGet) },
			};

			m_exprScope = m_scriptEngine.CreateScope(m_scopeVars);
			InitScopeImports(m_exprScope);
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

		void InitScopeImports(ScriptScope scope)
		{
			// XXX perhaps this can also be done with C# somehow...
			m_scriptEngine.Execute("import Dwarrowdelf", scope);
		}

		public void Shutdown()
		{
			m_scriptEngine.Runtime.Shutdown();
			m_scriptEngine.Runtime.IO.RedirectToConsole();
		}

		public void SetPlayer(Player player)
		{
			if (player != null)
			{
				m_exprScope.SetVariable("player", player);
				m_scopeVars["player"] = player;
			}
			else
			{
				m_exprScope.RemoveVariable("player");
				m_scopeVars.Remove("player");
			}
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
				var str = e.Message;
				m_user.Send(new Messages.IPOutputMessage() { Text = str });
			}
		}

		public void ExecScript(string script, KeyValuePair<string, object>[] args)
		{
			try
			{
				var scope = m_scriptEngine.CreateScope(m_scopeVars);
				InitScopeImports(scope);

				if (args != null)
					foreach (var kvp in args)
						scope.SetVariable(kvp.Key, kvp.Value);

				m_scriptEngine.Execute(script, scope);
			}
			catch (Exception e)
			{
				var str = m_scriptEngine.GetService<ExceptionOperations>().FormatException(e);
				m_user.Send(new Messages.IPOutputMessage() { Text = str });
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
