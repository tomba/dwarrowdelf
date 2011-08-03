using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.Threading;

namespace Dwarrowdelf.Client
{
	public partial class App : Application
	{
		public new static App Current { get { return (App)Application.Current; } }
		internal new static MainWindow MainWindow { get { return (MainWindow)Application.Current.MainWindow; } }

		protected override void OnStartup(StartupEventArgs e)
		{
			Thread.CurrentThread.Name = "Main";

			Trace.TraceInformation("Start");

			int magic = 0;
			GameAction.MagicNumberGenerator = () => Math.Abs(Interlocked.Increment(ref magic));

			base.OnStartup(e);
		}
	}
}
