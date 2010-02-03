using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MyGame
{
	public interface IAreaData
	{
		IList<SymbolInfo> Symbols { get; }
		Materials Materials { get; }
		Stream DrawingStream { get; }
		Buildings Buildings { get; }
	}
}
