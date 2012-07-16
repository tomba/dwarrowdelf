using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace Dwarrowdelf.Client
{
	sealed partial class App : Application
	{
		public new static App Current { get { return (App)Application.Current; } }
		internal new static UI.MainWindow MainWindow { get { return (UI.MainWindow)Application.Current.MainWindow; } }

		protected override void OnStartup(StartupEventArgs e)
		{
			if (Debugger.IsAttached == false)
				AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

			base.OnStartup(e);
		}

		void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			var exc = (Exception)e.ExceptionObject;

			DumpException(exc);

			if (e.IsTerminating == false)
				Environment.Exit(0);
		}

		void DumpException(Exception e)
		{
			var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
				"dwarrowdelf-crash.txt");

			StringBuilder sb = new StringBuilder();

			sb.AppendLine(DateTime.Now.ToString());

			sb.AppendLine("---");

			sb.Append(e.ToString());

			File.WriteAllText(path, sb.ToString());
		}
	}
}
