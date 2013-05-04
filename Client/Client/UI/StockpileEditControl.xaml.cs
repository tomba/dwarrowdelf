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

			ItemFilter itemFillter;

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

		public StockpileCriteriaEditable(ItemFilter source)
		{
			if (source.ItemIDMask == null)
				this.ItemIDs = new ObservableCollection<ItemID>();
			else
				this.ItemIDs = new ObservableCollection<ItemID>(source.ItemIDMask.EnumValues);

			if (source.ItemCategoryMask == null)
				this.ItemCategories = new ObservableCollection<ItemCategory>();
			else
				this.ItemCategories = new ObservableCollection<ItemCategory>(source.ItemCategoryMask.EnumValues);

			if (source.MaterialIDMask == null)
				this.MaterialIDs = new ObservableCollection<MaterialID>();
			else
				this.MaterialIDs = new ObservableCollection<MaterialID>(source.MaterialIDMask.EnumValues);

			if (source.MaterialCategoryMask == null)
				this.MaterialCategories = new ObservableCollection<MaterialCategory>();
			else
				this.MaterialCategories = new ObservableCollection<MaterialCategory>(source.MaterialCategoryMask.EnumValues);
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

		public ItemFilter ToItemFilter()
		{
			ItemIDMask im = null;
			if (this.ItemIDs.Any())
				im = new ItemIDMask(this.ItemIDs);

			ItemCategoryMask icm = null;
			if (this.ItemCategories.Any())
				icm = new ItemCategoryMask(this.ItemCategories);

			MaterialIDMask mim = null;
			if (this.MaterialIDs.Any())
				mim = new MaterialIDMask(this.MaterialIDs);

			MaterialCategoryMask mcm = null;
			if (this.MaterialCategories.Any())
				mcm = new MaterialCategoryMask(this.MaterialCategories);

			return new ItemFilter(im, icm, mim, mcm);
		}
	}
}
