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
	sealed partial class LogOnDialog : Window
	{
		public LogOnDialog()
		{
			InitializeComponent();
		}

		public void AppendText(string text)
		{
			textBox.AppendText(text);
			textBox.AppendText(Environment.NewLine);
		}
	}
}
