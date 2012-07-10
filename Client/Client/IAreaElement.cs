using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	interface IAreaElement
	{
		EnvironmentObject Environment { get; }
		IntGrid2Z Area { get; }
		string Description { get; }
		SymbolID SymbolID { get; }
		GameColor EffectiveColor { get; }
	}
}
