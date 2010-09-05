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

			m_world.WorldChanged += HandleChanges;
		}

		public void Stop()
		{
			m_world.WorldChanged -= HandleChanges;

			m_writer.Close();
		}

		public void LogFullState()
		{
			m_world.EnterReadLock();
			m_writer.WriteLine("XXX full world state");
			m_world.ExitReadLock();
		}

		void HandleChanges(Change change)
		{
			m_writer.WriteLine(change);
			m_writer.Flush();
		}
	}
}
