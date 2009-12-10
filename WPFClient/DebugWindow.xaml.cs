using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace MyGame
{
	public class DebugEntry
	{
		static Stopwatch m_watch;

		static DebugEntry()
		{
			m_watch = new Stopwatch();
			m_watch.Start();
		}

		public DebugEntry(string msg, DebugFlag flags)
		{
			this.Message = msg;
			this.Flags = flags;
			this.Time = m_watch.Elapsed;
		}

		public TimeSpan Time { get; private set; }
		public DebugFlag Flags { get; set; }
		public string Message { get; private set; }
	}

	public partial class DebugWindow : Window
	{
		ObservableCollection<DebugEntry> m_debugCollection = new ObservableCollection<DebugEntry>();
		System.Windows.Threading.DispatcherTimer m_markTimer;
		StreamWriter m_logFile;
		bool m_scrollToEnd = true;

		public DebugWindow()
		{
			m_markTimer = new System.Windows.Threading.DispatcherTimer();
			m_markTimer.Interval = TimeSpan.FromSeconds(4);
			m_markTimer.Tick += new EventHandler(MarkTimerTick);

			m_logFile = File.CreateText("test.log");

			InitializeComponent();
		}

		void MarkTimerTick(object sender, EventArgs e)
		{
			m_markTimer.Stop();
			var entry = new DebugEntry("", DebugFlag.Mark);
			m_debugCollection.Add(entry);

			while (m_debugCollection.Count > 500)
				m_debugCollection.RemoveAt(0);

			if (m_scrollToEnd)
				logListView.ScrollIntoView(entry);
		}

		public ObservableCollection<DebugEntry> DebugEntries { get { return m_debugCollection; } }

		public void AddRange(IEnumerable<DebugEntry> entries)
		{
			m_markTimer.Stop();

			DebugEntry last = null;

			foreach (var e in entries)
			{
				m_debugCollection.Add(e);
				last = e;

				m_logFile.WriteLine(String.Format("{0} | {1}: {2}", e.Time, e.Flags, e.Message));
			}

			m_logFile.Flush();

			while (m_debugCollection.Count > 500)
				m_debugCollection.RemoveAt(0);

			if (m_scrollToEnd && last != null)
				logListView.ScrollIntoView(last);

			m_markTimer.Start();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			Debug.Assert(GameData.Data.MyTraceListener == null);

			GameData.Data.MyTraceListener = new ClientDebugListener();
			if (MyGame.Properties.Settings.Default.DebugClient)
				MyDebug.Listener = GameData.Data.MyTraceListener;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (WindowPlacement)Properties.Settings.Default.DebugWindowPlacement;
			if (p != null)
				Win32.LoadWindowPlacement(this, p);
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);

			if (MyGame.Properties.Settings.Default.DebugClient)
				MyDebug.Listener = null;
			if (App.Current.Server != null)
				App.Current.Server.TraceListener = null;
			GameData.Data.MyTraceListener = null;

			var p = Win32.SaveWindowPlacement(this);
			Properties.Settings.Default.DebugWindowPlacement = p;
			Properties.Settings.Default.Save();
		}
	}
}
