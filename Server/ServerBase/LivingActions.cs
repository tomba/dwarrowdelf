using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class Living
	{
		void InitializeAction(GameAction action, out int ticks)
		{
			if (action is WaitAction)
			{
				ticks = ((WaitAction)action).WaitTicks;
			}
			else if (action is MineAction)
			{
				ticks = 3;
			}
			else if (action is FellTreeAction)
			{
				ticks = 5;
			}
			else if (action is MoveAction)
			{
				ticks = 1;
			}
			else if (action is BuildItemAction)
			{
				ticks = 8;
			}
			else if (action is ConsumeAction)
			{
				ticks = 6;
			}
			else
			{
				ticks = 1;
			}
		}


		void Perform(GameAction action, out bool success)
		{
			if (action is MoveAction)
			{
				PerformMove((MoveAction)action, out success);
			}
			else if (action is WaitAction)
			{
				PerformWait((WaitAction)action, out success);
			}
			else if (action is GetAction)
			{
				PerformGet((GetAction)action, out success);
			}
			else if (action is DropAction)
			{
				PerformDrop((DropAction)action, out success);
			}
			else if (action is ConsumeAction)
			{
				PerformConsume((ConsumeAction)action, out success);
			}
			else if (action is MineAction)
			{
				PerformMine((MineAction)action, out success);
			}
			else if (action is FellTreeAction)
			{
				PerformFellTree((FellTreeAction)action, out success);
			}
			else if (action is BuildItemAction)
			{
				PerformBuildItem((BuildItemAction)action, out success);
			}
			else
			{
				throw new NotImplementedException();
			}
		}


		void PerformGet(GetAction action, out bool success)
		{
			success = false;

			if (this.Environment == null)
				return;

			if (this.ActionTicksLeft > 0)
			{
				success = true;
				return;
			}

			var list = this.Environment.GetContents(this.Location);
			if (list == null)
				return;

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var item = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (item == null)
					throw new Exception();

				if (item.MoveTo(this) == false)
					throw new Exception();
			}

			success = true;
		}

		void PerformDrop(DropAction action, out bool success)
		{
			success = false;

			if (this.Environment == null)
				return;

			if (this.ActionTicksLeft > 0)
			{
				success = true;
				return;
			}

			var list = this.Inventory;
			if (list == null)
				throw new Exception();

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var ob = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (ob == null)
					throw new Exception();

				if (ob.MoveTo(this.Environment, this.Location) == false)
					throw new Exception();
			}

			success = true;
		}

		void PerformConsume(ConsumeAction action, out bool success)
		{
			success = false;

			if (this.ActionTicksLeft > 0)
			{
				success = true;
				return;
			}

			var inv = this.Inventory;
			if (inv == null)
				throw new Exception();

			var ob = inv.FirstOrDefault(o => o.ObjectID == action.ItemObjectID);

			var item = ob as ItemObject;

			if (item == null)
			{
				success = false;
				return;
			}

			var refreshment = item.RefreshmentValue;
			var nutrition = item.NutritionalValue;

			if (refreshment == 0 || nutrition == 0)
			{
				success = false;
				return;
			}

			ob.Destruct();

			this.WaterFullness += refreshment;
			this.FoodFullness += nutrition;

			success = true;
		}

		void PerformMove(MoveAction action, out bool success)
		{
			// this should check if movement is blocked, even when TicksLeft > 0
			if (this.ActionTicksLeft == 0)
				success = MoveDir(action.Direction);
			else
				success = true;
		}

		void PerformMine(MineAction action, out bool success)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id == InteriorID.Wall)
			{
				if (this.ActionTicksLeft == 0)
					this.Environment.SetInteriorID(p, InteriorID.Empty);
				success = true;
			}
			else
			{
				success = false;
			}
		}

		void PerformFellTree(FellTreeAction action, out bool success)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id == InteriorID.Tree)
			{
				if (this.ActionTicksLeft == 0)
				{
					var material = this.Environment.GetInteriorMaterialID(p);
					this.Environment.SetInteriorID(p, InteriorID.Empty);
					var log = new ItemObject()
					{
						Name = "Log",
						MaterialID = material,
						SymbolID = Dwarrowdelf.SymbolID.Log,
						Color = GameColor.SaddleBrown,
					};
					log.Initialize(this.World);
					var ok = log.MoveTo(this.Environment, p);
					Debug.Assert(ok);
				}
				success = true;
			}
			else
			{
				success = false;
			}
		}

		void PerformWait(WaitAction action, out bool success)
		{
			success = true;
		}

		void PerformBuildItem(BuildItemAction action, out bool success)
		{
			var building = this.Environment.GetBuildingAt(this.Location);

			if (building == null)
			{
				success = false;
				return;
			}

			if (this.ActionTicksLeft != 0)
			{
				success = building.VerifyBuildItem(this, action.SourceObjectIDs);
			}
			else
			{
				success = building.PerformBuildItem(this, action.SourceObjectIDs);
			}
		}
	}
}
