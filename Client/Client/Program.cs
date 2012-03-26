using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	static class Program
	{
		[STAThread]
		[LoaderOptimization(LoaderOptimization.MultiDomain)]
		public static void Main()
		{
			var app = new App();
			app.InitializeComponent();
			app.Run();
		}
	}
}
