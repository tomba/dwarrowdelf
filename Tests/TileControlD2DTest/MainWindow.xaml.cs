using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Dwarrowdelf;

namespace TileControlD2DTest
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			tc.BitmapGenerator = new BitmapGen();
			tc.TileSize = 32;

			var map = new Dwarrowdelf.Client.TileControlD2D.RenderMap();
			map.Size = new IntSize(20, 20);
			tc.RenderMap = map;

			map.ArrayGrid.Grid[1, 1].Floor.SymbolID = SymbolID.Floor;
			map.ArrayGrid.Grid[2, 2].Floor.SymbolID = SymbolID.Floor;
		}
	}

	class BitmapGen : Dwarrowdelf.Client.TileControlD2D.IBitmapGenerator
	{
		public BitmapSource GetBitmap(SymbolID symbolID, GameColor color)
		{
			var dv = new DrawingVisual();
			var dc = dv.RenderOpen();
			dc.DrawEllipse(Brushes.Red, new Pen(Brushes.Blue, 2), new Point(16, 16), 16, 16);
			dc.Close();
			
			var bmp = new RenderTargetBitmap(32, 32, 96, 96, PixelFormats.Default);
			bmp.Render(dv);

			return bmp;
		}

		public int NumDistinctBitmaps
		{
			get { return 2; }
		}

		public int TileSize
		{
			get
			{
				return 32;
			}
			set
			{
				throw new NotImplementedException();
			}
		}
	}
}
