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

		VisualCollection m_tileCollection;
		VisualCollection m_childCollection;

		Rectangle m_selectionRect;
		IntPoint m_selectionStart;
		IntPoint m_selectionEnd;

		protected MapControlBase()
		{
			m_updateTimer = new DispatcherTimer(DispatcherPriority.Render);
			m_updateTimer.Tick += UpdateTimerTick;
			m_updateTimer.Interval = TimeSpan.FromMilliseconds(15);

			m_tileCollection = new VisualCollection(this);
			m_childCollection = new VisualCollection(this);

			m_selectionRect = new Rectangle();
			m_selectionRect.Visibility = Visibility.Hidden;
			m_selectionRect.Width = this.TileSize;
			m_selectionRect.Height = this.TileSize;
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

		public bool SelectionEnabled { get; set; }

		public static readonly DependencyProperty TileSizeProperty = DependencyProperty.Register(
			"TileSize", typeof(double), typeof(MapControlBase),
			new FrameworkPropertyMetadata(32.0, FrameworkPropertyMetadataOptions.AffectsArrange),
			v => ((double)v) >= 2);

		public double TileSize
		{
			get { return (double)GetValue(TileSizeProperty); }
			set { SetValue(TileSizeProperty, value); }
		}

		public static readonly DependencyProperty CenterPosProperty = DependencyProperty.Register(
			"CenterPos", typeof(IntPoint), typeof(MapControlBase),
			new FrameworkPropertyMetadata(new IntPoint(), FrameworkPropertyMetadataOptions.AffectsArrange));

		public IntPoint CenterPos
		{
			get { return (IntPoint)GetValue(CenterPosProperty); }
			set
			{
				if (value == this.CenterPos)
					return;

				SetValue(CenterPosProperty, value);
				UpdateTiles();
			}
		}

		public static IntPoint GetCorner1(DependencyObject obj)
		{
			return (IntPoint)obj.GetValue(Corner1Property);
		}

		public static void SetCorner1(DependencyObject obj, IntPoint value)
		{
			obj.SetValue(Corner1Property, value);
		}

		public static readonly DependencyProperty Corner1Property =
			DependencyProperty.RegisterAttached("Corner1", typeof(IntPoint), typeof(MapControlBase));


		public static IntPoint GetCorner2(DependencyObject obj)
		{
			return (IntPoint)obj.GetValue(Corner2Property);
		}

		public static void SetCorner2(DependencyObject obj, IntPoint value)
		{
			obj.SetValue(Corner2Property, value);
		}

		public static readonly DependencyProperty Corner2Property =
			DependencyProperty.RegisterAttached("Corner2", typeof(IntPoint), typeof(MapControlBase));

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
			//MyDebug.WriteLine("Arrange");

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

			// selection rect
			{
				var ir = new IntRect(m_selectionStart, m_selectionEnd);
				ir = new IntRect(ir.X1Y1, new IntSize(ir.Width + 1, ir.Height + 1));

				Rect r = new Rect(MapLocationToScreenPoint(new IntPoint(ir.X1, ir.Y2)),
					new Size(ir.Width * this.TileSize, ir.Height * this.TileSize));

				m_selectionRect.Width = r.Width;
				m_selectionRect.Height = r.Height;
				m_selectionRect.Arrange(r);
			}

			// children
			foreach (FrameworkElement elem in m_childCollection)
			{
				var p2 = MapLocationToScreenPoint(GetCorner1(elem));
				var p1 = MapLocationToScreenPoint(GetCorner2(elem));

				elem.Arrange(new Rect(p1, p2));
			}

			return s;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(Brushes.Black, null, new Rect(this.RenderSize));
		}

		protected override int VisualChildrenCount
		{
			get { return m_tileCollection.Count + m_childCollection.Count + 1; }
		}

		protected override Visual GetVisualChild(int index)
		{
			if (index == m_tileCollection.Count + m_childCollection.Count)
				return m_selectionRect;

			if (index >= m_tileCollection.Count)
				return m_childCollection[index - m_tileCollection.Count];

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

		public IntRect SelectionRect
		{
			get
			{
				if (m_selectionRect.Visibility != Visibility.Visible)
					return new IntRect();

				var p1 = m_selectionStart;
				var p2 = m_selectionEnd;

				IntRect r = new IntRect(p1, p2);
				r = r.Inflate(1, 1);
				return r;
			}

			set
			{
				if (this.SelectionEnabled == false)
					return;

				if (value.Width == 0 || value.Height == 0)
				{
					m_selectionRect.Visibility = Visibility.Hidden;
					return;
				}

				var newStart = value.X1Y1;
				var newEnd = value.X2Y2 - new IntVector(1, 1);

				if ((newStart != m_selectionStart) || (newEnd != m_selectionEnd))
				{
					m_selectionStart = newStart;
					m_selectionEnd = newEnd;
					InvalidateArrange();
				}

				m_selectionRect.Visibility = Visibility.Visible;

				if (SelectionChanged != null)
					SelectionChanged();
			}
		}

		protected override void OnMouseDown(MouseButtonEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			if (e.LeftButton != MouseButtonState.Pressed)
			{
				base.OnMouseDown(e);
				return;
			}

			Point pos = e.GetPosition(this);

			var newStart = ScreenPointToMapLocation(pos);
			var newEnd = newStart;

			if ((newStart != m_selectionStart) || (newEnd != m_selectionEnd))
			{
				m_selectionStart = newStart;
				m_selectionEnd = newEnd;
				InvalidateArrange();
			}

			m_selectionRect.Visibility = Visibility.Visible;

			CaptureMouse();

			e.Handled = true;

			if (SelectionChanged != null)
				SelectionChanged();

			base.OnMouseDown(e);
		}

		protected override void OnMouseMove(MouseEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			if (!IsMouseCaptured)
			{
				base.OnMouseMove(e);
				return;
			}

			Point pos = e.GetPosition(this);

			int limit = 4;
			int cx = this.CenterPos.X;
			int cy = this.CenterPos.Y;

			if (this.ActualWidth - pos.X < limit)
				++cx;
			else if (pos.X < limit)
				--cx;

			if (this.ActualHeight - pos.Y < limit)
				--cy;
			else if (pos.Y < limit)
				++cy;

			var p = new IntPoint(cx, cy);
			this.CenterPos = p;

			var newEnd = ScreenPointToMapLocation(pos);

			if (newEnd != m_selectionEnd)
			{
				m_selectionEnd = newEnd;
				InvalidateArrange();
			}

			e.Handled = true;

			if (SelectionChanged != null)
				SelectionChanged();

			base.OnMouseMove(e);
		}

		protected override void OnMouseUp(MouseButtonEventArgs e)
		{
			if (this.SelectionEnabled == false)
				return;

			ReleaseMouseCapture();

			base.OnMouseUp(e);
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

		public event Action SelectionChanged;

		protected abstract UIElement CreateTile();
		protected abstract void UpdateTile(UIElement tile, IntPoint loc);
	}

}
