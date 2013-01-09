using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Collections;

namespace Dwarrowdelf.Client
{
	[SaveGameObject(ClientObject = true)]
	public sealed class LivingObject : ConcreteObject, ILivingObject
	{
		// XXX we need a global lock
		static object m_visionMapLock = new object();

		uint m_losMapVersion;
		IntPoint3 m_losLocation;
		Grid2D<bool> m_visionMap;

		Dwarrowdelf.AI.IAI m_ai;
		bool m_isControllable;

		public ReadOnlyObservableCollection<Tuple<SkillID, byte>> Skills { get; private set; }
		ObservableCollection<Tuple<SkillID, byte>> m_skills;

		public ReadOnlyObservableCollection<Tuple<ArmorSlot, ItemObject>> ArmorSlots { get; private set; }
		ObservableCollection<Tuple<ArmorSlot, ItemObject>> m_armorSlots;

		[SaveGameProperty]
		BitArray m_enabledLabors;

		public BitArray EnabledLabors { get { return m_enabledLabors; } }

		/// <summary>
		/// For Design-time only
		/// </summary>
		public LivingObject()
			: this(null, new ObjectID(ObjectType.Living, 1234))
		{
			var r = new Random();

			var props = new Tuple<PropertyID, object>[]
			{
				new Tuple<PropertyID, object>(PropertyID.Name, "Testname"),
				new Tuple<PropertyID, object>(PropertyID.Gender, LivingGender.Male),
				new Tuple<PropertyID, object>(PropertyID.MaterialID, MaterialID.Flesh),
			};

			var data = new LivingData()
			{
				ObjectID = new ObjectID(ObjectType.Living, (uint)r.Next(5000)),
				LivingID = Dwarrowdelf.LivingID.Dwarf,
				CreationTick = r.Next(),
				CreationTime = DateTime.Now,
				Properties = props,
			};

			ReceiveObjectData(data);

			var item = new ItemObject();
			item.MoveTo(this, new IntPoint3());

			ReceiveObjectDataEnd();
		}

		public LivingObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.IsLiving = true;

			// XXX Create only for dwarves
			m_enabledLabors = new BitArray(EnumHelpers.GetEnumMax<LaborID>() + 1);
			m_enabledLabors.SetAll(true);

			m_armorSlots = new ObservableCollection<Tuple<ArmorSlot, ItemObject>>();
			this.ArmorSlots = new ReadOnlyObservableCollection<Tuple<ArmorSlot, ItemObject>>(m_armorSlots);
		}

