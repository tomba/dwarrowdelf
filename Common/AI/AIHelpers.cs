using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.AI
{
	public static class AIHelpers
	{
		public static ILivingObject FindNearestEnemy(ILivingObject living, LivingCategory categories)
		{
			return FindNearestEnemy(living.Environment, living.Location, living.VisionRange, categories);
		}

		public static ILivingObject FindNearestEnemy(IEnvironmentObject env, IntPoint3D location, int range, LivingCategory categories)
		{
			int maxSide = 2 * range + 1;

			var rect = new IntRectZ(location.X - maxSide / 2, location.Y - maxSide / 2, maxSide, maxSide, location.Z);

			return env.GetContents(rect)
				.OfType<ILivingObject>()
				.Where(o => (o.LivingCategory & categories) != 0)
				.OrderBy(o => (location - o.Location).Length)
				.FirstOrDefault();
		}

		public static IEnumerable<ILivingObject> FindEnemies(ILivingObject living, LivingCategory categories)
		{
			return FindEnemies(living.Environment, living.Location, living.VisionRange, categories);
		}

		public static IEnumerable<ILivingObject> FindEnemies(IEnvironmentObject env, IntPoint3D location, int range, LivingCategory categories)
		{
			int maxSide = 2 * range + 1;

			var rect = new IntRectZ(location.X - maxSide / 2, location.Y - maxSide / 2, maxSide, maxSide, location.Z);

			return env.GetContents(rect)
				.OfType<ILivingObject>()
				.Where(o => (o.LivingCategory & categories) != 0)
				.OrderBy(o => (location - o.Location).Length);
		}
	}
}
