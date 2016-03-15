using SharpDX;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Dwarrowdelf.Client
{
	public partial class DebugWindow : Window
	{
		DebugWindowData m_data;
		DispatcherTimer m_timer;
		MyGame m_game;

		public DebugWindow()
		{
			InitializeComponent();
		}

		internal void SetGame(MyGame game)
		{
			m_game = game;

			m_data = new DebugWindowData(game);
			this.DataContext = m_data;

			m_timer = new DispatcherTimer();
			m_timer.Tick += m_data.Update;
			m_timer.Interval = TimeSpan.FromSeconds(0.25);
			m_timer.IsEnabled = true;

			var m_scene = m_game.TerrainRenderer;

			cbBorders.Checked += (s, e) => m_scene.Effect.DisableBorders = true;
			cbBorders.Unchecked += (s, e) => m_scene.Effect.DisableBorders = false;

			cbLight.Checked += (s, e) => m_scene.Effect.DisableLight = true;
			cbLight.Unchecked += (s, e) => m_scene.Effect.DisableLight = false;

			cbOcclusion.Checked += (s, e) => m_scene.Effect.DisableOcclusion = true;
			cbOcclusion.Unchecked += (s, e) => m_scene.Effect.DisableOcclusion = false;

			cbTexture.Checked += (s, e) => m_scene.Effect.DisableTexture = true;
			cbTexture.Unchecked += (s, e) => m_scene.Effect.DisableTexture = false;

			cbOcclusionDebug.Checked += (s, e) => m_scene.Effect.EnableOcclusionDebug = true;
			cbOcclusionDebug.Unchecked += (s, e) => m_scene.Effect.EnableOcclusionDebug = false;

			cbBigUnknownChunk.Checked += (s, e) => { Chunk.UseBigUnknownChunk = true; m_scene.ChunkManager.InvalidateChunks(); };
			cbBigUnknownChunk.Unchecked += (s, e) => { Chunk.UseBigUnknownChunk = false; m_scene.ChunkManager.InvalidateChunks(); };

			buttonInvalidate.Click += (s, e) => m_scene.ChunkManager.InvalidateChunks();

			cbWireframe.Checked += OnRenderStateCheckBoxChanged;
			cbWireframe.Unchecked += OnRenderStateCheckBoxChanged;
			cbCulling.Checked += OnRenderStateCheckBoxChanged;
			cbCulling.Unchecked += OnRenderStateCheckBoxChanged;

			tunable1.Value = m_scene.Effect.Tunable1;
			tunable1.ValueChanged += (s, e) => { m_scene.Effect.Tunable1 = (float)tunable1.Value; };

			tunable2.Value = m_scene.Effect.Tunable2;
			tunable2.ValueChanged += (s, e) => { m_scene.Effect.Tunable2 = (float)tunable2.Value; };
		}

		void OnRenderStateCheckBoxChanged(object sender, EventArgs e)
		{
			bool disableCull = cbCulling.IsChecked.Value;
			bool wire = cbWireframe.IsChecked.Value;

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

	}

	class DebugWindowData : INotifyPropertyChanged
	{
		MyGame m_game;

		public DebugWindowData(MyGame game)
		{
			m_game = game;

			m_game.ViewGridProvider.ViewGridCornerChanged += (o, n) => Notify("");
		}

		public string CameraPos
		{
			get
			{
				var campos = m_game.Camera.Position;
				var chunkpos = (campos / Chunk.CHUNK_SIZE).ToFloorIntVector3();
				return String.Format("{0:F2}/{1:F2}/{2:F2} (Chunk {3}/{4}/{5})",
					campos.X, campos.Y, campos.Z,
					chunkpos.X, chunkpos.Y, chunkpos.Z);
			}
		}

		public int Vertices { get { return m_game.TerrainRenderer.VerticesRendered; } }
		public string Chunks { get { return m_game.TerrainRenderer.ChunkManager.ChunkCountDebug; } }
		public int ChunkRecalcs { get { return m_game.TerrainRenderer.ChunkRecalcs; } }
		public string GC { get; set; }

		public IntVector3 ViewCorner1 { get { return m_game.ViewGridProvider.ViewCorner1; } }
		public IntVector3 ViewCorner2 { get { return m_game.ViewGridProvider.ViewCorner2; } }

		public int ViewMaxX
		{
			get { return m_game.Environment != null ? m_game.Environment.Width - 1 : 0; }
		}

		public int ViewMaxY
		{
			get { return m_game.Environment != null ? m_game.Environment.Height - 1 : 0; }
		}

		public int ViewMaxZ
		{
			get { return m_game.Environment != null ? m_game.Environment.Depth - 1 : 0; }
		}

		public int ViewZ
		{
			get { return m_game.ViewGridProvider.ViewCorner2.Z; }
			set { m_game.ViewGridProvider.ViewCorner2 = m_game.ViewGridProvider.ViewCorner2.SetZ(value); }
		}

		public int ViewX1
		{
			get { return m_game.ViewGridProvider.ViewCorner1.X; }
			set { m_game.ViewGridProvider.ViewCorner1 = m_game.ViewGridProvider.ViewCorner1.SetX(value); }
		}

		public int ViewX2
		{
			get { return m_game.ViewGridProvider.ViewCorner2.X; }
			set { m_game.ViewGridProvider.ViewCorner2 = m_game.ViewGridProvider.ViewCorner2.SetX(value); }
		}

		public int ViewY1
		{
			get { return m_game.ViewGridProvider.ViewCorner1.Y; }
			set { m_game.ViewGridProvider.ViewCorner1 = m_game.ViewGridProvider.ViewCorner1.SetY(value); }
		}

		public int ViewY2
		{
			get { return m_game.ViewGridProvider.ViewCorner2.Y; }
			set { m_game.ViewGridProvider.ViewCorner2 = m_game.ViewGridProvider.ViewCorner2.SetY(value); }
		}

		public string VoxelData
		{
			get
			{
				var mln = m_game.MousePositionService.MapLocation;

				if (mln.HasValue)
				{
					var ml = mln.Value;

					return m_game.TerrainRenderer.ChunkManager.GetChunkDebug(ml);
				}
				else
				{
					return "";
				}
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void Update(object sender, EventArgs e)
		{
			Notify("");

			m_game.TerrainRenderer.ChunkRecalcs = 0;
		}

		void Notify(string name)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}
}
