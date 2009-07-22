using System;
using System.Collections.Generic;
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
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace MyGame 
{
	public class MapControl : FrameworkElement
	{
		int m_columns = 5;
		int m_rows = 5;
		
		double m_tileSize = 40;

		Canvas m_effectsCanvas = new Canvas();
		Rectangle m_hiliteRectangle = new Rectangle();

		MapTile[] m_mapTiles = new MapTile[0];

		public MapControl()
		{
			this.Focusable = true;

			this.AddVisualChild(m_effectsCanvas);

			m_hiliteRectangle.Width = m_tileSize;
			m_hiliteRectangle.Height = m_tileSize;
			m_hiliteRectangle.Stroke = Brushes.Blue;
			m_hiliteRectangle.StrokeThickness = 2;
			m_effectsCanvas.Children.Add(m_hiliteRectangle);
		}

		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		public double TileSize
		{
			get { return m_tileSize; }

			set
			{
				if (value < 2)
					throw new ArgumentException();

				if (m_tileSize != value)
				{
					m_tileSize = value;

					m_hiliteRectangle.Width = m_tileSize;
					m_hiliteRectangle.Height = m_tileSize;

					InvalidateVisual();
				}
			}
		}

		protected override Size MeasureOverride(Size s)
		{
			int columns;
			int rows;

			if (Double.IsInfinity(s.Width))
				columns = 20;
			else
				columns = (int)(s.Width / m_tileSize);

			if (Double.IsInfinity(s.Height))
				rows = 20;
			else
				rows = (int)(s.Height / m_tileSize);

			m_effectsCanvas.Measure(s);

			return new Size(columns * m_tileSize, rows * m_tileSize);
		}

		void ReCreateMapTiles()
		{
			if (m_mapTiles != null)
			{
				for (int i = 0; i < m_mapTiles.Length; ++i)
				{
					this.RemoveVisualChild(m_mapTiles[i]);
					m_mapTiles[i] = null;
				}

				m_mapTiles = null;
			}

			m_mapTiles = new MapTile[m_columns * m_rows];

			for (int i = 0; i < m_mapTiles.Length; ++i)
			{
				MapTile tile = new MapTile(this);
				m_mapTiles[i] = tile;
				this.AddVisualChild(tile);
			}
		}

		public event Action DimensionsChangedEvent;

		protected override Size ArrangeOverride(Size s)
		{
			int newColumns = (int)(s.Width / m_tileSize);
			int newRows = (int)(s.Height / m_tileSize);

			if (newColumns != m_columns || newRows != m_rows)
			{
				m_columns = newColumns;
				m_rows = newRows;

				ReCreateMapTiles();

				if (DimensionsChangedEvent != null)
					DimensionsChangedEvent();
			}

			for (int i = 0; i < m_mapTiles.Length; ++i)
			{
				int y = i / m_columns;
				int x = i % m_columns;
				m_mapTiles[i].Arrange(new Rect(x * m_tileSize, y * m_tileSize, m_tileSize, m_tileSize));
			}

			m_effectsCanvas.Arrange(new Rect(this.RenderSize));

			return base.ArrangeOverride(s);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}

		protected override int VisualChildrenCount
		{
			// +1 for effect canvas
			get { return m_mapTiles.Length + 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			if (index < m_mapTiles.Length)
				return m_mapTiles[index];

			// canvas is last, so it's on top of tiles
			return m_effectsCanvas;
		}

		public MapTile GetTile(int x, int y)
		{
			return m_mapTiles[x + y * m_columns];
		}

		public void TileFromPoint(Point p, out int x, out int y)
		{
			x = (int)(p.X / m_tileSize);
			y = (int)(p.Y / m_tileSize);
		}
	}
}
