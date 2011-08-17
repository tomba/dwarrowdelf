using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class Living : LocatabletGameObject, ILiving
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;

		Dwarrowdelf.AI.AssignmentAI m_ai;
		bool m_isControllable;

		public ReadOnlyObservableCollection<Tuple<SkillID, byte>> Skills { get; private set; }
		ObservableCollection<Tuple<SkillID, byte>> m_skills;

		public Living(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.IsLiving = true;
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (LivingData)_data;

			this.LivingInfo = Dwarrowdelf.Livings.GetLivingInfo(data.LivingID);
			this.SymbolID = LivingSymbols.GetSymbol(this.LivingID);

			base.Deserialize(_data);

			this.CurrentAction = data.CurrentAction;
			this.ActionPriority = data.ActionPriority;
			this.ActionTicksUsed = data.ActionTicksUsed;
			this.ActionTotalTicks = data.ActionTotalTicks;
			this.ActionUserID = data.ActionUserID;

			this.Description = this.Name ?? this.LivingInfo.Name;

			if (data.Skills != null)
				m_skills = new ObservableCollection<Tuple<SkillID, byte>>(data.Skills);
			else
				m_skills = new ObservableCollection<Tuple<SkillID, byte>>();
			this.Skills = new ReadOnlyObservableCollection<Tuple<SkillID, byte>>(m_skills);
		}

		public LivingID LivingID { get { return this.LivingInfo.ID; } }
		public LivingInfo LivingInfo { get; private set; }
		public LivingCategory LivingCategory { get { return this.LivingInfo.Category; } }

		[Serializable]
		class LivingSave
		{
			public Dwarrowdelf.AI.AssignmentAI AI;
		}

		public override object Save()
		{
			if (!this.IsControllable)
				return null;

			return new LivingSave()
			{
				AI = this.AI,
			};
		}

		public override void Restore(object data)
		{
			var save = (LivingSave)data;

			// XXX this will discard the AI created when the server sends ControllablesDataMessage

			this.AI = save.AI;
		}

		public bool IsControllable
		{
			get { return m_isControllable; }

			set
			{
				if (m_isControllable == value)
					return;

				if (value == true)
				{
					GameData.Data.World.Controllables.Add(this);
					this.AI = new Dwarrowdelf.AI.JobManagerAI(this);
					//this.AI = new FighterAI(this);
				}
				else
				{
					GameData.Data.World.Controllables.Remove(this);
					this.AI = null;
				}

				m_isControllable = value;
			}
		}

		public Dwarrowdelf.AI.AssignmentAI AI
		{
			get { return m_ai; }

			private set
			{
				if (m_ai != null)
					m_ai.AssignmentChanged -= OnAIAssignmentChanged;

				m_ai = value;

				if (m_ai != null)
				{
					if (m_ai is Dwarrowdelf.AI.JobManagerAI)
						((Dwarrowdelf.AI.JobManagerAI)m_ai).JobManager = this.World.JobManager;
					m_ai.AssignmentChanged += OnAIAssignmentChanged;
				}

				Notify("AI");
			}
		}

		GameAction m_currentAction;
		public GameAction CurrentAction
		{
			get { return m_currentAction; }
			private set { m_currentAction = value; Notify("CurrentAction"); }
		}

		GameAction m_previousAction;
		public GameAction PreviousAction
		{
			get { return m_previousAction; }
			private set { m_previousAction = value; Notify("PreviousAction"); }
		}

		int m_actionTicksUsed;
		public int ActionTicksUsed
		{
			get { return m_actionTicksUsed; }
			private set { m_actionTicksUsed = value; Notify("ActionTicksUsed"); }
		}

		int m_actionTotalTicks;
		public int ActionTotalTicks
		{
			get { return m_actionTotalTicks; }
			private set { m_actionTotalTicks = value; Notify("ActionTotalTicks"); }
		}

		ActionPriority m_actionPriority;
		public ActionPriority ActionPriority
		{
			get { return m_actionPriority; }
			private set { m_actionPriority = value; Notify("ActionPriority"); }
		}

		public bool HasAction { get { return this.CurrentAction != null; } }
		public int ActionUserID { get; private set; }

		public GameAction DecideAction()
		{
			GameAction action = null;

			if (this.AI != null)
				action = this.AI.DecideAction(ActionPriority.User);

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
			this.ActionPriority = change.Priority;
			this.ActionUserID = change.UserID;

			if (this.AI != null)
				this.AI.ActionStarted(change);
		}

		public void HandleActionProgress(ActionProgressChange change)
		{
			Debug.Assert(this.HasAction);

			this.ActionTotalTicks = change.TotalTicks;
			this.ActionTicksUsed = change.TicksUsed;

			if (this.AI != null)
				this.AI.ActionProgress(change);
		}

		public void HandleActionDone(ActionDoneChange change)
		{
			Debug.Assert(this.HasAction);

			if (change.State != ActionState.Done)
			{
				string name;
				string failStr;

				if (this.Name != null)
					name = this.Name;
				else
					name = ToString();

				switch (change.State)
				{
					case ActionState.Fail:
						failStr = "failed";
						break;

					case ActionState.Abort:
						failStr = "aborted";
						break;

					default:
						throw new Exception();
				}

				GameData.Data.AddGameEvent(this, "{0} {1} action {2}: {3}", name, failStr, this.CurrentAction, change.Error);
			}

			if (this.AI != null)
				this.AI.ActionDone(change);

			//Debug.Print("ActionDone({0}: {1})", this, this.CurrentAction);
			this.PreviousAction = this.CurrentAction;
			this.CurrentAction = null;
			this.ActionPriority = Dwarrowdelf.ActionPriority.Undefined;
			this.ActionTotalTicks = 0;
			this.ActionTicksUsed = 0;
		}

		public void RequestAction(GameAction action)
		{
			Debug.Print("RequestAction({0}: {1})", this, action);

			GameData.Data.User.SignalLivingHasAction(this, action);
		}

		public bool UserActionPossible()
		{
			return !this.HasAction || this.ActionPriority < ActionPriority.High;
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
			Debug.Assert(this.World.LivingVisionMode == LivingVisionMode.LOS);

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
				l => env.GetInterior(new IntPoint3D(l, z)).IsBlocker);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		public override string ToString()
		{
			if (this.IsDestructed)
				return "<DestructedObject>";

			return String.Format("{0} ({1})", this.Name ?? this.LivingInfo.Name, this.ObjectID);
		}


		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.HitPoints:
					this.HitPoints = (int)value;
					break;

				case PropertyID.MaxHitPoints:
					this.MaxHitPoints = (int)value;
					break;

				case PropertyID.SpellPoints:
					this.SpellPoints = (int)value;
					break;

				case PropertyID.MaxSpellPoints:
					this.MaxSpellPoints = (int)value;
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

				case PropertyID.Gender:
					this.Gender = (LivingGender)value;
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

		int m_maxHitPoints;
		public int MaxHitPoints
		{
			get { return m_maxHitPoints; }
			private set { m_maxHitPoints = value; Notify("MaxHitPoints"); }
		}

		int m_spellPoints;
		public int SpellPoints
		{
			get { return m_spellPoints; }
			private set { m_spellPoints = value; Notify("SpellPoints"); }
		}

		int m_maxSpellPoints;
		public int MaxSpellPoints
		{
			get { return m_maxSpellPoints; }
			private set { m_maxSpellPoints = value; Notify("MaxSpellPoints"); }
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

		LivingGender m_gender;
		public LivingGender Gender
		{
			get { return m_gender; }
			private set { m_gender = value; Notify("Gender"); }
		}

		public byte GetSkillLevel(SkillID skill)
		{
			var tuple = m_skills.FirstOrDefault(t => t.Item1 == skill);

			if (tuple == null)
				return 0;

			return tuple.Item2;
		}

		public void SetSkillLevel(SkillID skill, byte level)
		{
			for (int i = 0; i < m_skills.Count; ++i)
			{
				if (m_skills[i].Item1 == skill)
				{
					if (level == 0)
						m_skills.RemoveAt(i);
					else
						m_skills[i] = new Tuple<SkillID, byte>(skill, level);
					return;
				}
			}

			m_skills.Add(new Tuple<SkillID, byte>(skill, level));
		}
	}

	static class LivingSymbols
	{
		static SymbolID[] s_symbols;

		static LivingSymbols()
		{
			var arr = (LivingID[])Enum.GetValues(typeof(LivingID));
			var max = arr.Max(i => (int)i);
			s_symbols = new SymbolID[max + 1];

			var set = new Action<LivingID, SymbolID>((lid, sid) => s_symbols[(int)lid] = sid);

			set(LivingID.Dwarf, SymbolID.Player);

			var symbolIDs = (SymbolID[])Enum.GetValues(typeof(SymbolID));

			for (int i = 1; i < s_symbols.Length; ++i)
			{
				if (s_symbols[i] != SymbolID.Undefined)
					continue;

				var livingID = (LivingID)i;

				var symbolID = symbolIDs.Single(sid => sid.ToString() == livingID.ToString());
				s_symbols[i] = symbolID;
			}
		}

		public static SymbolID GetSymbol(LivingID id)
		{
			var sym = s_symbols[(int)id];

			if (sym == SymbolID.Undefined)
				return SymbolID.Unknown;

			return sym;
		}
	}
}
