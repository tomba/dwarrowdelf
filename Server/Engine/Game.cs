using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace Dwarrowdelf.Server
{
	public sealed class Game : MarshalByRefObject, IGame
	{
		public GameEngine Engine { get; private set; }
		public IArea Area { get; private set; }

		public string GameAreaName { get; private set; }
		public string GameDir { get; private set; }

		public Game(IArea area, string gameDir)
		{
			this.Area = area;

			this.Engine = new GameEngine(this, gameDir);
		}

		public void Connect(DirectConnection clientConnection)
		{
			DirectConnectionListener.NewConnection(clientConnection);
		}

		public void Init()
		{
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Server");
		}

		public void CreateWorld()
		{
			this.Engine.Create();
		}

		public void LoadWorld(Guid id)
		{
			this.Engine.Load(id);
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			this.Engine.Run(serverStartWaitHandle);
		}

		public void Stop()
		{
			this.Engine.Stop();
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
