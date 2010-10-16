using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client.TileControlD2D
{
	public interface IRenderViewRenderer
	{
		RenderMap GetRenderMap(int columns, int rows, bool useSimpleTiles);
	}
}
