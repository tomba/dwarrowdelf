using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace MyGame.Client
{
	public abstract class MapControlBase : Control
	{
		int m_columns;
		int m_rows;

		DispatcherTimer m_updateTimer;

		UIElement[,] m_tileArray;
		UIElement[,] m_backTileArray;

		public event Action MapChanged;

		protected MapControlBase()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(15);

			m_tileArray = new UIElement[0, 0];
			m_backTileArray = new UIElement[0, 0];

			ClipToBounds = true; // does this slow down?
		}

		public int Columns { get { return m_columns; } }
		public int Rows { get { return m_rows; } }

		public static readonly DependencyProperty TileSizeProperty = DependencyProperty.Register(
			"TileSize", typeof(int), typeof(MapControlBase),
			new FrameworkPropertyMetadata(32, FrameworkPropertyMetadataOptions.AffectsArrange),
			v => ((int)v) >= 2);

		public int TileSize
		{
			get { return (int)GetValue(TileSizeProperty); }
			set
			{
				SetValue(TileSizeProperty, value);

				UpdateColumnsRows(this.RenderSize, value);
				UpdateOffset(this.RenderSize, value);
			}
		}

		void UpdateColumnsRows(Size size, int tileSize)
		{
			// odd number of columns and rows, so that the center tile is in the center
			int newColumns = (int)Math.Ceiling(size.Width / tileSize) | 1;
			int newRows = (int)Math.Ceiling(size.Height / tileSize) | 1;

			if (newColumns != m_columns || newRows != m_rows)
			{
				m_columns = newColumns;
				m_rows = newRows;

				ReCreateMapTiles();

				if (MapChanged != null)
					MapChanged();
			}
		}

		protected IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-this.Columns / 2, this.Rows / 2); }
		}

		IntPoint m_centerPos;
		public IntPoint CenterPos
		{
			get { return m_centerPos; }
			set
			{
				if (value == this.CenterPos)
					return;

				var v = value - this.CenterPos;

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

				m_centerPos = value;
				UpdateTiles();
				InvalidateArrange();
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			//MyDebug.WriteLine("Measure");

			if (double.IsPositiveInfinity(availableSize.Width) ||
				double.IsPositiveInfinity(availableSize.Height))
				return new Size(640, 480);

			return availableSize;
		}

		void ReCreateMapTiles()
		{
			foreach (var tile in m_tileArray)
				this.RemoveVisualChild(tile);

			int numTiles = m_columns * m_rows;

			m_tileArray = new UIElement[m_rows, m_columns];
			m_backTileArray = new UIElement[m_rows, m_columns];

			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var tile = CreateTile();
					this.AddVisualChild(tile);
					m_tileArray[y, x] = tile;
				}
			}

			UpdateTiles();
		}

		Vector m_offset;
		void UpdateOffset(Size size, int tileSize)
		{
			var dx = ((tileSize * m_columns) - size.Width) / 2;
			var dy = ((tileSize * m_rows) - size.Height) / 2;
			m_offset = new Vector(dx, dy);
		}

		protected override Size ArrangeOverride(Size s)
		{
			UpdateColumnsRows(s, this.TileSize);
			UpdateOffset(s, this.TileSize);

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

		public UIElement GetTile(IntPoint l)
		{
			return m_tileArray[l.Y, l.X];
		}

		public IntPoint ScreenPointToScreenLocation(Point p)
		{
			p += m_offset;
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var loc = ScreenPointToScreenLocation(p);
			loc = new IntPoint(loc.X, -loc.Y);
			return loc + (IntVector)this.TopLeftPos;
		}

		public Point MapLocationToScreenPoint(IntPoint loc)
		{
			loc -= (IntVector)this.TopLeftPos;
			loc = new IntPoint(loc.X, -loc.Y + 1);
			var p = new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
			p -= m_offset;
			return p;
		}

		public Point ScreenLocationToScreenPoint(IntPoint loc)
		{
			throw new NotImplementedException();
		}

		public void InvalidateTiles()
		{
			//MyDebug.WriteLine("InvalidateTiles");
			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			m_updateTimer.Stop();
			UpdateTiles();
		}

		protected void UpdateTiles()
		{
			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var tile = m_tileArray[y, x];

					IntPoint loc = this.TopLeftPos + new IntVector(x, -y);

					UpdateTile(tile, loc, new IntPoint(x, y));
				}
			}
		}

		protected abstract UIElement CreateTile();
		protected abstract void UpdateTile(UIElement tile, IntPoint loc, IntPoint sl);
	}

}
