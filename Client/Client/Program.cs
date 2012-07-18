using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace Dwarrowdelf.Client
{
	static class Program
	{
		public static Stopwatch StartupStopwatch;

		[STAThread]
		// XXX Note: MultiDomain doesn't seem to work with ClickOnce web install
		[LoaderOptimization(LoaderOptimization.MultiDomain)]
		public static void Main()
		{
			Thread.CurrentThread.Name = "CMain";
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Client");

			Trace.TraceInformation("Start");

			StartupStopwatch = Stopwatch.StartNew();

			var splashScreen = new System.Windows.SplashScreen("Images/splash.png");
			splashScreen.Show(true, true);

			var app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
}
