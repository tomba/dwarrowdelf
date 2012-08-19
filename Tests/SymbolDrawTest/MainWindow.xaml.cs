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
using System.Diagnostics;

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

#if asd
			Drawing drawing;

			drawing = Test('☻');

			list.Items.Add(new DrawingImage(drawing));
	
			for (int i = 33; i < 200; ++i)
			{
				char c = (char)i;
				drawing = Test(c);
				if (drawing == null)
					continue;
				list.Items.Add(new DrawingImage(drawing));
			}

#else
			var tileset = new TileSet("DefaultTileSet.xaml");
			tileset.Load();

			foreach (SymbolID s in Enum.GetValues(typeof(SymbolID)))
			{
				if (s == SymbolID.Undefined)
					continue;

				var drawing = tileset.GetDetailedDrawing(s, Dwarrowdelf.GameColor.None);

				list.Items.Add(new DrawingImage(drawing));
			}
#endif
		}

		Drawing Test(char ch)
		{
			var typeFace = new Typeface(new FontFamily("arial"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

			int fontSize = 64;

			DrawingGroup dGroup = new DrawingGroup();
			var brush = new SolidColorBrush(Colors.Black);
			using (DrawingContext dc = dGroup.Open())
			{
				var formattedText = new FormattedText(
						ch.ToString(),
						System.Globalization.CultureInfo.InvariantCulture,
						FlowDirection.LeftToRight,
						typeFace,
						fontSize, Brushes.Black, null, TextFormattingMode.Display);

				bool drawOutline = false;
				double outlineThickness = 0;
				CharRenderMode mode = CharRenderMode.Caps;
				bool reverse = false;

				var geometry = formattedText.BuildGeometry(new System.Windows.Point(0, 0));
				var pen = drawOutline ? new Pen(Brushes.Black, outlineThickness) : null;
				var bounds = pen != null ? geometry.GetRenderBounds(pen) : geometry.Bounds;

				if (bounds.IsEmpty)
					return null;

				Rect bb;

				switch (mode)
				{
					case CharRenderMode.Full:
						{
							double size = formattedText.Height;
							bb = new Rect(bounds.X + bounds.Width / 2 - size / 2, 0, size, size);
						}
						break;

					case CharRenderMode.Caps:
						{
							double size = typeFace.CapsHeight * fontSize;
							bb = new Rect(bounds.X + bounds.Width / 2 - size / 2, formattedText.Baseline - size,
								size, size);
						}
						break;

					case CharRenderMode.Free:
						bb = bounds;
						break;

					default:
						throw new Exception();
				}

				if (reverse)
					geometry = new CombinedGeometry(GeometryCombineMode.Exclude, new RectangleGeometry(bb), geometry);

				dc.DrawRectangle(Brushes.Transparent, null, bb);

				dc.DrawGeometry(brush, pen, geometry);

				/*
				var dl = new Action<double>((y) =>
					dc.DrawLine(new Pen(Brushes.Red, 1), new Point(bb.Left, y), new Point(bb.Right, y)));

				dl(0);
				dl(formattedText.Baseline);
				dl(fontSize);
				dl(formattedText.Height);
				dl(formattedText.Baseline - typeFace.CapsHeight * fontSize);
				 */
			}

			return dGroup;
		}
	}
}
