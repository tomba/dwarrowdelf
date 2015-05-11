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

		public DebugWindow()
		{
			InitializeComponent();
		}

		internal void SetGame(MyGame game)
		{
			m_data = new DebugWindowData(game);
			this.DataContext = m_data;

			m_timer = new DispatcherTimer();
			m_timer.Tick += m_data.Update;
			m_timer.Interval = TimeSpan.FromSeconds(1);
			m_timer.IsEnabled = true;
		}
	}

	class DebugWindowData : INotifyPropertyChanged
	{
		MyGame m_game;

		public DebugWindowData(MyGame game)
		{
			m_game = game;
		}

		public string CameraPos { get; set; }
		public string Vertices { get; set; }
		public string Chunks { get; set; }
		public string ChunkRecalcs { get; set; }
		public string GC { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		int m_c0;
		int m_c1;
		int m_c2;

		public void Update(object sender, EventArgs e)
		{
			var campos = m_game.Camera.Position;
			var chunkpos = (campos / Chunk.CHUNK_SIZE).ToFloorIntVector3();

			var terrainRenderer = m_game.TerrainRenderer;

			this.CameraPos= String.Format("{0:F2}/{1:F2}/{2:F2} (Chunk {3}/{4}/{5})",
				campos.X, campos.Y, campos.Z,
				chunkpos.X, chunkpos.Y, chunkpos.Z);
			this.Vertices = terrainRenderer.VerticesRendered.ToString();
			this.Chunks = terrainRenderer.ChunkManager.ChunkCountDebug;
			this.ChunkRecalcs = terrainRenderer.ChunkRecalcs.ToString();
			terrainRenderer.ChunkRecalcs = 0;




			var c0 = System.GC.CollectionCount(0);
			var c1 = System.GC.CollectionCount(1);
			var c2 = System.GC.CollectionCount(2);

			var d0 = c0 - m_c0;
			var d1 = c1 - m_c1;
			var d2 = c2 - m_c2;

			this.GC = String.Format("GC0 {0}/s, GC1 {1}/s, GC2 {2}/s",
				d0, d1, d2);

			m_c0 = c0;
			m_c1 = c1;
			m_c2 = c2;



			Notify("");
		}

		void Notify(string name)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(name));
		}
	}
}
