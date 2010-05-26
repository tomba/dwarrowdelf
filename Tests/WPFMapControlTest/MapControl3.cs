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

namespace WPFMapControlTest
{
	class MapControl3 : Control
	{
		Map m_map;
		DrawingGroup m_drawing;
		int m_tileSize = 16;
		int m_columns;
		int m_rows;

		DrawingCache m_drawingCache;
		SymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;

		public MapControl3()
		{
			m_drawingCache = new DrawingCache();
			m_symbolDrawingCache = new SymbolDrawingCache(m_drawingCache);
			m_symbolBitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, m_tileSize);

			m_map = new Map(512, 512);
			for (int y = 0; y < m_map.Height; ++y)
			{
				for (int x = 0; x < m_map.Width; ++x)
				{
					m_map.MapArray[y, x] = (x + (y % 2)) % 2 == 0 ? (byte)50 : (byte)255;
				}
			}
		}

		public double TileSize { get; set; }
		public IntPoint CenterPos { get; set; }

		protected override Size MeasureOverride(Size constraint)
		{
			MyDebug.WriteLine("Measure");

			return base.MeasureOverride(constraint);
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			MyDebug.WriteLine("Arrange");

			var columns = (int)(arrangeBounds.Width / m_tileSize);
			var rows = (int)(arrangeBounds.Height / m_tileSize);

			if (columns != m_columns || rows != m_rows)
			{
				m_columns = columns;
				m_rows = rows;

				var bmp = m_symbolBitmapCache.GetBitmap(SymbolID.Undefined, Colors.Black, false);

				var drawingCollection = new DrawingCollection();

				for (int y = 0; y < m_rows; ++y)
				{
					for (int x = 0; x < m_columns; ++x)
					{
						var imageDrawing = new ImageDrawing(bmp, new Rect(new Point(x * m_tileSize, y * m_tileSize),
							new Size(m_tileSize, m_tileSize)));
						drawingCollection.Add(imageDrawing);
						imageDrawing = new ImageDrawing(bmp, new Rect(new Point(x * m_tileSize, y * m_tileSize), 
							new Size(m_tileSize, m_tileSize)));
						drawingCollection.Add(imageDrawing);
					}
				}

				var drawingGroup = new DrawingGroup();
				drawingGroup.Children = drawingCollection;
				m_drawing = drawingGroup;
			}

			Render();

			return base.ArrangeOverride(arrangeBounds);
		}

		void Render()
		{
			var drawingGroup = m_drawing.Children;
			var arr = m_map.MapArray;

			for (int y = 0; y < m_rows; ++y)
			{
				for (int x = 0; x < m_columns; ++x)
				{
					var d = (ImageDrawing)drawingGroup[(y * m_columns + x) * 2];
					ImageSource bmp;
					if (arr[y, x] < 100)
						bmp = m_symbolBitmapCache.GetBitmap(SymbolID.Wall, Colors.Black, false);
					else
						bmp = m_symbolBitmapCache.GetBitmap(SymbolID.Floor, Colors.Black, false);
					d.ImageSource = bmp;
				}
			}
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawDrawing(m_drawing);
			//base.OnRender(drawingContext);
		}

		IntPoint m_lastPos;

		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			var p = e.GetPosition(this);
			int x = (int)(p.X / m_tileSize);
			int y = (int)(p.Y / m_tileSize);

			var pos = new IntPoint(x, y);

			if (m_lastPos == pos)
				return;

			var arr = m_map.MapArray;
			arr[y, x] = (byte)(~arr[y, x]);
			m_lastPos = pos;
			InvalidateVisual();
		}
#if asd
		// called for each visible tile
		protected void UpdateTile(Visual _tile, IntPoint ml)
		{
			MapControlTile2 tile = (MapControlTile2)_tile;

			Color c;

			if (m_map.Bounds.Contains(ml))
			{

				byte b = m_map[ml.X, ml.Y];
				c = Color.FromRgb(b, b, b);
			}
			else
			{
				c = Color.FromRgb(0, 0, 0);
			}

			if (c != tile.Color)
			{
				tile.Color = c;
				tile.Update();
			}
		}
#endif

		class MapControlTile2 : DrawingVisual
		{
			MapControl2 m_parent;
			public Color Color { get; set; }

			public MapControlTile2(MapControl2 parent, double x, double y)
			{
				m_parent = parent;
				this.VisualOffset = new Vector(x, y);
			}

			public void Update()
			{
				var drawingContext = this.RenderOpen();
				drawingContext.DrawRectangle(new SolidColorBrush(this.Color), null,
					new Rect(new Size(m_parent.TileSize, m_parent.TileSize)));
				drawingContext.Close();
			}
		}
	}

}
