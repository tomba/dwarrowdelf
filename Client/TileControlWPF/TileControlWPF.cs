using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public class TileControlWPF : TileControlBase
	{
		RenderData<RenderTileDetailed> m_renderData;
		ISymbolDrawingCache m_symbolDrawingCache;
		SymbolBitmapCache m_symbolBitmapCache;
		SolidColorBrush m_bgBrush;

		public TileControlWPF()
		{
			m_bgBrush = new SolidColorBrush();
		}

		public void Dispose()
		{
		}

		protected override void Render(DrawingContext dc, Size renderSize)
		{
			dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, renderSize.Width, renderSize.Height));

			if (m_renderData == null)
				return;

			if (m_symbolBitmapCache == null)
				m_symbolBitmapCache = new SymbolBitmapCache(m_symbolDrawingCache, (int)this.TileSize);

			if (m_symbolBitmapCache.TileSize != (int)this.TileSize)
				m_symbolBitmapCache.TileSize = (int)this.TileSize;

			var grid = m_renderData.ArrayGrid.Grid;

			dc.PushTransform(new TranslateTransform(this.RenderOffset.X, this.RenderOffset.Y));
			dc.PushTransform(new ScaleTransform(this.TileSize, this.TileSize));

			for (int y = 0; y < this.GridSize.Height && y < m_renderData.Size.Height; ++y)
			{
				for (int x = 0; x < this.GridSize.Width && x < m_renderData.Size.Width; ++x)
				{
					var rect = new Rect(x, y, 1, 1);
					Render(dc, ref grid[y, x].Terrain, rect);
					Render(dc, ref grid[y, x].Interior, rect);
					Render(dc, ref grid[y, x].Object, rect);
					Render(dc, ref grid[y, x].Top, rect);
				}
			}

			dc.Pop();
			dc.Pop();
		}

		void Render(DrawingContext dc, ref RenderTileLayer layer, Rect rect)
		{
			var s = layer.SymbolID;

			if (s == SymbolID.Undefined)
				return;

			if (layer.BgColor != GameColor.None)
			{
				var rgb = layer.BgColor.ToGameColorRGB();
				m_bgBrush.Color = Color.FromRgb(rgb.R, rgb.G, rgb.B);
				dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(rgb.R, rgb.G, rgb.B)), null, rect);
			}

			var bitmap = m_symbolBitmapCache.GetBitmap(s, layer.Color);
			dc.DrawImage(bitmap, rect);
		}

		public void SetRenderData(IRenderData renderData)
		{
			if (!(renderData is RenderData<RenderTileDetailed>))
				throw new NotSupportedException();

			m_renderData = (RenderData<RenderTileDetailed>)renderData;

			InvalidateTileData();
		}

		public void InvalidateSymbols()
		{
			m_symbolBitmapCache = null;
		}

		public ISymbolDrawingCache SymbolDrawingCache
		{
			get { return m_symbolDrawingCache; }

			set
			{
				m_symbolDrawingCache = value;
				m_symbolBitmapCache = null;
			}
		}
	}
}
