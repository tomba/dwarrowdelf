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
using System.Collections.ObjectModel;

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

			if (stockpile == null || stockpile.Criteria == null)
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
			Stockpile.DestructStockpile(stockpile);

			this.DataContext = null;

			var wnd = Window.GetWindow(this);
			wnd.Close();
		}

		private void Apply_Button_Click(object sender, RoutedEventArgs e)
		{
			if (this.DataContext == null)
				return;

			var stockpile = (Stockpile)this.DataContext;

			IItemMaterialFilter itemFillter;

			if (this.Criteria.IsNotEmpty)
				itemFillter = this.Criteria.ToItemFilter();
			else
				itemFillter = null;

			stockpile.SetCriteria(itemFillter);
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

	sealed class StockpileCriteriaEditable
	{
		public StockpileCriteriaEditable()
		{
			this.ItemIDs = new ObservableCollection<ItemID>();
			this.ItemCategories = new ObservableCollection<ItemCategory>();
			this.MaterialIDs = new ObservableCollection<MaterialID>();
			this.MaterialCategories = new ObservableCollection<MaterialCategory>();
		}

		public StockpileCriteriaEditable(IItemMaterialFilter source)
		{
			this.ItemIDs = new ObservableCollection<ItemID>(source.ItemIDs);
			this.ItemCategories = new ObservableCollection<ItemCategory>(source.ItemCategories);
			this.MaterialIDs = new ObservableCollection<MaterialID>(source.MaterialIDs);
			this.MaterialCategories = new ObservableCollection<MaterialCategory>(source.MaterialCategories);
		}

		public ObservableCollection<ItemID> ItemIDs { get; set; }
		public ObservableCollection<ItemCategory> ItemCategories { get; set; }
		public ObservableCollection<MaterialID> MaterialIDs { get; set; }
		public ObservableCollection<MaterialCategory> MaterialCategories { get; set; }

		public bool IsNotEmpty
		{
			get
			{
				return this.ItemIDs.Any() || this.ItemCategories.Any() || this.MaterialIDs.Any() || this.MaterialCategories.Any();
			}
		}

		public IItemMaterialFilter ToItemFilter()
		{
			return new ItemFilter(this.ItemIDs, this.ItemCategories, this.MaterialIDs, this.MaterialCategories);
		}
	}

}
