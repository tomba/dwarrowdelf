using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Runtime.InteropServices;

namespace MemoryMappedLog
{
	public class LogEntry
	{
		public DateTime DateTime { get; set; }
		public int Flags { get; set; }
		public string Message { get; set; }
	}

	public static class MMLog
	{
		struct LogHeader
		{
			public int CurrentIndex;
		}

		struct EntryHeader
		{
			public DateTime DateTime;
			public int Flags;
			public int TextLength;
		}

		const int EntrySize = 512;
		const int EntryCount = 1024;

		static int s_logHeaderSize;
		static int s_entryHeaderSize;
		static int s_maxTextSize;

		static MemoryMappedFile s_mmf;
		static MemoryMappedViewAccessor s_view;
		static ASCIIEncoding s_encoding;
		static EventWaitHandle s_writeEventHandle;
		static Mutex s_indexMutex;

		static MMLog()
		{
			s_logHeaderSize = Marshal.SizeOf(typeof(LogHeader));
			s_entryHeaderSize = Marshal.SizeOf(typeof(EntryHeader));
			s_maxTextSize = EntrySize - s_entryHeaderSize;

			s_mmf = MemoryMappedFile.CreateOrOpen("MMLog.File", s_logHeaderSize + EntryCount * EntrySize);
			s_view = s_mmf.CreateViewAccessor(0, 0);
			s_encoding = new ASCIIEncoding();
			s_writeEventHandle = new EventWaitHandle(false, EventResetMode.AutoReset, "MMLog.WaitHandle");
			s_indexMutex = new Mutex(false, "MMLog.Mutex");
		}

		public static void Append(int flags, string str)
		{
			int idx = IncrementCurrentIndex();
			WriteEntry(idx, flags, str);
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

		static void WriteArray(int entryIndex, byte[] arr, int len)
		{
			if (len > EntrySize)
				throw new Exception();

			int offset = GetEntryOffset(entryIndex);
			s_view.WriteArray<byte>(offset, arr, 0, len);
			s_writeEventHandle.Set();
		}

		static void WriteEntry(int entryIndex, int flags, string str)
		{
			var buf = s_encoding.GetBytes(str);
			var bufLen = Math.Min(buf.Length, s_maxTextSize);

			EntryHeader header;
			header.DateTime = DateTime.Now;
			header.Flags = flags;
			header.TextLength = bufLen;

			int offset = GetEntryOffset(entryIndex);
			s_view.Write<EntryHeader>(offset, ref header);
			s_view.WriteArray<byte>(offset + s_entryHeaderSize, buf, 0, bufLen);
			s_writeEventHandle.Set();
		}

		static LogEntry ReadEntry(int entryIndex)
		{
			int offset = GetEntryOffset(entryIndex);

			var entry = new LogEntry();

			EntryHeader header;
			s_view.Read<EntryHeader>(offset, out header);

			byte[] buf = new byte[header.TextLength];
			int l = s_view.ReadArray<byte>(offset + s_entryHeaderSize, buf, 0, header.TextLength);
			if (l != header.TextLength)
				throw new Exception();
			var str = s_encoding.GetString(buf);

			entry.DateTime = header.DateTime;
			entry.Flags = header.Flags;
			entry.Message = str;

			return entry;
		}

		public static void RegisterChangeCallback(Action callback)
		{
			ThreadPool.RegisterWaitForSingleObject(s_writeEventHandle, (a, b) => callback(), null, -1, true);
		}
	}
}
