using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

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

		List<Player> m_players = new List<Player>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "Engine");

		[GameProperty]
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

			m_minTickTimer = new Timer(this.MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this.MaxMoveTimerCallback);
		}

		public void Create()
		{
			if (m_world != null)
				throw new Exception();

			m_world = new World(WorldTickMethod.Simultaneous);

			this.World.Initialize(InitializeWorld);
		}

		protected abstract void InitializeWorld();

		public void Load(string saveFile)
		{
			if (m_world != null)
				throw new Exception();

			var saveData = LoadWorld(Path.Combine(m_gameDir, saveFile));

			m_world = saveData.World;
			m_players = saveData.Players;
			foreach (var p in m_players)
				p.Init(this);
		}

		void VerifyAccess()
		{
			if (Thread.CurrentThread != m_gameThread)
				throw new Exception();
		}

		public void Run()
		{
			m_gameThread = Thread.CurrentThread;

			bool again = true;

			this.World.TurnStartEvent += OnTurnStart;
			this.World.TickEnded += OnTickEnded;
			this.World.TickOngoingEvent += OnTickOnGoing;

			while (m_exit == false)
			{
				if (!again)
					m_gameSignal.WaitOne();

				again = this.World.Work();
			}

			this.World.TickOngoingEvent -= OnTickOnGoing;
			this.World.TickEnded -= OnTickEnded;
			this.World.TurnStartEvent -= OnTurnStart;
		}

		public void Stop()
		{
			m_exit = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}

		void OnTickOnGoing()
		{
			// XXX We should catch ProceedTurnReceived directly, and do this there
			if (m_world.TickMethod == WorldTickMethod.Simultaneous && m_players.Count > 0 && m_players.All(u => u.ProceedTurnReceived))
				this.World.SetForceMove();
		}

		bool _IsTimeToStartTick()
		{
			if (m_config.RequirePlayer)
			{
				if (m_players.Count == 0 || m_players.All(p => p.IsConnected == false))
					return false;
			}

			if (m_config.RequirePlayerInGame)
			{
				if (!m_players.Any(u => u.IsPlayerInGame))
					return false;
			}

			return true;
		}

		bool IsTimeToStartTick()
		{
			bool r = _IsTimeToStartTick();
			trace.TraceVerbose("IsTimeToStartTick = {0}", r);
			return r;
		}

		public void CheckForStartTick()
		{
			// XXX feels like a hack
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

		void MinTickTimerCallback(object stateInfo)
		{
			trace.TraceVerbose("MinTickTimerCallback");
			if (IsTimeToStartTick())
			{
				this.World.SetOkToStartTick();
				SignalWorld();
			}
		}

		void OnTurnStart(Living living)
		{
			if (this.UseMaxMoveTime)
				m_maxMoveTimer.Change(m_config.MaxMoveTime, TimeSpan.FromMilliseconds(-1));
		}

		void MaxMoveTimerCallback(object stateInfo)
		{
			trace.TraceVerbose("MaxMoveTimerCallback");
			this.World.SetForceMove();
			SignalWorld();
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


		// XXX
		void AddPlayer(Player player)
		{
			VerifyAccess();
			m_players.Add(player);
		}

		void RemovePlayer(Player player)
		{
			VerifyAccess();
			bool ok = m_players.Remove(player);
			Debug.Assert(ok);
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
			player.Init(this);

			AddPlayer(player);

			return player;
		}

		public abstract Living[] CreateControllables(Player player);

		public void Save()
		{
			int tick = m_world.TickNumber;
			string name = String.Format("save-{0}.json", tick);
			Save(name);
		}

		public void Save(string saveFile)
		{
			var saveData = new SaveData()
			{
				World = this.World,
				Players = m_players,
			};

			SaveWorld(saveData, Path.Combine(m_gameDir, saveFile));
		}

		static void SaveWorld(SaveData saveData, string savePath)
		{
			Trace.TraceInformation("Saving game {0}", savePath);
			var watch = Stopwatch.StartNew();


			var stream = new System.IO.MemoryStream();

			var serializer = new Dwarrowdelf.JsonSerializer(stream);
			serializer.Serialize(saveData);

			stream.Position = 0;
			//stream.CopyTo(Console.OpenStandardOutput());

			stream.Position = 0;
			using (var file = File.Create(savePath))
				stream.WriteTo(file);

			watch.Stop();
			Trace.TraceInformation("Saving game took {0}", watch.Elapsed);
		}

		static SaveData LoadWorld(string savePath)
		{
			Trace.TraceInformation("Loading game {0}", savePath);
			var watch = Stopwatch.StartNew();

			SaveData world;

			var stream = File.OpenRead(savePath);
			var deserializer = new Dwarrowdelf.JsonDeserializer(stream);
			world = deserializer.Deserialize<SaveData>();

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);

			return world;
		}

		[Serializable]
		class SaveData
		{
			public World World;
			public List<Player> Players;
		}
	}
}
