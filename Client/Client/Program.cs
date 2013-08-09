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
		// Note: MultiDomain is faster when using separate appdomain for server
		// XXX MultiDomain doesn't seem to work with ClickOnce web install
		//[LoaderOptimization(LoaderOptimization.MultiDomain)]
		public static void Main(string[] args)
		{
			Program.StartupStopwatch = Stopwatch.StartNew();

			Thread.CurrentThread.Name = "CMain";
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Client");

			Trace.TraceInformation("Start");

			if (Debugger.IsAttached == false)
			{
				var splashScreen = new System.Windows.SplashScreen("Images/splash.png");
				splashScreen.Show(true, true);
			}

			var app = new App();
			app.InitializeComponent();
			app.Run();

			Trace.TraceInformation("Stop");
		}
	}
}
