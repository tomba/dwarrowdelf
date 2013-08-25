using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Dwarrowdelf.Client.UI
{
	/// <summary>
	/// Interaction logic for GCDebugWindow.xaml
	/// </summary>
	public partial class GCDebugWindow : Window
	{
		DispatcherTimer m_timer;

		public GCDebugWindow()
		{
			this.Loaded += GCDebugWindow_Loaded;
			this.Closing += GCDebugWindow_Closing;
			InitializeComponent();
		}

		void GCDebugWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			m_timer.Stop();
		}

		void GCDebugWindow_Loaded(object sender, RoutedEventArgs e)
		{
			m_timer = new DispatcherTimer();
			m_timer.Interval = TimeSpan.FromSeconds(1);
			m_timer.Tick += timer_Tick;
			m_timer.Start();
		}

		int m_c0;
		int m_c1;
		int m_c2;

		void timer_Tick(object sender, EventArgs e)
		{
			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var d0 = c0 - m_c0;
			var d1 = c1 - m_c1;
			var d2 = c2 - m_c2;

			tb1.Text = String.Format("GC0 {0}, {1}/s", c0, d0);
			tb2.Text = String.Format("GC1 {0}, {1}/s", c1, d1);
			tb3.Text = String.Format("GC2 {0}, {1}/s", c2, d2);

			m_c0 = c0;
			m_c1 = c1;
			m_c2 = c2;
		}
	}
}
