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
using System.Windows.Controls.Primitives;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Interaction logic for ItemObjectPropertiesControl.xaml
	/// </summary>
	public partial class ItemObjectPropertiesControl : UserControl
	{
		public ItemObjectPropertiesControl()
		{
			InitializeComponent();
		}

		private void Debug_Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (ToggleButton)sender;
			var tag = (string)button.Tag;

			var value = button.IsChecked.GetValueOrDefault();

			var ob = (MovableObject)this.DataContext;

			var args = new Dictionary<string, object>()
			{
				{ "obid", ob.ObjectID },
			};

			var script = String.Format("world.GetObject(obid).{0} = {1}", tag, value ? "True" : "False");

			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}

		private void Uninstall_Button_Click(object sender, RoutedEventArgs e)
		{
			var env = App.GameWindow.Map;
			var ob = (ItemObject)this.DataContext;

			env.InstallItemManager.AddUninstallJob(ob);
		}
	}
}
