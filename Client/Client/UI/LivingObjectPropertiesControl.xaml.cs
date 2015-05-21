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
	public partial class LivingObjectPropertiesControl : UserControl
	{
		public LivingObjectPropertiesControl()
		{
			InitializeComponent();
		}

		private void Button_Click_Server_Trace(object sender, RoutedEventArgs e)
		{
			var b = (Button)sender;
			var traceLevel = (System.Diagnostics.TraceLevel)Enum.Parse(typeof(System.Diagnostics.TraceLevel), (string)b.Tag);
			var living = (LivingObject)this.DataContext;

			DebugScriptMessages.SendSetLivingTraceLevel(living, traceLevel);
		}
	}
}
