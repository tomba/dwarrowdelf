using System;
using System.Windows;

namespace Dwarrowdelf.Client.TileControl
{
	public interface ITileControl
	{
		double TileSize { get; set; }
		IntSize GridSize { get; }

		void SetRenderData(IRenderData renderData);
		ISymbolDrawingCache SymbolDrawingCache { get; set; }

		void InvalidateTileRender();

		Point ScreenLocationToScreenPoint(Point loc);
		Point ScreenPointToScreenLocation(Point p);

		event Action<IntSize> TileLayoutChanged;
		event Action<Size> AboutToRender;
	}
}
