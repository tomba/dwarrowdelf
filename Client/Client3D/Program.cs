using System;
using System.IO;

namespace Client3D
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			//SharpDX.Configuration.EnableObjectTracking = true;

			System.Threading.Thread.CurrentThread.Name = "Main";

			System.Diagnostics.Trace.TraceInformation("Start");

			var path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), "save");
			if (Directory.Exists(path) == false)
				Directory.CreateDirectory(path);

			using (var game = new MyGame())
				game.Run();
		}
	}
}
