using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public enum InteriorID : byte
	{
		Undefined,
		Empty,
		NaturalWall,
		Stairs,
		Sapling,
		Tree,
	}

	[Flags]
	public enum InteriorFlags
	{
		None = 0,
		Blocker = 1 << 0,
		Mineable = 1 << 1,
	}

	public class InteriorInfo
	{
		public InteriorID ID { get; set; }
		public string Name { get; set; }
		public InteriorFlags Flags { get; set; }

		public bool Blocker { get { return (this.Flags & InteriorFlags.Blocker) != 0; } }
		public bool IsSeeThrough { get { return !this.Blocker; } }
		public bool IsWaterPassable { get { return !this.Blocker; } }

		public bool IsMineable { get { return (this.Flags & InteriorFlags.Mineable) != 0; } }
	}

	public static class Interiors
	{
		static InteriorInfo[] s_interiors;

		static Interiors()
		{
			var asm = System.Reflection.Assembly.GetExecutingAssembly();

			InteriorInfo[] interiors;

			using (var stream = asm.GetManifestResourceStream("Dwarrowdelf.Data.Interiors.xaml"))
			{
				var settings = new System.Xaml.XamlXmlReaderSettings()
				{
					LocalAssembly = asm,
				};
				using (var reader = new System.Xaml.XamlXmlReader(stream, settings))
					interiors = (InteriorInfo[])System.Xaml.XamlServices.Load(reader);
			}

			var max = interiors.Max(m => (int)m.ID);
			s_interiors = new InteriorInfo[max + 1];

			foreach (var item in interiors)
			{
				if (s_interiors[(int)item.ID] != null)
					throw new Exception("Duplicate entry");

				if (item.Name == null)
					item.Name = item.ID.ToString().ToLowerInvariant();

				s_interiors[(int)item.ID] = item;
			}

			s_interiors[0] = new InteriorInfo()
			{
				ID = InteriorID.Undefined,
				Name = "<undefined>",
			};
		}

		public static InteriorInfo GetInterior(InteriorID id)
		{
			return s_interiors[(int)id];
		}
	}
}
