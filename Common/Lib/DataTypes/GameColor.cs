using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

/*
			var props = typeof(Colors).GetProperties();
			List<Tuple<string, Color>> list = new List<Tuple<string, Color>>();
			foreach (var prop in props)
			{
				if (prop.PropertyType != typeof(Color))
					continue;

				var name = prop.Name;

				if (name == "Transparent")
					continue;

				var color = (Color)prop.GetValue(null, null);
				list.Add(new Tuple<string, Color>(name, color));
			}

			list.Sort((a, b) => String.Compare(a.Item1, b.Item1));

			var writer = System.IO.File.CreateText(@"c:\temp\kala.cs");

			writer.WriteLine("None = 0,");
			foreach (var t in list)
			{
				writer.WriteLine("{0},", t.Item1);
			}
			writer.WriteLine("NumColors,");

			writer.WriteLine(String.Format("new GameColorRGB({0},{1},{2}),", 0, 0, 0));
			foreach (var t in list)
			{
				var c = t.Item2;
				writer.WriteLine(String.Format("new GameColorRGB({0},{1},{2}),", c.R, c.G, c.B));
			}

			writer.Close();

			Application.Current.Shutdown();
			return;
*/

namespace Dwarrowdelf
{
	// Stored in render tile data, needs to be byte
	public enum GameColor : byte
	{
		None = 0,
		AliceBlue,
		AntiqueWhite,
		Aqua,
		Aquamarine,
		Azure,
		Beige,
		Bisque,
		Black,
		BlanchedAlmond,
		Blue,
		BlueViolet,
		Brown,
		BurlyWood,
		CadetBlue,
		Chartreuse,
		Chocolate,
		Coral,
		CornflowerBlue,
		Cornsilk,
		Crimson,
		Cyan,
		DarkBlue,
		DarkCyan,
		DarkGoldenrod,
		DarkGray,
		DarkGreen,
		DarkKhaki,
		DarkMagenta,
		DarkOliveGreen,
		DarkOrange,
		DarkOrchid,
		DarkRed,
		DarkSalmon,
		DarkSeaGreen,
		DarkSlateBlue,
		DarkSlateGray,
		DarkTurquoise,
		DarkViolet,
		DeepPink,
		DeepSkyBlue,
		DimGray,
		DodgerBlue,
		Firebrick,
		FloralWhite,
		ForestGreen,
		Fuchsia,
		Gainsboro,
		GhostWhite,
		Gold,
		Goldenrod,
		Gray,
		Green,
		GreenYellow,
		Honeydew,
		HotPink,
		IndianRed,
		Indigo,
		Ivory,
		Khaki,
		Lavender,
		LavenderBlush,
		LawnGreen,
		LemonChiffon,
		LightBlue,
		LightCoral,
		LightCyan,
		LightGoldenrodYellow,
		LightGray,
		LightGreen,
		LightPink,
		LightSalmon,
		LightSeaGreen,
		LightSkyBlue,
		LightSlateGray,
		LightSteelBlue,
		LightYellow,
		Lime,
		LimeGreen,
		Linen,
		Magenta,
		Maroon,
		MediumAquamarine,
		MediumBlue,
		MediumOrchid,
		MediumPurple,
		MediumSeaGreen,
		MediumSlateBlue,
		MediumSpringGreen,
		MediumTurquoise,
		MediumVioletRed,
		MidnightBlue,
		MintCream,
		MistyRose,
		Moccasin,
		NavajoWhite,
		Navy,
		OldLace,
		Olive,
		OliveDrab,
		Orange,
		OrangeRed,
		Orchid,
		PaleGoldenrod,
		PaleGreen,
		PaleTurquoise,
		PaleVioletRed,
		PapayaWhip,
		PeachPuff,
		Peru,
		Pink,
		Plum,
		PowderBlue,
		Purple,
		Red,
		RosyBrown,
		RoyalBlue,
		SaddleBrown,
		Salmon,
		SandyBrown,
		SeaGreen,
		SeaShell,
		Sienna,
		Silver,
		SkyBlue,
		SlateBlue,
		SlateGray,
		Snow,
		SpringGreen,
		SteelBlue,
		Tan,
		Teal,
		Thistle,
		Tomato,
		Turquoise,
		Violet,
		Wheat,
		White,
		WhiteSmoke,
		Yellow,
		YellowGreen,
	}

