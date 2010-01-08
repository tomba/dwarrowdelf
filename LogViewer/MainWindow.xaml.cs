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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.IO;
using MyGame.MemoryMappedLog;
using System.Globalization;

namespace LogViewer
{
	[Flags]
	public enum DebugFlag : int
	{
		None,
		Mark,
		Client,
		Server,
	}

	[ValueConversion(typeof(int), typeof(DebugFlag))]
	public class IntToDebugFlagConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (DebugFlag)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (DebugFlag)value;
		}
	}

	public partial class MainWindow : Window
	{
		ObservableCollection<LogEntry> m_debugCollection = new ObservableCollection<LogEntry>();
		public ObservableCollection<LogEntry> DebugEntries { get { return m_debugCollection; } }
		System.Windows.Threading.DispatcherTimer m_markTimer;
		StreamWriter m_logFile;
		bool m_scrollToEnd = true;
		int m_logIndex;

		public bool Halt { get; set; }

		public MainWindow()
		{
			m_markTimer = new System.Windows.Threading.DispatcherTimer();
			m_markTimer.Interval = TimeSpan.FromSeconds(4);
			m_markTimer.Tick += new EventHandler(MarkTimerTick);

			m_logFile = File.CreateText("test.log");
			
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			OnNewEntries2();

			MMLog.RegisterChangeCallback(OnNewEntries);
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.WindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);

			var p = Win32.Helpers.SaveWindowPlacement(this);
			Properties.Settings.Default.WindowPlacement = p;
			Properties.Settings.Default.Save();
		}

		void OnNewEntries()
		{
			this.Dispatcher.BeginInvoke(new Action(OnNewEntries2));
			MMLog.RegisterChangeCallback(OnNewEntries);
		}

		void OnNewEntries2()
		{
			if (this.Halt == true)
				return;

			var entries = MMLog.ReadNewEntries(m_logIndex, out m_logIndex);
			AddRange(entries);
		}

		void MarkTimerTick(object sender, EventArgs e)
		{
			m_markTimer.Stop();
			var entry = new LogEntry() { Message = "", Flags = (int)DebugFlag.Mark };
			m_debugCollection.Add(entry);

			while (m_debugCollection.Count > 500)
				m_debugCollection.RemoveAt(0);

			if (m_scrollToEnd)
				logListView.ScrollIntoView(entry);
		}

		public void AddRange(IEnumerable<LogEntry> entries)
		{
			m_markTimer.Stop();

			LogEntry last = null;

			foreach (var e in entries)
			{
				m_debugCollection.Add(e);
				last = e;

				m_logFile.WriteLine(String.Format("{0} | {1}: {2}", e.DateTime, e.Flags, e.Message));
			}

			m_logFile.Flush();

			while (m_debugCollection.Count > 500)
				m_debugCollection.RemoveAt(0);

			if (m_scrollToEnd && last != null)
				logListView.ScrollIntoView(last);

			m_markTimer.Start();
		}

		void OnClearClicked(object sender, RoutedEventArgs e)
		{
			m_debugCollection.Clear();
		}
	}
}
