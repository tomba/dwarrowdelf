using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Dwarrowdelf.Client.TileControl;
using Dwarrowdelf;
using Dwarrowdelf.Client;

namespace TileControlD3DWinFormsTest
{
	static class Program
	{
		[STAThread]
		static void Main()
		{
			var form = new Form1();
			form.Width = 1024;
			form.Height = 1024;
			form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			form.Show();

			int frames = 0;
			DateTime time = DateTime.Now;

			SlimDX.Windows.MessagePump.Run(form, () =>
			{
				frames++;

				var now = DateTime.Now;

				if (now >= time.AddSeconds(1))
				{
					var td = now - time;

					var fps = (int)(frames * (1000.0 / td.TotalMilliseconds));
					System.Diagnostics.Trace.TraceInformation("fps {0}", fps);

					frames = 0;
					time = now;
				}

				form.Render();
			});

		}
	}
}
