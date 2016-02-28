using System;

namespace Dwarrowdelf.Client.UI
{
	public enum ClientToolMode
	{
		None = 0,

		Info,
		View,

		DesignationRemove,
		DesignationMine,
		DesignationStairs,
		DesignationFellTree,

		CreateStockpile,
		InstallItem,
		BuildItem,

		ConstructWall,
		ConstructFloor,
		ConstructPavement,
		ConstructRemove,

		// Debug
		SetTerrain,
		CreateItem,
		CreateLiving,
	}
}
