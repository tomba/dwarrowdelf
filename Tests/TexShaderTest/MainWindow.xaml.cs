using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TexShaderTest
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			m_bmp = new WriteableBitmap(600, 600, 96, 96, PixelFormats.Bgr32, null);

			img.Source = m_bmp;

			Draw();
		}

		WriteableBitmap m_bmp;

		unsafe void Draw()
		{
			m_bmp.Lock();

			for (int y = 0; y < m_bmp.PixelHeight; ++y)
			{
				int* p = (int*)(m_bmp.BackBuffer + m_bmp.BackBufferStride * y);

				for (int x = 0; x < m_bmp.PixelWidth; ++x)
				{
					double fx = (double)x / m_bmp.PixelWidth;
					double fy = (double)y / m_bmp.PixelHeight;

					Color3 c = PS(fx, fy);

					p[x] = c.ToBgra();
				}
			}

			m_bmp.AddDirtyRect(new Int32Rect(0, 0, m_bmp.PixelWidth, m_bmp.PixelHeight));
			m_bmp.Unlock();
		}

		Color3 PS(double x, double y)
		{
			Color3 c = new Color3(1, 1, 1);

			double f = Math.Cos((x - y) * 2 * Math.PI);

			f = saturate(f);

			c *= 1 - (float)f;

			return c;
		}

		double saturate(double v)
		{
			if (v < 0)
				return 0;
			if (v > 1)
				return 1;
			return v;
		}
	}
}
