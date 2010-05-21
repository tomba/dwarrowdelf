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
using System.Text.RegularExpressions;

namespace LogViewer
{
	public partial class MainWindow : Window
	{
		public List<LogRule> LogRules { get; private set; }

		ObservableCollection<LogEntry> m_debugCollection = new ObservableCollection<LogEntry>();
		public ObservableCollection<LogEntry> DebugEntries { get { return m_debugCollection; } }
		StreamWriter m_logFile;
		bool m_scrollToEnd = true;
		int m_logIndex;
		DateTime m_lastDateTime;

		public bool Halt { get; set; }

		string m_showOnlyString;
		public Regex m_showOnlyRegex;
		public string ShowOnly
		{
			get
			{
				return m_showOnlyString;
			}

			set
			{
				m_showOnlyString = value;

				if (m_showOnlyString == null || m_showOnlyString.Length == 0)
					m_showOnlyRegex = null;
				else
					m_showOnlyRegex = new Regex(value, RegexOptions.IgnoreCase);
				filterTextBox.Tag = m_showOnlyRegex;

				var dataView = CollectionViewSource.GetDefaultView(logListView.ItemsSource);
				dataView.Refresh();
			}
		}

		public MainWindow()
		{
			LogRules = new List<LogRule>();
			LogRules.Add(new LogRule() { Message = new Regex("^Start$"), Brush = Brushes.LightGreen });
			LogRules.Add(new LogRule() { Message = new Regex("^-- Tick .* started --$"), Brush = Brushes.LightGreen });
			LogRules.Add(new LogRule() { Component = new Regex("^Server$"), Brush = Brushes.LightGray });
			LogRules.Add(new LogRule() { Component = new Regex("^Mark$"), Brush = Brushes.Blue });

			m_logFile = File.CreateText("test.log");

			m_lastDateTime = new DateTime(0);

			InitializeComponent();

			var cmd = new RoutedUICommand("filter", "filter", typeof(MainWindow),
				new InputGestureCollection() { new KeyGesture(Key.F, ModifierKeys.Control) }
				);
			var binding = new CommandBinding(cmd);
			binding.Executed += new ExecutedRoutedEventHandler(binding_Executed);
			CommandBindings.Add(binding);
		}

		void binding_Executed(object sender, ExecutedRoutedEventArgs e)
		{
			filterTextBox.Focus();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var conv = (LogEntryToBgBrushConverter)this.Resources["bgConverter"];
			conv.Wnd = this;

			var dataView = CollectionViewSource.GetDefaultView(logListView.ItemsSource);

			dataView.Filter = DataFilter;

			OnNewEntries();

			MMLog.RegisterChangeCallback(OnNewEntriesSafe);
		}

		bool DataFilter(object item)
		{
			var entry = (LogEntry)item;

			if (m_showOnlyRegex != null)
			{
				return m_showOnlyRegex.IsMatch(entry.Message);
			}

			foreach (var rule in LogRules)
			{
				bool match =
					(rule.Component == null || rule.Component.IsMatch(entry.Component)) &&
					(rule.Thread == null || rule.Thread.IsMatch(entry.Thread)) &&
					(rule.Message == null || rule.Message.IsMatch(entry.Message));

				if (match)
					return !rule.Gag;
			}

			return true;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			base.OnSourceInitialized(e);

			var p = (Win32.WindowPlacement)Properties.Settings.Default.WindowPlacement;
			if (p != null)
				Win32.Helpers.LoadWindowPlacement(this, p);

			ListView l = logListView;
			GridView g = l.View as GridView;
			double total = 0;
			for (int i = 0; i < g.Columns.Count - 1; i++)
			{
				total += g.Columns[i].ActualWidth;
			}

			g.Columns[g.Columns.Count - 1].Width = l.ActualWidth - total;
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);

			var p = Win32.Helpers.SaveWindowPlacement(this);
			Properties.Settings.Default.WindowPlacement = p;
			Properties.Settings.Default.Save();
		}

