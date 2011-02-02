using System;
using System.Windows;

namespace Dwarrowdelf.Client.TileControl
{
	public interface ITileControl
	{
		int TileSize { get; set; }
		IntSize GridSize { get; }

		void SetRenderData(IRenderData renderData);
		ISymbolDrawingCache SymbolDrawingCache { get; set; }

		void InvalidateRender();

		Point ScreenLocationToScreenPoint(IntPoint loc);
		IntPoint ScreenPointToScreenLocation(Point p);

		event Action<IntSize> TileLayoutChanged;
		event Action<Size> AboutToRender;
	}
}
