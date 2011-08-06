using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Dwarrowdelf.Client
{
	public interface ISymbolDrawingCache
	{
		event Action DrawingsChanged;
		Drawing GetDrawing(SymbolID symbolID, GameColor color);
	}
}
