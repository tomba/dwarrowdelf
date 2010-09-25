using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Dwarrowdelf.Client
{
	enum DesignationType
	{
		Mine,
	}

	class Designation : IDrawableArea
	{
		public DesignationType Type { get; private set; }
		public IntCuboid Area { get; private set; }
		public Environment Environment { get; private set; }

		public Brush Fill { get { return Brushes.DimGray; } }
		public double Opacity { get { return 0.5; } }

		public Designation(Environment env, DesignationType type, IntCuboid area)
		{
			this.Environment = env;
			this.Type = type;
			this.Area = area;
		}
	}
}
