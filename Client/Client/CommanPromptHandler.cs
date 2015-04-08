using Dwarrowdelf.Client.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	static class CommandPromptHandler
	{
		public static void Handle(string str)
		{
			Action<string> handler;

			if (s_commandMap.TryGetValue(str, out handler) == false)
			{
				Events.AddInformative("Unknown command '{0}'", str);
				return;
			}

			handler(str);
		}

		static readonly Dictionary<string, Action<string>> s_commandMap = new Dictionary<string, Action<string>>()
		{
			{ "n", HandleDirection },
			{ "ne", HandleDirection },
			{ "e", HandleDirection },
			{ "se", HandleDirection },
			{ "s", HandleDirection },
			{ "sw", HandleDirection },
			{ "w", HandleDirection },
			{ "nw", HandleDirection },
			{ "u", HandleDirection },
			{ "d", HandleDirection },

			{ "get", HandleGetItem },
			{ "drop", HandleDropItem },

			{ "wear", HandleWearItem },
			{ "remove", HandleRemoveItem },

			{ "i", HandleInventory },
		};

		static Direction StringToDirection(string str)
		{
			switch (str)
			{
				case "n":
					return Direction.North;
				case "ne":
					return Direction.NorthEast;
				case "e":
					return Direction.East;
				case "se":
					return Direction.SouthEast;
				case "s":
					return Direction.South;
				case "sw":
					return Direction.SouthWest;
				case "w":
					return Direction.West;
				case "nw":
					return Direction.NorthWest;
				case "d":
					return Direction.Down;
				case "u":
					return Direction.Up;
				default:
					throw new Exception();
			}
		}

		static void HandleDirection(string str)
		{
			var dir = StringToDirection(str);

			HandleDirection(dir);
		}

		public static void HandleDirection(Direction dir)
		{
			var ob = GameData.Data.FocusedObject;

			var target = ob.Environment.GetContents(ob.Location + dir).OfType<LivingObject>().FirstOrDefault();

			GameAction action;

			if (target == null)
				action = new MoveAction(dir);
			else
				action = new AttackAction(target);

			action.GUID = new ActionGUID(ob.World.PlayerID, 0);
			ob.RequestAction(action);
		}

		static void HandleGetItem(string str)
		{
			var living = GameData.Data.FocusedObject;

			if (living == null)
				return;

			var obs = living.Environment.GetContents(living.Location).OfType<ItemObject>();

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = GameData.Data.MainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Get Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				var action = new GetItemAction(ob);
				action.GUID = new ActionGUID(living.World.PlayerID, 0);
				living.RequestAction(action);
			}
		}

		static void HandleDropItem(string str)
		{
			var living = GameData.Data.FocusedObject;

			if (living == null)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = GameData.Data.MainWindow;
			dlg.DataContext = living.Inventory;
			dlg.Title = "Drop Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				var action = new DropItemAction(ob);
				action.GUID = new ActionGUID(living.World.PlayerID, 0);
				living.RequestAction(action);
			}
		}

		static void HandleWearItem(string str)
		{
			var living = GameData.Data.FocusedObject;

			if (living == null)
				return;

			var obs = living.Inventory.OfType<ItemObject>()
				.Where(o => ((o.IsArmor || o.IsWeapon) && o.IsEquipped == false));

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = GameData.Data.MainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Wear/Wield Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				GameAction action;
				if (ob.IsArmor || ob.IsWeapon)
					action = new EquipItemAction(ob);
				else
					throw new Exception();
				action.GUID = new ActionGUID(living.World.PlayerID, 0);
				living.RequestAction(action);
			}
		}

		static void HandleRemoveItem(string str)
		{
			var living = GameData.Data.FocusedObject;

			if (living == null)
				return;

			var obs = living.Inventory.OfType<ItemObject>().Where(o => o.IsEquipped);

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = GameData.Data.MainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Remove Item";

			var ret = dlg.ShowDialog();

			if (ret.HasValue && ret.Value == true)
			{
				var ob = dlg.SelectedItem;

				GameAction action;
				if (ob.IsArmor || ob.IsWeapon)
					action = new UnequipItemAction(ob);
				else
					throw new Exception();
				action.GUID = new ActionGUID(living.World.PlayerID, 0);
				living.RequestAction(action);
			}
		}

		static void HandleInventory(string str)
		{
			var living = GameData.Data.FocusedObject;

			if (living == null)
				return;

			var obs = living.Inventory;

			if (obs.Any() == false)
				return;

			var dlg = new ItemSelectorDialog();
			dlg.Owner = GameData.Data.MainWindow;
			dlg.DataContext = obs;
			dlg.Title = "Inventory";

			dlg.ShowDialog();
		}
	}
}
