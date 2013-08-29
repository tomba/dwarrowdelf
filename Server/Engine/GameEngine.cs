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
		/// <summary>
		/// Require a player to be connected for ticks to proceed
		/// </summary>
		public bool RequirePlayer;

		/// <summary>
		/// Maximum time for one living to make its move. After this time has passed, the living
		/// will be skipped
		/// </summary>
		public TimeSpan MaxMoveTime;

		/// <summary>
		/// Minimum time between ticks. Ticks will never proceed faster than this.
		/// </summary>
		public TimeSpan MinTickTime;
	}

	[SaveGameObject]
	public class GameEngine : MarshalByRefObject, IGame
	{
		string m_gameDir;

		[SaveGameProperty]
		World m_world;

		[SaveGameProperty]
		List<Player> m_players;

		[SaveGameProperty]
		GameConfig m_config;

		[SaveGameProperty]
		public Guid LastSaveID { get; private set; }
		[SaveGameProperty]
		public Guid LastLoadID { get; private set; }

		[SaveGameProperty]
		public GameMode GameMode { get; private set; }

		[SaveGameProperty]
		int m_playerIDCounter;

		int m_playersConnected;

		GameDispatcher m_dispatcher;

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "Engine");

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

		public IGameManager GameManager { get; private set; }


		public GameEngine(World world, GameMode mode)
		{
			m_dispatcher = new GameDispatcher();

			m_world = world;

			this.GameMode = mode;

			m_players = new List<Player>();

			m_playerIDCounter = 2;

			m_config = new GameConfig
			{
				RequirePlayer = true,
				MaxMoveTime = TimeSpan.Zero,
				MinTickTime = TimeSpan.FromMilliseconds(50),
			};

			m_minTickTimer = new Timer(this._MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this._MaxMoveTimerCallback);

			this.LastSaveID = Guid.Empty;
			this.LastLoadID = Guid.Empty;
		}

		GameEngine(SaveGameContext ctx)
		{
			m_dispatcher = new GameDispatcher();

			m_minTickTimer = new Timer(this._MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this._MaxMoveTimerCallback);
		}

		public void Init(string gameDir, IGameManager gameManager)
		{
			if (this.GameManager != null)
				throw new Exception();

			m_gameDir = gameDir;
			this.GameManager = gameManager;
		}

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

			this.LastSaveID = id;

			var saveDir = Path.Combine(m_gameDir, id.ToString());

			Directory.CreateDirectory(saveDir);


			/* Save game intro */
			var saveEntry = new SaveEntry()
			{
				ID = id,
				DateTime = DateTime.Now,
				GameMode = this.GameMode,
				Tick = m_world.TickNumber,
			};

			using (var stream = File.Create(Path.Combine(saveDir, "intro.json")))
			using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream))
				serializer.Serialize(saveEntry);


			/* Save game */

			var savePath = Path.Combine(saveDir, "server.json");

			Trace.TraceInformation("Saving game {0}", savePath);
			var watch = Stopwatch.StartNew();

			using (var stream = File.Create(savePath))
			using (var serializer = new Dwarrowdelf.SaveGameSerializer(stream))
				serializer.Serialize(this);

			watch.Stop();
			Trace.TraceInformation("Saving game took {0}", watch.Elapsed);
		}

		public void SaveClientData(int playerID, Guid id, string data)
		{
			if (ServerConfig.DisableSaving)
				return;

			var saveDir = Path.Combine(m_gameDir, id.ToString());

			if (!Directory.Exists(saveDir))
				throw new Exception();

			if (this.LastSaveID != id)
				throw new Exception();

			string saveFile = String.Format("client-{0}.json", playerID);
			File.WriteAllText(Path.Combine(saveDir, saveFile), data);
		}

		public string LoadClientData(int playerID, Guid id)
		{
			var saveDir = Path.Combine(m_gameDir, id.ToString());
			string saveFile = String.Format("client-{0}.json", playerID);

			saveFile = Path.Combine(saveDir, saveFile);

			if (File.Exists(saveFile))
				return File.ReadAllText(saveFile);
			else
				return null;
		}

		public static GameEngine Load(string gameDir, Guid id)
		{
			var savePath = Path.Combine(gameDir, id.ToString(), "server.json");

			Trace.TraceInformation("Loading game {0}", savePath);
			var watch = Stopwatch.StartNew();

			GameEngine engine;

			using (var stream = File.OpenRead(savePath))
			using (var deserializer = new Dwarrowdelf.SaveGameDeserializer(stream))
				engine = deserializer.Deserialize<GameEngine>();

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);

			foreach (var p in engine.m_players)
				engine.InitPlayer(p);

			engine.LastLoadID = engine.LastSaveID;

			return engine;
		}

		void VerifyAccess()
		{
			m_dispatcher.VerifyAccess();
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			this.World.TickStarted += OnTickStarted;
			this.World.TickEnded += OnTickEnded;
			this.World.TurnStarting += OnTurnStart;

			PipeConnectionListener.StartListening(_OnNewConnection);
			TcpConnectionListener.StartListening(_OnNewConnection);
			DirectConnectionListener.StartListening(_OnNewConnection);

			trace.TraceInformation("The server is ready.");

			if (serverStartWaitHandle != null)
				serverStartWaitHandle.Set();

			CheckForStartTick();

			// Enter the main loop

			m_dispatcher.Run(MainWork);

			trace.TraceInformation("Server exiting");

			DirectConnectionListener.StopListening();
			TcpConnectionListener.StopListening();
			PipeConnectionListener.StopListening();

			this.World.TurnStarting -= OnTurnStart;
			this.World.TickEnded -= OnTickEnded;
			this.World.TickStarted -= OnTickStarted;

			// Need to disconnect the sockets
			foreach (var player in m_players)
			{
				if (player.IsConnected)
					player.Disconnect();
			}

			trace.TraceInformation("Server exit");
		}

		bool MainWork()
		{
			CheckNewConnections();

			foreach (var player in m_players)
			{
				if (player.IsConnected)
					player.HandleNewMessages();
			}

			return this.World.Work();
		}

		public void Stop()
		{
			m_dispatcher.Shutdown();
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
			m_dispatcher.BeginInvoke(OnNewConnection, connection);
		}

		void OnNewConnection(object state)
		{
			IConnection connection = (IConnection)state;

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

		async void HandleNewConnection(IConnection connection, Messages.LogOnRequestMessage request)
		{
			VerifyAccess();

			var name = request.Name;

			// from universal user object
			int userID = GetUserID(name);

			var player = FindPlayer(userID);

			if (player == null)
			{
				// new player needs to be created between ticks

				if (this.World.IsTickOnGoing)
					await this.World.WaitTickEnded();	// XXX needs cancellation support

				trace.TraceInformation("New player {0}", name);

				player = CreatePlayer(userID);

				var controllables = this.GameManager.SetupWorldForNewPlayer(player);
				player.SetupControllablesForNewPlayer(controllables);
			}

			player.Connect(connection);
			m_playersConnected++;

			CheckForStartTick();
		}

		int GetUserID(string name)
		{
			if (name == "tomba")
				return 1234;
			else
				throw new Exception();
		}

		void InitPlayer(Player player)
		{
			VerifyAccess();
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

			var playerID = m_playerIDCounter++;

			trace.TraceInformation("Creating new player, uid {0}, pid {1}", userID, playerID);
			player = new Player(userID, playerID, this);

			m_players.Add(player);
			InitPlayer(player);

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

		void OnTickStarted()
		{
			// If tick has started, but there are no players, proceed with the turn
			if (m_playersConnected == 0)
				this.World.SetProceedTurn();
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
			m_dispatcher.BeginInvoke(_ => CheckForStartTick(), null);
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
			m_dispatcher.BeginInvoke(_ => MaxMoveTimerCallback(), null);
		}

		void MaxMoveTimerCallback()
		{
			this.World.SetProceedTurn();
		}

		public void SignalWorld()
		{
			m_dispatcher.Signal();
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
			switch (m_world.TickMethod)
			{
				case WorldTickMethod.Simultaneous:
					if (m_players.Count > 0 && m_players.All(u => u.IsProceedTurnReplyReceived))
					{
						this.World.SetProceedTurn();
						SignalWorld();
					}

					break;

				case WorldTickMethod.Sequential:
					this.World.SetProceedTurn();
					SignalWorld();
					break;
			}
		}

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
