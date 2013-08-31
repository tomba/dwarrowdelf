using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	public sealed class TurnHandler
	{
		World m_world;
		ClientUser m_user;

		MyTraceSource turnTrace = new MyTraceSource("Dwarrowdelf.Turn", "ClientUser");

		Dictionary<LivingObject, GameAction> m_actionMap = new Dictionary<LivingObject, GameAction>();
		bool m_proceedTurnSent;

		public bool IsAutoAdvanceTurnEnabled { get; set; }
		public LivingObject FocusedObject { get; set; }

		public TurnHandler(World world, ClientUser user)
		{
			m_world = world;
			m_user = user;

			m_world.TurnEnded += OnTurnEnded;
			m_world.TurnStarted += OnTurnStarted;

			LivingObject.LivingRequestsAction += OnLivingRequestsAction;
			user.DisconnectEvent += user_DisconnectEvent;
		}

		void user_DisconnectEvent()
		{
			LivingObject.LivingRequestsAction -= OnLivingRequestsAction;
		}

		// Called from world
		void OnTurnStarted(ObjectID livingID)
		{
			turnTrace.TraceVerbose("TurnStart: {0}", livingID);

			if (m_world.IsOurTurn == false)
				return;

			if (this.IsAutoAdvanceTurnEnabled)
			{
				if (this.FocusedObject == null || this.FocusedObject.HasAction)
					SendProceedTurn();
			}
		}

		// Called from world
		void OnTurnEnded()
		{
			turnTrace.TraceVerbose("TurnEnd");
			m_proceedTurnSent = false;
		}

		void OnLivingRequestsAction(LivingObject living, GameAction action)
		{
			turnTrace.TraceVerbose("SignalLivingHasAction({0}, {1}", living, action);

			if (m_world.IsOurTurn == false)
			{
				turnTrace.TraceWarning("SignalLivingHasAction when not our turn");
				return;
			}

			m_actionMap[living] = action;

			SendProceedTurn();
		}

		public void SendProceedTurn()
		{
			turnTrace.TraceVerbose("SendProceedTurn");

			if (m_world.IsOurTurn == false)
			{
				turnTrace.TraceWarning("SendProceedTurn when not our turn");
				return;
			}

			if (m_proceedTurnSent)
			{
				turnTrace.TraceWarning("SendProceedTurn when proceed turn already sent");
				return;
			}

			var list = new List<KeyValuePair<ObjectID, GameAction>>();

			IEnumerable<LivingObject> livings;

			if (m_world.CurrentLivingID == ObjectID.AnyObjectID)
			{
				// livings which the user can control (ie. server not doing high priority action)
				livings = m_world.Controllables.Where(l => l.UserActionPossible());
			}
			else
			{
				var living = m_world.GetObject<LivingObject>(m_world.CurrentLivingID);
				if (living.UserActionPossible() == false)
					throw new NotImplementedException();
				livings = new LivingObject[] { living };
			}

			var focusedObject = this.FocusedObject;

			foreach (var living in livings)
			{
				GameAction action;

				if (m_actionMap.TryGetValue(living, out action) == false)
				{
					// skip AI if we're directly controlling the living
					if (focusedObject != living)
						action = living.DecideAction();
					else
						action = living.CurrentAction;
				}

				Debug.Assert(action == null || action.GUID.IsNull == false);

				if (action != living.CurrentAction)
				{
					turnTrace.TraceVerbose("{0}: selecting new action {1}", living, action);
					list.Add(new KeyValuePair<ObjectID, GameAction>(living.ObjectID, action));
				}
			}

			m_user.Send(new Dwarrowdelf.Messages.ProceedTurnReplyMessage() { Actions = list.ToArray() });

			m_proceedTurnSent = true;
			m_actionMap.Clear();
		}
	}
}
