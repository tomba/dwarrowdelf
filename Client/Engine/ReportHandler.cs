using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	public sealed class ReportHandler
	{
		static Dictionary<Type, Action<ReportHandler, GameReport>> s_reportHandlerMap;

		static ReportHandler()
		{
			var reportTypes = Helpers.GetNonabstractSubclasses(typeof(GameReport));

			s_reportHandlerMap = new Dictionary<Type, Action<ReportHandler, GameReport>>(reportTypes.Count());

			foreach (var type in reportTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<ReportHandler, GameReport>("HandleReport", type);
				if (method == null)
					throw new NotImplementedException(String.Format("No HandleReport method found for {0}", type.Name));
				s_reportHandlerMap[type] = method;
			}
		}


		World m_world;
		bool m_verboseReports = false;

		public ReportHandler(World world)
		{
			m_world = world;
		}


		string GetPrintableItemName(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
			{
				return "nothing";
			}
			else
			{
				var ob = m_world.FindObject(objectID);
				if (ob == null)
					return "something";
				else
					return ob.ToString();
			}
		}

		string GetPrintableLivingName(ObjectID objectID)
		{
			if (objectID == ObjectID.NullObjectID)
			{
				return "nobody";
			}
			else
			{
				var ob = m_world.FindObject(objectID);
				if (ob == null)
					return "somebody";
				else
					return ob.ToString();
			}
		}


		public void HandleReportMessage(Dwarrowdelf.Messages.ReportMessage msg)
		{
			var report = msg.Report;
			var method = s_reportHandlerMap[report.GetType()];
			method(this, report);
		}

		void HandleReport(DeathReport report)
		{
			var target = m_world.FindObject<LivingObject>(report.LivingObjectID);
			Events.AddGameEvent(target, "{0} dies", target);
		}

		void HandleReport(MoveActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);

			if (report.Success == false && living.IsControllable)
				Events.AddGameEvent(living, "{0} failed to move {1}: {2}", living, report.Direction, report.FailReason);
			//else
			//	Events.AddGameEvent(living, "{0} moved {1}", living, report.Direction);
		}

		void HandleReport(HaulActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);
			var itemName = GetPrintableItemName(report.ItemObjectID);

			if (report.Success == false)
				Events.AddGameEvent(living, "{0} failed to haul {1} to {2}: {3}", living, itemName, report.Direction, report.FailReason);
			//else
			//	Events.AddGameEvent(living, "{0} hauled {1} to {2}", living, itemName, report.Direction);
		}

		void HandleItemActionReport(ItemActionReport report, string verb1, string verb2, bool verboseReportMode = false)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);
			var itemName = GetPrintableItemName(report.ItemObjectID);

			if (report.Success == false)
				Events.AddGameEvent(living, "{0} failed to {1} {2}: {3}", living, verb2, itemName, report.FailReason);
			else if (verboseReportMode == false || m_verboseReports)
				Events.AddGameEvent(living, "{0} {1} {2}", living, verb1, itemName);
		}

		void HandleReport(EquipItemActionReport report)
		{
			HandleItemActionReport(report, "equips", "equip");
		}

		void HandleReport(UnequipItemActionReport report)
		{
			HandleItemActionReport(report, "removes", "remove");
		}

		void HandleReport(GetItemActionReport report)
		{
			HandleItemActionReport(report, "gets", "get", true);
		}

		void HandleReport(DropItemActionReport report)
		{
			HandleItemActionReport(report, "drops", "drop", true);
		}

		void HandleReport(CarryItemActionReport report)
		{
			HandleItemActionReport(report, "starts carrying", "start carrying");
		}

		void HandleReport(ConsumeActionReport report)
		{
			HandleItemActionReport(report, "consumes", "consume", true);
		}

		void HandleReport(InstallItemActionReport report)
		{
			switch (report.Mode)
			{
				case InstallMode.Install:
					HandleItemActionReport(report, "installs", "install");
					break;

				case InstallMode.Uninstall:
					HandleItemActionReport(report, "uninstalls", "uninstall");
					break;

				default:
					throw new Exception();
			}
		}

		void HandleReport(BuildItemActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);
			var item = m_world.FindObject<ItemObject>(report.ItemObjectID);

			if (report.Success)
				Events.AddGameEvent(living, "{0} builds item {1}", living, item);
			else
				Events.AddGameEvent(living, "{0} fails to build item {1}: {2}", living, report.BuildableItemKey, report.FailReason);
		}

		void HandleReport(ConstructActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);

			if (report.Success)
				Events.AddGameEvent(living, "{0} constructs {1}", living, report.Mode);
			else
				Events.AddGameEvent(living, "{0} fails to construct {1}: {2}", living, report.Mode, report.FailReason);
		}

		void HandleReport(AttackActionReport report)
		{
			var attacker = m_world.FindObject<LivingObject>(report.LivingObjectID);
			string aname = attacker != null ? attacker.ToString() : "somebody";
			string tname = GetPrintableLivingName(report.TargetObjectID);

			if (!report.Success)
			{
				Events.AddGameEvent(attacker, "{0} fails to attack {1}: {2}", aname, tname, report.FailReason);
				return;
			}

			string msg;

			if (report.IsHit)
			{
				switch (report.DamageCategory)
				{
					case DamageCategory.None:
						msg = String.Format("{0} hits {1}, dealing {2} damage", aname, tname, report.Damage);
						break;

					case DamageCategory.Melee:
						msg = String.Format("{0} hits {1}, dealing {2} damage", aname, tname, report.Damage);
						break;

					default:
						throw new Exception();
				}
			}
			else
			{
				msg = String.Format("{0} misses {1}", aname, tname);
			}

			Events.AddGameEvent(attacker, msg);
		}

		void HandleReport(MineActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
			{
				switch (report.MineActionType)
				{
					case MineActionType.Mine:
						Events.AddGameEvent(living, "{0} mines {1} ({2})", living, report.Location, report.Direction);
						break;

					case MineActionType.Stairs:
						Events.AddGameEvent(living, "{0} creates stairs {1} ({2})", living, report.Location, report.Direction);
						break;
				}
			}
			else
			{
				Events.AddGameEvent(living, "{0} fails to mine {1} ({2}): {3}", living, report.Location, report.Direction, report.FailReason);
			}
		}

		void HandleReport(FellTreeActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
				Events.AddGameEvent(living, "{0} fells {1} {2}", living, report.MaterialID, report.TileID);
			else
				Events.AddGameEvent(living, "{0} fails to fell tree: {1}", living, report.FailReason);
		}

		void HandleReport(SleepActionReport report)
		{
			var living = m_world.FindObject<LivingObject>(report.LivingObjectID);
			if (report.Success == false)
				Events.AddGameEvent(living, "{0} fails to sleep: {1}", living, report.FailReason);
			else if (m_verboseReports)
				Events.AddGameEvent(living, "{0} wakes up", living);
		}
	}
}
