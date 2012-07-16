using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	static class Program
	{
		public static Stopwatch StartupStopwatch;

		[STAThread]
		[LoaderOptimization(LoaderOptimization.MultiDomain)]
		public static void Main()
		{
			StartupStopwatch = Stopwatch.StartNew();

			var splashScreen = new System.Windows.SplashScreen("Images/splash.png");
			splashScreen.Show(true, true);

			var app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
}
