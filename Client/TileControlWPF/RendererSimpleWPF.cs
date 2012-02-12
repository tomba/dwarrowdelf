using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class RendererSimpleWPF : RendererBaseWPF
	{
		RenderData<RenderTileSimple> m_renderData;

		public RendererSimpleWPF(RenderData<RenderTileSimple> renderData)
			: base(renderData)
		{
			m_renderData = renderData;
		}

		protected override void RenderTile(DrawingContext dc, int x, int y)
		{
			int idx = m_renderData.GetIdx(x, y);
			var sid = m_renderData.Grid[idx].SymbolID;

			if (sid == SymbolID.Undefined)
				return;

			var rect = new Rect(x, y, 1, 1);

			var bitmap = this.SymbolBitmapCache.GetBitmap(sid, GameColor.None);
			dc.DrawImage(bitmap, rect);
		}
	}
}
