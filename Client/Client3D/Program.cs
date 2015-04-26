using System;

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

			using (var game = new MyGame())
				game.Run();
		}
	}
}
