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
				var wnd = new MainForm();

				// This must be created after touching WinForms, so that SynchronizationContext is valid
				GameData.Create();

				using (var game = new MyGame())
				{
					wnd.Show();

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
