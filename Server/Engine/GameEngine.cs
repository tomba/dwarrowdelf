using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.ComponentModel;

namespace Dwarrowdelf.Server
{
	[Serializable]
	sealed class GameConfig
	{
		// Require a player to be connected for ticks to proceed
		public bool RequirePlayer;

		// Maximum time for one living to make its move. After this time has passed, the living
		// will be skipped
		public TimeSpan MaxMoveTime;

		// Minimum time between ticks. Ticks will never proceed faster than this.
		public TimeSpan MinTickTime;
	}

	public class GameEngine : MarshalByRefObject, IGame
	{
		string m_gameDir;
		World m_world;

		volatile bool m_exit = false;
		AutoResetEvent m_gameSignal = new AutoResetEvent(true);

		int m_playersConnected;

		List<Player> m_players = new List<Player>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "Engine");

		GameConfig m_config = new GameConfig
		{
			RequirePlayer = true,
			MaxMoveTime = TimeSpan.Zero,
			MinTickTime = TimeSpan.FromMilliseconds(50),
		};

		/// <summary>
		/// Timer is used to start the tick after MinTickTime
		/// </summary>
		Timer m_minTickTimer;

		/// <summary>
		/// Timer is used to timeout player turn after MaxMoveTime
		/// </summary>
		Timer m_maxMoveTimer;

		public World World { get { return m_world; } }
		bool UseMinTickTime { get { return m_config.MinTickTime != TimeSpan.Zero; } }
		bool UseMaxMoveTime { get { return m_config.MaxMoveTime != TimeSpan.Zero; } }

		/// <summary>
		/// Used for VerifyAccess
		/// </summary>
		Thread m_gameThread;

		public IGameManager GameManager { get; private set; }


		public static GameEngine Create(string gameDir, World world, IGameManager gameManager)
		{
			var game = new GameEngine(gameDir);
			game.Create(world, gameManager);
			return game;
		}

		public static GameEngine Load(string gameDir, Guid save)
		{
			throw new Exception();
		}


		GameEngine(string gameDir)
		{
			m_gameDir = gameDir;

			m_minTickTimer = new Timer(this._MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this._MaxMoveTimerCallback);
		}

		void Create(World world, IGameManager gameManager)
		{
			if (m_world != null)
				throw new Exception();

			this.LastSaveID = Guid.Empty;
			this.LastLoadID = Guid.Empty;

			m_world = world;
			this.GameManager = gameManager;
		}

		public Guid LastSaveID { get; private set; }
		public Guid LastLoadID { get; private set; }

		public void Save()
		{
			//var id = Guid.NewGuid();
			// XXX use fixed guid to help testing
			var id = new Guid(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1);

			var msg = new Dwarrowdelf.Messages.SaveClientDataRequestMessage() { ID = id };
			foreach (var p in m_players.Where(p => p.IsConnected))
				p.Send(msg);

			if (ServerConfig.DisableSaving)
			{
				Trace.TraceError("Warning: Saving is disabled");
				return;
			}

			int tick = m_world.TickNumber;

			var saveDir = Path.Combine(m_gameDir, id.ToString());

			Directory.CreateDirectory(saveDir);

			var now = DateTime.Now;

			File.WriteAllText(Path.Combine(saveDir, "TIMESTAMP"), now.ToString("u"));
			File.WriteAllText(Path.Combine(saveDir, "TICK"), tick.ToString());

			using (var writer = File.CreateText(Path.Combine(saveDir, "_info.txt")))
			{
				writer.WriteLine("date {0:u}", now);
				writer.WriteLine("tick {0}", tick);
				writer.WriteLine("players");
				foreach (var p in m_players)
				{
					writer.WriteLine("\t{0}: {1}", p.UserID, p.IsConnected ? "connected" : "not connected");
				}
			}

			var saveData = new SaveData()
			{
				World = this.World,
				Players = m_players,
				ID = id,
			};

			SaveWorld(saveData, Path.Combine(saveDir, "server.json"));

			this.LastSaveID = id;
		}

		public void SaveClientData(int userID, Guid id, string data)
		{
			if (ServerConfig.DisableSaving)
				return;

			var saveDir = Path.Combine(m_gameDir, id.ToString());

			if (!Directory.Exists(saveDir))
				throw new Exception();

			if (this.LastSaveID != id)
				throw new Exception();

			string saveFile = String.Format("client-{0}.json", userID);
			File.WriteAllText(Path.Combine(saveDir, saveFile), data);
		}

