using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace Dwarrowdelf.Client.TileControl
{
	public interface ISymbolTileRenderer : ITileRenderer
	{
		void SetTileSet(ITileSet tileset);
	}
}
