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
		public InteriorInfo(InteriorID id, InteriorFlags flags)
		{
			this.ID = id;
			this.Name = id.ToString();
			this.Flags = flags;
		}

		public InteriorID ID { get; private set; }
		public string Name { get; private set; }
		public InteriorFlags Flags { get; private set; }

		public bool Blocker { get { return (this.Flags & InteriorFlags.Blocker) != 0; } }
		public bool IsSeeThrough { get { return !this.Blocker; } }
		public bool IsWaterPassable { get { return !this.Blocker; } }

		public bool IsMineable { get { return (this.Flags & InteriorFlags.Mineable) != 0; } }
	}

	public static class Interiors
	{
		static InteriorInfo[] s_interiorList;

		static Interiors()
		{
			var arr = (InteriorID[])Enum.GetValues(typeof(InteriorID));
			var max = arr.Max();
			s_interiorList = new InteriorInfo[(int)max + 1];

			foreach (var field in typeof(Interiors).GetFields())
			{
				if (field.FieldType != typeof(InteriorInfo))
					continue;

				var interInfo = (InteriorInfo)field.GetValue(null);
				s_interiorList[(int)interInfo.ID] = interInfo;
			}
		}

		public static InteriorInfo GetInterior(InteriorID id)
		{
			return s_interiorList[(int)id];
		}

		public static readonly InteriorInfo Undefined = new InteriorInfo(InteriorID.Undefined, 0);

		public static readonly InteriorInfo Empty = new InteriorInfo(InteriorID.Empty, 0);
		public static readonly InteriorInfo Wall = new InteriorInfo(InteriorID.NaturalWall, InteriorFlags.Blocker | InteriorFlags.Mineable);
		public static readonly InteriorInfo Stairs = new InteriorInfo(InteriorID.Stairs, 0);
		public static readonly InteriorInfo Sapling = new InteriorInfo(InteriorID.Sapling, 0);
		public static readonly InteriorInfo Tree = new InteriorInfo(InteriorID.Tree, InteriorFlags.Blocker);
	}
}
