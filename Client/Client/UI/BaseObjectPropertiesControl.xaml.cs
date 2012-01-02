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

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Interaction logic for BaseObjectPropertiesControl.xaml
	/// </summary>
	public partial class BaseObjectPropertiesControl : UserControl
	{
		public BaseObjectPropertiesControl()
		{
			InitializeComponent();
		}

		private void Destruct_Button_Click(object sender, RoutedEventArgs e)
		{
			var ob = (BaseObject)this.DataContext;

			var msg = new Dwarrowdelf.Messages.ItemDebugMessage()
			{
				ItemID = ob.ObjectID,
				Destruct = true,
			};

			GameData.Data.Connection.Send(msg);
		}
	}
}
