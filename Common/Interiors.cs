using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	// Stored in TileData, needs to be byte
	public enum InteriorID : byte
	{
		Undefined = 0,
		Empty,
		BuiltWall,
		Pavement,
		Stairs,
		Sapling,
		Tree,
		Ore,
		Grass,
	}

	[Flags]
	public enum InteriorFlags
	{
		None = 0,
		Blocker = 1 << 0,
	}

	public sealed class InteriorInfo
	{
		public InteriorID ID { get; internal set; }
		public string Name { get; internal set; }
		public InteriorFlags Flags { get; internal set; }

		public bool IsBlocker { get { return (this.Flags & InteriorFlags.Blocker) != 0; } }
		public bool IsSeeThrough { get { return !this.IsBlocker; } }
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

		/// <summary>
		/// Is Interior empty or a "soft" item that can be removed automatically
		/// </summary>
		public static bool IsClear(this InteriorID id)
		{
			return id == InteriorID.Empty || id == InteriorID.Grass || id == InteriorID.Sapling;
		}

		/// <summary>
		/// Is Interior a sapling or a full grown tree
		/// </summary>
		public static bool IsTree(this InteriorID id)
		{
			return id == InteriorID.Tree || id == InteriorID.Sapling;
		}

		public static InteriorInfo GetInterior(InteriorID id)
		{
			return s_interiors[(int)id];
		}
	}
}
