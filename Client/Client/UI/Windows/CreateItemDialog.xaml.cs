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

namespace Dwarrowdelf.Client.UI
{
	sealed partial class CreateItemDialog : Window
	{
		public ItemID ItemID { get; set; }
		public MaterialID MaterialID { get; set; }

		public EnvironmentObject Environment { get; set; }
		public IntGrid3 Area { get; set; }

		public CreateItemDialog()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			itemIDListBox.ItemsSource = EnumHelpers.GetEnumValues<ItemID>().OrderBy(id => id.ToString()).ToArray();
			materialIDListBox.ItemsSource = EnumHelpers.GetEnumValues<MaterialID>().OrderBy(id => id.ToString()).ToArray();

			base.OnInitialized(e);
		}

		public void SetContext(EnvironmentObject env, IntGrid3 area)
		{
			this.Environment = env;
			this.Area = area;
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
