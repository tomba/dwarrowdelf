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
	partial class SetTerrainDialog : Window
	{
		public TerrainID? TerrainID { get; set; }
		public MaterialID? TerrainMaterialID { get; set; }

		public InteriorID? InteriorID { get; set; }
		public MaterialID? InteriorMaterialID { get; set; }

		public bool? Water { get; set; }
		public bool? Grass { get; set; }

		public SetTerrainDialog()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			terrainIDListBox.ItemsSource = Enum.GetValues(typeof(TerrainID)).Cast<TerrainID>().OrderBy(id => id.ToString()).ToArray();
			terrainMaterialListBox.ItemsSource = Enum.GetValues(typeof(MaterialID)).Cast<MaterialID>().OrderBy(id => id.ToString()).ToArray();

			interiorIDListBox.ItemsSource = Enum.GetValues(typeof(InteriorID)).Cast<InteriorID>().OrderBy(id => id.ToString()).ToArray();
			interiorMaterialListBox.ItemsSource = Enum.GetValues(typeof(MaterialID)).Cast<MaterialID>().OrderBy(id => id.ToString()).ToArray();

			base.OnInitialized(e);
		}

		public void SetContext(Environment env, IntCuboid area)
		{
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
