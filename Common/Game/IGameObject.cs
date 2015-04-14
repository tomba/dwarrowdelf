using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf
{
	public interface IWorld
	{
		int TickNumber { get; }
		int Year { get; }
		GameSeason Season { get; }
		event Action TickStarted;
		Random Random { get; }
	}

	public interface IIdentifiable
	{
		ObjectID ObjectID { get; }
		ObjectType ObjectType { get; }
	}

	public interface IBaseObject : IIdentifiable
	{
		IWorld World { get; }
		bool IsDestructed { get; }
		event Action<IBaseObject> Destructed;
	}

	public interface IContainerObject : IBaseObject
	{
		IEnumerable<IMovableObject> Contents { get; }
	}

	public interface IEnvironmentObject : IContainerObject
	{
		VisibilityMode VisibilityMode { get; }

		IntSize3 Size { get; }
		bool Contains(IntVector3 p);

		TerrainID GetTerrainID(IntVector3 l);
		MaterialID GetTerrainMaterialID(IntVector3 l);

		InteriorID GetInteriorID(IntVector3 l);
		MaterialID GetInteriorMaterialID(IntVector3 l);

		MaterialInfo GetTerrainMaterial(IntVector3 l);

		MaterialInfo GetInteriorMaterial(IntVector3 l);

		TileData GetTileData(IntVector3 l);

		bool GetTileFlags(IntVector3 l, TileFlags flags);

		bool HasContents(IntVector3 pos);
		IEnumerable<IMovableObject> GetContents(IntVector3 pos);
		IEnumerable<IMovableObject> GetContents(IntGrid2Z rect);
	}

	public interface IMovableObject : IContainerObject
	{
		/// <summary>
		/// Return Container as IEnvironmentObject
		/// </summary>
		IEnvironmentObject Environment { get; }
		IContainerObject Container { get; }
		IntVector3 Location { get; }
	}

	public interface IConcreteObject : IMovableObject
	{
		string Name { get; }
		GameColor Color { get; }
		MaterialInfo Material { get; }
		MaterialCategory MaterialCategory { get; }
		MaterialID MaterialID { get; }

		// Contents of type IItemObject
		IEnumerable<IItemObject> Inventory { get; }
	}

	public interface ILivingObject : IConcreteObject
	{
		GameAction CurrentAction { get; }
		ActionPriority ActionPriority { get; }
		bool HasAction { get; }

		LivingID LivingID { get; }
		LivingCategory LivingCategory { get; }

		int VisionRange { get; }

		byte GetSkillLevel(SkillID skill);

		int Hunger { get; }
		int Thirst { get; }
		int Exhaustion { get; }

		MyTraceSource Trace { get; }
	}

	public interface IItemObject : IConcreteObject
	{
		ItemInfo ItemInfo { get; }
		ItemCategory ItemCategory { get; }
		ItemID ItemID { get; }

		object ReservedBy { get; set; }
		bool IsReserved { get; }

		bool IsInstalled { get; }

		bool IsArmor { get; }
		bool IsWeapon { get; }

		ArmorInfo ArmorInfo { get; }
		WeaponInfo WeaponInfo { get; }

		ILivingObject Equipper { get; }

		bool IsEquipped { get; }

		int NutritionalValue { get; }
		int RefreshmentValue { get; }
	}

	public enum LivingVisionMode
	{
		/// <summary>
		/// everything inside VisionRange is visible
		/// </summary>
		SquareFOV,

		/// <summary>
		/// use LOS algorithm
		/// </summary>
		LOS,
	}

	public enum VisibilityMode
	{
		/// <summary>
		/// Everything visible
		/// </summary>
		AllVisible,

		/// <summary>
		/// Areas inside the mountain are not visible
		/// </summary>
		GlobalFOV,

		/// <summary>
		/// Things seen by controllables are visible
		/// </summary>
		LivingLOS,
	}

	public enum GameSeason
	{
		Spring,
		Summer,
		Autumn,
		Winter,
	}
}
