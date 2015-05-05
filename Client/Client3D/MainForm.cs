using SharpDX.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dwarrowdelf.Client
{
	public partial class MainForm : Form
	{
		public Control GameSurface { get { return gameSurfaceControl; } }

		public MainForm()
		{
			InitializeComponent();

			this.Width = 1024;
			this.Height = 800;
			this.Location = new System.Drawing.Point(300, 0);
			this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
		}
	}

	class GameSurfaceControl : RenderControl
	{

	}
}
