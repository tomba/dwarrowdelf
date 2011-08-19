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
	/// <summary>
	/// Interaction logic for ObjectEditDialog.xaml
	/// </summary>
	public partial class ObjectEditDialog : Window
	{
		public ObjectEditDialog()
		{
			InitializeComponent();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				e.Handled = true;
				this.Close();
				return;
			}

			base.OnKeyDown(e);
		}
	}
}
