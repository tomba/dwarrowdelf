using System;

namespace Client3D
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			//SharpDX.Configuration.EnableObjectTracking = true;

			using (var game = new MyGame())
				game.Run();
		}
	}
}
