using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace Dwarrowdelf
{
	// XXX mvoe to server
	public sealed class SaveManager
	{
		public string GameDir { get; private set; }

		List<SaveEntry> m_entries = new List<SaveEntry>();

		public SaveManager(string gameDir)
		{
			this.GameDir = gameDir;

			if (!Directory.Exists(gameDir))
				Directory.CreateDirectory(gameDir);

			foreach (var dir in Directory.EnumerateDirectories(gameDir))
			{
				try
				{
					var idStr = Path.GetFileName(dir);
					var datetimeStr = File.ReadAllText(Path.Combine(dir, "TIMESTAMP"));
					var tickStr = File.ReadAllText(Path.Combine(dir, "TICK"));

					var id = Guid.Parse(idStr);
					var dateTime = DateTime.ParseExact(datetimeStr, "u", CultureInfo.InvariantCulture);
					var tick = int.Parse(tickStr);

					var entry = new SaveEntry(id, dateTime, tick);

					m_entries.Add(entry);
				}
				catch (Exception)
				{
					Trace.TraceError("Broken save dir {0}", dir);
					continue;
				}
			}
		}

		public void DeleteAll()
		{
			var dirs = Directory.EnumerateDirectories(this.GameDir);
			foreach (var dir in dirs)
				Directory.Delete(dir, true);

			m_entries.Clear();
		}

		public Guid GetLatestSaveFile()
		{
			if (m_entries.Count == 0)
				return Guid.Empty;

			var arr = m_entries.ToArray();
			Array.Sort(arr, (e1, e2) => -DateTime.Compare(e1.DateTime, e2.DateTime));
			return arr[0].ID;
		}
	}

	sealed class SaveEntry
	{
		public SaveEntry(Guid id, DateTime dateTime, int tick)
		{
			this.ID = id;
			this.DateTime = dateTime;
			this.Tick = tick;
		}

		public Guid ID { get; private set; }
		public DateTime DateTime { get; private set; }
		public int Tick { get; private set; }
	}
}
