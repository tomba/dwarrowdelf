using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	public interface IAreaElement
	{
		void Register();
		void Unregister();
		EnvironmentObject Environment { get; }
		IntGrid2Z Area { get; }
		string Description { get; }
		SymbolID SymbolID { get; }
		GameColor EffectiveColor { get; }
	}
}
