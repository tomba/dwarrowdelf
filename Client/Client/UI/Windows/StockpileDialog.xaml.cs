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

namespace Dwarrowdelf.Client.UI
{
	partial class StockpileDialog : Window
	{
		public StockpileType StockpileType { get; private set; }

		public StockpileDialog()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			var list = new List<StockpileType>();

			foreach (StockpileType id in Enum.GetValues(typeof(StockpileType)))
			{
				if (id == Client.StockpileType.None)
					continue;

				list.Add(id);
			}

			this.DataContext = list;

			base.OnInitialized(e);
		}

		public void SetContext(Environment env, IntRectZ area)
		{
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}

		private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var lb = (ListBox)sender;

			StockpileType res = Client.StockpileType.None;

			foreach (StockpileType t in lb.SelectedItems)
				res |= t;

			this.StockpileType = res;
		}
	}
}
