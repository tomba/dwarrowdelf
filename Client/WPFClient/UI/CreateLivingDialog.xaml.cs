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
	partial class CreateLivingDialog : Window
	{
		public Environment Environment { get; set; }
		public IntRectZ Area { get; set; }

		public string LivingName { get; set; }
		public SymbolID SymbolID { get; set; }
		public GameColor Color { get; set; }

		public CreateLivingDialog()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			symbolIDListBox.ItemsSource = Enum.GetValues(typeof(SymbolID)).Cast<SymbolID>().OrderBy(id => id.ToString()).ToArray();
			colorListBox.ItemsSource = Enum.GetValues(typeof(GameColor)).Cast<GameColor>().OrderBy(id => id.ToString()).ToArray();

			base.OnInitialized(e);
		}

		public void SetContext(Environment env, IntRectZ area)
		{
			this.Environment = env;
			this.Area = area;

			areaTextBox.Text = String.Format("{0},{1} {2}x{3}", area.X, area.Y, area.Width, area.Height);
		}

		private void OkButton_Click(object sender, RoutedEventArgs e)
		{
			this.DialogResult = true;
		}
	}
}