		public string LoadClientData(int userID, Guid id)
		{
			var saveDir = Path.Combine(m_gameDir, id.ToString());
			string saveFile = String.Format("client-{0}.json", userID);

			saveFile = Path.Combine(saveDir, saveFile);

			if (File.Exists(saveFile))
				return File.ReadAllText(saveFile);
			else
				return null;
		}

		public void Load(Guid id)
		{
			if (m_world != null)
				throw new Exception();

			var saveData = LoadWorld(Path.Combine(m_gameDir, id.ToString(), "server.json"));

			m_world = saveData.World;

			foreach (var p in saveData.Players)
				AddPlayer(p);

			this.LastLoadID = saveData.ID;
		}

		void VerifyAccess()
		{
			if (m_gameThread != null && Thread.CurrentThread != m_gameThread)
				throw new Exception();
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			m_gameThread = Thread.CurrentThread;

			this.World.TurnStarting += OnTurnStart;
			this.World.TickEnded += OnTickEnded;
			this.World.HandleMessagesEvent += OnHandleMessagesEvent;

			PipeConnectionListener.StartListening(_OnNewConnection);
			TcpConnectionListener.StartListening(_OnNewConnection);
			DirectConnectionListener.StartListening(_OnNewConnection);

			trace.TraceInformation("The server is ready.");

			if (serverStartWaitHandle != null)
				serverStartWaitHandle.Set();

			bool again = true;

			while (m_exit == false)
			{
				if (!again)
					m_gameSignal.WaitOne();

				again = this.World.Work();
			}

			trace.TraceInformation("Server exiting");

			DirectConnectionListener.StopListening();
			TcpConnectionListener.StopListening();
			PipeConnectionListener.StopListening();

			this.World.HandleMessagesEvent -= OnHandleMessagesEvent;
			this.World.TickEnded -= OnTickEnded;
			this.World.TurnStarting -= OnTurnStart;

			// Need to disconnect the sockets
			foreach (var player in m_players)
			{
				if (player.IsConnected)
					player.Disconnect();
			}

			trace.TraceInformation("Server exit");
		}

		public void Stop()
		{
			m_exit = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}

		public void Connect(DirectConnection clientConnection)
		{
			DirectConnectionListener.NewConnection(clientConnection);
		}

		List<IConnection> m_newConns = new List<IConnection>();

		// called in worker thread context
		void _OnNewConnection(IConnection connection)
		{
			trace.TraceInformation("New connection");
			m_world.BeginInvokeInstant(new Action<IConnection>(OnNewConnection), connection);
			SignalWorld();
		}

		void OnNewConnection(IConnection connection)
		{
			m_newConns.Add(connection);
			connection.NewMessageEvent += SignalWorld;
			CheckNewConnections();
		}

		void CheckNewConnections()
		{
			for (int i = m_newConns.Count - 1; i >= 0; --i)
			{
				var conn = m_newConns[i];

				Messages.Message msg;

				if (conn.TryGetMessage(out msg))
				{
					m_newConns.RemoveAt(i);

					var request = msg as Messages.LogOnRequestMessage;
					if (request == null)
						throw new Exception("bad initial message received");

					HandleNewConnection(conn, request);
				}
			}
		}

		void OnHandleMessagesEvent()
		{
			CheckNewConnections();

			foreach (var player in m_players)
			{
				if (player.IsConnected)
					player.HandleNewMessages();
			}
		}

		void HandleNewConnection(IConnection connection, Messages.LogOnRequestMessage request)
		{
			VerifyAccess();

			var name = request.Name;

			// from universal user object
			int userID = GetUserID(name);

			var player = FindPlayer(userID);

			if (player == null)
			{
				m_world.BeginInvoke(new Action<IConnection, Messages.LogOnRequestMessage>(HandleNewPlayer), connection, request);
				SignalWorld();
			}
			else
			{
				player.Connect(connection);
				m_playersConnected++;

				CheckForStartTick();
			}
		}

		void HandleNewPlayer(IConnection connection, Messages.LogOnRequestMessage request)
		{
			var name = request.Name;

			trace.TraceInformation("New player {0}", name);

			int userID = GetUserID(name);

			var player = CreatePlayer(userID);

			var controllables = this.GameManager.SetupWorldForNewPlayer(player);
			foreach (var l in controllables)
				player.AddControllable(l);

			player.Connect(connection);
			m_playersConnected++;

			CheckForStartTick();
		}

