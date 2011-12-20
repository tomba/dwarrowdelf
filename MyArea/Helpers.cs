using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;

namespace MyArea
{
	class Helpers
	{
		public readonly static Random MyRandom = new Random(123);

		public static void AddBattleGear(LivingObject living)
		{
			Helpers.AddRandomWeapon(living);
			Helpers.AddRandomArmor(living, ArmorSlot.Torso);
			Helpers.AddRandomArmor(living, ArmorSlot.Head);
			Helpers.AddRandomArmor(living, ArmorSlot.Hands);
			Helpers.AddRandomArmor(living, ArmorSlot.Feet);
		}

		public static void AddRandomArmor(LivingObject living, ArmorSlot slot)
		{
			var itemIDs = Items.GetItemInfos(ItemCategory.Armor).Where(ii => ii.ArmorInfo.Slot == slot).ToArray();
			var itemID = itemIDs[Helpers.MyRandom.Next(itemIDs.Length)].ID;

			AddArmor(living, itemID);
		}

		public static void AddArmor(LivingObject living, ItemID itemID)
		{
			var world = living.World;

			var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
			var material = materials[Helpers.MyRandom.Next(materials.Length)].ID;

			var itemBuilder = new ItemObjectBuilder(itemID, material);
			var item = itemBuilder.Create(world);

			item.MoveTo(living);

			living.WearArmor(item);
		}

		public static void AddWeapon(LivingObject living, ItemID itemID)
		{
			var world = living.World;

			var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
			var material = materials[Helpers.MyRandom.Next(materials.Length)].ID;

			var itemBuilder = new ItemObjectBuilder(itemID, material);
			var item = itemBuilder.Create(world);

			item.MoveTo(living);

			living.WieldWeapon(item);
		}

		public static void AddRandomWeapon(LivingObject living)
		{
			var itemIDs = Items.GetItemInfos(ItemCategory.Weapon).ToArray();
			var itemID = itemIDs[Helpers.MyRandom.Next(itemIDs.Length)].ID;
			AddWeapon(living, itemID);
		}
	}
}
