using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dwarrowdelf;
using Dwarrowdelf.Server;
using System.Threading;

namespace MyArea
{
	static class Helpers
	{
		readonly static ThreadLocal<Random> s_random = new ThreadLocal<Random>(() => new Random(123));

		public static int GetRandomInt()
		{
			return s_random.Value.Next();
		}

		public static int GetRandomInt(int exclusiveMax)
		{
			return s_random.Value.Next(exclusiveMax);
		}

		public static void AddGem(LivingObject living)
		{
			var world = living.World;

			var materials = Materials.GetMaterials(MaterialCategory.Gem).ToArray();
			var material = materials[Helpers.GetRandomInt(materials.Length)].ID;

			var itemBuilder = new ItemObjectBuilder(ItemID.Gem, material);
			var item = itemBuilder.Create(world);

			item.MoveTo(living);
		}

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
			var itemID = itemIDs[Helpers.GetRandomInt(itemIDs.Length)].ID;

			AddArmor(living, itemID);
		}

		public static void AddArmor(LivingObject living, ItemID itemID)
		{
			var world = living.World;

			var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
			var material = materials[Helpers.GetRandomInt(materials.Length)].ID;

			var itemBuilder = new ItemObjectBuilder(itemID, material);
			var item = itemBuilder.Create(world);

			item.MoveTo(living);

			living.WearArmor(item);
		}

		public static void AddWeapon(LivingObject living, ItemID itemID)
		{
			var world = living.World;

			var materials = Materials.GetMaterials(MaterialCategory.Metal).ToArray();
			var material = materials[Helpers.GetRandomInt(materials.Length)].ID;

			var itemBuilder = new ItemObjectBuilder(itemID, material);
			var item = itemBuilder.Create(world);

			item.MoveTo(living);

			living.WieldWeapon(item);
		}

		public static void AddRandomWeapon(LivingObject living)
		{
			var itemIDs = Items.GetItemInfos(ItemCategory.Weapon).ToArray();
			var itemID = itemIDs[Helpers.GetRandomInt(itemIDs.Length)].ID;
			AddWeapon(living, itemID);
		}
	}
}