		int GetUserID(string name)
		{
			if (name == "tomba")
				return 1;
			else
				throw new Exception();
		}

		void AddPlayer(Player player)
		{
			VerifyAccess();
			m_players.Add(player);
			player.Init(this);
			player.ProceedTurnReceived += OnPlayerProceedTurnReceived;
			player.DisconnectEvent += OnPlayerDisconnected;
		}

		void RemovePlayer(Player player)
		{
			VerifyAccess();
			bool ok = m_players.Remove(player);
			Debug.Assert(ok);

			player.ProceedTurnReceived -= OnPlayerProceedTurnReceived;
			player.DisconnectEvent -= OnPlayerDisconnected;
		}

		Player FindPlayer(int userID)
		{
			return m_players.SingleOrDefault(u => u.UserID == userID);
		}

		Player CreatePlayer(int userID)
		{
			var player = FindPlayer(userID);

			if (player != null)
				throw new Exception();

			trace.TraceInformation("Creating new player {0}", userID);
			player = new Player(userID);

			AddPlayer(player);

			return player;
		}

		bool _IsTimeToStartTick()
		{
			// XXX check if this.UseMinTickTime && enough time passed

			if (m_config.RequirePlayer && m_playersConnected == 0)
				return false;

			return true;
		}

		bool IsTimeToStartTick()
		{
			VerifyAccess();
			bool r = _IsTimeToStartTick();
			trace.TraceVerbose("IsTimeToStartTick = {0}", r);
			return r;
		}

		void CheckForStartTick()
		{
			VerifyAccess();
			if (IsTimeToStartTick())
				this.World.SetOkToStartTick();
		}

		void OnTickEnded()
		{
			if (this.UseMinTickTime)
			{
				m_minTickTimer.Change(m_config.MinTickTime, TimeSpan.FromMilliseconds(-1));
			}
			else
			{
				CheckForStartTick();
			}
		}

		void _MinTickTimerCallback(object stateInfo)
		{
			trace.TraceVerbose("MinTickTimerCallback");
			this.World.BeginInvokeInstant(new Action(CheckForStartTick));
			SignalWorld();
		}

		void OnTurnStart(LivingObject living)
		{
			if (this.UseMaxMoveTime)
				m_maxMoveTimer.Change(m_config.MaxMoveTime, TimeSpan.FromMilliseconds(-1));

			// XXX use TurnEnded to cancel?
		}

		void _MaxMoveTimerCallback(object stateInfo)
		{
			trace.TraceVerbose("MaxMoveTimerCallback");
			this.World.BeginInvokeInstant(new Action(MaxMoveTimerCallback));
			SignalWorld();
		}

		void MaxMoveTimerCallback()
		{
			this.World.SetProceedTurn();
		}

		public void SignalWorld()
		{
			m_gameSignal.Set();
		}

		public void SetMinTickTime(TimeSpan minTickTime)
		{
			VerifyAccess();
			m_config.MinTickTime = minTickTime;
		}

		void OnPlayerDisconnected(Player player)
		{
			m_playersConnected--;

			Debug.Assert(m_players.Count(p => p.IsConnected) == m_playersConnected);
		}

		void OnPlayerProceedTurnReceived(Player player)
		{
			if (m_world.TickMethod == WorldTickMethod.Simultaneous && m_players.Count > 0 && m_players.All(u => u.IsProceedTurnReplyReceived))
			{
				this.World.SetProceedTurn();
				SignalWorld();
			}
		}

		static void SaveWorld(SaveData saveData, string savePath)
		{
			Trace.TraceInformation("Saving game {0}", savePath);
			var watch = Stopwatch.StartNew();

			using (var stream = File.Create(savePath))
			using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream))
				serializer.Serialize(saveData);

			watch.Stop();
			Trace.TraceInformation("Saving game took {0}", watch.Elapsed);
		}

		static SaveData LoadWorld(string savePath)
		{
			Trace.TraceInformation("Loading game {0}", savePath);
			var watch = Stopwatch.StartNew();

			SaveData saveData;

			using (var stream = File.OpenRead(savePath))
			using (var deserializer = new Dwarrowdelf.SaveGameDeserializer(stream))
				saveData = deserializer.Deserialize<SaveData>();

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);

			return saveData;
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}

		[Serializable]
		sealed class SaveData
		{
			public World World;
			public List<Player> Players;
			public Guid ID;
		}
	}
}
