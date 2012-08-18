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
using Dwarrowdelf.Client.Symbols;
using Dwarrowdelf.Client;

namespace SymbolDrawTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			var tileset = new TileSet("DefaultTileSet.xaml");
			tileset.Load();

			foreach (SymbolID s in Enum.GetValues(typeof(SymbolID)))
			{
				if (s == SymbolID.Undefined)
					continue;

				var drawing = tileset.GetDetailedDrawing(s, Dwarrowdelf.GameColor.None);

				list.Items.Add(new DrawingImage(drawing));
			}
		}
	}
}
