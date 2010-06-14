using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MyGame.Client
{
	public abstract class TileControl<T> : Control where T : UIElement, new()
	{
		int m_columns;
		int m_rows;

		T[,] m_tileArray;
		T[,] m_backTileArray;

		Vector m_offset;
		int m_tileSize;

		bool m_updateNeeded;

		protected TileControl()
		{
			m_tileArray = new T[0, 0];
			m_backTileArray = new T[0, 0];

			ClipToBounds = true; // does this slow down?
		}

		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		public int TileSize
		{
			get { return m_tileSize; }
			set
			{
				value = MyMath.IntClamp(value, 64, 8);

				m_tileSize = value;

				UpdateColumnsRows(this.RenderSize, value);
				UpdateOffset(this.RenderSize, value);
				OnTileSizeChanged(value);
			}
		}

		protected virtual void OnTileSizeChanged(int newSize) { }

		void UpdateColumnsRows(Size size, int tileSize)
		{
			// odd number of columns and rows, so that the center tile is in the center
			int newColumns = MyMath.IntDivRound((int)Math.Ceiling(size.Width), tileSize) | 1;
			int newRows = MyMath.IntDivRound((int)Math.Ceiling(size.Height), tileSize) | 1;

			if (newColumns != m_columns || newRows != m_rows)
			{
				//MyDebug.WriteLine("UpdateColumnsRows");

				m_columns = newColumns;
				m_rows = newRows;

				ReCreateMapTiles();

				OnGridSizeChanged(newColumns, newRows);
			}
		}

		protected virtual void OnGridSizeChanged(int newColumns, int newRows) { }

		void ReCreateMapTiles()
		{
			//MyDebug.WriteLine("ReCreateMapTiles");

			foreach (var tile in m_tileArray)
				this.RemoveVisualChild(tile);

			int numTiles = m_columns * m_rows;

			m_tileArray = new T[m_rows, m_columns];
			m_backTileArray = new T[m_rows, m_columns];

			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var tile = new T();
					this.AddVisualChild(tile);
					m_tileArray[y, x] = tile;
				}
			}

			InvalidateTiles();
		}

		void UpdateOffset(Size size, int tileSize)
		{
			var dx = ((tileSize * m_columns) - (int)Math.Ceiling(size.Width)) / 2;
			var dy = ((tileSize * m_rows) - (int)Math.Ceiling(size.Height)) / 2;
			m_offset = new Vector(dx, dy);
		}

		protected void InvalidateTiles()
		{
			m_updateNeeded = true;
			InvalidateArrange();
		}

		protected override Size ArrangeOverride(Size s)
		{
			//MyDebug.WriteLine("Arrange");

			UpdateColumnsRows(s, this.TileSize);
			UpdateOffset(s, this.TileSize);

			if (this.RenderSize != s)
			{
				OnSizeChanged();
			}

			if (m_updateNeeded)
			{
				m_updateNeeded = false;
				UpdateTilesOverride(m_tileArray);
			}

			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var tile = m_tileArray[y, x];

					var p = new Point(x * this.TileSize, y * this.TileSize);
					p -= m_offset;
					var rect = new Rect(p, new Size(this.TileSize, this.TileSize));
					tile.Arrange(rect);
				}
			}

			return s;
		}

		protected virtual void OnSizeChanged() { }
		protected abstract void UpdateTilesOverride(T[,] tileArray);

		// Tells TileControl to scroll and wrap the tiles. This optimizes drawing, as most of the contents of the tiles stay the same
		protected void ScrollTiles(IntVector v)
		{
			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					int toX = (x - v.X) % m_columns;
					if (toX < 0)
						toX += m_columns;

					int toY = (y + v.Y) % m_rows;
					if (toY < 0)
						toY += m_rows;

					m_backTileArray[toY, toX] = m_tileArray[y, x];
				}
			}

			var tmp = m_tileArray;
			m_tileArray = m_backTileArray;
			m_backTileArray = tmp;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}

		protected override int VisualChildrenCount
		{
			get { return m_columns * m_rows; }
		}

		protected override Visual GetVisualChild(int index)
		{
			int x = index % m_columns;
			int y = index / m_columns;
			return m_tileArray[y, x];
		}

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			p += m_offset;
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p -= m_offset;
			return p;
		}
	}

}
