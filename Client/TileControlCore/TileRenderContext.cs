using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Dwarrowdelf;

namespace Dwarrowdelf.Client.TileControl
{
	public sealed class TileRenderContext
	{
		public double TileSize;
		public Point RenderOffset;
		public IntSize RenderGridSize;

		public bool TileDataInvalid;
		public bool TileRenderInvalid;
	}
}
