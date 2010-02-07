using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using MyGame;
using MyGame.Client;

namespace WPFMapControlTest
{
	class MapControl2 : MapControlBase2
	{
		Grid2D<byte> m_map;

		public MapControl2()
		{
			m_map = new Grid2D<byte>(512, 512);
			for (int y = 0; y < m_map.Height; ++y)
			{
				for (int x = 0; x < m_map.Width; ++x)
				{
					m_map[x, y] = (x + (y % 2)) % 2 == 0 ? (byte)50 : (byte)255;
				}
			}
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var ml = base.ScreenPointToMapLocation(e.GetPosition(this));
			if (m_map.Bounds.Contains(ml))
			{
				m_map[ml] = (byte)(~m_map[ml]);
				base.InvalidateTiles();
			}
		}

		protected override Visual CreateTile(double x, double y)
		{
			return new MapControlTile2(this, x, y);
		}

		// called for each visible tile
		protected override void UpdateTile(Visual _tile, IntPoint ml)
		{
			MapControlTile2 tile = (MapControlTile2)_tile;

			Color c;

			if (m_map.Bounds.Contains(ml))
			{

				byte b = m_map[ml.X, ml.Y];
				c = Color.FromRgb(b, b, b);
			}
			else
			{
				c = Color.FromRgb(0, 0, 0);
			}

			if (c != tile.Color)
			{
				tile.Color = c;
				tile.Update();
			}
		}

		class MapControlTile2 : DrawingVisual
		{
			MapControl2 m_parent;
			public Color Color { get; set; }

			public MapControlTile2(MapControl2 parent, double x, double y)
			{
				m_parent = parent;
				this.VisualOffset = new Vector(x, y);
			}

			public void Update()
			{
				var drawingContext = this.RenderOpen();
				drawingContext.DrawRectangle(new SolidColorBrush(this.Color), null,
					new Rect(new Size(m_parent.TileSize, m_parent.TileSize)));
				drawingContext.Close();
			}
		}
	}

}
