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
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace MemoryMappedLog
{

	public partial class RulesWindow : Window
	{
		static Dictionary<Color, string> s_colorMap = new Dictionary<Color, string>();
		static RulesWindow()
		{
			foreach (var prop in typeof(Colors).GetProperties())
			{
				if (prop.PropertyType != typeof(Color))
					continue;

				string name = prop.Name;
				var color = (Color)prop.GetValue(null, null);
				s_colorMap[color] = name;
			}
		}

		public class StrLogRule
		{
			public string Component { get; set; }
			public string Thread { get; set; }
			public string Message { get; set; }
			public string Color { get; set; }
			public bool Gag { get; set; }
		}

		public ObservableCollection<StrLogRule> LogRules { get; private set; }
		public List<LogRule> ResultLogRules { get; private set; }

		public RulesWindow(List<LogRule> list)
		{
			this.LogRules = new ObservableCollection<StrLogRule>();

			foreach (var r in list)
			{
				string color;
				if (s_colorMap.ContainsKey(r.Brush.Color))
					color = s_colorMap[r.Brush.Color];
				else
					color = r.Brush.Color.ToString();

				this.LogRules.Add(new StrLogRule()
				{
					Component = r.Component != null ? r.Component.ToString() : null,
					Thread = r.Thread != null ? r.Thread.ToString() : null,
					Message = r.Message != null ? r.Message.ToString() : null,
					Color = color,
					Gag = r.Gag,
				});
			}

			InitializeComponent();
		}

		void OnOkClicked(object sender, RoutedEventArgs e)
		{
			List<LogRule> list = new List<LogRule>();

			try
			{
				foreach (var rule in this.LogRules)
				{
					if (rule.Color == null || rule.Color.Length == 0)
						rule.Color = "White";

					var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(rule.Color));
					brush.Freeze();

					list.Add(new LogRule()
					{
						Component = (rule.Component != null && rule.Component.Length > 0) ? new Regex(rule.Component) : null,
						Thread = (rule.Thread != null && rule.Thread.Length > 0) ? new Regex(rule.Thread) : null,
						Message = (rule.Message != null && rule.Message.Length > 0) ? new Regex(rule.Message) : null,
						Brush = brush,
						Gag = rule.Gag,
					});
				}
			}
			catch (Exception exc)
			{
				MessageBox.Show(exc.Message, "Failed to parse values");
				return;
			}

			this.ResultLogRules = list;
			this.DialogResult = true;
		}

		void OnRemoveClicked(object sender, RoutedEventArgs e)
		{
			if (ruleGrid.SelectedItem == null)
				return;

			var rule = ruleGrid.SelectedItem as StrLogRule;

			if (rule == null)
				return;

			this.LogRules.Remove(rule);
		}

		void OnUpClicked(object sender, RoutedEventArgs e)
		{
			if (ruleGrid.SelectedItem == null)
				return;

			var rule = ruleGrid.SelectedItem as StrLogRule;

			if (rule == null)
				return;

			var idx = this.LogRules.IndexOf(rule);

			if (idx > 0)
				this.LogRules.Move(idx, idx - 1);
		}

		void OnDownClicked(object sender, RoutedEventArgs e)
		{
			if (ruleGrid.SelectedItem == null)
				return;

			var rule = ruleGrid.SelectedItem as StrLogRule;

			if (rule == null)
				return;

			var idx = this.LogRules.IndexOf(rule);

			if (idx < this.LogRules.Count - 1)
				this.LogRules.Move(idx, idx + 1);
		}
	}
}
