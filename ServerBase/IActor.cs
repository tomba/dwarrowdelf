using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public interface IActor
	{
		bool IsInteractive { get; } // XXX should be removed
		void DetermineAction();
	}
}
