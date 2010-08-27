using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public interface IIdentifiable
	{
		ObjectID ObjectID { get; }
	}

	public interface IBuildingObject : IIdentifiable
	{
		IEnvironment Environment { get; }
		int Z { get; }
		IntRect Area { get; }
	}

	public interface IGameObject : IIdentifiable
	{
		IEnvironment Environment { get; }
		IntPoint3D Location { get; }
	}

	public interface IEnvironment : IGameObject
	{
		IEnumerable<Direction> GetDirectionsFrom(IntPoint3D p);
		InteriorInfo GetInterior(IntPoint3D l);
	}

	public interface ILiving : IGameObject
	{
		void DoAction(GameAction action);
		void DoSkipAction();
	}

	public interface IItemObject : IGameObject
	{
	}
}
