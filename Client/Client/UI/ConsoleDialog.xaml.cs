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
using Dwarrowdelf.Messages;

namespace Dwarrowdelf.Client.UI
{
	public partial class ConsoleDialog : Window
	{
		public ConsoleDialog()
		{
			InitializeComponent();
		}

		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
				return;
			}

			base.OnKeyDown(e);
		}

		private void InputTextBox_TextEntered(string str)
		{
			if (string.IsNullOrEmpty(str))
			{
				Close();
				return;
			}
			
			var msg = new IPCommandMessage() { Text = str };
			GameData.Data.Connection.Send(msg);
		}
	}
}
