using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControl
{
	public interface IRenderData
	{
		IntRect Bounds { get; }
		IntSize Size { get; set; }
		void Clear();
	}
}
