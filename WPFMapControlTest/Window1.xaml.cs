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
using MyGame;

namespace WPFMapControlTest
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();

			this.PreviewKeyDown += Window_PreKeyDown;

		}

		void Window_PreKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Add)
			{
				map.TileSize += 8;

				e.Handled = true;
			}
			else if (e.Key == Key.Subtract)
			{
				if (map.TileSize <= 16)
					return;

				map.TileSize -= 8;

				e.Handled = true;
			}
			else if (KeyIsDir(e.Key))
			{
				Direction dir = KeyToDir(e.Key);
				IntVector v = IntVector.FromDirection(dir);
				map.CenterPos += v;

				e.Handled = true;
			}
		}


		Direction KeyToDir(Key key)
		{
			Direction dir;

			switch (key)
			{
				case Key.Up: dir = Direction.North; break;
				case Key.Down: dir = Direction.South; break;
				case Key.Left: dir = Direction.West; break;
				case Key.Right: dir = Direction.East; break;
				case Key.Home: dir = Direction.NorthWest; break;
				case Key.End: dir = Direction.SouthWest; break;
				case Key.PageUp: dir = Direction.NorthEast; break;
				case Key.PageDown: dir = Direction.SouthEast; break;
				default:
					throw new Exception();
			}

			return dir;
		}

		bool KeyIsDir(Key key)
		{
			switch (key)
			{
				case Key.Up: break;
				case Key.Down: break;
				case Key.Left: break;
				case Key.Right: break;
				case Key.Home: break;
				case Key.End: break;
				case Key.PageUp: break;
				case Key.PageDown: break;
				default:
					return false;
			}
			return true;
		}
	}
}
