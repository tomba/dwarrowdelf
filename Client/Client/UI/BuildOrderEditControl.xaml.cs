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

		public BuildingInfo BuildingInfo
		{
			get { return (BuildingInfo)GetValue(BuildingInfoProperty); }
			set { SetValue(BuildingInfoProperty, value); }
		}

		public static readonly DependencyProperty BuildingInfoProperty =
			DependencyProperty.Register("BuildingInfo", typeof(BuildingInfo), typeof(BuildOrderEditControl), new UIPropertyMetadata(null));

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
					this.BuildMaterialsView = m_buildableItem.BuildMaterials.Select(bimi => new BuildMaterialsView(bimi)).ToArray();
				else
					this.BuildMaterialsView = new BuildMaterialsView[0];

				Notify("BuildableItem");
				Notify("BuildMaterialsView");
			}
		}


		public BuildOrder ToBuildOrder()
		{
			var spec = new BuildSpec(this.BuildableItem);

			for (int idx = 0; idx < spec.BuildableItem.BuildMaterials.Count; ++idx)
			{
				var itemIDs = this.BuildMaterialsView[idx].ItemIDs.Where(i => i.IsSelected).Select(i => i.Value).ToArray();
				var materialIDs = this.BuildMaterialsView[idx].MaterialIDs.Where(i => i.IsSelected).Select(i => i.Value).ToArray();

				spec.ItemSpecs[idx] = new ItemFilter(itemIDs, materialIDs);
			}

			var bo = new BuildOrder(spec);

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

		public BuildMaterialsView(BuildableItemMaterialInfo matInfo)
		{
			if (matInfo.ItemID.HasValue)
			{
				this.ItemIDs = new SelectableItem<ItemID>[] {
					new SelectableItem<ItemID>(ItemSelChanged, matInfo.ItemID.Value) { IsSelected = true } 
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
					new SelectableItem<MaterialID>(MatSelChanged, matInfo.MaterialID.Value) { IsSelected = true } 
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
					if (this.ItemCategory.HasValue)
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
					if (this.MaterialCategory.HasValue)
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

	sealed class SelectableItem<T>
	{
		Action<T> m_callback;

		public T Value { get; private set; }

		public SelectableItem(Action<T> callback, T value)
		{
			m_callback = callback;
			this.Value = value;
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
				m_callback(this.Value);
			}
		}
	}


}
