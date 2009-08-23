using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MyGame
{
	public interface IAreaData
	{
		Terrains Terrains { get; }
		Objects Objects { get; }
		Stream DrawingStream { get; }
	}

}