	[Serializable]
	public struct GameColorRGB : IEquatable<GameColorRGB>
	{
		public static readonly int NUMCOLORS = Enum.GetNames(typeof(GameColor)).Length;

		readonly byte m_r;
		readonly byte m_g;
		readonly byte m_b;

		static readonly GameColorRGB[] s_rgbArray = new GameColorRGB[]
		{
			new GameColorRGB(0,0,0),
			new GameColorRGB(240,248,255),
			new GameColorRGB(250,235,215),
			new GameColorRGB(0,255,255),
			new GameColorRGB(127,255,212),
			new GameColorRGB(240,255,255),
			new GameColorRGB(245,245,220),
			new GameColorRGB(255,228,196),
			new GameColorRGB(0,0,0),
			new GameColorRGB(255,235,205),
			new GameColorRGB(0,0,255),
			new GameColorRGB(138,43,226),
			new GameColorRGB(165,42,42),
			new GameColorRGB(222,184,135),
			new GameColorRGB(95,158,160),
			new GameColorRGB(127,255,0),
			new GameColorRGB(210,105,30),
			new GameColorRGB(255,127,80),
			new GameColorRGB(100,149,237),
			new GameColorRGB(255,248,220),
			new GameColorRGB(220,20,60),
			new GameColorRGB(0,255,255),
			new GameColorRGB(0,0,139),
			new GameColorRGB(0,139,139),
			new GameColorRGB(184,134,11),
			new GameColorRGB(169,169,169),
			new GameColorRGB(0,100,0),
			new GameColorRGB(189,183,107),
			new GameColorRGB(139,0,139),
			new GameColorRGB(85,107,47),
			new GameColorRGB(255,140,0),
			new GameColorRGB(153,50,204),
			new GameColorRGB(139,0,0),
			new GameColorRGB(233,150,122),
			new GameColorRGB(143,188,143),
			new GameColorRGB(72,61,139),
			new GameColorRGB(47,79,79),
			new GameColorRGB(0,206,209),
			new GameColorRGB(148,0,211),
			new GameColorRGB(255,20,147),
			new GameColorRGB(0,191,255),
			new GameColorRGB(105,105,105),
			new GameColorRGB(30,144,255),
			new GameColorRGB(178,34,34),
			new GameColorRGB(255,250,240),
			new GameColorRGB(34,139,34),
			new GameColorRGB(255,0,255),
			new GameColorRGB(220,220,220),
			new GameColorRGB(248,248,255),
			new GameColorRGB(255,215,0),
			new GameColorRGB(218,165,32),
			new GameColorRGB(128,128,128),
			new GameColorRGB(0,128,0),
			new GameColorRGB(173,255,47),
			new GameColorRGB(240,255,240),
			new GameColorRGB(255,105,180),
			new GameColorRGB(205,92,92),
			new GameColorRGB(75,0,130),
			new GameColorRGB(255,255,240),
			new GameColorRGB(240,230,140),
			new GameColorRGB(230,230,250),
			new GameColorRGB(255,240,245),
			new GameColorRGB(124,252,0),
			new GameColorRGB(255,250,205),
			new GameColorRGB(173,216,230),
			new GameColorRGB(240,128,128),
			new GameColorRGB(224,255,255),
			new GameColorRGB(250,250,210),
			new GameColorRGB(211,211,211),
			new GameColorRGB(144,238,144),
			new GameColorRGB(255,182,193),
			new GameColorRGB(255,160,122),
			new GameColorRGB(32,178,170),
			new GameColorRGB(135,206,250),
			new GameColorRGB(119,136,153),
			new GameColorRGB(176,196,222),
			new GameColorRGB(255,255,224),
			new GameColorRGB(0,255,0),
			new GameColorRGB(50,205,50),
			new GameColorRGB(250,240,230),
			new GameColorRGB(255,0,255),
			new GameColorRGB(128,0,0),
			new GameColorRGB(102,205,170),
			new GameColorRGB(0,0,205),
			new GameColorRGB(186,85,211),
			new GameColorRGB(147,112,219),
			new GameColorRGB(60,179,113),
			new GameColorRGB(123,104,238),
			new GameColorRGB(0,250,154),
			new GameColorRGB(72,209,204),
			new GameColorRGB(199,21,133),
			new GameColorRGB(25,25,112),
			new GameColorRGB(245,255,250),
			new GameColorRGB(255,228,225),
			new GameColorRGB(255,228,181),
			new GameColorRGB(255,222,173),
			new GameColorRGB(0,0,128),
			new GameColorRGB(253,245,230),
			new GameColorRGB(128,128,0),
			new GameColorRGB(107,142,35),
			new GameColorRGB(255,165,0),
			new GameColorRGB(255,69,0),
			new GameColorRGB(218,112,214),
			new GameColorRGB(238,232,170),
			new GameColorRGB(152,251,152),
			new GameColorRGB(175,238,238),
			new GameColorRGB(219,112,147),
			new GameColorRGB(255,239,213),
			new GameColorRGB(255,218,185),
			new GameColorRGB(205,133,63),
			new GameColorRGB(255,192,203),
			new GameColorRGB(221,160,221),
			new GameColorRGB(176,224,230),
			new GameColorRGB(128,0,128),
			new GameColorRGB(255,0,0),
			new GameColorRGB(188,143,143),
			new GameColorRGB(65,105,225),
			new GameColorRGB(139,69,19),
			new GameColorRGB(250,128,114),
			new GameColorRGB(244,164,96),
			new GameColorRGB(46,139,87),
			new GameColorRGB(255,245,238),
			new GameColorRGB(160,82,45),
			new GameColorRGB(192,192,192),
			new GameColorRGB(135,206,235),
			new GameColorRGB(106,90,205),
			new GameColorRGB(112,128,144),
			new GameColorRGB(255,250,250),
			new GameColorRGB(0,255,127),
			new GameColorRGB(70,130,180),
			new GameColorRGB(210,180,140),
			new GameColorRGB(0,128,128),
			new GameColorRGB(216,191,216),
			new GameColorRGB(255,99,71),
			new GameColorRGB(64,224,208),
			new GameColorRGB(238,130,238),
			new GameColorRGB(245,222,179),
			new GameColorRGB(255,255,255),
			new GameColorRGB(245,245,245),
			new GameColorRGB(255,255,0),
			new GameColorRGB(154,205,50),
		};

