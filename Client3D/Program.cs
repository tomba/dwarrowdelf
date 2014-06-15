using System;

namespace Client3D
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			using (var game = new MyGame())
				game.Run();
		}
	}
}
