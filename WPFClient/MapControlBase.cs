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
	public class MapControlBase : FrameworkElement
	{
		int m_columns;
		int m_rows;
		
		double m_tileSize = 40;

		MapTile[] m_mapTiles = new MapTile[0];

		public MapControlBase()
		{
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

			return base.ArrangeOverride(s);
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}

		protected override int VisualChildrenCount
		{
			get { return m_mapTiles.Length; }
		}

		protected override Visual GetVisualChild(int index)
		{
			return m_mapTiles[index];
		}

		public MapTile GetTile(Location l)
		{
			return m_mapTiles[l.X + l.Y * m_columns];
		}

		public Location TileFromPoint(Point p)
		{
			return new Location((int)(p.X / m_tileSize), (int)(p.Y / m_tileSize));
		}
	}
}
