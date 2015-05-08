using System;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Client
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			//SharpDX.Configuration.EnableObjectTracking = true;

			System.Threading.Thread.CurrentThread.Name = "CMain";
			MyTraceContext.ThreadTraceContext = new MyTraceContext("Client");

			Trace.TraceInformation("Start");

			var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "save");
			if (Directory.Exists(path) == false)
				Directory.CreateDirectory(path);

			var app = new App();
			//app.InitializeComponent(); // XXX enable if app contains resources
			app.Run();

			Trace.TraceInformation("Stop");
		}
	}
}
