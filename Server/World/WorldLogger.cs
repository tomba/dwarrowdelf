using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Dwarrowdelf.Server
{
	public class WorldLogger
	{
		World m_world;
		TextWriter m_writer;

		public WorldLogger()
		{
		}

		public void Start(World world, string path)
		{
			m_world = world;
			var dir = Path.GetDirectoryName(path);

			var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
			m_writer = new StreamWriter(stream);

			m_world.WorldChanged += HandleChanges;
		}

		public void Stop()
		{
			m_world.WorldChanged -= HandleChanges;

			m_writer.Close();
			m_writer = null;
			m_world = null;
		}

		void HandleChanges(Change change)
		{
			//m_writer.WriteLine(change);
			//m_writer.Flush();
		}
	}
}
