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
using System.Windows.Shapes;
using System.Windows.Media.Media3D;
using System.IO;

namespace MapWindow3D
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

			Create2();
		}

		void Create1()
		{
			int w = 256;
			int h = 256;

			var hm = new double[w * h];
			var idmap = new byte[w * h];

			using (var br = new BinaryReader(File.OpenRead("data.raw")))
			{
				for (int i = 0; i < hm.Length; ++i)
					hm[i] = br.ReadDouble();
				for (int i = 0; i < idmap.Length; ++i)
					idmap[i] = br.ReadByte();
			}

			var gm3d = new GeometryModel3D();

			//gm3d.Material = new DiffuseMaterial(Brushes.Green);

			gm3d.Material = new DiffuseMaterial(new ImageBrush(new BitmapImage(new Uri(@"C:\\Users\\Tomba\\Work\\dwarrowdelf\\Client\\Client\\debgrad.png"))));

			MeshGeometry3D geom = new MeshGeometry3D();

			var points = new Point3DCollection(w * h);
			var textureCoords = new PointCollection(w * h);

			for (int y = 0; y < h; ++y)
			{
				for (int x = 0; x < w; ++x)
				{
					var z = hm[y * w + x];

					points.Add(new Point3D(x, y, z));

					switch (idmap[y * w + x])
					{
						case 0:
							textureCoords.Add(new Point(0, 0));
							break;
						case 1:
							textureCoords.Add(new Point(1, 1));
							break;
					}
					//textureCoords.Add(new Point(0, 1));
					//textureCoords.Add(new Point(1, 0));

				}
			}

			geom.Positions = points;
			geom.TextureCoordinates = textureCoords;

			var indices = new Int32Collection((w - 1) * (h - 1) * 6);
			for (int y = 0; y < h - 1; ++y)
			{
				for (int x = 0; x < w - 1; ++x)
				{
					indices.Add(y * w + x);
					indices.Add(y * w + x + 1);
					indices.Add((y + 1) * w + x);

					indices.Add(y * w + x + 1);
					indices.Add((y + 1) * w + x + 1);
					indices.Add((y + 1) * w + x);
				}
			}
			geom.TriangleIndices = indices;

			gm3d.Geometry = geom;

			gm3d.Transform = new TranslateTransform3D(-w / 2, -h / 2, 0);

			ModelVisual3D model = new ModelVisual3D();
			model.Content = gm3d;
			this.viewPort.Children.Add(model);
		}

		void Create2()
		{
			int w = 256;
			int h = 256;

			var hm = new double[w * h];
			var idmap = new byte[w * h];

			using (var br = new BinaryReader(File.OpenRead("data.raw")))
			{
				for (int i = 0; i < hm.Length; ++i)
					hm[i] = br.ReadDouble();
				for (int i = 0; i < idmap.Length; ++i)
					idmap[i] = br.ReadByte();
			}

			var gm3d = new GeometryModel3D();

			//gm3d.Material = new DiffuseMaterial(Brushes.Green);
			gm3d.Material = new DiffuseMaterial(new ImageBrush(new BitmapImage(new Uri(@"C:\\Users\\Tomba\\Work\\dwarrowdelf\\Client\\Client\\debgrad.png"))));

			MeshGeometry3D geom = new MeshGeometry3D();

			var points = new Point3DCollection(w * h);
			var textureCoords = new PointCollection(w * h);
			var indices = new Int32Collection((w - 1) * (h - 1) * 6);

			for (int y = 0; y < h - 1; ++y)
			{
				for (int x = 0; x < w - 1; ++x)
				{
					Func<int, int, Point3D> f = (_x, _y) => new Point3D(_x, _y, hm[_y * w + _x] * 3);

					var tl = f(x, y);
					var tr = f(x + 1, y);
					var bl = f(x, y + 1);
					var br = f(x + 1, y + 1);

					Func<Point3D, Point> tc = (p) =>
					{
						switch (idmap[(int)(p.Y) * w + (int)(p.X)])
						{
							case 0:
								return new Point(0, 0);
							case 1:
								return new Point(1, 1);
						}
						throw new Exception();
					};

					points.Add(tl);
					points.Add(tr);
					points.Add(bl);

					textureCoords.Add(tc(tl));
					textureCoords.Add(tc(tr));
					textureCoords.Add(tc(bl));

					points.Add(br);
					points.Add(bl);
					points.Add(tr);

					textureCoords.Add(tc(br));
					textureCoords.Add(tc(bl));
					textureCoords.Add(tc(tr));
				}
			}

			geom.Positions = points;
			geom.TextureCoordinates = textureCoords;

			gm3d.Geometry = geom;

			gm3d.Transform = new TranslateTransform3D(-w / 2, -h / 2, 0);

			ModelVisual3D model = new ModelVisual3D();
			model.Content = gm3d;
			this.viewPort.Children.Add(model);
		}
	}

	class LookBackConverter : IValueConverter
	{
		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new Point3D(0, 0, 0) - (Point3D)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return null;
		}
		#endregion
	}

}
