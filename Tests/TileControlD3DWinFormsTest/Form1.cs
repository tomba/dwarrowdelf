using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Dwarrowdelf.Client.TileControl;
using Dwarrowdelf;
using Dwarrowdelf.Client;

namespace TileControlD3DWinFormsTest
{
	public partial class Form1 : Form
	{
		WinFormsScene m_scene;

		SymbolDrawingCache m_symbolDrawingCache;
		RenderData<RenderTileDetailed> m_renderData;

		public Form1()
		{
			m_scene = new WinFormsScene(this.Handle);

			m_symbolDrawingCache = new SymbolDrawingCache(new Uri("/Symbols/SymbolInfosGfx.xaml", UriKind.Relative));
			m_scene.SymbolDrawingCache = m_symbolDrawingCache;

			m_renderData = new RenderData<RenderTileDetailed>();
			m_scene.SetRenderData(m_renderData);

			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			m_scene.Dispose();
		}

		protected override void OnResize(EventArgs e)
		{
			m_scene.Resize(this.ClientSize.Width, this.ClientSize.Height);

			base.OnResize(e);
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			var ts = m_scene.TileSize;

			if (e.Delta > 0)
				ts *= 2;
			else
				ts /= 2;

			if (ts < 2)
				ts = 2;

			if (ts > 512)
				ts = 512;

			m_scene.TileSize = ts;

			base.OnMouseWheel(e);
		}

		public void Render()
		{
			var arr = m_renderData.ArrayGrid.Grid;

			Array.Clear(arr, 0, arr.Length);

			foreach (var sp in m_renderData.Bounds.Range())
			{
				var x = sp.X;
				var y = sp.Y;

				if (x == y)
				{
					arr[y, x].Floor.SymbolID = SymbolID.Grass;
					arr[y, x].Floor.Color = GameColor.None;
					arr[y, x].Interior.SymbolID = (SymbolID)((x % 10) + 1);
					arr[y, x].Interior.Color = (GameColor)((x % ((int)GameColor.NumColors - 1)) + 1);
				}
				else
				{
					arr[y, x].Floor.SymbolID = SymbolID.Grass;
					arr[y, x].Floor.Color = GameColor.None;
				}
			}

			m_scene.Render();

			m_scene.Present();
		}
	}
}
