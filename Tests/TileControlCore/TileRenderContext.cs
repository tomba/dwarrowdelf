﻿using System;
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
		public Vector RenderOffset;
		public IntSize2 RenderGridSize;

		public bool TileDataInvalid;
		public bool TileRenderInvalid;
	}
}
