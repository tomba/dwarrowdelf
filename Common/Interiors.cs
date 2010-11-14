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

	public class InteriorInfo
	{
		public InteriorInfo(InteriorID id, bool blocker)
		{
			this.ID = id;
			this.Name = id.ToString();
			this.Blocker = blocker;
		}

		public InteriorID ID { get; private set; }
		public string Name { get; private set; }
		public bool Blocker { get; private set; }
		public bool IsSeeThrough { get { return !this.Blocker; } }
		public bool IsWaterPassable { get { return !Blocker; } }
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

		public static readonly InteriorInfo Undefined = new InteriorInfo(InteriorID.Undefined, false);

		public static readonly InteriorInfo Empty = new InteriorInfo(InteriorID.Empty, false);
		public static readonly InteriorInfo Wall = new InteriorInfo(InteriorID.NaturalWall, true);
		public static readonly InteriorInfo Stairs = new InteriorInfo(InteriorID.Stairs, false);
		public static readonly InteriorInfo Sapling = new InteriorInfo(InteriorID.Sapling, false);
		public static readonly InteriorInfo Tree = new InteriorInfo(InteriorID.Tree, true);
	}
}
