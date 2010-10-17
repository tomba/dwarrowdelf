using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControlD2D
{
	public interface IRenderData
	{
		IntRect Bounds { get; }
		IntSize Size { get; set; }
		void Clear();
	}
}
