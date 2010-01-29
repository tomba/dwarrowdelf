using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Runtime.InteropServices;
using System.IO;

namespace MyGame.MemoryMappedLog
{
	public class LogEntry
	{
		public DateTime DateTime { get; set; }
		public string Component { get; set; }
		public string Thread { get; set; }
		public string Message { get; set; }

		public LogEntry(DateTime dateTime, string component = "", string thread = "", string message = "")
		{
			this.DateTime = dateTime;
			this.Component = component;
			this.Thread = thread;
			this.Message = message;
		}

		public LogEntry(BinaryReader reader)
		{
			this.DateTime = DateTime.FromBinary(reader.ReadInt64());
			this.Component = reader.ReadString();
			this.Thread = reader.ReadString();
			this.Message = reader.ReadString();
		}

		public static int Write(BinaryWriter writer, DateTime dateTime, string component, string thread, string message)
		{
			writer.Write(dateTime.ToBinary());
			writer.Write(component);
			writer.Write(thread);
			writer.Write(message);
			writer.Flush();
			return (int)writer.BaseStream.Position;
		}
	}

	public static class MMLog
	{
		struct LogHeader
		{
			public int CurrentIndex;
		}

		struct EntryHeader
		{
			public int PayloadLength;
		}

		const int EntrySize = 2048;
		const int EntryCount = 1024;

		static readonly int s_logHeaderSize;
		static readonly int s_entryHeaderSize;
		static readonly int s_maxPayloadSize;

		static MemoryMappedFile s_mmf;
		static MemoryMappedViewAccessor s_view;
		static ASCIIEncoding s_encoding;
		static EventWaitHandle s_writeEventHandle;
		static Mutex s_indexMutex;

		static MMLog()
		{
			s_logHeaderSize = Marshal.SizeOf(typeof(LogHeader));
			s_entryHeaderSize = Marshal.SizeOf(typeof(EntryHeader));
			s_maxPayloadSize = EntrySize - s_entryHeaderSize;

			s_mmf = MemoryMappedFile.CreateOrOpen("MMLog.File", s_logHeaderSize + EntryCount * EntrySize);
			s_view = s_mmf.CreateViewAccessor(0, 0);
			s_encoding = new ASCIIEncoding();
			s_writeEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "MMLog.WaitHandle");
			s_indexMutex = new Mutex(false, "MMLog.Mutex");
		}

		public static void Append(string component, string thread, string message)
		{
			var buffer = new byte[s_maxPayloadSize];
			int len;

			using (var w = new BinaryWriter(new MemoryStream(buffer)))
			{
				len = LogEntry.Write(w, DateTime.Now, component, thread, message);
			}

			int idx = IncrementCurrentIndex();
			WriteArray(idx, buffer, len);
		}

		public static LogEntry[] ReadNewEntries(int sinceIdx, out int newIdx)
		{
			newIdx = GetCurrentIndex();
			var idx = sinceIdx;
			//Console.WriteLine("old {0}, new {1}", idx, newIdx);

			var count = newIdx - idx;
			if (count < 0)
				count += MemoryMappedLog.MMLog.EntryCount;

			//Console.WriteLine("count {0}", count);

			LogEntry[] entries = new LogEntry[count];

			for (int i = 0; i < count; ++i)
			{
				var entry = ReadEntry(idx);

				//Console.WriteLine("{0,-5} {1,5}/{2,-5} : {3}, {4}, {5}", idx, i, count - 1, entry.DateTime.Ticks, entry.Flags, entry.Text);

				entries[i] = entry;

				idx = (idx + 1) % MemoryMappedLog.MMLog.EntryCount;
			}

			return entries;
		}

		static LogEntry ReadEntry(int entryIndex)
		{
			var buffer = new byte[s_maxPayloadSize];
			int len = ReadArray(entryIndex, buffer);

			using (var r = new BinaryReader(new MemoryStream(buffer)))
			{
				var entry = new LogEntry(r);
				return entry;
			}
		}

		static int GetCurrentIndex()
		{
			s_indexMutex.WaitOne();
			LogHeader header;
			s_view.Read<LogHeader>(0, out header);
			s_indexMutex.ReleaseMutex();
			return header.CurrentIndex;
		}

		static int IncrementCurrentIndex()
		{
			s_indexMutex.WaitOne();
			LogHeader header;
			s_view.Read<LogHeader>(0, out header);
			var oldIdx = header.CurrentIndex;
			var newIdx = oldIdx + 1;
			if (newIdx == EntryCount)
				newIdx = 0;
			header.CurrentIndex = newIdx;
			s_view.Write<LogHeader>(0, ref header);
			s_indexMutex.ReleaseMutex();
			return oldIdx;
		}

		static int GetEntryOffset(int entryIndex)
		{
			return s_logHeaderSize + entryIndex * EntrySize;
		}

		static void WriteArray(int entryIndex, byte[] array, int len)
		{
			if (len > s_maxPayloadSize)
				throw new Exception();

			EntryHeader header;
			header.PayloadLength = len;

			int offset = GetEntryOffset(entryIndex);
			s_view.Write<EntryHeader>(offset, ref header);
			s_view.WriteArray<byte>(offset + s_entryHeaderSize, array, 0, len);
			s_writeEventHandle.Set();
		}

		static int ReadArray(int entryIndex, byte[] array)
		{
			if (array.Length < s_maxPayloadSize)
				throw new Exception();

			int offset = GetEntryOffset(entryIndex);

			EntryHeader header;
			s_view.Read<EntryHeader>(offset, out header);

			int l = s_view.ReadArray<byte>(offset + s_entryHeaderSize, array, 0, header.PayloadLength);
			if (l != header.PayloadLength)
				throw new Exception();

			return l;
		}

		public static void RegisterChangeCallback(Action callback)
		{
			ThreadPool.RegisterWaitForSingleObject(s_writeEventHandle, (a, b) => callback(), null, -1, true);
		}
	}
}
