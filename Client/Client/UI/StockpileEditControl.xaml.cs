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

namespace Dwarrowdelf.Client.UI
{
	public partial class StockpileEditControl : UserControl
	{
		public StockpileEditControl()
		{
			InitializeComponent();
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			if (this.DataContext == null)
				return;

			var stockpile = (Stockpile)this.DataContext;
			stockpile.Environment.RemoveMapElement(stockpile);
			stockpile.Destruct();
			this.DataContext = null;

			var wnd = Window.GetWindow(this);
			wnd.Close();
		}
	}
}
