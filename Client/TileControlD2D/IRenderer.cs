using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.DirectX.Direct2D1;

namespace Dwarrowdelf.Client.TileControlD2D
{
	public interface IRenderer
	{
		void RenderTargetChanged();
		void TileSizeChanged(int tileSize);
		void SizeChanged();
		void Render(RenderTarget renderTarget, int columns, int rows, int tileSize);
	}
}
