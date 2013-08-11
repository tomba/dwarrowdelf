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
	sealed partial class ConsoleDialog : Window
	{
		IPRunner m_ipRunner;

		public ConsoleDialog()
		{
			InitializeComponent();

			if (GameData.Data.User != null)
				GameData.Data.User.IPMessageReceived += Writer;

			this.Closed += ConsoleDialog_Closed;
		}

		void ConsoleDialog_Closed(object sender, EventArgs e)
		{
			if (GameData.Data.User != null)
				GameData.Data.User.IPMessageReceived -= Writer;
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

			if (this.serverButton.IsChecked == true)
			{
				if (GameData.Data.User != null)
				{
					var msg = new IPExpressionMessage(str);
					GameData.Data.User.Send(msg);
				}
			}
			else if (this.clientButton.IsChecked == true)
			{
				if (m_ipRunner == null)
					m_ipRunner = new IPRunner(Writer);

				m_ipRunner.Exec(str);
			}
			else
			{
				throw new Exception();
			}
		}

		void Writer(string txt)
		{
			textListBox.Items.Add(txt);
		}
	}
}
