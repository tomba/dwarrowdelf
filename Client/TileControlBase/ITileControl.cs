using System;

namespace Dwarrowdelf.Client.TileControl
{
	public interface ITileControl
	{
		int Columns { get; }
		int Rows { get; }

		void SetRenderData(IRenderData renderData);
		int TileSize { get; set; }
		ISymbolDrawingCache SymbolDrawingCache { get; set; }

		void InvalidateRender();

		System.Windows.Point ScreenLocationToScreenPoint(Dwarrowdelf.IntPoint loc);
		Dwarrowdelf.IntPoint ScreenPointToScreenLocation(System.Windows.Point p);

		event Action TileArrangementChanged;
		event Action AboutToRender;
	}
}
