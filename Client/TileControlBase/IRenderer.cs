using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public interface IRenderer : IDisposable
	{
		RenderData<RenderTileDetailed> RenderData { get; set; }
		ISymbolDrawingCache SymbolDrawingCache { get; set; }

		void Render(DrawingContext dc, Size renderSize, RenderContext ctx);
	}

	public class RenderContext
	{
		public double TileSize;
		public Point RenderOffset;
		public IntSize RenderGridSize;

		public bool TileDataInvalid;
		public bool TileRenderInvalid;
	}

}
