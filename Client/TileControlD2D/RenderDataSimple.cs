using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;
using Microsoft.WindowsAPICodePack.DirectX.DXGI;

namespace Dwarrowdelf.Client.TileControlD2D
{
	public struct RenderTileSimple
	{
		public bool IsValid;

		public GameColorRGB Color;
		public byte DarknessLevel;
	}
}
