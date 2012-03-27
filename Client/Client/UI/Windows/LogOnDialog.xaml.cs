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
	/// Interaction logic for LogOnDialog.xaml
	/// </summary>
	sealed partial class LogOnDialog : Window
	{
		public LogOnDialog()
		{
			InitializeComponent();
			label1.Content = "";
			label2.Content = "";
		}

		public void SetText1(string text)
		{
			label1.Content = text;
		}

		public void SetText2(string text)
		{
			label2.Content = text;
		}
	}
}
