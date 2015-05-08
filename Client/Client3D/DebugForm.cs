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

namespace Dwarrowdelf.Client
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
			//m_timer.Start();

			var viewGridProvider = game.ViewGrid;
			viewGridProvider.ViewGridCornerChanged += OnViewGridCornerChanged;

			this.viewCorner1TextBox.Text = viewGridProvider.ViewCorner1.ToString();
			this.viewCorner2TextBox.Text = viewGridProvider.ViewCorner2.ToString();

			this.zCutTrackBar.ValueChanged += (s, e) =>
			{
				viewGridProvider.ViewCorner2 = viewGridProvider.ViewCorner2.SetZ(this.zCutTrackBar.Value);
				this.viewCorner2TextBox.Text = viewGridProvider.ViewCorner2.ToString();
			};

			this.xnCutTrackBar.ValueChanged += (s, e) =>
			{
				viewGridProvider.ViewCorner1 = viewGridProvider.ViewCorner1.SetX(this.xnCutTrackBar.Value);
				this.xnCutTrackBar.Value = viewGridProvider.ViewCorner1.X;
				this.viewCorner1TextBox.Text = viewGridProvider.ViewCorner1.ToString();
			};

			this.xpCutTrackBar.ValueChanged += (s, e) =>
			{
				viewGridProvider.ViewCorner2 = viewGridProvider.ViewCorner2.SetX(this.xpCutTrackBar.Value);
				this.xpCutTrackBar.Value = viewGridProvider.ViewCorner2.X;
				this.viewCorner2TextBox.Text = viewGridProvider.ViewCorner2.ToString();
			};

			this.ynCutTrackBar.ValueChanged += (s, e) =>
			{
				viewGridProvider.ViewCorner1 = viewGridProvider.ViewCorner1.SetY(this.ynCutTrackBar.Value);
				this.ynCutTrackBar.Value = viewGridProvider.ViewCorner1.Y;
				this.viewCorner1TextBox.Text = viewGridProvider.ViewCorner1.ToString();
			};

			this.ypCutTrackBar.ValueChanged += (s, e) =>
			{
				viewGridProvider.ViewCorner2 = viewGridProvider.ViewCorner2.SetY(this.ypCutTrackBar.Value);
				this.ypCutTrackBar.Value = viewGridProvider.ViewCorner2.Y;
				this.viewCorner2TextBox.Text = viewGridProvider.ViewCorner2.ToString();
			};

			this.checkBox3.CheckedChanged += (s, e) => m_scene.Effect.DisableBorders = checkBox3.Checked;
			this.checkBox4.CheckedChanged += (s, e) => m_scene.Effect.DisableLight = checkBox4.Checked;
			this.checkBox5.CheckedChanged += (s, e) => m_scene.Effect.DisableOcclusion = checkBox5.Checked;
			this.checkBox7.CheckedChanged += (s, e) => m_scene.Effect.DisableTexture = checkBox7.Checked;
		}

		void OnViewGridCornerChanged(Dwarrowdelf.IntVector3 arg1, Dwarrowdelf.IntVector3 arg2)
		{
			var map = GameData.Data.Map;

			if (map != null)
			{
				this.zCutTrackBar.Maximum = map.Depth - 1;
				this.xnCutTrackBar.Maximum = map.Width - 1;
				this.xpCutTrackBar.Maximum = map.Width - 1;
				this.ynCutTrackBar.Maximum = map.Width - 1;
				this.ypCutTrackBar.Maximum = map.Height - 1;
			}

			var viewGridProvider = m_game.ViewGrid;

			this.zCutTrackBar.Value = viewGridProvider.ViewCorner2.Z;
			this.xnCutTrackBar.Value = viewGridProvider.ViewCorner1.X;
			this.xpCutTrackBar.Value = viewGridProvider.ViewCorner2.X;
			this.ynCutTrackBar.Value = viewGridProvider.ViewCorner1.Y;
			this.ypCutTrackBar.Value = viewGridProvider.ViewCorner2.Y;
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

		int m_c0;
		int m_c1;
		int m_c2;

		void timer_Tick(object sender, EventArgs e)
		{
			Camera cam = null; // m_scene.Services.GetService<CameraProvider>();

			var campos = cam.Position;
			var chunkpos = (campos / Chunk.CHUNK_SIZE).ToFloorIntVector3();

			this.camPosTextBox.Text = String.Format("{0:F2}/{1:F2}/{2:F2} (Chunk {3}/{4}/{5})",
				campos.X, campos.Y, campos.Z,
				chunkpos.X, chunkpos.Y, chunkpos.Z);
			this.vertRendTextBox.Text = m_scene.VerticesRendered.ToString();
			this.chunksRenderedTextBox.Text = m_scene.ChunkManager.ChunkCountDebug;
			this.chunkRecalcsTextBox.Text = m_scene.ChunkRecalcs.ToString();
			m_scene.ChunkRecalcs = 0;

			var c0 = GC.CollectionCount(0);
			var c1 = GC.CollectionCount(1);
			var c2 = GC.CollectionCount(2);

			var d0 = c0 - m_c0;
			var d1 = c1 - m_c1;
			var d2 = c2 - m_c2;

			textBox1.Text = String.Format("GC0 {0}/s, GC1 {1}/s, GC2 {2}/s",
				d0, d1, d2);

			m_c0 = c0;
			m_c1 = c1;
			m_c2 = c2;
		}
	}
}
