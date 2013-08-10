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
	sealed partial class NetStatWindow : Window
	{
		public GameData Data { get { return GameData.Data; } }

		public NetStatWindow()
		{
			InitializeComponent();

			this.Loaded += NetStatWindow_Loaded;
			this.Closed += NetStatWindow_Closed;
		}

		void NetStatWindow_Closed(object sender, EventArgs e)
		{
			this.Data.NetStats.DisableMessageReporting();
		}

		void NetStatWindow_Loaded(object sender, RoutedEventArgs e)
		{
			this.Data.NetStats.EnableMessageReporting();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			this.Data.NetStats.Reset();
		}
	}

	sealed class NetStatWindowSample : ClientNetStatistics
	{
		public NetStatWindowSample()
		{
		}
	}
}