		GameColorRGB(byte r, byte g, byte b)
		{
			m_r = r;
			m_g = g;
			m_b = b;
		}

		public byte R { get { return m_r; } }
		public byte G { get { return m_g; } }
		public byte B { get { return m_b; } }

		public override string ToString()
		{
			return String.Format(System.Globalization.CultureInfo.InvariantCulture,
				"Color({0:X},{1:X},{2:X})", m_r, m_g, m_b);
		}

		#region IEquatable<GameColor> Members

		public bool Equals(GameColorRGB other)
		{
			return ((other.m_r == this.m_r) && (other.m_g == this.m_g) && (other.m_b == this.m_b));
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (!(obj is GameColorRGB))
				return false;

			return Equals((GameColorRGB)obj);
		}

		public override int GetHashCode()
		{
			return (m_r << 16) | (m_g << 8) | m_b;
		}

		public int ToRGBA()
		{
			return (255 << 24) | (m_b << 16) | (m_g << 8) | m_r;
		}

		public static bool operator ==(GameColorRGB left, GameColorRGB right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(GameColorRGB left, GameColorRGB right)
		{
			return !left.Equals(right);
		}

		public static GameColorRGB FromGameColor(GameColor color)
		{
			return s_rgbArray[(int)color];
		}
	}

	public static class GameColorExtensions
	{
		public static GameColorRGB ToGameColorRGB(this GameColor color)
		{
			return GameColorRGB.FromGameColor(color);
		}

	}
}
