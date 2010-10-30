using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public interface IWorld
	{
		int TickNumber { get; }
		event Action TickStartEvent;
	}

	public interface IIdentifiable
	{
		ObjectID ObjectID { get; }
	}

	public interface IBaseGameObject : IIdentifiable
	{
		IWorld World { get; }
	}

	public interface IBuildingObject : IBaseGameObject
	{
		BuildingInfo BuildingInfo { get; }
		IEnvironment Environment { get; }
		IntRect3D Area { get; }
	}

	public interface IGameObject : IBaseGameObject
	{
		IEnvironment Environment { get; }
		IntPoint3D Location { get; }

		MaterialClass MaterialClass { get; }
		MaterialID MaterialID { get; }
	}

	public interface IEnvironment : IGameObject
	{
		IntCuboid Bounds { get; }

		FloorID GetFloorID(IntPoint3D l);
		MaterialID GetFloorMaterialID(IntPoint3D l);

		InteriorID GetInteriorID(IntPoint3D l);
		MaterialID GetInteriorMaterialID(IntPoint3D l);

		FloorInfo GetFloor(IntPoint3D l);
		MaterialInfo GetFloorMaterial(IntPoint3D l);

		InteriorInfo GetInterior(IntPoint3D l);
		MaterialInfo GetInteriorMaterial(IntPoint3D l);

		TileData GetTileData(IntPoint3D l);
	}

	public interface ILiving : IGameObject
	{
		GameAction CurrentAction { get; }
		bool HasAction { get; }
	}

	public interface IItemObject : IGameObject
	{
		ItemClass ItemClass { get; }
		ItemType ItemID { get; }
		object ReservedBy { get; set; }
	}
}
