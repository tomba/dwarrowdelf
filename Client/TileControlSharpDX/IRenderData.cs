using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControl
{
	public interface IRenderData
	{
		int Width { get; }
		int Height { get; }
		IntSize2 Size { get; }
	}
}
