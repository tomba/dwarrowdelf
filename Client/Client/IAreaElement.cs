using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	interface IAreaElement
	{
		EnvironmentObject Environment { get; }
		IntRectZ Area { get; }
		string Description { get; }
	}
}
