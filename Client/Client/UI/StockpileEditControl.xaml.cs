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
	public sealed class SelectableMaterialID : SelectableValue<MaterialID>
	{
		public SelectableMaterialID(MaterialID material, bool selected)
			: base(material, selected)
		{
		}
	}

	public sealed class SelectableMaterialCategory : SelectableCollection<MaterialCategory, SelectableMaterialID>
	{
		public SelectableMaterialCategory(MaterialCategory category)
			: base(category, Materials.GetMaterials(category).Select(m => new SelectableMaterialID(m.ID, false)))
		{

		}
	}

	public sealed class SelectableItemID : SelectableValue<ItemID>
	{
		public SelectableItemID(ItemID material, bool selected)
			: base(material, selected)
		{
		}
	}

	public sealed class SelectableItemCategory : SelectableCollection<ItemCategory, SelectableItemID>
	{
		public SelectableItemCategory(ItemCategory category)
			: base(category, Dwarrowdelf.Items.GetItemInfos(category).Select(i => new SelectableItemID(i.ID, false)))
		{

		}
	}

	public class StockpileEditControlMockData
	{
		public List<SelectableMaterialCategory> MaterialsList { get; private set; }
		public List<SelectableItemCategory> ItemsList { get; private set; }

		public StockpileEditControlMockData()
		{
			this.MaterialsList = new List<SelectableMaterialCategory>(Materials.GetMaterialCategories().Select(c => new SelectableMaterialCategory(c)));
			this.ItemsList = new List<SelectableItemCategory>(Items.GetItemCategories().Select(i => new SelectableItemCategory(i)));
		}
	}

	sealed partial class StockpileEditControl : UserControl, INotifyPropertyChanged
	{
		public StockpileCriteriaEditable Criteria { get; set; }

		public StockpileEditControl()
		{
			InitializeComponent();

			this.DataContextChanged += StockpileEditControl_DataContextChanged;
		}

		void OnItemKeyDown(object sender, KeyEventArgs e)
		{
			if ((e.Key == Key.Space || e.Key == Key.Enter) && e.KeyboardDevice.Modifiers == ModifierKeys.None)
			{
				var tvi = (TreeViewItem)sender;
				var s = tvi.Header as ISelectable;

				if (s.IsSelected.HasValue)
					s.IsSelected = !s.IsSelected;
				else
					s.IsSelected = true;

				e.Handled = true;
			}
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
		public List<SelectableMaterialCategory> MaterialsList { get; set; }
		public List<SelectableItemCategory> ItemsList { get; private set; }

		public StockpileCriteriaEditable()
		{
			this.MaterialsList = new List<SelectableMaterialCategory>(Materials.GetMaterialCategories().Select(c => new SelectableMaterialCategory(c)));
			this.ItemsList = new List<SelectableItemCategory>(Items.GetItemCategories().Select(i => new SelectableItemCategory(i)));
		}

		public StockpileCriteriaEditable(ItemFilter source)
			: this()
		{
			if (source.ItemIDMask != null)
			{
				foreach (var itemID in source.ItemIDMask.EnumValues)
				{
					var cat = Items.GetItemInfo(itemID).Category;

					var sic = this.ItemsList.Find(c => c.Value == cat);

					var sii = sic.Items.First(i => i.Value == itemID);

					sii.IsSelected = true;
				}
			}

			if (source.ItemCategoryMask != null)
			{
				throw new Exception();
				/*
				foreach (var cat in source.ItemCategoryMask.EnumValues)
				{
					var sic = this.ItemsList.Find(c => c.Value == cat);

					sic.IsSelected = true;
				}*/
			}

			/*
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
			 */
		}

		public bool IsNotEmpty
		{
			get
			{
				return this.ItemsList.Any(sic => sic.IsSelected.HasValue == false || sic.IsSelected.Value == true);
				//return this.ItemIDs.Any() || this.ItemCategories.Any() || this.MaterialIDs.Any() || this.MaterialCategories.Any();
			}
		}

		public ItemFilter ToItemFilter()
		{
			var items = this.ItemsList
				.Where(sic => sic.IsSelected != false)
				.SelectMany(sic => sic.Items.Where(sii => sii.IsSelected == true))
				.Select(sii => sii.Value);

			ItemIDMask im = null;
			if (items.Any())
				im = new ItemIDMask(items);

			ItemCategoryMask icm = null;
			//if (itemCats.Any())
			//	icm = new ItemCategoryMask(itemCats);

			MaterialIDMask mim = null;
			//if (this.MaterialIDs.Any())
			//	mim = new MaterialIDMask(this.MaterialIDs);

			MaterialCategoryMask mcm = null;
			//if (this.MaterialCategories.Any())
			//	mcm = new MaterialCategoryMask(this.MaterialCategories);

			return new ItemFilter(im, icm, mim, mcm);
		}
	}
}
