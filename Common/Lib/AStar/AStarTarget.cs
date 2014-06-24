using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public interface IAStarTarget
	{
		bool GetIsTarget(IntVector3 p);
		ushort GetHeuristic(IntVector3 p);
		ushort GetCostBetween(IntVector3 src, IntVector3 dst);
		IEnumerable<Direction> GetValidDirs(IntVector3 p);
	}

	public abstract class AStarEnvTargetBase : IAStarTarget
	{
		public const int COST_DIAGONAL = 14;
		public const int COST_STRAIGHT = 10;

		protected IEnvironmentObject m_env;

		protected AStarEnvTargetBase(IEnvironmentObject env)
		{
			m_env = env;
		}

		public ushort GetCostBetween(IntVector3 src, IntVector3 dst)
		{
			ushort cost = (src - dst).ManhattanLength == 1 ? (ushort)COST_STRAIGHT : (ushort)COST_DIAGONAL;
			// XXX add tile weight
			return cost;
		}


		public IEnumerable<Direction> GetValidDirs(IntVector3 p)
		{
			return EnvironmentExtensions.GetDirectionsFrom(m_env, p);
		}

		public abstract bool GetIsTarget(IntVector3 p);
		public abstract ushort GetHeuristic(IntVector3 p);
	}

	public sealed class AStarDefaultTarget : AStarEnvTargetBase
	{
		IntVector3 m_destination;
		DirectionSet m_positioning;

		public AStarDefaultTarget(IEnvironmentObject env, IntVector3 destination, DirectionSet positioning)
			: base(env)
		{
			m_destination = destination;
			m_positioning = positioning;
		}

		public override bool GetIsTarget(IntVector3 p)
		{
			return p.IsAdjacentTo(m_destination, m_positioning);
		}

		public override ushort GetHeuristic(IntVector3 p)
		{
			var v = m_destination - p;

			int hDiagonal = Math.Min(Math.Min(Math.Abs(v.X), Math.Abs(v.Y)), Math.Abs(v.Z));
			int hStraight = v.ManhattanLength;
			int h = COST_DIAGONAL * hDiagonal + COST_STRAIGHT * (hStraight - 2 * hDiagonal);

			return (ushort)h;
		}
	}

	public sealed class AStarAreaTarget : AStarEnvTargetBase
	{
		IntGrid3 m_destination;

		public AStarAreaTarget(IEnvironmentObject env, IntGrid3 destination)
			: base(env)
		{
			m_destination = destination;
		}

		public override bool GetIsTarget(IntVector3 location)
		{
			return m_destination.Contains(location);
		}

		public override ushort GetHeuristic(IntVector3 location)
		{
			var dst = m_destination.Center;
			return (ushort)((dst - location).ManhattanLength * 10);
		}
	}

	public sealed class AStarDelegateTarget : AStarEnvTargetBase
	{
		Func<IntVector3, bool> m_func;

		public AStarDelegateTarget(IEnvironmentObject env, Func<IntVector3, bool> func)
			: base(env)
		{
			m_func = func;
		}

		public override bool GetIsTarget(IntVector3 location)
		{
			return m_func(location);
		}

		public override ushort GetHeuristic(IntVector3 location)
		{
			return 0;
		}
	}
}
