using System;
using System.Windows;

namespace Dwarrowdelf.Client.TileControl
{
	public interface ITileControl : IDisposable
	{
		double TileSize { get; set; }
		IntSize GridSize { get; }
		Point CenterPos { get; set; }

		void SetRenderData(IRenderData renderData);
		ISymbolDrawingCache SymbolDrawingCache { get; set; }

		void InvalidateTileData();
		void InvalidateTileRender();
		void InvalidateSymbols();

		Point MapLocationToScreenLocation(Point ml);
		Point ScreenLocationToMapLocation(Point sl);
		Point ScreenPointToMapLocation(Point p);
		Point MapLocationToScreenPoint(Point ml);
		Point ScreenPointToScreenLocation(Point p);
		Point ScreenLocationToScreenPoint(Point loc);

		event Action<IntSize, Point> TileLayoutChanged;
		event Action AboutToRender;
	}
}