		void OnNewEntriesSafe()
		{
			this.Dispatcher.BeginInvoke(new Action(OnNewEntries));
			MMLog.RegisterChangeCallback(OnNewEntriesSafe);
		}

		TimeSpan GetTimeSpan(DateTime entryDateTime)
		{
			var ldt = m_lastDateTime;
			m_lastDateTime = entryDateTime;
			return ldt.Ticks != 0 ? entryDateTime - ldt : TimeSpan.FromTicks(0);
		}

		void OnNewEntries()
		{
			if (this.Halt == true)
				return;

			var entries = MMLog.ReadNewEntries(m_logIndex, out m_logIndex);
			AddRange(entries);
		}

		public void Add(LogEntry entry)
		{
			m_debugCollection.Add(entry);

			while (m_debugCollection.Count > 500)
				m_debugCollection.RemoveAt(0);

			if (m_scrollToEnd)
				logListView.ScrollIntoView(entry);
		}

		public void AddRange(IEnumerable<LogEntry> entries)
		{
			LogEntry last = null;

			foreach (var e in entries)
			{
				m_debugCollection.Add(e);
				last = e;

				m_logFile.WriteLine(String.Format("{0} | {1}: {2}", e.DateTime, e.Component, e.Message));
			}

			m_logFile.Flush();

			while (m_debugCollection.Count > 500)
				m_debugCollection.RemoveAt(0);

			if (m_scrollToEnd && last != null)
				logListView.ScrollIntoView(last);
		}

		void OnClearClicked(object sender, RoutedEventArgs e)
		{
			m_debugCollection.Clear();
		}

		void OnMarkClicked(object sender, RoutedEventArgs e)
		{
			var entry = new LogEntry(DateTime.Now, component: "Mark");
			Add(entry);
		}

		void OnRulesClicked(object sender, RoutedEventArgs e)
		{
			var wnd = new RulesWindow(LogRules);
			wnd.Owner = this;
			var res = wnd.ShowDialog();
			if (res == true)
			{
				LogRules = wnd.ResultLogRules;

				var dataView = CollectionViewSource.GetDefaultView(logListView.ItemsSource);
				dataView.Refresh();
			}
		}

		void OnClearFilterClicked(object sender, RoutedEventArgs e)
		{
			filterTextBox.Text = null;
		}

		private void filterTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var tb = (TextBox)sender;

			if (m_showOnlyString == null)
				tb.Text = "";
			else
				tb.SelectAll();
		}

		private void filterTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			var tb = (TextBox)sender;

			if (m_showOnlyString.Length == 0)
				tb.Text = null;
		}

		private void filterTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Enter)
			{
				logListView.Focus();
				var last = m_debugCollection.LastOrDefault();
				if (last != null)
					logListView.ScrollIntoView(last);
			}
		}
	}

	public class LogRule
	{
		public Regex Component { get; set; }
		public Regex Thread { get; set; }
		public Regex Message { get; set; }
		public SolidColorBrush Brush { get; set; }
		public bool Gag { get; set; }
	}

	class RegexValidationRule : ValidationRule
	{
		public override ValidationResult Validate(object value, CultureInfo cultureInfo)
		{
			if (value == null)
				return ValidationResult.ValidResult;

			try
			{
				new Regex((string)value, RegexOptions.IgnoreCase);
			}
			catch (ArgumentException)
			{
				return new ValidationResult(false, "Invalid regexp pattern");
			}

			return ValidationResult.ValidResult;
		}
	}

	[ValueConversion(typeof(LogEntry), typeof(Brush))]
	class LogEntryToBgBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var entry = (LogEntry)value;

			foreach (var rule in Wnd.LogRules)
			{
				bool match =
					(rule.Component == null || rule.Component.IsMatch(entry.Component)) &&
					(rule.Thread == null || rule.Thread.IsMatch(entry.Thread)) &&
					(rule.Message == null || rule.Message.IsMatch(entry.Message));

				if (match)
					return rule.Brush;
			}

			return null;
		}

		public MainWindow Wnd { get; set; }

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
