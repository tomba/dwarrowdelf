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
	partial class ConstructBuildingDialog : Window
	{
		public BuildingID BuildingID { get; private set; }

		public ConstructBuildingDialog()
		{
			InitializeComponent();
		}

		public void SetContext(Environment env, IntRectZ area)
		{
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			BuildingID? type = null;

			switch (e.Key)
			{
				case Key.C:
					type = BuildingID.Carpenter;
					break;

				case Key.M:
					type = BuildingID.Mason;
					break;

				case Key.S:
					type = BuildingID.Smith;
					break;
			}

			if (type.HasValue)
			{
				this.BuildingID = type.Value;
				this.DialogResult = true;
				Close();
				return;
			}

			base.OnKeyDown(e);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var tag = (string)button.Tag;

			var type = (BuildingID)Enum.Parse(typeof(BuildingID), tag);

			this.BuildingID = type;

			this.DialogResult = true;
			Close();
		}
	}
}
