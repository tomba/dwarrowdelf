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

		Point MapLocationToScreenLocation(Point ml);
		Point ScreenLocationToMapLocation(Point sl);
		Point ScreenPointToMapLocation(Point p);
		Point MapLocationToScreenPoint(Point ml);
		Point ScreenPointToScreenLocation(Point p);
		Point ScreenLocationToScreenPoint(Point loc);

		event Action<IntSize> TileLayoutChanged;
		event Action AboutToRender;
	}
}
