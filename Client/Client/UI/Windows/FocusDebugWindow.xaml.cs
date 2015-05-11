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
using System.Windows.Threading;

namespace Dwarrowdelf.Client.UI
{
	public partial class FocusDebugWindow : Window
	{
		DispatcherTimer m_focusDebugTimer;

		public FocusDebugWindow()
		{
			InitializeComponent();

			m_focusDebugTimer = new DispatcherTimer();
			m_focusDebugTimer.Interval = TimeSpan.FromMilliseconds(250);
			m_focusDebugTimer.Tick += (o, ea) =>
			{
				this.FocusedElement = Keyboard.FocusedElement as UIElement;
			};
			m_focusDebugTimer.Start();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			base.OnClosing(e);

			m_focusDebugTimer.Stop();
		}

		public UIElement FocusedElement
		{
			get { return (UIElement)GetValue(FocusedElementProperty); }
			set { SetValue(FocusedElementProperty, value); }
		}

		public static readonly DependencyProperty FocusedElementProperty =
			DependencyProperty.Register("FocusedElement", typeof(UIElement), typeof(FocusDebugWindow), new UIPropertyMetadata(null));
	}
}
