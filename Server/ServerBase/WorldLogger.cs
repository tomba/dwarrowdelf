using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MyGame.Server
{
	class WorldLogger
	{
		World m_world;
		TextWriter m_writer;

		public WorldLogger(World world)
		{
			m_world = world;
		}

		public void Start()
		{
			var stream = File.Open("world.log", FileMode.Create, FileAccess.Write, FileShare.Read);
			m_writer = new StreamWriter(stream);

			m_world.HandleEndOfTurn += HandleEndOfTurn;
		}

		public void Stop()
		{
			m_world.HandleEndOfTurn -= HandleEndOfTurn;

			m_writer.Close();
		}

		public void LogFullState()
		{
			m_world.EnterReadLock();
			m_writer.WriteLine("XXX full world state");
			m_world.ExitReadLock();
		}

		void HandleEndOfTurn(IEnumerable<Change> changes)
		{
			foreach (var c in changes)
				m_writer.WriteLine(c);

			m_writer.Flush();
		}
	}
}
