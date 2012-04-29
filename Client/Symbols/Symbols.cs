using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace Dwarrowdelf.Client.Symbols
{
	sealed class SymbolCollection : KeyedCollection<SymbolID, Symbol>
	{
		protected override SymbolID GetKeyForItem(Symbol item)
		{
			return item.ID;
		}
	}

	[ContentProperty("Symbols")]
	sealed class SymbolSet
	{
		public SymbolSet()
		{
			Symbols = new SymbolCollection();
		}

		public string Name { get; set; }

		public FontFamily FontFamily { get; set; }
		public double FontSize { get; set; }
		public bool Outline { get; set; }
		public double OutlineThickness { get; set; }
		public string Drawings { get; set; }
		public string BitmapFile { get; set; }
		public string BitmapSizes { get; set; }

		public SymbolCollection Symbols { get; set; }
	}

	[ContentProperty("Graphics")]
	sealed class Symbol
	{
		public Symbol()
		{
			Graphics = new List<GfxBase>();
		}

		public SymbolID ID { get; set; }

		public List<GfxBase> Graphics { get; set; }
	}

	abstract class GfxBase
	{
	}

	abstract class VectorGfxBase : GfxBase
	{
		protected VectorGfxBase()
		{
			this.X = 0;
			this.Y = 0;
			this.W = 100;
			this.H = 100;
		}

		public int X { get; set; }
		public int Y { get; set; }
		public int W { get; set; }
		public int H { get; set; }
		public int Rotate { get; set; }
		public bool Opaque { get; set; }
		public double? Opacity { get; set; }
	}


	sealed class BitmapGfx : GfxBase
	{
		public int BitmapIndex { get; set; }
	}

	sealed class CharGfx : VectorGfxBase
	{
		public char Char { get; set; }
		public bool? Outline { get; set; }
		public double? OutlineThickness { get; set; }
		public FontFamily FontFamily { get; set; }
		public double? FontSize { get; set; }
		public GameColor? Color { get; set; }
		public GameColor? Background { get; set; }
		public bool Reverse { get; set; }
	}

	sealed class DrawingGfx : VectorGfxBase
	{
		public string DrawingName { get; set; }
	}

	[ContentProperty("Symbols")]
	sealed class CombinedGfx : VectorGfxBase
	{
		public CombinedGfx()
		{
			Symbols = new List<VectorGfxBase>();
		}

		public List<VectorGfxBase> Symbols { get; set; }
	}
}
