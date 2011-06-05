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
		// Require an user to be in game for ticks to proceed
		public bool RequireUser;

		// Require an controllables to be in game for ticks to proceed
		public bool RequireControllables;

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

		List<ServerUser> m_users = new List<ServerUser>();

		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Server.World", "Engine");

		[GameProperty]
		GameConfig m_config = new GameConfig
		{
			RequireUser = true,
			RequireControllables = false,
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
			m_world = new World(WorldTickMethod.Simultaneous);

			Init();
		}

		protected GameEngine(string gameDir, string saveFile)
		{
			m_gameDir = gameDir;
			m_world = LoadWorld(Path.Combine(m_gameDir, saveFile));

			Init();
		}

		void Init()
		{
			m_minTickTimer = new Timer(this.MinTickTimerCallback);
			m_maxMoveTimer = new Timer(this.MaxMoveTimerCallback);
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
			if (m_world.TickMethod == WorldTickMethod.Simultaneous && m_users.Count > 0 && m_users.All(u => u.ProceedTurnReceived))
				this.World.SetForceMove();
		}

		bool _IsTimeToStartTick()
		{
			if (m_config.RequireUser && m_users.Count == 0)
				return false;

			if (m_config.RequireControllables && !m_users.Any(u => u.Controllables.Count > 0))
				return false;

			return true;
		}

		bool IsTimeToStartTick()
		{
			bool r = _IsTimeToStartTick();
			trace.TraceVerbose("IsTimeToStartTick = {0}", r);
			return r;

		}

		void OnTickEnded()
		{
			if (this.UseMinTickTime)
			{
				m_minTickTimer.Change(m_config.MinTickTime, TimeSpan.FromMilliseconds(-1));
			}
			else
			{
				if (IsTimeToStartTick())
					this.World.SetOkToStartTick();
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


		public void AddUser(ServerUser user)
		{
			VerifyAccess();
			m_users.Add(user);

			if (IsTimeToStartTick())
				this.World.SetOkToStartTick();
		}

		public void RemoveUser(ServerUser user)
		{
			VerifyAccess();
			bool ok = m_users.Remove(user);
			Debug.Assert(ok);
		}

		public abstract ServerUser CreateUser(int userID);

		public void Save()
		{
			int tick = m_world.TickNumber;
			string name = String.Format("save-{0}.json", tick);
			Save(name);
		}

		public void Save(string saveFile)
		{
			SaveWorld(m_world, Path.Combine(m_gameDir, saveFile));
		}

		static void SaveWorld(World world, string savePath)
		{
			Trace.TraceInformation("Saving game {0}", savePath);
			var watch = Stopwatch.StartNew();


			var stream = new System.IO.MemoryStream();

			var serializer = new Dwarrowdelf.JsonSerializer(stream);
			serializer.Serialize(world);

			stream.Position = 0;
			//stream.CopyTo(Console.OpenStandardOutput());

			stream.Position = 0;
			using (var file = File.Create(savePath))
				stream.WriteTo(file);

			watch.Stop();
			Trace.TraceInformation("Saving game took {0}", watch.Elapsed);
		}

		static World LoadWorld(string savePath)
		{
			Trace.TraceInformation("Loading game {0}", savePath);
			var watch = Stopwatch.StartNew();

			World world;

			var stream = File.OpenRead(savePath);
			var deserializer = new Dwarrowdelf.JsonDeserializer(stream);
			world = (World)deserializer.Deserialize<World>();

			watch.Stop();
			Trace.TraceInformation("Loading game took {0}", watch.Elapsed);

			return world;
		}
	}
}
