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
using Dwarrowdelf.Client;

namespace TintTest
{
	public partial class MainWindow : Window
	{
		int[,] m_src;
		int[,] m_tint;

		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			CreateSource();
			CreateTint();
			CreateDest();

			base.OnInitialized(e);
		}

		void CreateSource()
		{
			var srcBmp = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgr32, null);

			var w = srcBmp.PixelWidth;
			var h = srcBmp.PixelHeight;

			var arr = new int[h, w];
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					arr[y, x] = 0x7f7f7f;
				}
			}

			srcBmp.WritePixels(new Int32Rect(0, 0, w, h), arr, w * 4, 0);

			m_src = arr;
			srcImage.Source = srcBmp;
		}

		void CreateTint()
		{
			var tintBmp = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgr32, null);

			var w = tintBmp.PixelWidth;
			var h = tintBmp.PixelHeight;

			var gameColorArr = (GameColor[])Enum.GetValues(typeof(GameColor));
			var colorArr = new GameColorRGB[gameColorArr.Length - 1];
			for (int i = 0; i < gameColorArr.Length - 1; ++i)
				colorArr[i] = GameColorRGB.FromGameColor(gameColorArr[i]);

			var arr = new int[h, w];
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					var idx = (y / 16) * (w / 16) + (x / 16);
					if (idx >= colorArr.Length)
						continue;

					arr[y, x] = colorArr[idx].ToInt32();
				}
			}

			tintBmp.WritePixels(new Int32Rect(0, 0, w, h), arr, w * 4, 0);

			m_tint = arr;
			tintImage.Source = tintBmp;
		}

		void CreateDest()
		{
			var dest = new WriteableBitmap(256, 256, 96, 96, PixelFormats.Bgr32, null);

			var w = dest.PixelWidth;
			var h = dest.PixelHeight;

			var arr = new int[h, w];
			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					var src = IntToColor(m_src[y, x]);
					var tint = IntToColor(m_tint[y, x]);

					var dst = TintColor(src, tint);

					arr[y, x] = ColorToInt(dst);
				}
			}

			dest.WritePixels(new Int32Rect(0, 0, w, h), arr, w * 4, 0);

			dstImage.Source = dest;
		}

		static Color IntToColor(int color)
		{
			return Color.FromRgb((byte)((color >> 16) & 0xff), (byte)((color >> 8) & 0xff), (byte)((color >> 0) & 0xff));
		}

		static int ColorToInt(Color color)
		{
			return (color.R << 16) | (color.G << 8) | (color.B);
		}

		static Color TintColor(Color c, Color tint)
		{
			double th, ts, tl;
			HSL.RGB2HSL(tint, out th, out ts, out tl);

			double ch, cs, cl;
			HSL.RGB2HSL(c, out ch, out cs, out cl);

			Color color = HSL.HSL2RGB(th, ts, cl);
			color.A = c.A;

			return color;
		}
	}
}
