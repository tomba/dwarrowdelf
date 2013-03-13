using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf
{
	public static class LivingExtensions
	{
		/// <summary>
		/// Determine if an object can move from its current location to dir
		/// </summary>
		public static bool CanMoveTo(this IMovableObject living, Direction dir)
		{
			var env = living.Environment;
			var src = living.Location;
			var dst = living.Location + dir;
			return env.CanMoveFrom(src, dir) && env.CanMoveTo(dst, dir);
		}
	}
}
