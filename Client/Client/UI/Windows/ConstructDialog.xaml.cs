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
using System.ComponentModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Dwarrowdelf.Client.UI
{

	public interface ISelectable<TItem>
	{
		bool? IsSelected { get; set; }
		event Action IsSelectedChanged;
		TItem Value { get; }
	}

	public class SelectableValue<T> : ISelectable<T>, INotifyPropertyChanged
	{
		public T Value { get; private set; }
		public event Action IsSelectedChanged;

		public SelectableValue(T value)
		{
			this.Value = value;
			m_isSelected = false;
		}

		public SelectableValue(T value, bool selected)
		{
			this.Value = value;
			m_isSelected = selected;
		}

		bool? m_isSelected;

		public bool? IsSelected
		{
			get
			{
				return m_isSelected;
			}

			set
			{
				if (value == m_isSelected)
					return;

				m_isSelected = value;
				Notify("IsSelected");
				if (this.IsSelectedChanged != null)
					this.IsSelectedChanged();
			}
		}


		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#endregion

	}

	public class SelectableValueCollection<TItem>
	{
		public SelectableValue<TItem>[] SelectableItems { get; private set; }

		public SelectableValueCollection(IEnumerable<TItem> items)
		{
			this.SelectableItems = items.Select(i => new SelectableValue<TItem>(i)).ToArray();
		}
	}





	public class SelectableCollection<THeader, TItem> : ISelectable<THeader>, INotifyPropertyChanged
	{
		public event Action IsSelectedChanged;

		public ObservableCollection<ISelectable<TItem>> Items { get; private set; }

		public SelectableCollection(THeader value)
		{
			this.Value = value;

			this.Items = new ObservableCollection<ISelectable<TItem>>();
			this.Items.CollectionChanged += Items_CollectionChanged;

			m_isSelected = false;
		}

		public SelectableCollection(THeader value, IEnumerable<ISelectable<TItem>> materials)
		{
			this.Value = value;

			this.Items = new ObservableCollection<ISelectable<TItem>>(materials);
			foreach (var i in this.Items)
				i.IsSelectedChanged += ItemIsSelectedChanged;

			this.Items.CollectionChanged += Items_CollectionChanged;

			CheckCollection();
		}

		void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (ISelectable<TItem> item in e.NewItems)
						item.IsSelectedChanged += ItemIsSelectedChanged;

					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (ISelectable<TItem> item in e.OldItems)
						item.IsSelectedChanged -= ItemIsSelectedChanged;

					break;

				default:
					throw new Exception();
			}

			CheckCollection();
		}

		public THeader Value { get; private set; }

		bool m_updatingIsSelected;

		void ItemIsSelectedChanged()
		{
			if (m_updatingIsSelected)
				return;

			CheckCollection();
		}

		void CheckCollection()
		{
			int undetermined = 0;
			int selected = 0;
			int unselected = 0;

			foreach (var item in this.Items)
			{
				if (item.IsSelected.HasValue == false)
				{
					undetermined++;
					break;
				}
				else if (item.IsSelected.Value == true)
				{
					selected++;
					if (unselected > 0)
						break;
				}
				else
				{
					unselected++;
					if (selected > 0)
						break;
				}
			}

			if (undetermined > 0 || (selected > 0 && unselected > 0))
				this.IsSelected = null;
			else if (selected == this.Items.Count)
				this.IsSelected = true;
			else
			{
				Debug.Assert(unselected == this.Items.Count);
				this.IsSelected = false;
			}
		}

		bool? m_isSelected;
		public bool? IsSelected
		{
			get
			{
				return m_isSelected;
			}

			set
			{
				if (m_isSelected == value)
					return;

				m_isSelected = value;

				if (m_isSelected.HasValue)
				{
					m_updatingIsSelected = true;
					foreach (var m in this.Items)
						m.IsSelected = m_isSelected.Value;
					m_updatingIsSelected = false;
				}

				Notify("IsSelected");
				if (this.IsSelectedChanged != null)
					this.IsSelectedChanged();

			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#endregion
	}





	public partial class ConstructDialog : Window
	{
		public ConstructDialog()
		{
			InitializeComponent();
		}

		private void Ok_Button_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}


		public ConstructMode ConstructMode
		{
			set
			{
				IItemMaterialFilter filter;

				switch (value)
				{
					case ConstructMode.Floor:
						filter = WorkHelpers.ConstructFloorItemFilter;
						break;
					/*
										case ConstructMode.Pavement:
											filter = WorkHelpers.ConstructPavementItemFilter;
											break;
											*/
					case ConstructMode.Wall:
						filter = WorkHelpers.ConstructWallItemFilter;
						break;

					default:
						throw new Exception();
				}


				IEnumerable<MaterialID> allowedMaterials = filter.MaterialIDs;
				IEnumerable<MaterialCategory> allowedMaterialCategories = filter.MaterialCategories;

				// XXX
				allowedMaterialCategories = allowedMaterialCategories.Concat(new MaterialCategory[] { MaterialCategory.Wood });

				var allMaterials = allowedMaterials.Select(id => Materials.GetMaterial(id))
					.Concat(allowedMaterialCategories.SelectMany(c => Materials.GetMaterials(c)))
					.Distinct();

				var distinctCategories = allMaterials.Select(m => m.Category).Distinct();
				/*
				m_categoryCollection = distinctCategories.Select(c =>
					{
						var materials = allMaterials.Where(mi => mi.Category == c).Select(mi => mi.ID);
						return new MaterialCategoryEntry(c, materials, true);
					})
					.ToArray();
				*/
				//materialCategoriesListBox.ItemsSource = m_categoryCollection;


				var item1 = new SelectableCollection<ItemID, MaterialCategory>(ItemID.Block);

				{
					var materials1 = new SelectableCollection<MaterialCategory, MaterialID>(MaterialCategory.Mineral);
					var materials2 = new SelectableCollection<MaterialCategory, MaterialID>(MaterialCategory.Rock);

					item1.Items.Add(materials1);
					item1.Items.Add(materials2);

					materials1.Items.Add(new SelectableValue<MaterialID>(MaterialID.Birch));
					materials1.Items.Add(new SelectableValue<MaterialID>(MaterialID.Chrysoprase));

					materials2.Items.Add(new SelectableValue<MaterialID>(MaterialID.Diorite));
					materials2.Items.Add(new SelectableValue<MaterialID>(MaterialID.Gold));
				}


				var item2 = new SelectableCollection<ItemID, MaterialCategory>(ItemID.Log);

				{
					var materials1 = new SelectableCollection<MaterialCategory, MaterialID>(MaterialCategory.Wood);
					var materials2 = new SelectableCollection<MaterialCategory, MaterialID>(MaterialCategory.Gem);

					item2.Items.Add(materials1);
					item2.Items.Add(materials2);

					materials1.Items.Add(new SelectableValue<MaterialID>(MaterialID.Copper, true));
					materials1.Items.Add(new SelectableValue<MaterialID>(MaterialID.Chrysoprase, true));

					materials2.Items.Add(new SelectableValue<MaterialID>(MaterialID.Emerald));
					materials2.Items.Add(new SelectableValue<MaterialID>(MaterialID.Gold));
				}


				var arr = new ISelectable<ItemID>[] { item1, item2 };


				itemIDListBox.ItemsSource = arr;
			}
		}

		public IItemFilter ItemFilter
		{
			get
			{
				return null;
				//var materials = m_categoryCollection.SelectMany(c => c.MaterialCollection).Select(s => s.Value);
				//return new ItemFilter(null, materials);
			}
		}
	}
}
