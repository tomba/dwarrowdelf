using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class Living : ClientGameObject, ILiving
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;

		Jobs.IAI m_ai;

		public Living(World world, ObjectID objectID)
			: base(world, objectID)
		{
			m_ai = new Jobs.JobManagerAI(this, this.World.JobManager);
			var aai = m_ai as Jobs.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged += OnAIAssignmentChanged;

			this.IsLiving = true;
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (LivingData)_data;

			base.Deserialize(_data);

			this.Description = this.Name;
		}

		GameAction m_currentAction;
		public GameAction CurrentAction
		{
			get { return m_currentAction; }
			private set { m_currentAction = value; Notify("CurrentAction"); }
		}

		int m_actionTicksLeft;
		public int ActionTicksLeft
		{
			get { return m_actionTicksLeft; }
			private set { m_actionTicksLeft = value; Notify("ActionTicksLeft"); }
		}

		public bool HasAction { get { return this.CurrentAction != null; } }
		public int ActionUserID { get; private set; }

		public GameAction DecideAction(ActionPriority priority)
		{
			GameAction action = null;

			if (m_ai != null)
				action = m_ai.DecideAction(priority);

			return action;
		}

		void OnAIAssignmentChanged(Jobs.IAssignment assignment)
		{
			if (assignment != null)
				this.ClientAssignment = assignment.GetType().Name;
			else
				this.ClientAssignment = null;
		}

		public void HandleActionStarted(ActionStartedChange change)
		{
			Debug.Assert(!this.HasAction);

			this.CurrentAction = change.Action;
			this.ActionTicksLeft = change.TicksLeft;
			this.ActionUserID = change.UserID;

			if (m_ai != null)
				m_ai.ActionStarted(change);
		}

		public void HandleActionProgress(ActionProgressChange change)
		{
			Debug.Assert(this.HasAction);

			this.ActionTicksLeft = change.TicksLeft;

			if (m_ai != null)
				m_ai.ActionProgress(change);

			if (change.TicksLeft == 0)
			{
				//Debug.Print("ActionDone({0}: {1})", this, this.CurrentAction);
				this.CurrentAction = null;
			}
		}

		public void RequestAction(GameAction action)
		{
			Debug.Print("RequestAction({0}: {1})", this, action);

			GameData.Data.Connection.SignalLivingHasAction(this, action);
		}

		public bool UserActionPossible()
		{
			return !this.HasAction || this.CurrentAction.Priority < ActionPriority.High;
		}

		public Grid2D<bool> VisionMap
		{
			get
			{
				UpdateLOS();
				return m_visionMap;
			}
		}

		void UpdateLOS()
		{
			Debug.Assert(this.Environment.VisibilityMode == VisibilityMode.LOS);

			if (this.Environment == null)
				return;

			if (m_losLocation == this.Location && m_losMapVersion == this.Environment.Version && m_visionMap != null)
				return;

			int visionRange = this.VisionRange;

			if (m_visionMap == null)
			{
				m_visionMap = new Grid2D<bool>(visionRange * 2 + 1, visionRange * 2 + 1,
					visionRange, visionRange);
				m_losMapVersion = 0;
			}

			var env = this.Environment;
			var z = this.Location.Z;

			s_losAlgo.Calculate(this.Location.ToIntPoint(), visionRange,
				m_visionMap, env.Bounds.Plane,
				l => env.GetInterior(new IntPoint3D(l, z)).Blocker);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		public override string ToString()
		{
			return String.Format("Living({0})", this.ObjectID.Value);
		}



		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.HitPoints:
					this.HitPoints = (int)value;
					break;

				case PropertyID.SpellPoints:
					this.SpellPoints = (int)value;
					break;

				case PropertyID.Strength:
					this.Strength = (int)value;
					break;

				case PropertyID.Dexterity:
					this.Dexterity = (int)value;
					break;

				case PropertyID.Constitution:
					this.Constitution = (int)value;
					break;

				case PropertyID.Intelligence:
					this.Intelligence = (int)value;
					break;

				case PropertyID.Wisdom:
					this.Wisdom = (int)value;
					break;

				case PropertyID.Charisma:
					this.Charisma = (int)value;
					break;

				case PropertyID.ArmorClass:
					this.ArmorClass = (int)value;
					break;

				case PropertyID.VisionRange:
					this.VisionRange = (int)value;
					break;

				case PropertyID.FoodFullness:
					this.FoodFullness = (int)value;
					break;

				case PropertyID.WaterFullness:
					this.WaterFullness = (int)value;
					break;

				case PropertyID.Assignment:
					this.ServerAssignment = (string)value;
					break;

				default:
					base.SetProperty(propertyID, value);
					break;
			}
		}

		int m_hitPoints;
		public int HitPoints
		{
			get { return m_hitPoints; }
			private set { m_hitPoints = value; Notify("HitPoints"); }
		}

		int m_spellPoints;
		public int SpellPoints
		{
			get { return m_spellPoints; }
			private set { m_spellPoints = value; Notify("SpellPoints"); }
		}

		int m_strength;
		public int Strength
		{
			get { return m_strength; }
			private set { m_strength = value; Notify("Strength"); }
		}

		int m_dexterity;
		public int Dexterity
		{
			get { return m_dexterity; }
			private set { m_dexterity = value; Notify("Dexterity"); }
		}

		int m_constitution;
		public int Constitution
		{
			get { return m_constitution; }
			private set { m_constitution = value; Notify("Constitution"); }
		}

		int m_intelligence;
		public int Intelligence
		{
			get { return m_intelligence; }
			private set { m_intelligence = value; Notify("Intelligence"); }
		}

		int m_wisdom;
		public int Wisdom
		{
			get { return m_wisdom; }
			private set { m_wisdom = value; Notify("Wisdom"); }
		}

		int m_charisma;
		public int Charisma
		{
			get { return m_charisma; }
			private set { m_charisma = value; Notify("Charisma"); }
		}

		int m_armorClass;
		public int ArmorClass
		{
			get { return m_armorClass; }
			private set { m_armorClass = value; Notify("ArmorClass"); }
		}

		int m_visionRange;
		public int VisionRange
		{
			get { return m_visionRange; }
			private set
			{
				m_visionRange = value;
				m_visionMap = null;
				Notify("VisionRange");
			}
		}

		int m_foodFullness;
		public int FoodFullness
		{
			get { return m_foodFullness; }
			private set { m_foodFullness = value; Notify("FoodFullness"); }
		}

		int m_waterFullness;
		public int WaterFullness
		{
			get { return m_waterFullness; }
			private set { m_waterFullness = value; Notify("WaterFullness"); }
		}

		string m_serverAssignment;
		public string ServerAssignment
		{
			get { return m_serverAssignment; }
			private set { m_serverAssignment = value; Notify("ServerAssignment"); }
		}

		string m_clientAssignment;
		public string ClientAssignment
		{
			get { return m_clientAssignment; }
			private set { m_clientAssignment = value; Notify("ClientAssignment"); }
		}
	}
}
