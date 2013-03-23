using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Dwarrowdelf.AI;

namespace Dwarrowdelf.Client
{
	[SaveGameObject]
	public sealed class ManualControlAI : IAI, INotifyPropertyChanged
	{
		ObservableCollection<GameAction> m_actions;
		public ReadOnlyObservableCollection<GameAction> Actions { get; private set; }
		GameAction m_currentAction;

		int m_playerID;
		int m_actionIDCounter;

		LivingObject Worker { get; set;}

		public ManualControlAI(LivingObject worker, int playerID)
		{
			this.Worker = worker;
			m_playerID = playerID;
			m_actions = new ObservableCollection<GameAction>();
			this.Actions = new ReadOnlyObservableCollection<GameAction>(m_actions);
		}

		ManualControlAI(SaveGameContext ctx)
		{
			this.Actions = new ReadOnlyObservableCollection<GameAction>(m_actions);
		}

		public void AddAction(GameAction action)
		{
			m_actions.Add(action);
		}

		#region IAI Members

		string IAI.Name { get { return "ManualControlAI"; } }

		ILivingObject IAI.Worker { get { return this.Worker; } }

		GameAction IAI.DecideAction(ActionPriority priority)
		{
			if (this.Worker.HasAction)
			{
				if (this.Worker.ActionPriority > priority)
					return this.Worker.CurrentAction;

				if (m_currentAction != null && this.Worker.CurrentAction.GUID == m_currentAction.GUID)
					return this.Worker.CurrentAction;
			}

			if (m_actions.Count == 0)
			{
				if (this.Worker.CurrentAction != null)
					return this.Worker.CurrentAction;

				return null;
			}

			m_currentAction = m_actions[0];

			var actionID = m_actionIDCounter++;
			m_currentAction.GUID = new ActionGUID(m_playerID, actionID);

			return m_currentAction;
		}

		void IAI.ActionStarted(ActionStartEvent e)
		{
			if (m_currentAction == null)
				return;

			if (e.Action.GUID != m_currentAction.GUID)
				m_currentAction = null;
		}

		void IAI.ActionProgress(ActionProgressEvent e)
		{
		}

		void IAI.ActionDone(ActionDoneEvent e)
		{
			if (m_currentAction == null)
				return;

			if (e.GUID == m_currentAction.GUID)
			{
				Debug.Assert(m_actions[0] == m_currentAction);
				m_currentAction = null;
				m_actions.RemoveAt(0);
			}
		}

		#endregion

		public override string ToString()
		{
			return "ManualControlAI";
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion

		void Notify(string info)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(info));
		}
	}
}
