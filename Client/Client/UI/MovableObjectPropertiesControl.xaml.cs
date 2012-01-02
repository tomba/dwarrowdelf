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

			var p = IntPoint3D.Parse(txt);

			var msg = new Dwarrowdelf.Messages.IPCommandMessage()
			{
				Text = String.Format("get({0}).MoveTo(Dwarrowdelf.IntPoint3D({1},{2},{3}))", ob.ObjectID.RawValue, p.X, p.Y, p.Z),
			};

			GameData.Data.Connection.Send(msg);
		}

		private void MoveDir_Button_Click(object sender, RoutedEventArgs e)
		{
			var button = (Button)sender;
			var tag = (string)button.Tag;

			var ob = (MovableObject)this.DataContext;

			var dir = (Direction)Enum.Parse(typeof(Direction), tag);

			var msg = new Dwarrowdelf.Messages.IPCommandMessage()
			{
				Text = String.Format("get({0}).MoveDir(Dwarrowdelf.Direction.{1})", ob.ObjectID.RawValue, dir),
			};

			GameData.Data.Connection.Send(msg);
		}
	}
}
