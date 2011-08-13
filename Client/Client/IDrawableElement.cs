using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows;

namespace Dwarrowdelf.Client
{
	interface IDrawableElement
	{
		Environment Environment { get; }
		IntCuboid Area { get; }
		FrameworkElement Element { get; }
		string Description { get; }
	}
}
