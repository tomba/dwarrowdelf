using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using Dwarrowdelf;
using System.Diagnostics;

namespace FOVTest
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			var listener = new MMLogTraceListener();
			Debug.Listeners.Clear();
			Debug.Listeners.Add(listener);
		}
	}
}
