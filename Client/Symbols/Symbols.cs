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
	sealed class SymbolCollection : KeyedCollection<SymbolID, BaseSymbol>
	{
		protected override SymbolID GetKeyForItem(BaseSymbol item)
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

		public FontFamily FontFamily { get; set; }
		public double FontSize { get; set; }
		public bool Outline { get; set; }
		public double OutlineThickness { get; set; }
		public string Drawings { get; set; }

		public SymbolCollection Symbols { get; set; }
	}

	abstract class BaseSymbol
	{
		protected BaseSymbol()
		{
			this.X = 0;
			this.Y = 0;
			this.W = 100;
			this.H = 100;
		}

		public SymbolID ID { get; set; }

		public int X { get; set; }
		public int Y { get; set; }
		public int W { get; set; }
		public int H { get; set; }
		public int Rotate { get; set; }
		public bool Opaque { get; set; }
		public double? Opacity { get; set; }
	}

	[ContentProperty("Char")]
	sealed class CharSymbol : BaseSymbol
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

	[ContentProperty("DrawingName")]
	sealed class DrawingSymbol : BaseSymbol
	{
		public string DrawingName { get; set; }
	}

	[ContentProperty("Symbols")]
	sealed class CombinedSymbol : BaseSymbol
	{
		public CombinedSymbol()
		{
			Symbols = new List<BaseSymbol>();
		}

		public List<BaseSymbol> Symbols { get; set; }
	}
}
