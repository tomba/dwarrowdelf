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

		Visual[] m_tileCollection;
		Visual[] m_backTileCollection;

		protected MapControlBase()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(15);

			m_tileCollection = new Visual[0];

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

				int newColumns = (int)Math.Ceiling(this.ActualWidth / this.TileSize);
				int newRows = (int)Math.Ceiling(this.ActualHeight / this.TileSize);

				if (newColumns != m_columns || newRows != m_rows)
				{
					m_columns = newColumns;
					m_rows = newRows;

					ReCreateMapTiles();
				}

			}
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
						int toX;

						toX = (x - v.X) % m_columns;
						if (toX < 0)
							toX += m_columns;

						int toY;
						toY = (y + v.Y) % m_rows;
						if (toY < 0)
							toY += m_rows;

						m_backTileCollection[toY * m_columns + toX] = m_tileCollection[y * m_columns + x];
					}
				}

				var tmp = m_tileCollection;
				m_tileCollection = m_backTileCollection;
				m_backTileCollection = tmp;

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
			foreach (var tile in m_tileCollection)
				this.RemoveVisualChild(tile);

			int numTiles = m_columns * m_rows;

			m_tileCollection = new Visual[numTiles];
			m_backTileCollection = new Visual[numTiles];

			for (int i = 0; i < m_tileCollection.Length; ++i)
			{
				var tile = CreateTile();
				this.AddVisualChild(tile);
				m_tileCollection[i] = tile;
			}

			UpdateTiles();
		}

		protected override Size ArrangeOverride(Size s)
		{
			int newColumns = (int)Math.Ceiling(s.Width / this.TileSize);
			int newRows = (int)Math.Ceiling(s.Height / this.TileSize);

			if (newColumns != m_columns || newRows != m_rows)
			{
				m_columns = newColumns;
				m_rows = newRows;

				ReCreateMapTiles();
			}

			int i = 0;
			foreach (UIElement tile in m_tileCollection)
			{
				int y = i / m_columns;
				int x = i % m_columns;
				tile.Arrange(new Rect(x * this.TileSize, y * this.TileSize, this.TileSize, this.TileSize));
				++i;
			}

			return s;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}

		protected override int VisualChildrenCount
		{
			get { return m_tileCollection.Length; }
		}

		protected override Visual GetVisualChild(int index)
		{
			return m_tileCollection[index];
		}

		public UIElement GetTile(IntPoint l)
		{
			return (UIElement)m_tileCollection[l.X + l.Y * m_columns];
		}

		IntPoint ScreenPointToScreenLocation(Point p)
		{
			return new IntPoint((int)(p.X / this.TileSize), (int)(p.Y / this.TileSize));
		}

		public IntPoint ScreenPointToMapLocation(Point p)
		{
			var loc = ScreenPointToScreenLocation(p);
			loc = new IntPoint(loc.X, -loc.Y);
			return loc + (IntVector)this.TopLeftPos;
		}

		Point MapLocationToScreenPoint(IntPoint loc)
		{
			loc -= (IntVector)this.TopLeftPos;
			loc = new IntPoint(loc.X, -loc.Y + 1);
			return new Point(loc.X * this.TileSize, loc.Y * this.TileSize);
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

		IntPoint TopLeftPos
		{
			get { return this.CenterPos + new IntVector(-m_columns / 2, m_rows / 2); }
		}

		protected void UpdateTiles()
		{
			int i = 0;
			foreach (UIElement tile in m_tileCollection)
			{
				int x = i % m_columns;
				int y = i / m_columns;
				IntPoint loc = this.TopLeftPos + new IntVector(x, -y);

				UpdateTile(tile, loc);

				++i;
			}
		}

		protected abstract UIElement CreateTile();
		protected abstract void UpdateTile(UIElement tile, IntPoint loc);
	}

}
