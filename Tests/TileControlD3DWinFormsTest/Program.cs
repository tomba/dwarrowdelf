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
			//form.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			form.Show();

			int frames = 0;
			DateTime time = DateTime.Now;

			SlimDX.Windows.MessagePump.Run(form, () =>
			{
				frames++;
				/*
				if (DateTime.Now >= time.AddSeconds(1))
				{
					form.SetFPS(frames);
					frames = 0;
					time = DateTime.Now;
				}
				*/

				form.Render();
			});

		}
	}
}
