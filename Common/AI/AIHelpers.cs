using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.AI
{
	public static class AIHelpers
	{
		public static ILivingObject FindNearbyEnemy(IEnvironmentObject env, IntPoint3D location, int range, LivingCategory categories)
		{
			int maxSide = 2 * range + 1;

			var rect = new IntRectZ(location.X - maxSide / 2, location.Y - maxSide / 2, maxSide, maxSide, location.Z);

			var target = env.GetContents(rect)
				.OfType<ILivingObject>()
				.Where(o => (o.LivingCategory & categories) != 0)
				.OrderBy(o => (location - o.Location).Length)
				.FirstOrDefault();

			return target;
		}
	}
}
