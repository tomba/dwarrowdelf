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
			public Func<LivingObject, GameAction, ActionState> ProcessAction;
		}

		static Dictionary<Type, ActionData> s_actionMethodMap;

		static LivingObject()
		{
			var actionTypes = Helpers.GetNonabstractSubclasses(typeof(GameAction));

			s_actionMethodMap = new Dictionary<Type, ActionData>(actionTypes.Count());

			foreach (var type in actionTypes)
			{
				var processAction = WrapperGenerator.CreateFuncWrapper<LivingObject, GameAction, ActionState>("ProcessAction", type);
				if (processAction == null)
					throw new Exception(String.Format("No ProcessAction method found for {0}", type.Name));

				s_actionMethodMap[type] = new ActionData()
				{
					ProcessAction = processAction,
				};
			}
		}

		int GetTicks(SkillID skillID)
		{
			var lvl = GetSkillLevel(skillID);
			return 20 / (lvl / 26 + 1);
		}

		ActionState ProcessAction(GameAction action)
		{
			var method = s_actionMethodMap[action.GetType()].ProcessAction;
			return method(this, action);
		}
	}
}
