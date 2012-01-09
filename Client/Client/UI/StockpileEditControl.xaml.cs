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
using System.ComponentModel;

namespace Dwarrowdelf.Client.UI
{
	sealed partial class StockpileEditControl : UserControl, INotifyPropertyChanged
	{
		public StockpileCriteriaEditable Criteria { get; set; }

		public StockpileEditControl()
		{
			InitializeComponent();

			this.DataContextChanged += StockpileEditControl_DataContextChanged;
		}

		void StockpileEditControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var stockpile = (Stockpile)this.DataContext;

			if (stockpile == null)
			{
				this.Criteria = new StockpileCriteriaEditable();
			}
			else
			{
				this.Criteria = new StockpileCriteriaEditable(stockpile.Criteria);
			}

			Notify("Criteria");
		}

		private void Destruct_Button_Click(object sender, RoutedEventArgs e)
		{
			if (this.DataContext == null)
				return;

			var stockpile = (Stockpile)this.DataContext;
			stockpile.Environment.RemoveAreaElement(stockpile);
			stockpile.Destruct();
			this.DataContext = null;

			var wnd = Window.GetWindow(this);
			wnd.Close();
		}

		private void Apply_Button_Click(object sender, RoutedEventArgs e)
		{
			if (this.DataContext == null)
				return;

			var stockpile = (Stockpile)this.DataContext;

			stockpile.SetCriteria(this.Criteria);
		}

		#region INotifyPropertyChanged Members

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