		public override void ReceiveObjectData(BaseGameObjectData _data)
		{
			var data = (LivingData)_data;

			this.LivingInfo = Dwarrowdelf.Livings.GetLivingInfo(data.LivingID);
			this.SymbolID = LivingSymbols.GetSymbol(this.LivingID);

			base.ReceiveObjectData(_data);

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

		public override void ReceiveObjectDataEnd()
		{
			base.ReceiveObjectDataEnd();

			RecalcArmorClass();
		}

		public LivingID LivingID { get { return this.LivingInfo.ID; } }
		public LivingInfo LivingInfo { get; private set; }
		public LivingCategory LivingCategory { get { return this.LivingInfo.Category; } }

		bool m_isManuallyControlled;
		public bool IsManuallyControlled
		{
			get { return m_isManuallyControlled; }
			set
			{
				if (value == m_isManuallyControlled)
					return;

				m_isManuallyControlled = value;

				if (m_isManuallyControlled)
					this.AI = new ManualControlAI(this, 1);
				else
					this.AI = new DwarfAI(this);

				Notify("IsManuallyControlled");
			}
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
					this.World.Controllables.Add(this);
					this.AI = new DwarfAI(this);
				}
				else
				{
					this.World.Controllables.Remove(this);
					this.AI = null;
				}

				m_isControllable = value;
			}
		}

		[SaveGameProperty]
		public Dwarrowdelf.AI.IAI AI
		{
			get { return m_ai; }

			private set
			{
				if (m_ai != null)
				{
					var aai = m_ai as Dwarrowdelf.AI.AssignmentAI;
					if (aai != null)
					{
						aai.Abort();
						aai.AssignmentChanged -= OnAIAssignmentChanged;
					}
				}

				m_ai = value;

				if (m_ai != null)
				{
					var jmai = m_ai as DwarfAI;
					if (jmai != null)
						jmai.JobManager = this.World.JobManager;

					var aai = m_ai as Dwarrowdelf.AI.AssignmentAI;
					if (aai != null)
						aai.AssignmentChanged += OnAIAssignmentChanged;
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

		public void HandleActionStartEvent(ActionStartEvent e)
		{
			Debug.Assert(!this.HasAction);

			this.CurrentAction = e.Action;
			this.ActionPriority = e.Priority;
			this.ActionUserID = e.UserID;

			if (this.AI != null)
				this.AI.ActionStarted(e);
		}

		public void HandleActionProgressEvent(ActionProgressEvent e)
		{
			Debug.Assert(this.HasAction);

			this.ActionTotalTicks = e.TotalTicks;
			this.ActionTicksUsed = e.TicksUsed;

			if (this.AI != null)
				this.AI.ActionProgress(e);
		}

		public void HandleActionDone(ActionDoneEvent e)
		{
			Debug.Assert(this.HasAction);

			//if (e.State != ActionState.Done)
			//	Debug.Print("ActionDone({0}, {1}, {2})", this, e.State, this.CurrentAction);

			if (this.AI != null)
				this.AI.ActionDone(e);

			this.PreviousAction = this.CurrentAction;
			this.CurrentAction = null;
			this.ActionPriority = Dwarrowdelf.ActionPriority.Undefined;
			this.ActionTotalTicks = 0;
			this.ActionTicksUsed = 0;
		}

		static public event Action<LivingObject, GameAction> LivingRequestsAction;

		public void RequestAction(GameAction action)
		{
			//Debug.Print("RequestAction({0}: {1})", this, action);
			if (LivingObject.LivingRequestsAction != null)
				LivingObject.LivingRequestsAction(this, action);
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

			lock (m_visionMapLock)
			{
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

				ShadowCastRecursive.Calculate(this.Location.ToIntPoint(), visionRange,
					m_visionMap, env.Size.Plane,
					p2 =>
					{
						var td = env.GetTileData(new IntPoint3(p2, z));
						return !td.IsUndefined && !td.IsSeeThrough;
					});

				m_losMapVersion = this.Environment.Version;
				m_losLocation = this.Location;
			}
		}

		public override string ToString()
		{
			string name;

			if (this.IsDestructed)
				name = "<DestructedObject>";
			else if (!this.IsInitialized)
				name = "<UninitializedObject>";
			else if (this.Name != null)
				name = this.Name;
			else
				name = this.LivingInfo.Name;

			return String.Format("{0} ({1})", name, this.ObjectID);
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

				case PropertyID.Size:
					this.Size = (int)value;
					break;

				case PropertyID.NaturalArmorClass:
					this.NaturalArmorClass = (int)value;
					break;

				case PropertyID.VisionRange:
					this.VisionRange = (int)value;
					break;

				case PropertyID.Hunger:
					this.Hunger = (int)value;
					break;

				case PropertyID.Thirst:
					this.Thirst = (int)value;
					break;

				case PropertyID.Exhaustion:
					this.Exhaustion = (int)value;
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

		int m_size;
		public int Size
		{
			get { return m_size; }
			private set { m_size = value; Notify("Size"); }
		}

		int m_naturalArmorClass;
		public int NaturalArmorClass
		{
			get { return m_naturalArmorClass; }
			private set { m_naturalArmorClass = value; Notify("NaturalArmorClass"); }
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

		int m_hunger;
		public int Hunger
		{
			get { return m_hunger; }
			private set { m_hunger = value; Notify("Hunger"); }
		}

		int m_thirst;
		public int Thirst
		{
			get { return m_thirst; }
			private set { m_thirst = value; Notify("Thirst"); }
		}

		int m_exhaustion;
		public int Exhaustion
		{
			get { return m_exhaustion; }
			private set { m_exhaustion = value; Notify("Exhaustion"); }
		}

		string m_serverAssignment;
		public string ServerAssignment
		{
			get { return m_serverAssignment; }
			private set { m_serverAssignment = value; Notify("ServerAssignment"); }
		}

		string m_clientAssignment;
		[SaveGameProperty]
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

		internal void OnItemIsEquippedChanged(ItemObject item, bool isEquipped)
		{
			if (item.IsArmor)
				OnArmorIsWornChanged(item, isEquipped);
			else if (item.IsWeapon)
				OnWeaponIsWieldedChanged(item, isEquipped);
			else
				throw new Exception();
		}

		void OnArmorIsWornChanged(ItemObject wearable, bool isWorn)
		{
			Debug.Assert(wearable.Parent == this);

			var slot = wearable.ArmorInfo.Slot;

			if (isWorn)
			{
				Debug.Assert(m_armorSlots.All(t => t.Item1 != slot));

				m_armorSlots.Add(new Tuple<ArmorSlot, ItemObject>(slot, wearable));
			}
			else
			{
				int idx = -1;

				for (int i = 0; i < m_armorSlots.Count; ++i)
				{
					if (m_armorSlots[i].Item1 == slot)
					{
						idx = i;
						break;
					}
				}

				if (idx == -1)
					throw new Exception();

				Debug.Assert(m_armorSlots[idx].Item2 == wearable);
				m_armorSlots.RemoveAt(idx);
			}

			RecalcArmorClass();
		}

		void RecalcArmorClass()
		{
			int ac = this.NaturalArmorClass;

			foreach (var kvp in m_armorSlots)
				ac += kvp.Item2.ArmorInfo.AC;

			this.ArmorClass = ac;
		}

		public ItemObject Weapon { get; private set; }

		void OnWeaponIsWieldedChanged(ItemObject weapon, bool isWielded)
		{
			Debug.Assert(weapon.Parent == this);

			if (isWielded)
			{
				this.Weapon = weapon;
				Notify("Weapon");
			}
			else
			{
				this.Weapon = null;
				Notify("Weapon");
			}
		}

		public void SetLaborEnabled(LaborID labor, bool set)
		{
			m_enabledLabors.Set((int)labor, set);
		}

		public bool GetLaborEnabled(LaborID labor)
		{
			return m_enabledLabors.Get((int)labor);
		}
	}

	static class LivingSymbols
	{
		static SymbolID[] s_symbols;

		static LivingSymbols()
		{
			s_symbols = new SymbolID[EnumHelpers.GetEnumMax<LivingID>() + 1];

			var set = new Action<LivingID, SymbolID>((lid, sid) => s_symbols[(int)lid] = sid);

			set(LivingID.Dwarf, SymbolID.Player);

			var symbolIDs = EnumHelpers.GetEnumValues<SymbolID>();

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
