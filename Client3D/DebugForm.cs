using SharpDX;
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
		MyGame m_game;
		TerrainRenderer m_scene;
		Timer m_timer;

		public DebugForm(MyGame game)
		{
			m_game = game;
			m_scene = game.TerrainRenderer;

			InitializeComponent();

			this.checkBox1.CheckedChanged += checkBox_CheckedChanged;
			this.checkBox2.CheckedChanged += checkBox_CheckedChanged;

			this.checkBox6.CheckedChanged += (s, e) =>
			{
				SharpDX.Toolkit.Graphics.PresentInterval ival = checkBox6.Checked ? SharpDX.Toolkit.Graphics.PresentInterval.Immediate :
					SharpDX.Toolkit.Graphics.PresentInterval.One;

				m_game.GraphicsDevice.Presenter.Description.PresentationInterval = ival;
			};

			if (m_scene == null)
				return;

			m_timer = new Timer();
			m_timer.Tick += timer_Tick;
			m_timer.Interval = 1000;
			m_timer.Start();

			this.viewCorner1TextBox.Text = m_scene.ViewCorner1.ToString();
			this.viewCorner2TextBox.Text = m_scene.ViewCorner2.ToString();

			var map = GlobalData.VoxelMap;

			this.zCutTrackBar.Maximum = map.Depth - 1;
			this.zCutTrackBar.Value = m_scene.ViewCorner2.Z;
			this.zCutTrackBar.ValueChanged += (s, e) =>
			{
				m_scene.ViewCorner2 = m_scene.ViewCorner2.SetZ(this.zCutTrackBar.Value);
				this.viewCorner2TextBox.Text = m_scene.ViewCorner2.ToString();
			};

			this.xnCutTrackBar.Maximum = map.Width - 1;
			this.xnCutTrackBar.Value = m_scene.ViewCorner1.X;
			this.xnCutTrackBar.ValueChanged += (s, e) =>
			{
				m_scene.ViewCorner1 = m_scene.ViewCorner1.SetX(this.xnCutTrackBar.Value);
				this.xnCutTrackBar.Value = m_scene.ViewCorner1.X;
				this.viewCorner1TextBox.Text = m_scene.ViewCorner1.ToString();
			};

			this.xpCutTrackBar.Maximum = map.Width - 1;
			this.xpCutTrackBar.Value = m_scene.ViewCorner2.X;
			this.xpCutTrackBar.ValueChanged += (s, e) =>
			{
				m_scene.ViewCorner2 = m_scene.ViewCorner2.SetX(this.xpCutTrackBar.Value);
				this.xpCutTrackBar.Value = m_scene.ViewCorner2.X;
				this.viewCorner2TextBox.Text = m_scene.ViewCorner2.ToString();
			};

			this.ynCutTrackBar.Maximum = map.Width - 1;
			this.ynCutTrackBar.Value = m_scene.ViewCorner1.Y;
			this.ynCutTrackBar.ValueChanged += (s, e) =>
			{
				m_scene.ViewCorner1 = m_scene.ViewCorner1.SetY(this.ynCutTrackBar.Value);
				this.ynCutTrackBar.Value = m_scene.ViewCorner1.Y;
				this.viewCorner1TextBox.Text = m_scene.ViewCorner1.ToString();
			};

			this.ypCutTrackBar.Maximum = map.Height - 1;
			this.ypCutTrackBar.Value = m_scene.ViewCorner2.Y;
			this.ypCutTrackBar.ValueChanged += (s, e) =>
			{
				m_scene.ViewCorner2 = m_scene.ViewCorner2.SetY(this.ypCutTrackBar.Value);
				this.ypCutTrackBar.Value = m_scene.ViewCorner2.Y;
				this.viewCorner2TextBox.Text = m_scene.ViewCorner2.ToString();
			};

			this.checkBox3.CheckedChanged += (s, e) => m_scene.Effect.ShowBorders = checkBox3.Checked;
			this.checkBox4.CheckedChanged += (s, e) => m_scene.Effect.DisableLight = checkBox4.Checked;
			this.checkBox5.CheckedChanged += (s, e) => m_scene.Effect.DisableOcclusion = checkBox5.Checked;
			this.checkBox7.CheckedChanged += (s, e) => m_scene.Effect.DisableTexture = checkBox7.Checked;
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			if (m_timer != null)
				m_timer.Stop();

			base.OnClosing(e);
		}

		void checkBox_CheckedChanged(object sender, EventArgs e)
		{
			bool disableCull = checkBox1.Checked;
			bool wire = checkBox2.Checked;

			SharpDX.Toolkit.Graphics.RasterizerState state;

			if (!disableCull && !wire)
				state = m_game.GraphicsDevice.RasterizerStates.CullBack;
			else if (disableCull && !wire)
				state = m_game.GraphicsDevice.RasterizerStates.CullNone;
			else if (!disableCull && wire)
				state = m_game.GraphicsDevice.RasterizerStates.WireFrame;
			else if (disableCull && wire)
				state = m_game.GraphicsDevice.RasterizerStates.WireFrameCullNone;
			else
				throw new Exception();

			m_game.RasterizerState = state;
		}

		void timer_Tick(object sender, EventArgs e)
		{
			var cam = m_scene.Services.GetService<ICameraService>();

			var campos = cam.Position;
			var chunkpos = (campos / Chunk.CHUNK_SIZE).ToFloorIntVector3();

			this.camPosTextBox.Text = String.Format("{0:F2}/{1:F2}/{2:F2} (Chunk {3}/{4}/{5})",
				campos.X, campos.Y, campos.Z,
				chunkpos.X, chunkpos.Y, chunkpos.Z);
			this.vertRendTextBox.Text = m_scene.VerticesRendered.ToString();
			this.chunksRenderedTextBox.Text = m_scene.ChunksRendered.ToString();
			this.chunkRecalcsTextBox.Text = m_scene.ChunkRecalcs.ToString();
		}
	}
}
