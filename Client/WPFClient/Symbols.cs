using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;

namespace MyGame.Client.Symbols
{
	public class SymbolCollection : KeyedCollection<SymbolID, BaseSymbol>
	{
		protected override SymbolID GetKeyForItem(BaseSymbol item)
		{
			return item.ID;
		}
	}

	[ContentProperty("Symbols")]
	public class SymbolSet
	{
		public SymbolSet()
		{
			Symbols = new SymbolCollection();
		}

		public string Font { get; set; }
		public double FontSize { get; set; }
		public bool Outline { get; set; }
		public double OutlineThickness { get; set; }
		public string Drawings { get; set; }

		public SymbolCollection Symbols { get; set; }

		Typeface m_typeface;
		public Typeface Typeface
		{
			get
			{
				if (m_typeface == null)
					m_typeface = new Typeface(new FontFamily(this.Font), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

				return m_typeface;
			}
		}
	}

	public abstract class BaseSymbol
	{
		protected BaseSymbol()
		{
			this.X = 0;
			this.Y = 0;
			this.W = 100;
			this.H = 100;
		}

		string m_name;
		public string Name
		{
			get { return m_name; }

			set
			{
				m_name = value;

				SymbolID id;
				if (Enum.TryParse<SymbolID>(value, out id) == false)
					throw new Exception();
				this.ID = id;
			}
		}

		public SymbolID ID { get; set; }

		public int X { get; set; }
		public int Y { get; set; }
		public int W { get; set; }
		public int H { get; set; }
		public int Rotate { get; set; }
		public bool Opaque { get; set; }
	}

	[ContentProperty("Char")]
	public class CharSymbol : BaseSymbol
	{
		public char Char { get; set; }
		public bool? Outline { get; set; }
		public double? OutlineThickness { get; set; }
		public string Font { get; set; }
		public double? FontSize { get; set; }
		public string Color { get; set; }
		public bool Reverse { get; set; }

		Typeface m_typeface;
		public Typeface Typeface
		{
			get
			{
				if (m_typeface == null && this.Font != null)
					m_typeface = new Typeface(new FontFamily(this.Font), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

				return m_typeface;
			}
		}

	}

	[ContentProperty("DrawingName")]
	public class DrawingSymbol : BaseSymbol
	{
		public string DrawingName { get; set; }
	}

	[ContentProperty("Symbols")]
	public class CombinedSymbol : BaseSymbol
	{
		public CombinedSymbol()
		{
			Symbols = new List<BaseSymbol>();
		}

		public List<BaseSymbol> Symbols { get; set; }
	}
}
