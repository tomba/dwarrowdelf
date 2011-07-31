using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public enum LivingID : byte
	{
		Undefined = 0,
		Dwarf,
		Sheep,

		/// <summary>
		/// Used for dynamically initialized livings
		/// </summary>
		Custom,
	}

	public enum LivingClass : byte
	{
		Undefined = 0,
		Civilized,
		Herbivore,
		Carnivore,

		Custom,
	}

	public enum LivingGender : byte
	{
		Undefined,
		Male,
		Female,
	}

	public class LivingInfo
	{
		public LivingID LivingID { get; set; }
		public string Name { get; set; }
		public LivingClass LivingClass { get; set; }
		public SymbolID Symbol { get; set; }
		public GameColor Color { get; set; }
	}

	public static class Livings
	{
		static LivingInfo[] s_livings;

		static Livings()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			LivingInfo[] livings;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Livings.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					livings = (LivingInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = livings.Max(i => (int)i.LivingID);
			s_livings = new LivingInfo[max + 1];

			foreach (var living in livings)
			{
				if (s_livings[(int)living.LivingID] != null)
					throw new Exception();

				if (living.Name == null)
					living.Name = living.LivingID.ToString().ToLowerInvariant();

				s_livings[(int)living.LivingID] = living;
			}

			s_livings[(int)LivingID.Custom] = new LivingInfo()
			{
				LivingID = LivingID.Custom,
				Name = "<undefined>",
				LivingClass = LivingClass.Custom,
				Symbol = SymbolID.Undefined,
			};
		}

		public static LivingInfo GetLivingInfo(LivingID livingID)
		{
			Debug.Assert(livingID != LivingID.Undefined);
			Debug.Assert(s_livings[(int)livingID] != null);

			return s_livings[(int)livingID];
		}
	}
}
