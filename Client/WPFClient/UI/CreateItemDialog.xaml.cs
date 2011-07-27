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

namespace Dwarrowdelf.Client
{
	partial class CreateItemDialog : Window
	{
		public ItemID ItemID { get; set; }
		public MaterialID MaterialID { get; set; }

		public Environment Environment { get; set; }
		public IntPoint3D Location { get; set; }

		public int X { get; set; }
		public int Y { get; set; }
		public int Z { get; set; }

		public CreateItemDialog()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			itemIDListBox.ItemsSource = Enum.GetValues(typeof(ItemID)).Cast<ItemID>().OrderBy(id => id.ToString()).ToArray();
			materialIDListBox.ItemsSource = Enum.GetValues(typeof(MaterialID)).Cast<MaterialID>().OrderBy(id => id.ToString()).ToArray();

			base.OnInitialized(e);
		}

		public void SetContext(Environment env, IntPoint3D location)
		{
			this.Environment = env;
			this.Location = location;
			this.X = location.X;
			this.Y = location.Y;
			this.Z = location.Z;

			environmentListBox.GetBindingExpression(ListBox.SelectedValueProperty).UpdateTarget();
			textBoxX.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
			textBoxY.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
			textBoxZ.GetBindingExpression(TextBox.TextProperty).UpdateTarget();
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			this.Location = new IntPoint3D(this.X, this.Y, this.Z);
			this.DialogResult = true;
		}
	}
}
