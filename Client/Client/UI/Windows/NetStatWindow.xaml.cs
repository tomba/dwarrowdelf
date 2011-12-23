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
		public NetStatWindow()
		{
			InitializeComponent();
		}

		public GameData Data { get { return GameData.Data; } }
	}

	sealed class NetStatWindowSample : ClientNetStatistics
	{
		public NetStatWindowSample()
		{
			base.SentBytes = 1;
			base.SentMessages = 2;

			AddReceivedMessages(new Messages.ChangeMessage());
			AddReceivedMessages(new Messages.ChangeMessage());
			AddReceivedMessages(new Messages.ChangeMessage());
			AddReceivedMessages(new Messages.EnterGameReplyEndMessage());

		}
	}
}
