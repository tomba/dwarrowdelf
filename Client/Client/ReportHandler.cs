using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Client
{
	class ReportHandler
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

		public ReportHandler(World world)
		{
			m_world = world;
		}

		public void HandleReportMessage(Dwarrowdelf.Messages.ReportMessage msg)
		{
			var report = msg.Report;
			var method = s_reportHandlerMap[report.GetType()];
			method(this, report);
		}

		void HandleReport(DeathReport report)
		{
			var target = m_world.GetObject<LivingObject>(report.LivingObjectID);
			GameData.Data.AddGameEvent(target, "{0} dies", target);
		}

		void HandleReport(MoveActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);

			if (report.Success == false && living.IsControllable)
				GameData.Data.AddGameEvent(living, "{0} failed to move {1}: {2}", living, report.Direction, report.FailReason);
			//else
			//	GameData.Data.AddGameEvent(living, "{0} moved {1}", living, report.Direction);
		}

		void HandleItemActionReport(ItemActionReport report, string verb1, string verb2)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			var item = report.ItemObjectID != ObjectID.NullObjectID ? m_world.GetObject<ItemObject>(report.ItemObjectID) : null;

			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} {1} {2}", living, verb1, item);
			else if (item != null)
				GameData.Data.AddGameEvent(living, "{0} failed to {1} {2}: {3}", living, verb2, item, report.FailReason);
			else
				GameData.Data.AddGameEvent(living, "{0} failed to {1} something: {2}", living, verb2, report.FailReason);
		}

		void HandleReport(WearArmorActionReport report)
		{
			HandleItemActionReport(report, "wears", "wear");
		}

		void HandleReport(RemoveArmorActionReport report)
		{
			HandleItemActionReport(report, "removes", "remove");
		}

		void HandleReport(WieldWeaponActionReport report)
		{
			HandleItemActionReport(report, "wields", "wield");
		}

		void HandleReport(RemoveWeaponActionReport report)
		{
			HandleItemActionReport(report, "removes", "remove");
		}

		void HandleReport(GetActionReport report)
		{
			HandleItemActionReport(report, "gets", "get");
		}

		void HandleReport(DropActionReport report)
		{
			HandleItemActionReport(report, "drops", "drop");
		}

		void HandleReport(ConsumeActionReport report)
		{
			HandleItemActionReport(report, "consumes", "consume");
		}

		void HandleReport(ConstructBuildingActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} builds {1}", living, report.BuildingID);
			else
				GameData.Data.AddGameEvent(living, "{0} fails to build {1}: {2}", living, report.BuildingID, report.FailReason);
		}

		void HandleReport(DestructBuildingActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			var building = m_world.FindObject<BuildingObject>(report.BuildingObjectID);

			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} destructs {1}", living, building);
			else
				GameData.Data.AddGameEvent(living, "{0} fails to destruct {1}: {2}", living, building, report.FailReason);
		}

		void HandleReport(BuildItemActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} builds item XXX", living, report);
			else
				GameData.Data.AddGameEvent(living, "{0} fails to build item XXX: {1}", living, report.FailReason);
		}

		void HandleReport(AttackActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} attacks XXX", living, report);
			else
				GameData.Data.AddGameEvent(living, "{0} fails to attack XXX: {1}", living, report.FailReason);
		}

		void HandleReport(MineActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} mines", living, report);
			else
				GameData.Data.AddGameEvent(living, "{0} fails to mine: {1}", living, report.FailReason);
		}

		void HandleReport(FellTreeActionReport report)
		{
			var living = m_world.GetObject<LivingObject>(report.LivingObjectID);
			if (report.Success)
				GameData.Data.AddGameEvent(living, "{0} fells tree", living, report);
			else
				GameData.Data.AddGameEvent(living, "{0} fails to fell tree: {1}", living, report.FailReason);
		}

	}
}
