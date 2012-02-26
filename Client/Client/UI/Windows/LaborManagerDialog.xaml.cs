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
using System.Collections;
using System.Dynamic;
using System.Windows.Controls.Primitives;

namespace Dwarrowdelf.Client.UI
{
	public partial class LaborManagerDialog : Window
	{
		public LaborManagerDialog()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			GenerateGrid();
		}

		void GenerateGrid()
		{
			var controllables = GameData.Data.World.Controllables;

			grid.Columns.Clear();
			grid.Items.Clear();

			var template = (DataTemplate)FindResource("dataGridHeaderTemplate");

			foreach (var lid in EnumHelpers.GetEnumValues<LaborID>().Skip(1))
			{
				grid.Columns.Add(new MyCheckBoxDataCell()
				{
					Header = lid.ToString(),
					Binding = new Binding(lid.ToString()),
					HeaderTemplate = template
				});
			}


			var arr = controllables.Select(l =>
			{
				var o = new ExpandoObject() as IDictionary<string, object>;

				o["LivingObject"] = l;
				o["Name"] = l.Name;

				foreach (var lid in EnumHelpers.GetEnumValues<LaborID>().Skip(1))
					o[lid.ToString()] = l.EnabledLabors.Get((int)lid);

				return o;
			});

			grid.ItemsSource = arr;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			foreach (dynamic o in grid.Items)
			{
				var living = (LivingObject)o.LivingObject;

				var d = (IDictionary<string, object>)o;

				foreach (var lid in EnumHelpers.GetEnumValues<LaborID>().Skip(1))
				{
					living.SetLaborEnabled(lid, (bool)d[lid.ToString()]);
				}
			}

			this.Close();
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

	}

	class MyCheckBoxDataCell : DataGridBoundColumn
	{
		protected override FrameworkElement GenerateEditingElement(DataGridCell cell, object dataItem)
		{
			throw new NotImplementedException();
		}

		protected override FrameworkElement GenerateElement(DataGridCell cell, object dataItem)
		{
			CheckBox cb = (cell != null) ? (cell.Content as CheckBox) : null;

			if (cb == null)
				cb = new CheckBox();

			cb.HorizontalAlignment = HorizontalAlignment.Center;
			cb.VerticalAlignment = VerticalAlignment.Center;
			cb.DataContext = dataItem;
			cb.SetBinding(CheckBox.IsCheckedProperty, this.Binding);

			return cb;
		}
	}
}
