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
		int m_columns;
		int m_rows;

		IntPoint m_pos;

		double m_tileSize = 32;

		DispatcherTimer m_updateTimer;

		VisualCollection m_tileCollection;

		Rectangle m_selectionRect;
		IntPoint m_selectionStart;
		IntPoint m_selectionEnd;

		public MapControlBase()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(15);

			m_tileCollection = new VisualCollection(this);

			m_selectionRect = new Rectangle();
			m_selectionRect.Visibility = Visibility.Hidden;
			m_selectionRect.Width = m_tileSize;
			m_selectionRect.Height = m_tileSize;
			m_selectionRect.Stroke = Brushes.Blue;
			m_selectionRect.StrokeThickness = 1;
			m_selectionRect.Fill = new SolidColorBrush(Colors.Blue);
			m_selectionRect.Fill.Opacity = 0.2;
			m_selectionRect.Fill.Freeze();
			AddVisualChild(m_selectionRect);

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
					OnTileSizeChanged(value);
				}
			}
		}

		protected virtual void OnTileSizeChanged(double newSize) { }

		public IntPoint Pos
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

			if (m_selectionRect.Visibility == Visibility.Visible)
			{			
				var p1 = m_selectionStart - m_pos;
				var p2 = m_selectionEnd - m_pos;

				Rect r = new Rect(new Point(p1.X * m_tileSize, p1.Y * m_tileSize),
					new Point(p2.X * m_tileSize, p2.Y * m_tileSize));

				r.Width += m_tileSize;
				r.Height += m_tileSize;

				m_selectionRect.Width = r.Width;
				m_selectionRect.Height = r.Height;
				m_selectionRect.Arrange(r);
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
				return m_selectionRect;

			return m_tileCollection[index];
		}

		public UIElement GetTile(IntPoint l)
		{
			return (UIElement)m_tileCollection[l.X + l.Y * m_columns];
		}

		public IntPoint LocationFromPoint(Point p)
		{
			return new IntPoint((int)(p.X / m_tileSize), (int)(p.Y / m_tileSize));
		}

		public IntPoint MapLocationFromPoint(Point p)
		{
			return LocationFromPoint(p) + (IntVector)m_pos;
		}

		public IntRect SelectionRect
		{
			get
			{
				if (m_selectionRect.Visibility != Visibility.Visible)
					return new IntRect();

				var p1 = m_selectionStart;
				var p2 = m_selectionEnd;

				IntRect r = new IntRect(p1, p2);

				r.Width += 1;
				r.Height += 1;

				return r;
			}

			set
			{
				if (value.Width == 0 || value.Height == 0)
				{
					m_selectionRect.Visibility = Visibility.Hidden;
					return;
				}

				m_selectionStart = value.TopLeft;
				m_selectionEnd = value.BottomRight - new IntVector(1, 1);

				m_selectionRect.Visibility = Visibility.Visible;

				InvalidateArrange();

				if (SelectionChanged != null)
					SelectionChanged();
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
				return;

			Point pos = e.GetPosition(this);

			m_selectionStart = LocationFromPoint(pos) + (IntVector)m_pos;
			m_selectionEnd = m_selectionStart;
			m_selectionRect.Visibility = Visibility.Visible;

			InvalidateArrange();

			CaptureMouse();

			e.Handled = true;

			if (SelectionChanged != null)
				SelectionChanged();

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

			IntPoint l = LocationFromPoint(pos) + (IntVector)m_pos;
			m_selectionEnd = l;

			InvalidateArrange();
			
			e.Handled = true;

			if (SelectionChanged != null)
				SelectionChanged();

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
				IntPoint loc = m_pos + new IntVector(x, y);

				UpdateTile(tile, loc);

				++i;
			}
		}

		public event Action SelectionChanged;

		protected abstract UIElement CreateTile();
		protected abstract void UpdateTile(UIElement tile, IntPoint loc);
	}

}
