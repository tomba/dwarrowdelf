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
using MyGame;
using System.Windows.Threading;

namespace MyGame
{
	public abstract class MapControlBase : Control
	{
		public class TileSelection
		{
			public Location Start;
			public Location End;
		}

		int m_columns;
		int m_rows;

		Location m_pos;

		double m_tileSize = 32;

		DispatcherTimer m_updateTimer;

		VisualCollection m_tileCollection;

		Rectangle m_selRect;

		public TileSelection Selection { get; private set; }

		public MapControlBase()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(15);

			m_tileCollection = new VisualCollection(this);

			m_selRect = new Rectangle();
			m_selRect.Visibility = Visibility.Hidden;
			m_selRect.Width = m_tileSize;
			m_selRect.Height = m_tileSize;
			m_selRect.Stroke = Brushes.Blue;
			m_selRect.StrokeThickness = 1;
			m_selRect.Fill = new SolidColorBrush(Colors.Blue);
			m_selRect.Fill.Opacity = 0.2;
			m_selRect.Fill.Freeze();
			AddVisualChild(m_selRect);

			ClipToBounds = true; // does this slow down?
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

		public Location Pos
		{
			get { return m_pos; }
			set
			{
				m_pos = value;
				InvalidateVisual();
			}
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return availableSize;
		}

		void ReCreateMapTiles()
		{
			int numTiles = m_columns * m_rows;

			if (m_tileCollection.Count > numTiles)
			{
				m_tileCollection.RemoveRange(numTiles, m_tileCollection.Count - numTiles);
			}
			else if (m_tileCollection.Count < numTiles)
			{
				int newTiles = numTiles - m_tileCollection.Count;
				for (int i = 0; i < newTiles; ++i)
				{
					var tile = CreateTile();
					m_tileCollection.Add(tile);
				}
			}

			UpdateTiles();
		}

		protected override Size ArrangeOverride(Size s)
		{
			int newColumns = (int)Math.Ceiling(s.Width / m_tileSize);
			int newRows = (int)Math.Ceiling(s.Height / m_tileSize);

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
				tile.Arrange(new Rect(x * m_tileSize, y * m_tileSize, m_tileSize, m_tileSize));
				++i;
			}

			if (this.Selection != null)
			{
				double x, y, w, h;
				Location l1 = this.Selection.Start;
				Location l2 = this.Selection.End;

				x = (Math.Min(l1.X, l2.X) - m_pos.X) * m_tileSize;
				y = (Math.Min(l1.Y, l2.Y) - m_pos.Y) * m_tileSize;
				w = (Math.Abs(l1.X - l2.X) + 1) * m_tileSize;
				h = (Math.Abs(l1.Y - l2.Y) + 1) * m_tileSize;

				m_selRect.Width = w;
				m_selRect.Height = h;
				m_selRect.Arrange(new Rect(x, y, w, h));
			}

			return s;
		}
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}
		

		protected override int VisualChildrenCount
		{
			get { return m_tileCollection.Count + 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			if (index == m_tileCollection.Count)
				return m_selRect;

			return m_tileCollection[index];
		}

		public UIElement GetTile(Location l)
		{
			return (UIElement)m_tileCollection[l.X + l.Y * m_columns];
		}

		public Location LocationFromPoint(Point p)
		{
			return new Location((int)(p.X / m_tileSize), (int)(p.Y / m_tileSize));
		}

		public Location MapLocationFromPoint(Point p)
		{
			return LocationFromPoint(p) + m_pos;
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			Point pos = e.GetPosition(this);

			if (e.RightButton == MouseButtonState.Pressed)
			{
				m_selRect.Visibility = Visibility.Hidden;
				this.Selection = null;
				if (SelectionChanged != null)
					SelectionChanged();
				//e.Handled = true;
				return;
			}

			Focus();

			this.Selection = new TileSelection();
			this.Selection.Start = LocationFromPoint(pos) + m_pos;
			this.Selection.End = LocationFromPoint(pos) + m_pos;
			m_selRect.Visibility = Visibility.Visible;

			if (SelectionChanged != null)
				SelectionChanged();

			InvalidateArrange();

			CaptureMouse();

			e.Handled = true;

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (!IsMouseCaptured)
				return;

			Point pos = e.GetPosition(this);

			int limit = 4;

			if (this.ActualWidth - pos.X < limit)
			{
				++m_pos.X;
				InvalidateTiles();
			}
			else if (pos.X < limit)
			{
				--m_pos.X;
				InvalidateTiles();
			}

			if (this.ActualHeight - pos.Y < limit)
			{
				++m_pos.Y;
				InvalidateTiles();
			}
			else if (pos.Y < limit)
			{
				--m_pos.Y;
				InvalidateTiles();
			}

			Location l = LocationFromPoint(pos);
			if (l != this.Selection.End)
			{
				this.Selection.End = l + m_pos;

				if (SelectionChanged != null)
					SelectionChanged();

				InvalidateArrange();
			}

			e.Handled = true;

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			ReleaseMouseCapture();

			base.OnMouseUp(e);
		}

		public void InvalidateTiles()
		{
			if (!m_updateTimer.IsEnabled)
				m_updateTimer.Start();
		}

		void UpdateTimerTick(object sender, EventArgs e)
		{
			m_updateTimer.Stop();
			UpdateTiles();
		}

		void UpdateTiles()
		{
			int i = 0;
			foreach (UIElement tile in m_tileCollection)
			{
				int x = i % m_columns;
				int y = i / m_columns;
				Location loc = m_pos + new Location(x, y);

				UpdateTile(tile, loc);

				++i;
			}
		}

		public event Action SelectionChanged;

		protected abstract UIElement CreateTile();
		protected abstract void UpdateTile(UIElement tile, Location loc);
	}

}
