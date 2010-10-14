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

namespace TerrainGenTest
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		WriteableBitmap m_bmp;

		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_bmp = TerrainWriter.Do();

			image.Source = m_bmp;
			image.Width = m_bmp.PixelWidth;
			image.Height = m_bmp.PixelHeight;

			RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

			Canvas.SetLeft(image, this.Width / 2 - image.Width / 2);
			Canvas.SetTop(image, this.Height / 2 - image.Height / 2);

			this.PreviewKeyDown += Window_PreKeyDown;
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


		void Window_PreKeyDown(object sender, KeyEventArgs e)
		{
			if (KeyIsDir(e.Key))
			{
				e.Handled = true;
				Direction dir = KeyToDir(e.Key);

				var v = IntVector.FromDirection(dir);

				Canvas.SetLeft(image, Canvas.GetLeft(image) - v.X * 128);
				Canvas.SetTop(image, Canvas.GetTop(image) + v.Y * 128);

				//map.CenterPos += IntVector.FromDirection(dir);
			}
			else if (e.Key == Key.Add)
			{
				e.Handled = true;
				slider1.Value *= 2;
				//map.TileSize += 8;
			}

			else if (e.Key == Key.Subtract)
			{
				e.Handled = true;
				slider1.Value /= 2;
				//if (map.TileSize <= 16)
				//	return;
				//map.TileSize -= 8;
			}
		}

		private void slider1_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (m_bmp == null)
				return;

			var v = slider1.Value;

			image.Width = m_bmp.PixelWidth * v;
			image.Height = m_bmp.PixelHeight * v;

		}
	}
}
