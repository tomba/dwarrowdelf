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

		bool Contains(IntPoint2 p);
		void SetSize(IntSize2 size);
		void Clear();
	}
}
