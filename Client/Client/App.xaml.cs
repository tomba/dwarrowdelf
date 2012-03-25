using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace Dwarrowdelf.Client
{
	sealed partial class App : Application
	{
		public new static App Current { get { return (App)Application.Current; } }
		internal new static UI.MainWindow MainWindow { get { return (UI.MainWindow)Application.Current.MainWindow; } }

		protected override void OnStartup(StartupEventArgs e)
		{
			Thread.CurrentThread.Name = "Main";

			Trace.TraceInformation("Start");

			base.OnStartup(e);
		}
	}
}
