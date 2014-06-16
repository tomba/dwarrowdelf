using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client3D
{
	internal partial class DebugForm : Form
	{
		TestRenderer m_scene;
		Timer m_timer;

		public DebugForm()
		{
			InitializeComponent();

			m_timer = new Timer();
			m_timer.Tick += timer_Tick;
			m_timer.Interval = 1000;
			m_timer.Start();
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			m_timer.Stop();

			base.OnClosing(e);
		}

		public void SetScene(TestRenderer scene)
		{
			m_scene = scene;

			this.viewCorner1TextBox.Text = scene.ViewCorner1.ToString();
			this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();

			this.zCutTrackBar.Maximum = scene.Map.Depth - 1;
			this.zCutTrackBar.Value = scene.ViewCorner2.Z;
			this.zCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner2 = scene.ViewCorner2.SetZ(this.zCutTrackBar.Value);
				this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();
			};

			this.xnCutTrackBar.Maximum = scene.Map.Width - 1;
			this.xnCutTrackBar.Value = scene.ViewCorner1.X;
			this.xnCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner1 = scene.ViewCorner1.SetX(this.xnCutTrackBar.Value);
				this.xnCutTrackBar.Value = scene.ViewCorner1.X;
				this.viewCorner1TextBox.Text = scene.ViewCorner1.ToString();
			};

			this.xpCutTrackBar.Maximum = scene.Map.Width - 1;
			this.xpCutTrackBar.Value = scene.ViewCorner2.X;
			this.xpCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner2 = scene.ViewCorner2.SetX(this.xpCutTrackBar.Value);
				this.xpCutTrackBar.Value = scene.ViewCorner2.X;
				this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();
			};

			this.ynCutTrackBar.Maximum = scene.Map.Width - 1;
			this.ynCutTrackBar.Value = scene.ViewCorner1.Y;
			this.ynCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner1 = scene.ViewCorner1.SetY(this.ynCutTrackBar.Value);
				this.ynCutTrackBar.Value = scene.ViewCorner1.Y;
				this.viewCorner1TextBox.Text = scene.ViewCorner1.ToString();
			};

			this.ypCutTrackBar.Maximum = scene.Map.Width - 1;
			this.ypCutTrackBar.Value = scene.ViewCorner2.Y;
			this.ypCutTrackBar.ValueChanged += (s, e) =>
			{
				scene.ViewCorner2 = scene.ViewCorner2.SetY(this.ypCutTrackBar.Value);
				this.ypCutTrackBar.Value = scene.ViewCorner2.Y;
				this.viewCorner2TextBox.Text = scene.ViewCorner2.ToString();
			};
		}

		void timer_Tick(object sender, EventArgs e)
		{
			var cam = m_scene.Services.GetService<ICameraService>();

			this.camPosTextBox.Text = String.Format("{0:F2}/{1:F2}/{2:F2}",
				cam.Position.X, cam.Position.Y, cam.Position.Z);
			this.vertRendTextBox.Text = m_scene.VerticesRendered.ToString();
			this.chunkRecalcsTextBox.Text = m_scene.ChunkRecalcs.ToString();
		}
	}
}
