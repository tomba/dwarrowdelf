using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Diagnostics;

namespace Dwarrowdelf
{
	public enum LivingID
	{
		Undefined = 0,
		Dwarf,
		Sheep,
		Wolf,
		Dragon,
		Orc,
	}

	[Flags]
	public enum LivingCategory
	{
		None = 0,
		Civilized = 1 << 0,
		Herbivore = 1 << 1,
		Carnivore = 1 << 2,
		Monster = 1 << 3,
	}

	public enum LivingGender
	{
		Undefined,
		Male,
		Female,
	}

	public sealed class LivingInfo
	{
		public LivingID ID { get; internal set; }
		public string Name { get; internal set; }
		public LivingCategory Category { get; internal set; }
		public GameColor Color { get; internal set; }
		public int Level { get; internal set; }
		public int Size { get; internal set; }
		public int NaturalAC { get; internal set; }
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

			var max = livings.Max(i => (int)i.ID);
			s_livings = new LivingInfo[max + 1];

			foreach (var living in livings)
			{
				if (s_livings[(int)living.ID] != null)
					throw new Exception();

				if (living.Name == null)
					living.Name = living.ID.ToString().ToLowerInvariant();

				s_livings[(int)living.ID] = living;
			}
		}

		public static LivingInfo GetLivingInfo(LivingID livingID)
		{
			Debug.Assert(livingID != LivingID.Undefined);
			Debug.Assert(s_livings[(int)livingID] != null);

			return s_livings[(int)livingID];
		}
	}
}
