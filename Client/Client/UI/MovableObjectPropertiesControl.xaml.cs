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
	public partial class MovableObjectPropertiesControl : UserControl
	{
		public MovableObjectPropertiesControl()
		{
			InitializeComponent();
		}

		private void Move_Button_Click(object sender, RoutedEventArgs e)
		{
			var ob = (MovableObject)this.DataContext;

			var txt = dstTextBox.Text;

			var p = IntVector3.Parse(txt);

			var args = new Dictionary<string, object>()
			{
				{ "obid", ob.ObjectID },
				{ "p", p },
			};

			var script = "world.GetObject(obid).MoveTo(p)";

			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}

		private void MoveDir_Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var tag = (string)button.Tag;

			var ob = (MovableObject)this.DataContext;

			var dir = (Direction)Enum.Parse(typeof(Direction), tag);

			var args = new Dictionary<string, object>()
			{
				{ "obid", ob.ObjectID },
				{ "dir", dir },
			};

			var script = "world.GetObject(obid).MoveDir(dir)";

			var msg = new Dwarrowdelf.Messages.IPScriptMessage(script, args);

			GameData.Data.User.Send(msg);
		}
	}
}
