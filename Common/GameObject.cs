using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public interface IWorld
	{
		int TickNumber { get; }
		event Action TickStarting;
		Random Random { get; }
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
		IntRectZ Area { get; }
	}

	public interface IGameObject : IBaseGameObject
	{
		IEnvironment Environment { get; }
		IBaseGameObject Parent { get; }
		IntPoint3D Location { get; }

		MaterialClass MaterialClass { get; }
		MaterialID MaterialID { get; }
	}

	public interface IEnvironment : IGameObject, AStar.IAStarEnvironment
	{
		IntCuboid Bounds { get; }
		bool Contains(IntPoint3D p);

		TerrainID GetTerrainID(IntPoint3D l);
		MaterialID GetTerrainMaterialID(IntPoint3D l);

		InteriorID GetInteriorID(IntPoint3D l);
		MaterialID GetInteriorMaterialID(IntPoint3D l);

		TerrainInfo GetTerrain(IntPoint3D l);
		MaterialInfo GetTerrainMaterial(IntPoint3D l);

		InteriorInfo GetInterior(IntPoint3D l);
		MaterialInfo GetInteriorMaterial(IntPoint3D l);

		TileData GetTileData(IntPoint3D l);

		bool GetHidden(IntPoint3D l);
	}

	public interface ILiving : IGameObject
	{
		GameAction CurrentAction { get; }
		bool HasAction { get; }
		bool IsDestructed { get; }

		byte GetSkillLevel(SkillID skill);
	}

	public interface IItemObject : IGameObject
	{
		ItemClass ItemClass { get; }
		ItemID ItemID { get; }
		object ReservedBy { get; set; }
	}

	public interface IPlayer
	{
		void Send(Dwarrowdelf.Messages.ClientMessage message);
	}
}
