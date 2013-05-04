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
	public partial class ConstructDialog : Window
	{
		List<SelectableMaterialCategory> m_categories;

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
				IItemFilter filter;

				switch (value)
				{
					case ConstructMode.Floor:
						this.Title = "Construct Floor";
						filter = WorkHelpers.ConstructFloorItemFilter;
						break;

					case ConstructMode.Pavement:
						this.Title = "Construct Pavement";
						filter = WorkHelpers.ConstructPavementItemFilter;
						break;

					case ConstructMode.Wall:
						this.Title = "Construct Wall";
						filter = WorkHelpers.ConstructWallItemFilter;
						break;

					default:
						throw new Exception();
				}

				IEnumerable<MaterialID> allowedMaterials;
				IEnumerable<MaterialCategory> allowedCategories;

				if (filter is ItemFilter)
				{
					var f = (ItemFilter)filter;

					allowedMaterials = f.MaterialIDs;
					allowedCategories = f.MaterialCategories;
				}
				else if (filter is OrItemFilter)
				{
					// XXX

					var f = (OrItemFilter)filter;

					var f1 = (ItemFilter)f[0];
					var f2 = (ItemFilter)f[1];

					allowedMaterials = f1.MaterialIDs.Concat(f2.MaterialIDs);
					allowedCategories = f1.MaterialCategories.Concat(f2.MaterialCategories);
				}
				else
				{
					throw new Exception();
				}

				var allMaterials = allowedMaterials.Select(id => Materials.GetMaterial(id))
					.Concat(allowedCategories.SelectMany(c => Materials.GetMaterials(c)))
					.Distinct();

				var categories = allMaterials.Select(m => m.Category).Distinct();

				List<SelectableMaterialCategory> scats = new List<SelectableMaterialCategory>();
				foreach (var c in categories)
				{
					var scat = new SelectableMaterialCategory(c);

					foreach (var mat in allMaterials.Where(mi => mi.Category == c).Select(mi => mi.ID))
					{
						var smat = new SelectableMaterial(mat, true);
						scat.Items.Add(smat);
					}

					scats.Add(scat);
				}

				m_categories = scats;
				materialCategoriesListBox.ItemsSource = scats;
			}
		}

		public ItemFilter GetItemFilter()
		{
			var materials = m_categories
				.Where(c => c.IsSelected != false)
				.SelectMany(c => c.Items.Where(m => m.IsSelected == true))
				.Select(s => s.Value);

			return new ItemFilter(null, materials);
		}


		sealed class SelectableMaterial : SelectableValue<MaterialID>
		{
			public SelectableMaterial(MaterialID material, bool selected)
				: base(material, selected)
			{
			}
		}

		sealed class SelectableMaterialCategory : SelectableCollection<MaterialCategory, SelectableMaterial>
		{
			public SelectableMaterialCategory(MaterialCategory category)
				: base(category)
			{
			}
		}

		sealed class SelectableItemID : SelectableCollection<ItemID, SelectableMaterialCategory>
		{
			public SelectableItemID(ItemID itemID)
				: base(itemID)
			{
			}
		}
	}
}
