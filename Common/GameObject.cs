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
		BuildingInfo BuildingInfo { get; }
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
		FloorID GetFloorID(IntPoint3D l);
		MaterialID GetFloorMaterialID(IntPoint3D l);

		InteriorID GetInteriorID(IntPoint3D l);
		MaterialID GetInteriorMaterialID(IntPoint3D l);

		FloorInfo GetFloor(IntPoint3D l);
		MaterialInfo GetFloorMaterial(IntPoint3D l);

		InteriorInfo GetInterior(IntPoint3D l);
		MaterialInfo GetInteriorMaterial(IntPoint3D l);

		IEnumerable<Direction> GetDirectionsFrom(IntPoint3D p);
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
