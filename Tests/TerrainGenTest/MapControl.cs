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

namespace TerrainGenTest
{
	class MapControl : MapControlBase
	{
		Grid2D<double> m_originalGrid;
		Grid2D<double> m_grid;
		TerrainGen m_gen;

		public MapControl()
		{
			this.TileSize = 16;
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_gen = new TerrainGen(5, 10, 5, 0.75);
			m_originalGrid = m_gen.Grid;

			m_grid = m_originalGrid;
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		// called for each visible tile
		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			MapControlTile tile = (MapControlTile)_tile;

			var grid = m_grid;

			if (!grid.Bounds.Contains(ml))
			{
				tile.Color = Colors.Red;
				return;
			}

			var d = grid[ml];

			d = (d - m_gen.Min) / ((m_gen.Max - m_gen.Min) / 255);
			int i = (int)d;

			Color c = Color.FromRgb((byte)(i), (byte)(i), (byte)i);
			tile.Color = c;
		}
	}

	class MapControlTile : UIElement
	{
		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
			"Color", typeof(Color), typeof(MapControlTile),
			new PropertyMetadata(ValueChangedCallback));

		public Color Color
		{
			get { return (Color)GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		static void ValueChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs e)
		{
			((MapControlTile)ob).InvalidateVisual();
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(new SolidColorBrush(this.Color), null, new Rect(this.RenderSize));
		}
	}
}
