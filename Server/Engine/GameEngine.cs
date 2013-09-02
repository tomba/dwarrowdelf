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

		public bool IronPythonEnabled;
	}

	[SaveGameObject]
	public abstract class GameEngine : MarshalByRefObject, IGame
	{
		string m_gameDir;

		[SaveGameProperty]
		public World World { get; private set; }

		// Connected users
		List<User> m_users;

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

		bool UseMinTickTime { get { return m_config.MinTickTime != TimeSpan.Zero; } }
		bool UseMaxMoveTime { get { return m_config.MaxMoveTime != TimeSpan.Zero; } }

		bool m_minTickTimePassed = true;

		protected GameEngine(string gameDir, GameMode gameMode, WorldTickMethod tickMethod)
		{
			CommonInit();

			m_gameDir = gameDir;

			this.GameMode = gameMode;
			this.World = new World(gameMode, tickMethod);

			m_players = new List<Player>();

			m_playerIDCounter = 2;

			m_config = new GameConfig
			{
				RequirePlayer = true,
				MaxMoveTime = TimeSpan.Zero,
				MinTickTime = TimeSpan.FromMilliseconds(50),
				IronPythonEnabled = false,
			};

			this.LastSaveID = Guid.Empty;
			this.LastLoadID = Guid.Empty;
		}

		protected GameEngine(SaveGameContext ctx)
		{
			CommonInit();
		}

		void CommonInit()
		{
			m_dispatcher = new GameDispatcher();

			m_minTickTimer = new Timer(this._MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this._MaxMoveTimerCallback);

			m_users = new List<User>();
		}

		void SetupAfterLoad(string gameDir)
		{
			m_gameDir = gameDir;

			foreach (var p in m_players)
				InitPlayer(p);

			this.LastLoadID = this.LastSaveID;
		}

		void VerifyAccess()
		{
			m_dispatcher.VerifyAccess();
		}

		public void Run(EventWaitHandle serverStartWaitHandle)
		{
			this.World.TickStarted += OnTickStarted;
			this.World.TickEnded += OnTickEnded;
			this.World.TurnStarting += OnTurnStarting;
			this.World.TurnEnded += OnTurnEnded;

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

			this.World.TurnEnded -= OnTurnEnded;
			this.World.TurnStarting -= OnTurnStarting;
			this.World.TickEnded -= OnTickEnded;
			this.World.TickStarted -= OnTickStarted;

			// Need to disconnect the sockets
			foreach (var user in m_users)
			{
				if (user.IsConnected)
					user.Disconnect();
			}

			trace.TraceInformation("Server exit");
		}

		bool MainWork()
		{
			List<User> disconnectedUsers = null;

			foreach (var user in m_users)
			{
				if (user.IsConnected)
					user.PollNewMessages();

				if (user.IsConnected == false)
				{
					if (disconnectedUsers == null)
						disconnectedUsers = new List<User>();
					disconnectedUsers.Add(user);
				}
			}

			if (disconnectedUsers != null)
			{
				foreach (var user in disconnectedUsers)
					m_users.Remove(user);
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

		// called in worker thread context
		void _OnNewConnection(IConnection connection)
		{
			trace.TraceInformation("New connection");
			m_dispatcher.BeginInvoke(OnNewConnection, connection);
		}

		async void OnNewConnection(object state)
		{
			VerifyAccess();

			IConnection connection = (IConnection)state;

			var msg = await connection.GetMessageAsync();
			if (msg == null)
				return;

			var request = msg as Messages.LogOnRequestMessage;
			if (request == null)
				throw new Exception("bad initial message received");

			var name = request.Name;

			// from universal user object
			int userID = GetUserID(name);

			if (m_users.Any(u => u.UserID == userID))
				throw new Exception("User already connected");

			connection.NewMessageEvent += Signal;

			var user = new User(connection, userID, name, this, m_config.IronPythonEnabled);
			user.DisconnectEvent += OnUserDisconnected;
			m_users.Add(user);

			trace.TraceInformation("User {0} connected", user);

			var player = m_players.SingleOrDefault(p => p.UserID == userID);

			if (player == null)
			{
				player = m_players.FirstOrDefault(p => p.UserID == 0);

				if (player == null)
					throw new Exception("game full");
			}

			user.SetPlayer(player);

			m_playersConnected++;

			CheckForStartTick();
		}

		void OnUserDisconnected(User user)
		{
			trace.TraceInformation("User {0} disconnected", user);
			// Note: user will be removed from m_users in the MainWork
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

		protected void AddPlayer(Player player)
		{
			var playerID = m_playerIDCounter++;

			m_players.Add(player);
			InitPlayer(player);
		}

		bool _IsTimeToStartTick()
		{
			if (m_config.RequirePlayer && m_playersConnected == 0)
				return false;

			if (this.UseMinTickTime && m_minTickTimePassed == false)
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
				m_minTickTimePassed = false;
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
			m_dispatcher.BeginInvoke(_ => { m_minTickTimePassed = true; CheckForStartTick(); }, null);
		}

		void OnTurnStarting(LivingObject living)
		{
			if (this.UseMaxMoveTime)
				m_maxMoveTimer.Change(m_config.MaxMoveTime, TimeSpan.FromMilliseconds(-1));
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

		void OnTurnEnded(LivingObject living)
		{
			// Cancel the timer
			if (this.UseMaxMoveTime)
				m_maxMoveTimer.Change(TimeSpan.FromMilliseconds(-1), TimeSpan.FromMilliseconds(-1));

			// XXX There's a race. The timer could fire before the cancellation, but the timer action could be run after.
		}

		public void Signal()
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
			switch (this.World.TickMethod)
			{
				case WorldTickMethod.Simultaneous:
					if (m_players.Count > 0 && m_players.Where(p => p.IsConnected).All(p => p.IsProceedTurnReplyReceived))
					{
						this.World.SetProceedTurn();
						Signal();
					}

					break;

				case WorldTickMethod.Sequential:
					this.World.SetProceedTurn();
					Signal();
					break;
			}
		}

		public override object InitializeLifetimeService()
		{
			return null;
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
				Tick = this.World.TickNumber,
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

			engine.SetupAfterLoad(gameDir);

			return engine;
		}

	}
}
