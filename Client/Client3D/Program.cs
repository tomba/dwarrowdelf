using System;
using System.Diagnostics;
using System.IO;

namespace Dwarrowdelf.Client
{
	enum ThreeDMode
	{
		WpfSharpDXElement,
		WpfHwndHost,
		WinForms,
	}

	static class Program
	{
		public static readonly ThreeDMode Mode = ThreeDMode.WinForms;

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

			if (Mode == ThreeDMode.WinForms)
			{
				using (var game = new MyGame())
				{
					var wnd = new MainForm();
					wnd.Show();

					// this will force instantiating GameData.Data in the main thread
					GameData.Create();

					var debugForm = new DebugForm(game);
					debugForm.Show(wnd);

					wnd.Activate();

					game.Run(wnd.GameSurface);
				}
			}
			else
			{
				var app = new App();
				app.InitializeComponent();
				app.Run();
			}

			Trace.TraceInformation("Stop");
		}
	}
}
