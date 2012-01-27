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

namespace Dwarrowdelf.Client.UI
{

	public interface ISelectableCollection : ISelectable
	{

	}

	public interface ISelectable
	{
		bool? IsSelected { get; set; }
		event Action IsSelectedChanged;
	}



	public class SelectableMaterialID : SelectableValue<MaterialID>
	{
		public SelectableMaterialID(MaterialID materialID)
			: base(materialID)
		{
		}
	}

	public class SelectableValue<T> : ISelectable, INotifyPropertyChanged
	{
		public T Value { get; private set; }
		public event Action IsSelectedChanged;

		public SelectableValue(T value)
		{
			this.Value = value;
		}

		public SelectableValue(T value, bool selected)
		{
			this.Value = value;
			m_isSelected = selected;
		}

		bool m_isSelected;

		public bool? IsSelected
		{
			get
			{
				return m_isSelected;
			}

			set
			{
				var b = value.GetValueOrDefault();

				m_isSelected = b;
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





	public class SelectableCollection<T> : ISelectable, INotifyPropertyChanged
	{
		public event Action IsSelectedChanged;

		public ObservableCollection<ISelectable> Items { get; private set; }

		public SelectableCollection(T value, IEnumerable<ISelectable> materials)
		{
			this.Value = value;

			this.Items = new ObservableCollection<ISelectable>(materials);
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
					foreach (ISelectable item in e.NewItems)
						item.IsSelectedChanged += ItemIsSelectedChanged;

					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (ISelectable item in e.OldItems)
						item.IsSelectedChanged -= ItemIsSelectedChanged;

					break;

				default:
					throw new Exception();
			}
		}

		public T Value { get; private set; }

		bool m_updatingIsSelected;

		void ItemIsSelectedChanged()
		{
			if (m_updatingIsSelected)
				return;

			CheckCollection();
		}

		void CheckCollection()
		{
			var c = this.Items.Count(m => m.IsSelected.GetValueOrDefault());

			if (c == this.Items.Count)
				this.IsSelected = true;
			else if (c > 0)
				this.IsSelected = null;
			else
				this.IsSelected = false;
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
				if (m_updatingIsSelected)
					return;

				if (m_isSelected == value)
					return;

				m_updatingIsSelected = true;

				m_isSelected = value;
				Notify("IsSelected");
				if (this.IsSelectedChanged != null)
					this.IsSelectedChanged();


				if (m_isSelected.HasValue)
				{
					foreach (var m in this.Items)
						m.IsSelected = m_isSelected.Value;
				}

				m_updatingIsSelected = false;
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


				var materials1 = new MaterialID[] { MaterialID.Birch, MaterialID.Chrysoprase }.Select(m => new SelectableValue<MaterialID>(m));
				var materials2 = new MaterialID[] { MaterialID.Diorite, MaterialID.Gold }.Select(m => new SelectableValue<MaterialID>(m));



				var selectableMaterials1 = new SelectableCollection<MaterialCategory>(MaterialCategory.Mineral, materials1);
				var selectableMaterials2 = new SelectableCollection<MaterialCategory>(MaterialCategory.Rock, materials2);


				var arr = new ISelectable[] { selectableMaterials1, selectableMaterials2 };

				materialCategoriesListBox.ItemsSource = arr;
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
