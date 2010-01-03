using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Threading;

using MyGame;
using MyGame.Client;

namespace WPFMapControlTest
{
	class MapControl : MapControlBase
	{
		public MapControl()
		{
		}

		protected override UIElement CreateTile()
		{
			return new MapControlTile();
		}

		// called for each visible tile
		protected override void UpdateTile(UIElement _tile, IntPoint ml)
		{
			MapControlTile tile = (MapControlTile)_tile;

			// get color from mapdata using ml (map location)

			Color c = Color.FromRgb((byte)(ml.X * ml.Y), (byte)(ml.X + ml.Y), (byte)ml.X);
			tile.Color = c;
		}
	}

	class MapControlTile : UIElement
	{
		public MapControlTile()
		{
			this.IsHitTestVisible = false;
		}

		public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
			"Color", typeof(Color), typeof(MapControlTile),
			new PropertyMetadata(ValueChangedCallback));

		public Color Color
		{
			get { return (Color)GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		static void ValueChangedCallback(DependencyObject ob, DependencyPropertyChangedEventArgs e)
		{
			((MapControlTile)ob).InvalidateVisual();
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			drawingContext.DrawRectangle(new SolidColorBrush(this.Color), null, new Rect(this.RenderSize));
		}
	}
}
