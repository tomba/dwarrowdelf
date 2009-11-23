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
		Terrains Terrains { get; }
		Materials Materials { get; }
		IList<ObjectInfo> Objects { get; }
		Stream DrawingStream { get; }
	}
}
