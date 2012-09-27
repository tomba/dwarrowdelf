using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public sealed partial class LivingObject : ConcreteObject, ILivingObject
	{
		internal static LivingObject Create(World world, LivingObjectBuilder builder)
		{
			var ob = new LivingObject(builder);
			ob.Initialize(world);
			return ob;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			//Debug.Print("[{0}]: {1}", this, String.Format(format, args));
		}

		uint m_losMapVersion;
		IntPoint3 m_losLocation;
		Grid2D<bool> m_visionMap;
		[SaveGameProperty]
		Dwarrowdelf.AI.IAI m_ai;

		[SaveGameProperty("Skills")]
		Dictionary<SkillID, byte> m_skillMap;

		[SaveGameProperty("ArmorSlots")]
		Dictionary<ArmorSlot, ItemObject> m_armorSlots;

		[SaveGameProperty("Weapon")]
		public ItemObject Weapon { get; private set; }

		LivingObject(LivingObjectBuilder builder)
			: base(ObjectType.Living, builder)
		{
			Debug.Assert(builder.LivingID != Dwarrowdelf.LivingID.Undefined);

			this.LivingID = builder.LivingID;

			m_maxHitPoints = m_hitPoints = builder.HitPoints;
			m_armorClass = builder.AC;

			m_strength = builder.Str;
			m_dexterity = builder.Dex;
			m_constitution = builder.Con;
			m_intelligence = builder.Int;
			m_wisdom = builder.Wis;
			m_charisma = builder.Cha;
			m_size = builder.Siz;

			m_gender = builder.Gender;

			m_visionRange = builder.VisionRange;
			m_foodFullness = builder.FoodFullness;
			m_waterFullness = builder.WaterFullness;

			m_assignment = "";

			m_skillMap = new Dictionary<SkillID, byte>();
			foreach (var kvp in builder.SkillMap)
				if (kvp.Value != 0)
					m_skillMap[kvp.Key] = kvp.Value;

			m_armorSlots = new Dictionary<ArmorSlot, ItemObject>();
		}

		LivingObject(SaveGameContext ctx)
			: base(ctx, ObjectType.Living)
		{
			this.World.TickStarting += OnTickStart;

			var aai = m_ai as Dwarrowdelf.AI.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged += OnAIAssignmentChanged;

			foreach (var kvp in m_armorSlots)
				kvp.Value.Wearer = this;
			if (this.Weapon != null)
				this.Weapon.Wielder = this;
		}

		protected override void Initialize(World world)
		{
			base.Initialize(world);
			world.AddLiving(this);
			world.TickStarting += OnTickStart;
		}

		public override void Destruct()
		{
			var aai = m_ai as Dwarrowdelf.AI.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged -= OnAIAssignmentChanged;

			m_ai = null;
			this.CurrentAction = null;
			this.ActionPriority = Dwarrowdelf.ActionPriority.Undefined;
			this.ActionTotalTicks = 0;
			this.ActionTicksUsed = 0;
			this.ActionUserID = 0;

			this.World.TickStarting -= OnTickStart;
			this.World.RemoveLiving(this);
			base.Destruct();
		}

		void OnTickStart()
		{
			// XXX track only for dwarves
			if (this.FoodFullness > 0)
				this.FoodFullness--;

			if (this.WaterFullness > 0)
				this.WaterFullness--;
		}

		[SaveGameProperty]
		public LivingID LivingID { get; private set; }
		public LivingCategory LivingCategory { get { return this.LivingInfo.Category; } }
		public LivingInfo LivingInfo { get { return Livings.GetLivingInfo(this.LivingID); } }

		[SaveGameProperty("HitPoints")]
		int m_hitPoints;
		public int HitPoints
		{
			get { return m_hitPoints; }
			set { if (m_hitPoints == value) return; m_hitPoints = value; NotifyInt(PropertyID.HitPoints, value); }
		}

		[SaveGameProperty("MaxHitPoints")]
		int m_maxHitPoints;
		public int MaxHitPoints
		{
			get { return m_maxHitPoints; }
			set { if (m_maxHitPoints == value) return; m_maxHitPoints = value; NotifyInt(PropertyID.MaxHitPoints, value); }
		}

		[SaveGameProperty("SpellPoints")]
		int m_spellPoints;
		public int SpellPoints
		{
			get { return m_spellPoints; }
			set { if (m_spellPoints == value) return; m_spellPoints = value; NotifyInt(PropertyID.SpellPoints, value); }
		}

		[SaveGameProperty("MaxSpellPoints")]
		int m_maxSpellPoints;
		public int MaxSpellPoints
		{
			get { return m_maxSpellPoints; }
			set { if (m_maxSpellPoints == value) return; m_maxSpellPoints = value; NotifyInt(PropertyID.MaxSpellPoints, value); }
		}

		[SaveGameProperty("Strength")]
		int m_strength;
		public int Strength
		{
			get { return m_strength; }
			set { if (m_strength == value) return; m_strength = value; NotifyInt(PropertyID.Strength, value); }
		}

		[SaveGameProperty("Dexterity")]
		int m_dexterity;
		public int Dexterity
		{
			get { return m_dexterity; }
			set { if (m_dexterity == value) return; m_dexterity = value; NotifyInt(PropertyID.Dexterity, value); }
		}

		[SaveGameProperty("Constitution")]
		int m_constitution;
		public int Constitution
		{
			get { return m_constitution; }
			set { if (m_constitution == value) return; m_constitution = value; NotifyInt(PropertyID.Constitution, value); }
		}

		[SaveGameProperty("Intelligence")]
		int m_intelligence;
		public int Intelligence
		{
			get { return m_intelligence; }
			set { if (m_intelligence == value) return; m_intelligence = value; NotifyInt(PropertyID.Intelligence, value); }
		}

		[SaveGameProperty("Wisdom")]
		int m_wisdom;
		public int Wisdom
		{
			get { return m_wisdom; }
			set { if (m_wisdom == value) return; m_wisdom = value; NotifyInt(PropertyID.Wisdom, value); }
		}

		[SaveGameProperty("Charisma")]
		int m_charisma;
		public int Charisma
		{
			get { return m_charisma; }
			set { if (m_charisma == value) return; m_charisma = value; NotifyInt(PropertyID.Charisma, value); }
		}

		[SaveGameProperty("Size")]
		int m_size;
		public int Size
		{
			get { return m_size; }
			set { if (m_size == value) return; m_size = value; NotifyInt(PropertyID.Size, value); }
		}

		[SaveGameProperty("ArmorClass")]
		int m_armorClass;
		public int ArmorClass
		{
			get { return m_armorClass; }
			set { if (m_armorClass == value) return; m_armorClass = value; NotifyInt(PropertyID.ArmorClass, value); }
		}

		[SaveGameProperty("VisionRange")]
		int m_visionRange;
		public int VisionRange
		{
			get { return m_visionRange; }
			set { if (m_visionRange == value) return; m_visionRange = value; NotifyInt(PropertyID.VisionRange, value); m_visionMap = null; }
		}

		[SaveGameProperty("FoodFullness")]
		int m_foodFullness;
		public int FoodFullness
		{
			get { return m_foodFullness; }
			set { if (m_foodFullness == value) return; m_foodFullness = value; NotifyInt(PropertyID.FoodFullness, value); }
		}

		[SaveGameProperty("WaterFullness")]
		int m_waterFullness;
		public int WaterFullness
		{
			get { return m_waterFullness; }
			set { if (m_waterFullness == value) return; m_waterFullness = value; NotifyInt(PropertyID.WaterFullness, value); }
		}

		// String representation of assignment, for client use
		[SaveGameProperty("Assignment")]
		string m_assignment;
		public string Assignment
		{
			get { return m_assignment; }
			set { if (m_assignment == value) return; m_assignment = value; NotifyString(PropertyID.Assignment, value); }
		}

		[SaveGameProperty("Gender")]
		LivingGender m_gender;
		public LivingGender Gender
		{
			get { return m_gender; }
			set { if (m_gender == value) return; m_gender = value; NotifyValue(PropertyID.Gender, value); }
		}

		public byte GetSkillLevel(SkillID skill)
		{
			byte skillValue;
			if (m_skillMap.TryGetValue(skill, out skillValue))
				return skillValue;
			return 0;
		}

		public void SetSkillLevel(SkillID skill, byte level)
		{
			byte oldLevel = GetSkillLevel(skill);

			if (level == 0)
				m_skillMap.Remove(skill);
			else
				m_skillMap[skill] = level;

			if (level != oldLevel)
				this.World.AddChange(new SkillChange(this, skill, level));
		}

		public void WearArmor(ItemObject wearable)
		{
			Debug.Assert(wearable.IsArmor);
			Debug.Assert(this.Inventory.Contains(wearable));
			Debug.Assert(wearable.ArmorInfo != null);
			Debug.Assert(!m_armorSlots.ContainsKey(wearable.ArmorInfo.Slot));

			wearable.Wearer = this;
			m_armorSlots[wearable.ArmorInfo.Slot] = wearable;

			this.World.AddChange(new WearArmorChange(this, wearable.ArmorInfo.Slot, wearable));
		}

		public void RemoveArmor(ItemObject wearable)
		{
			Debug.Assert(wearable.IsArmor);
			Debug.Assert(m_armorSlots.ContainsKey(wearable.ArmorInfo.Slot));
			Debug.Assert(m_armorSlots[wearable.ArmorInfo.Slot] == wearable);
			Debug.Assert(wearable.Wearer == this);

			wearable.Wearer = null;
			m_armorSlots.Remove(wearable.ArmorInfo.Slot);

			this.World.AddChange(new RemoveArmorChange(this, wearable.ArmorInfo.Slot));
		}

		public void WieldWeapon(ItemObject weapon)
		{
			Debug.Assert(weapon.IsWeapon);
			Debug.Assert(this.Inventory.Contains(weapon));
			Debug.Assert(weapon.WeaponInfo != null);
			Debug.Assert(this.Weapon == null);

			this.Weapon = weapon;
			weapon.Wielder = this;

			this.World.AddChange(new WieldWeaponChange(this, weapon));
		}

		public void RemoveWeapon(ItemObject weapon)
		{
			Debug.Assert(weapon.IsWeapon);
			Debug.Assert(this.Weapon == weapon);
			Debug.Assert(weapon.Wielder == this);

			this.Weapon.Wielder = null;
			this.Weapon = null;

			this.World.AddChange(new RemoveWeaponChange(this));
		}

		[SaveGameProperty("CarriedItem")]
		ItemObject m_carriedItem;
		public ItemObject CarriedItem
		{
			get { return m_carriedItem; }
			set { m_carriedItem = value; }
		}

		protected override void CollectObjectData(BaseGameObjectData baseData, ObjectVisibility visibility)
		{
			base.CollectObjectData(baseData, visibility);

			var data = (LivingData)baseData;

			data.LivingID = this.LivingID;

			if ((visibility & ObjectVisibility.Private) != 0)
			{
				data.CurrentAction = this.CurrentAction;
				data.ActionPriority = this.ActionPriority;
				data.ActionTicksUsed = this.ActionTicksUsed;
				data.ActionTotalTicks = this.ActionTotalTicks;
				data.ActionUserID = this.ActionUserID;

				data.Skills = m_skillMap.Select(kvp => new Tuple<SkillID, byte>(kvp.Key, kvp.Value)).ToArray();
			}

			data.ArmorSlots = m_armorSlots.Select(kvp => new Tuple<ArmorSlot, ObjectID>(kvp.Key, kvp.Value.ObjectID)).ToArray();
			data.WeaponID = this.Weapon != null ? this.Weapon.ObjectID : ObjectID.NullObjectID;
		}

		public override void SendTo(IPlayer player, ObjectVisibility visibility)
		{
			Debug.Assert(visibility != ObjectVisibility.None);

			var data = new LivingData();

			CollectObjectData(data, visibility);

			player.Send(new Messages.ObjectDataMessage(data));

			base.SendTo(player, visibility);
		}

		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();

			props[PropertyID.HitPoints] = m_hitPoints;
			props[PropertyID.MaxHitPoints] = m_maxHitPoints;
			props[PropertyID.SpellPoints] = m_spellPoints;
			props[PropertyID.MaxSpellPoints] = m_maxSpellPoints;
			props[PropertyID.Strength] = m_strength;
			props[PropertyID.Dexterity] = m_dexterity;
			props[PropertyID.Constitution] = m_constitution;
			props[PropertyID.Intelligence] = m_intelligence;
			props[PropertyID.Wisdom] = m_wisdom;
			props[PropertyID.Charisma] = m_charisma;
			props[PropertyID.Size] = m_size;
			props[PropertyID.ArmorClass] = m_armorClass;
			props[PropertyID.VisionRange] = m_visionRange;
			props[PropertyID.FoodFullness] = m_foodFullness;
			props[PropertyID.WaterFullness] = m_waterFullness;
			props[PropertyID.Assignment] = m_assignment;
			props[PropertyID.Gender] = m_gender;

			return props;
		}

		public IPlayer Controller { get; set; }

		public void SetAI(Dwarrowdelf.AI.IAI ai)
		{
			m_ai = ai;

			var aai = m_ai as Dwarrowdelf.AI.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged += OnAIAssignmentChanged;
		}

		public Grid2D<bool> VisionMap
		{
			get
			{
				Debug.Assert(World.LivingVisionMode == LivingVisionMode.LOS);
				UpdateLOS();
				return m_visionMap;
			}
		}

		public void ReceiveDamage(LivingObject attacker, DamageCategory cat, int damage)
		{
			this.HitPoints -= damage;

			if (this.HitPoints <= 0)
				Die();
		}

		protected override void OnChildRemoved(MovableObject child)
		{
			var item = child as ItemObject;

			if (item != null)
			{
				// If the armor/weapon is forcibly moved with MoveToLow, we handle it here.
				if (item.IsArmor && item.IsWorn)
					RemoveArmor(item);
				else if (item.IsWeapon && item.IsWielded)
					RemoveWeapon(item);
			}

			base.OnChildRemoved(child);
		}

		public void Die()
		{
			Trace.TraceInformation("{0} dies", this);

			this.World.AddReport(new DeathReport(this));

			var builder = new ItemObjectBuilder(ItemID.Corpse, this.MaterialID);
			builder.Name = this.Name ?? this.LivingInfo.Name;
			var corpse = builder.Create(this.World);
			bool ok = corpse.MoveTo(this.Environment, this.Location);
			if (!ok)
				Trace.TraceWarning("Failed to move corpse");

			// make a copy, as the collection will be modified
			foreach (ItemObject item in this.Inventory.ToList())
			{
				if (item.IsArmor && item.IsWorn)
					RemoveArmor(item);
				else if (item.IsWeapon && item.IsWielded)
					RemoveWeapon(item);

				ok = item.MoveTo(this.Environment, this.Location);
				if (!ok)
					throw new Exception();
			}

			this.Destruct();
		}

		void SendReport(ActionReport report)
		{
			this.World.AddReport(report);
		}

		void SendFailReport(ActionReport report, string message)
		{
			report.SetFail(message);
			this.World.AddReport(report);
		}

		// called during tick processing. the world state is not quite valid.
		public void ProcessAction()
		{
			Debug.Assert(this.World.IsWritable);

			GameAction action = this.CurrentAction;
			// if action was cancelled just now, the actor misses the turn
			if (action == null)
			{
				D("PerformAction: skipping");
				return;
			}

			D("PerformAction: {0}", action);

			bool ok = true;

			int totalTicks = GetActionTotalTicks(action);

			if (totalTicks == -1)
				ok = false;
			else
				this.ActionTotalTicks = totalTicks;

			this.ActionTicksUsed++;

			if (ok && this.ActionTicksUsed < this.ActionTotalTicks)
			{
				HandleActionProgress();
			}
			else
			{
				if (ok)
					ok = PerformAction(action);

				if (ok)
				{
					D("Action Done({0})", this.CurrentAction);

					HandleActionDone(ActionState.Done);
				}
				else
				{
					D("Action Failed({0})", this.CurrentAction);

					HandleActionDone(ActionState.Fail);
				}
			}
		}

		void HandleActionProgress()
		{
			D("ActionProgress({0}, {1}/{2})", this.CurrentAction, this.ActionTicksUsed, this.ActionTotalTicks);

			var e = new ActionProgressEvent()
			{
				MagicNumber = this.CurrentAction.MagicNumber,
				UserID = this.ActionUserID,
				TicksUsed = this.ActionTicksUsed,
				TotalTicks = this.ActionTotalTicks,
			};

			if (m_ai != null)
				m_ai.ActionProgress(e);

			var c = new ActionProgressChange(this)
			{
				ActionProgressEvent = e,
			};

			this.World.AddChange(c);
		}

		void HandleActionDone(ActionState state)
		{
			var e = new ActionDoneEvent()
			{
				MagicNumber = this.CurrentAction.MagicNumber,
				UserID = this.ActionUserID,
				State = state,
				Action = this.CurrentAction,
			};

			if (m_ai != null)
				m_ai.ActionDone(e);

			var c = new ActionDoneChange(this)
			{
				ActionDoneEvent = e,
			};

			this.CurrentAction = null;
			this.ActionPriority = Dwarrowdelf.ActionPriority.Undefined;
			this.ActionTotalTicks = this.ActionTicksUsed = 0;
			this.ActionUserID = 0;

			this.World.AddChange(c);
		}

		// Actor stuff
		[SaveGameProperty]
		public GameAction CurrentAction { get; private set; }
		public bool HasAction { get { return this.CurrentAction != null; } }

		[SaveGameProperty]
		public int ActionTotalTicks { get; private set; }
		[SaveGameProperty]
		public int ActionTicksUsed { get; private set; }

		[SaveGameProperty]
		public ActionPriority ActionPriority { get; private set; }

		[SaveGameProperty]
		public int ActionUserID { get; private set; }

		public void StartAction(GameAction action, ActionPriority priority)
		{
			StartAction(action, priority, 0);
		}

		public void StartAction(GameAction action, ActionPriority priority, int userID)
		{
			D("DoAction: {0}, uid: {1}", action, userID);

			Debug.Assert(!this.HasAction);
			Debug.Assert(priority != ActionPriority.Undefined);
			Debug.Assert(action.MagicNumber != 0);

			this.CurrentAction = action;
			this.ActionPriority = priority;
			this.ActionTotalTicks = 0;
			this.ActionTicksUsed = 0;
			this.ActionUserID = userID;

			var e = new ActionStartEvent()
			{
				Action = action,
				Priority = priority,
				UserID = userID,
			};

			if (m_ai != null)
				m_ai.ActionStarted(e);

			var c = new ActionStartedChange(this)
			{
				ActionStartEvent = e,
			};

			this.World.AddChange(c);
		}

		public void CancelAction()
		{
			if (!this.HasAction)
				throw new Exception();

			D("CancelAction({0}, uid: {1})", this.CurrentAction, this.ActionUserID);
			HandleActionDone(ActionState.Abort);
		}

		public void TurnStarted()
		{
			if (m_ai != null)
				DecideAction(ActionPriority.High);
		}

		public void TurnPreRun()
		{
			if (m_ai != null)
				DecideAction(ActionPriority.Idle);
		}

		void DecideAction(ActionPriority priority)
		{
			var action = m_ai.DecideAction(priority);

			if (action != this.CurrentAction)
			{
				if (this.HasAction)
				{
					if (action != null && this.ActionPriority > priority)
						throw new Exception();

					CancelAction();
				}

				if (action != null)
					StartAction(action, priority);
			}
		}

		void OnAIAssignmentChanged(Jobs.IAssignment assignment)
		{
			if (assignment != null)
				this.Assignment = assignment.GetType().Name;
			else
				this.Assignment = null;
		}

		protected override void OnParentChanged()
		{
			m_losMapVersion = 0;
		}

		void UpdateLOS()
		{
			if (this.Environment == null)
				return;

			if (World.LivingVisionMode != LivingVisionMode.LOS)
				throw new Exception();

			if (m_losLocation == this.Location &&
				m_losMapVersion == this.Environment.Version &&
				m_visionMap != null)
				return;

			if (m_visionMap == null)
			{
				m_visionMap = new Grid2D<bool>(this.VisionRange * 2 + 1, this.VisionRange * 2 + 1,
					this.VisionRange, this.VisionRange);
				m_losMapVersion = 0;
			}

			int z = this.Z;
			var env = this.Environment;
			ShadowCastRecursive.Calculate(new IntPoint2(this.Location.X, this.Location.Y), this.VisionRange, m_visionMap, env.Size.Plane,
				p2 => !env.GetTileData(new IntPoint3(p2, z)).IsSeeThrough);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		// does this living see location l in env
		public bool Sees(EnvironmentObject env, IntPoint3 l)
		{
			if (env != this.Environment)
				return false;

			IntVector3 dl = l - this.Location;

			// XXX livings don't currently see up or down
			if (dl.Z != 0)
				return false;

			// is the target outside range?
			if (Math.Abs(dl.X) > this.VisionRange || Math.Abs(dl.Y) > this.VisionRange)
				return false;

			switch (World.LivingVisionMode)
			{
				case LivingVisionMode.SquareFOV:
					return true;

				case LivingVisionMode.LOS:
					if (this.VisionMap[new IntPoint2(dl.X, dl.Y)] == false)
						return false;

					return true;

				default:
					throw new Exception();
			}
		}

		IEnumerable<IntPoint2> GetVisibleLocationsSimpleFOV()
		{
			var bounds2D = this.Environment.Size.Plane;

			for (int y = this.Y - this.VisionRange; y <= this.Y + this.VisionRange; ++y)
			{
				for (int x = this.X - this.VisionRange; x <= this.X + this.VisionRange; ++x)
				{
					IntPoint2 loc = new IntPoint2(x, y);
					if (!bounds2D.Contains(loc))
						continue;

					yield return loc;
				}
			}
		}

		IEnumerable<IntPoint2> GetVisibleLocationsLOS()
		{
			return this.VisionMap.GetIndexValueEnumerable().
					Where(kvp => kvp.Value == true).
					Select(kvp => kvp.Key + new IntVector2(this.X, this.Y));
		}

		public IEnumerable<IntPoint2> GetVisibleLocations()
		{
			switch (World.LivingVisionMode)
			{
				case LivingVisionMode.LOS:
					return GetVisibleLocationsLOS();

				case LivingVisionMode.SquareFOV:
					return GetVisibleLocationsSimpleFOV();

				default:
					throw new Exception();
			}
		}

		public override string ToString()
		{
			string name;

			if (this.IsDestructed)
				name = "<DestructedObject>";
			else if (this.Name != null)
				name = this.Name;
			else
				name = this.LivingInfo.Name;

			return String.Format("{0} ({1})", name, this.ObjectID);
		}
	}


	public sealed class LivingObjectBuilder : ConcreteObjectBuilder
	{
		public LivingID LivingID { get; set; }
		public int VisionRange { get; set; }
		public int FoodFullness { get; set; }
		public int WaterFullness { get; set; }
		public Dictionary<SkillID, byte> SkillMap { get; private set; }
		public LivingGender Gender { get; set; }
		public int HitPoints;
		public int AC;

		public int Str, Dex, Con, Int, Wis, Cha, Siz;

		public LivingObjectBuilder(LivingID livingID)
		{
			this.LivingID = livingID;

			var li = Dwarrowdelf.Livings.GetLivingInfo(this.LivingID);

			this.Color = li.Color;
			this.HitPoints = li.Level * 10;
			this.AC = li.Level;

			Str = Dex = Con = Int = Wis = Cha = li.Level * 10;
			Siz = li.Size;

			this.MaterialID = Dwarrowdelf.MaterialID.Flesh;
			this.VisionRange = 10;
			this.FoodFullness = 500;
			this.WaterFullness = 500;

			this.SkillMap = new Dictionary<SkillID, byte>();
		}

		public LivingObject Create(World world)
		{
			return LivingObject.Create(world, this);
		}

		public void SetSkillLevel(SkillID skill, byte level)
		{
			this.SkillMap[skill] = level;
		}
	}
}
