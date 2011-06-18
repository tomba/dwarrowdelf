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
	class GameConfig
	{
		// Require a player to be connected for ticks to proceed
		public bool RequirePlayer;

		// Require a player to be in game for ticks to proceed
		public bool RequirePlayerInGame;

		// Maximum time for one living to make its move. After this time has passed, the living
		// will be skipped
		public TimeSpan MaxMoveTime;

		// Minimum time between ticks. Ticks will never proceed faster than this.
		public TimeSpan MinTickTime;
	}

	public abstract class GameEngine
	{
		string m_gameDir;
		World m_world;

		volatile bool m_exit = false;
		AutoResetEvent m_gameSignal = new AutoResetEvent(true);

		int m_playersConnected;
		int m_playersInGame;

		List<Player> m_players = new List<Player>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "Engine");

		GameConfig m_config = new GameConfig
		{
			RequirePlayer = true,
			RequirePlayerInGame = false,
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

		protected GameEngine(string gameDir)
		{
			m_gameDir = gameDir;

			m_minTickTimer = new Timer(this._MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this._MaxMoveTimerCallback);
		}

		public void Create()
		{
			if (m_world != null)
				throw new Exception();

			this.LastSaveID = Guid.Empty;
			this.LastLoadID = Guid.Empty;

			m_world = new World(WorldTickMethod.Simultaneous);

			this.World.Initialize(InitializeWorld);
		}

		protected abstract void InitializeWorld();

		public Guid LastSaveID { get; private set; }
		public Guid LastLoadID { get; private set; }

		public void Save()
		{
			var id = Guid.NewGuid();

			var msg = new Dwarrowdelf.Messages.SaveClientDataRequestMessage() { ID = id };
			foreach (var p in m_players.Where(p => p.IsConnected && p.IsInGame))
				p.Send(msg);

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
					writer.WriteLine("\t{0}: {1}, {2}", p.UserID, p.IsConnected ? "connected" : "not connected",
						p.IsInGame ? "in game" : "not in game");
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
			var saveDir = Path.Combine(m_gameDir, id.ToString());

			if (!Directory.Exists(saveDir))
				throw new Exception();

			if (this.LastSaveID != id)
				throw new Exception();

			string saveFile = String.Format("client-{0}.json", userID, id);
			File.WriteAllText(Path.Combine(saveDir, saveFile), data);
		}

		public string LoadClientData(int userID, Guid id)
		{
			string saveFile = String.Format("client-{0}-{1}.json", userID, id);
			if (File.Exists(saveFile))
				return File.ReadAllText(Path.Combine(m_gameDir, saveFile));
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

		public void Run()
		{
			m_gameThread = Thread.CurrentThread;

			bool again = true;

			this.World.TurnStarting += OnTurnStart;
			this.World.TickEnded += OnTickEnded;

			while (m_exit == false)
			{
				if (!again)
					m_gameSignal.WaitOne();

				again = this.World.Work();
			}

			this.World.TickEnded -= OnTickEnded;
			this.World.TurnStarting -= OnTurnStart;
		}

		public void Stop()
		{
			m_exit = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}

		bool _IsTimeToStartTick()
		{
			// XXX check if this.UseMinTickTime && enough time passed

			if (m_config.RequirePlayer && m_playersConnected == 0)
				return false;

			if (m_config.RequirePlayerInGame && m_playersInGame == 0)
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

		void OnTurnStart(Living living)
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


		void AddPlayer(Player player)
		{
			VerifyAccess();
			m_players.Add(player);
			player.Init(this);
			player.PropertyChanged += OnPlayerPropertyChanged;
			player.ProceedTurnReceived += OnPlayerProceedTurnReceived;

			if (player.IsConnected)
				++m_playersConnected;
			if (player.IsInGame)
				++m_playersInGame;
		}

		void RemovePlayer(Player player)
		{
			VerifyAccess();
			bool ok = m_players.Remove(player);
			Debug.Assert(ok);

			player.ProceedTurnReceived -= OnPlayerProceedTurnReceived;
			player.PropertyChanged -= OnPlayerPropertyChanged;

			if (player.IsConnected)
				--m_playersConnected;
			if (player.IsInGame)
				--m_playersInGame;
		}

		void OnPlayerProceedTurnReceived(Player player)
		{
			if (m_world.TickMethod == WorldTickMethod.Simultaneous && m_players.Count > 0 && m_players.All(u => u.IsProceedTurnReceived))
			{
				this.World.SetProceedTurn();
				SignalWorld();
			}
		}

		void OnPlayerPropertyChanged(object ob, PropertyChangedEventArgs args)
		{
			var player = (Player)ob;

			if (args.PropertyName == "IsConnected")
			{
				if (player.IsConnected)
				{
					++m_playersConnected;
					CheckForStartTick();
				}
				else
				{
					--m_playersConnected;
				}

				Debug.Assert(m_playersConnected >= 0);
			}
			else if (args.PropertyName == "IsInGame")
			{
				if (player.IsInGame)
				{
					++m_playersInGame;
					CheckForStartTick();
				}
				else
				{
					--m_playersInGame;
				}

				Debug.Assert(m_playersInGame >= 0);
			}
			else
			{
				throw new Exception();
			}
		}

		public Player FindPlayer(int userID)
		{
			return m_players.SingleOrDefault(u => u.UserID == userID);
		}

		public Player CreatePlayer(int userID)
		{
			var player = FindPlayer(userID);

			if (player != null)
				throw new Exception();

			trace.TraceInformation("Creating new player {0}", userID);
			player = new Player(userID);

			AddPlayer(player);

			return player;
		}

		public abstract Living[] CreateControllables(Player player);

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

		[Serializable]
		class SaveData
		{
			public World World;
			public List<Player> Players;
			public Guid ID;
		}
	}
}
