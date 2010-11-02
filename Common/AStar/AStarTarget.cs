using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.AStar
{
	public interface IAStarTarget
	{
		bool GetIsTarget(IntPoint3D location);
		ushort GetHeuristic(IntPoint3D location);
	}

	public class AStarDefaultTarget : IAStarTarget
	{
		IntPoint3D m_destination;
		Positioning m_positioning;

		public AStarDefaultTarget(IntPoint3D destination, Positioning positioning)
		{
			m_destination = destination;
			m_positioning = positioning;
		}

		public bool GetIsTarget(IntPoint3D location)
		{
			return location.IsAdjacentTo(m_destination, m_positioning);
		}

		public ushort GetHeuristic(IntPoint3D location)
		{
			var v = m_destination - location;
#if !asd
			int hDiagonal = Math.Min(Math.Min(Math.Abs(v.X), Math.Abs(v.Y)), Math.Abs(v.Z));
			int hStraight = v.ManhattanLength;
			int h = AStar.COST_DIAGONAL * hDiagonal + AStar.COST_STRAIGHT * (hStraight - 2 * hDiagonal);
#else
			int h = v.ManhattanLength * AStar.COST_STRAIGHT;
#endif
			return (ushort)h;
		}
	}

	public class AStarAreaTarget : IAStarTarget
	{
		IntCuboid m_destination;

		public AStarAreaTarget(IntCuboid destination)
		{
			m_destination = destination;
		}

		public bool GetIsTarget(IntPoint3D location)
		{
			return m_destination.Contains(location);
		}

		public ushort GetHeuristic(IntPoint3D location)
		{
			var dst = new IntPoint3D((m_destination.X1 + m_destination.X2) / 2, (m_destination.Y1 + m_destination.Y2) / 2, (m_destination.Z1 + m_destination.Z2) / 2);
			return (ushort)((dst - location).ManhattanLength * 10);
		}
	}

	public class AStarDelegateTarget : Dwarrowdelf.AStar.IAStarTarget
	{
		Func<IntPoint3D, bool> m_func;

		public AStarDelegateTarget(Func<IntPoint3D, bool> func)
		{
			m_func = func;
		}

		public bool GetIsTarget(IntPoint3D location)
		{
			return m_func(location);
		}

		public ushort GetHeuristic(IntPoint3D location)
		{
			return 0;
		}
	}
}
