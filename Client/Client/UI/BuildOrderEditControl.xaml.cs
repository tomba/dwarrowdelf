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
	sealed partial class BuildOrderEditControl : UserControl, INotifyPropertyChanged
	{
		public BuildOrderEditControl()
		{
			InitializeComponent();
		}

		public WorkbenchInfo WorkbenchInfo
		{
			get { return (WorkbenchInfo)GetValue(WorkbenchInfoProperty); }
			set { SetValue(WorkbenchInfoProperty, value); }
		}

		public static readonly DependencyProperty WorkbenchInfoProperty =
			DependencyProperty.Register("WorkbenchInfo", typeof(WorkbenchInfo), typeof(BuildOrderEditControl), new UIPropertyMetadata(null));

		public BuildOrderEditable EditableBuildOrder
		{
			get { return (BuildOrderEditable)GetValue(EditableBuildOrderProperty); }
			set { SetValue(EditableBuildOrderProperty, value); }
		}

		public static readonly DependencyProperty EditableBuildOrderProperty =
			DependencyProperty.Register("EditableBuildOrder", typeof(BuildOrderEditable), typeof(BuildOrderEditControl),
			new UIPropertyMetadata(new BuildOrderEditable()));


		public event Action AddButtonClicked;

		void AddButton_Click(object sender, RoutedEventArgs e)
		{
			if (AddButtonClicked != null)
				AddButtonClicked();
		}

		private void itemListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (AddButtonClicked != null)
				AddButtonClicked();
		}

		private void UpdateButton_Click(object sender, RoutedEventArgs e)
		{

		}

		#region INotifyPropertyChanged Members

		void Notify(string propertyName)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	sealed class BuildOrderEditable : INotifyPropertyChanged
	{
		public BuildOrderEditable()
		{
		}

		public BuildOrderEditable(BuildOrder buildOrder)
		{
			this.BuildableItem = buildOrder.BuildableItem;
		}

		public BuildOrderEditable(BuildableItem buildableItem)
		{
			this.BuildableItem = buildableItem;
		}

		public BuildMaterialsView[] BuildMaterialsView { get; private set; }

		BuildableItem m_buildableItem;

		public BuildableItem BuildableItem
		{
			get { return m_buildableItem; }

			set
			{
				m_buildableItem = value;

				if (m_buildableItem != null)
					this.BuildMaterialsView = m_buildableItem.FixedBuildMaterials.Select(bimi => new BuildMaterialsView(bimi)).ToArray();
				else
					this.BuildMaterialsView = new BuildMaterialsView[0];

				Notify("BuildableItem");
				Notify("BuildMaterialsView");
			}
		}


		public BuildOrder ToBuildOrder()
		{
			var numMaterials = this.BuildableItem.FixedBuildMaterials.Count;

			var filters = new IItemFilter[numMaterials];

			for (int idx = 0; idx < numMaterials; ++idx)
			{
				var itemIDs = this.BuildMaterialsView[idx].ItemIDs.Where(i => i.IsSelected).Select(i => i.Value).ToArray();
				var materialIDs = this.BuildMaterialsView[idx].MaterialIDs.Where(i => i.IsSelected).Select(i => i.Value).ToArray();

				ItemIDMask im = null;
				if (itemIDs.Any())
					im = new ItemIDMask(itemIDs);

				MaterialIDMask mim = null;
				if (materialIDs.Any())
					mim = new MaterialIDMask(materialIDs);

				filters[idx] = new ItemFilter(im, null, mim, null);
			}

			var bo = new BuildOrder(this.BuildableItem, filters);

			return bo;
		}

		#region INotifyPropertyChanged Members

		void Notify(string propertyName)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}


	sealed class BuildMaterialsView : INotifyPropertyChanged
	{
		public SelectableItem<ItemID>[] ItemIDs { get; private set; }
		ItemCategory? ItemCategory { get; set; }
		public SelectableItem<MaterialID>[] MaterialIDs { get; private set; }
		MaterialCategory? MaterialCategory { get; set; }

		public BuildMaterialsView(FixedMaterialFilter matInfo)
		{
			if (matInfo.ItemID.HasValue)
			{
				this.ItemIDs = new SelectableItem<ItemID>[] {
					new SelectableItem<ItemID>(ItemSelChanged, matInfo.ItemID.Value)
				};
			}
			else if (matInfo.ItemCategory.HasValue)
			{
				this.ItemIDs = Items.GetItemInfos(matInfo.ItemCategory.Value)
					.Select(mi => new SelectableItem<ItemID>(ItemSelChanged, mi.ID))
					.ToArray();
			}
			else
			{
				this.ItemIDs = Items.GetItemInfos()
					.Select(mi => new SelectableItem<ItemID>(ItemSelChanged, mi.ID))
					.ToArray();
			}

			if (matInfo.MaterialID.HasValue)
			{
				this.MaterialIDs = new SelectableItem<MaterialID>[] {
					new SelectableItem<MaterialID>(MatSelChanged, matInfo.MaterialID.Value)
				};
			}
			else if (matInfo.MaterialCategory.HasValue)
			{
				this.MaterialIDs = Materials.GetMaterials(matInfo.MaterialCategory.Value)
					.Select(mi => new SelectableItem<MaterialID>(MatSelChanged, mi.ID))
					.ToArray();
			}
			else
			{
				this.MaterialIDs = Materials.GetMaterials()
					.Select(mi => new SelectableItem<MaterialID>(MatSelChanged, mi.ID))
					.ToArray();
			}

			this.ItemCategory = matInfo.ItemCategory;
			this.MaterialCategory = matInfo.MaterialCategory;
		}

		public bool HasMultipleItems
		{
			get { return this.ItemIDs.Length > 1; }
		}

		public string ItemString
		{
			get
			{
				var selected = this.ItemIDs.Where(i => i.IsSelected).ToArray();

				if (selected.Length == 0)
				{
					if (this.ItemIDs.Length == 1)
						return this.ItemIDs[0].Value.ToString();
					else if (this.ItemCategory.HasValue)
						return "Any " + this.ItemCategory.ToString();
					else
						return "Any";
				}

				if (selected.Length == 1)
					return selected[0].Value.ToString();

				return "Multiple";
			}
		}

		public bool HasMultipleMaterials
		{
			get { return this.MaterialIDs.Length > 1; }
		}

		public string MaterialString
		{
			get
			{
				var selected = this.MaterialIDs.Where(i => i.IsSelected).ToArray();

				if (selected.Length == 0)
				{
					if (this.MaterialIDs.Length == 1)
						return this.MaterialIDs[0].Value.ToString();
					else if (this.MaterialCategory.HasValue)
						return "Any " + this.MaterialCategory.ToString();
					else
						return "Any";
				}

				if (selected.Length == 1)
					return selected[0].Value.ToString();

				return "Multiple";
			}
		}

		void MatSelChanged(MaterialID materialID)
		{
			Notify("MaterialString");
		}

		void ItemSelChanged(ItemID itemID)
		{
			Notify("ItemString");
		}

		#region INotifyPropertyChanged Members

		void Notify(string propertyName)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
		}

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}

	public class SelectableItem<T> : INotifyPropertyChanged
	{
		Action<T> m_callback;

		public T Value { get; private set; }

		public SelectableItem(T value)
		{
			this.Value = value;
		}

		public SelectableItem(Action<T> callback, T value)
		{
			m_callback = callback;
			this.Value = value;
		}

		public SelectableItem(Action<T> callback, T value, bool selected)
		{
			m_callback = callback;
			this.Value = value;
			m_isSelected = selected;
		}

		bool m_isSelected;

		public bool IsSelected
		{
			get
			{
				return m_isSelected;
			}

			set
			{
				m_isSelected = value;
				if (m_callback != null)
					m_callback(this.Value);
				Notify("IsSelected");
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


}
