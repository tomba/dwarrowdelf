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
		IntSize m_selectionSize;

		public MapControlBase()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(15);

			m_tileCollection = new VisualCollection(this);

			m_selectionRect = new Rectangle();
			//m_selectionRect.Visibility = Visibility.Hidden;
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

			if (!m_selectionSize.IsEmpty)
			{
				double x, y, w, h;
				IntPoint l1 = m_selectionStart - m_pos;
				IntSize l2 = m_selectionSize;

				x = l1.X * m_tileSize;
				y = l1.Y * m_tileSize;
				w = l2.Width * m_tileSize;
				h = l2.Height * m_tileSize;

				m_selectionRect.Width = w;
				m_selectionRect.Height = h;
				m_selectionRect.Arrange(new Rect(x, y, w, h));
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
			return LocationFromPoint(p) + m_pos;
		}

		public IntPoint SelectionStart
		{
			get { return m_selectionStart; }

			set
			{
				m_selectionStart = value;
				/*
				if (m_selectionStart != m_selectionEnd)
					m_selectionRect.Visibility = Visibility.Visible;
				else
					m_selectionRect.Visibility = Visibility.Hidden;
				*/
				InvalidateVisual();

				if (SelectionChanged != null)
					SelectionChanged();
			}
		}

		public IntSize SelectionSize
		{
			get { return m_selectionSize; }

			set
			{
				m_selectionSize = value;
				/*
				if (m_selectionStart != m_selectionEnd)
					m_selectionRect.Visibility = Visibility.Visible;
				else
					m_selectionRect.Visibility = Visibility.Hidden;
				*/
				InvalidateVisual();

				if (SelectionChanged != null)
					SelectionChanged();
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (e.LeftButton != MouseButtonState.Pressed)
				return;

			Point pos = e.GetPosition(this);

			this.SelectionStart = LocationFromPoint(pos) + m_pos;
			this.SelectionSize = new IntSize(1, 1);
			m_selectionRect.Visibility = Visibility.Visible;

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

			IntPoint l = LocationFromPoint(pos) + m_pos;
			IntSize s = new IntSize(l.X - m_selectionStart.X, l.Y - m_selectionStart.Y);

			m_selectionSize = s;
			InvalidateArrange();
			
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
				IntPoint loc = m_pos + new IntPoint(x, y);

				UpdateTile(tile, loc);

				++i;
			}
		}

		public event Action SelectionChanged;

		protected abstract UIElement CreateTile();
		protected abstract void UpdateTile(UIElement tile, IntPoint loc);
	}

}
