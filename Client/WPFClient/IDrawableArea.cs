using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace Dwarrowdelf.Client
{
	interface IDrawableArea
	{
		Environment Environment { get; }
		int Z { get; }
		IntRect Area { get; }
		Brush Fill { get; }
		double Opacity { get; }
	}
}
