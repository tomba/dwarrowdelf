using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		sealed class ActionData
		{
			public Func<LivingObject, GameAction, bool> ActionHandler;
			public Func<LivingObject, GameAction, int> GetTotalTicks;
		}

		static Dictionary<Type, ActionData> s_actionMethodMap;

		static LivingObject()
		{
			var actionTypes = Helpers.GetNonabstractSubclasses(typeof(GameAction));

			s_actionMethodMap = new Dictionary<Type, ActionData>(actionTypes.Count());

			foreach (var type in actionTypes)
			{
				var actionHandler = WrapperGenerator.CreateFuncWrapper<LivingObject, GameAction, bool>("PerformAction", type);
				if (actionHandler == null)
					throw new Exception(String.Format("No PerformAction method found for {0}", type.Name));

				var tickInitializer = WrapperGenerator.CreateFuncWrapper<LivingObject, GameAction, int>("GetTotalTicks", type);
				if (tickInitializer == null)
					throw new Exception(String.Format("No GetTotalTicks method found for {0}", type.Name));

				s_actionMethodMap[type] = new ActionData()
				{
					ActionHandler = actionHandler,
					GetTotalTicks = tickInitializer,
				};
			}
		}

		int GetTicks(SkillID skillID)
		{
			var lvl = GetSkillLevel(skillID);
			return 20 / (lvl / 26 + 1);
		}

		int GetActionTotalTicks(GameAction action)
		{
			var method = s_actionMethodMap[action.GetType()].GetTotalTicks;
			return method(this, action);
		}

		bool PerformAction(GameAction action)
		{
			Debug.Assert(this.ActionTotalTicks <= this.ActionTicksUsed);

			var method = s_actionMethodMap[action.GetType()].ActionHandler;
			return method(this, action);
		}
	}
}
