using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.AI
{
	public static class AIHelpers
	{
		public static ILiving FindNearbyEnemy(ILiving living, LivingCategory classMask)
		{
			var env = living.Environment;
			var center = living.Location;

			int r = living.VisionRange;
			int maxSide = 2 * r + 1;

			var rect = new IntRectZ(center.X - maxSide / 2, center.Y - maxSide / 2, maxSide, maxSide, living.Location.Z);

			var target = env.GetContents(rect)
				.Where(o => o != living)
				.OfType<ILiving>()
				.Where(o => (o.LivingCategory & classMask) != 0)
				.OrderBy(o => (center - o.Location).Length)
				.FirstOrDefault();

			return target;
		}
	}
}
