using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Dwarrowdelf.Server
{
	public abstract class GameEngine
	{
		string m_gameDir;
		World m_world;

		volatile bool m_exit = false;
		AutoResetEvent m_gameSignal = new AutoResetEvent(true);

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

		[GameProperty]
		GameConfig m_config = new GameConfig
		{
			RequireUser = true,
			RequireControllables = false,
			MaxMoveTime = TimeSpan.Zero,
			MinTickTime = TimeSpan.FromMilliseconds(50),
		};

		bool UseMinTickTime { get { return m_config.MinTickTime != TimeSpan.Zero; } }

		/// <summary>
		/// Timer is used to start the tick after MinTickTime
		/// </summary>
		Timer m_minTickTimer;


		bool UseMaxMoveTime { get { return m_config.MaxMoveTime != TimeSpan.Zero; } }

		/// <summary>
		/// Timer is used to timeout player turn after MaxMoveTime
		/// </summary>
		Timer m_maxMoveTimer;

		public World World { get { return m_world; } }

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

		public void Stop()
		{
			m_exit = true;
			Thread.MemoryBarrier();
			SignalWorld();
		}

		public void Run()
		{
			bool again = true;

			this.World.TurnStartEvent += OnTurnStart;
			this.World.TickEnded += OnTickEnded;

			while (m_exit == false)
			{
				if (!again)
					m_gameSignal.WaitOne();
				again = this.World.Work();
			}

			this.World.TickEnded -= OnTickEnded;
			this.World.TurnStartEvent -= OnTurnStart;
		}

		void OnTickEnded()
		{
			if (this.UseMinTickTime)
			{
				m_minTickTimer.Change(m_config.MinTickTime, TimeSpan.FromMilliseconds(-1));
			}
			else
			{
				this.World.SetOkToStartTick();
			}
		}

		void MinTickTimerCallback(object stateInfo)
		{
			//trace.TraceVerbose("MinTickTimerCallback");
			this.World.SetOkToStartTick();
			SignalWorld();
		}

		void OnTurnStart(Living living)
		{
			if (this.UseMaxMoveTime)
				m_maxMoveTimer.Change(m_config.MaxMoveTime, TimeSpan.FromMilliseconds(-1));
		}

		void MaxMoveTimerCallback(object stateInfo)
		{
			//trace.TraceVerbose("MaxMoveTimerCallback");
			this.World.SetForceMove();
			SignalWorld();
		}

		public void SignalWorld()
		{
			m_gameSignal.Set();
		}

		public void SetMinTickTime(TimeSpan minTickTime)
		{
			m_config.MinTickTime = minTickTime;
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
