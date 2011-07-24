using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	[SaveGameObject(UseRef = true)]
	public partial class Living : ServerGameObject, ILiving
	{
		internal static Living Create(World world, LivingBuilder builder)
		{
			var ob = new Living(builder);
			ob.Initialize(world);
			return ob;
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			//Debug.Print("[{0}]: {1}", this, String.Format(format, args));
		}

		static ILOSAlgo s_losAlgo = new LOSShadowCast1(); // XXX note: not re-entrant

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;
		[SaveGameProperty]
		Jobs.IAI m_ai;

		[SaveGameProperty("Skills")]
		Dictionary<SkillID, byte> m_skillMap;

		Living(LivingBuilder builder)
			: base(ObjectType.Living, builder)
		{
			m_visionRange = builder.VisionRange;
			m_foodFullness = builder.FoodFullness;
			m_waterFullness = builder.WaterFullness;

			m_assignment = "";

			m_skillMap = new Dictionary<SkillID, byte>();
			foreach (var kvp in builder.SkillMap)
				if (kvp.Value != 0)
					m_skillMap[kvp.Key] = kvp.Value;
		}

		Living(SaveGameContext ctx)
			: base(ctx, ObjectType.Living)
		{
			this.World.TickStarting += OnTickStart;

			var aai = m_ai as Jobs.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged += OnAIAssignmentChanged;
		}

		protected override void Initialize(World world)
		{
			base.Initialize(world);
			world.AddLiving(this);
			world.TickStarting += OnTickStart;
		}

		public override void Destruct()
		{
			var aai = m_ai as Jobs.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged -= OnAIAssignmentChanged;

			m_ai = null;
			this.CurrentAction = null;
			this.ActionTicksLeft = 0;
			this.ActionUserID = 0;

			this.World.TickStarting -= OnTickStart;
			this.World.RemoveLiving(this);
			base.Destruct();
		}

		void OnTickStart()
		{
			if (this.FoodFullness > 0)
				this.FoodFullness--;

			if (this.WaterFullness > 0)
				this.WaterFullness--;
		}

		[SaveGameProperty("HitPoints")]
		int m_hitPoints;
		public int HitPoints
		{
			get { return m_hitPoints; }
			set { if (m_hitPoints == value) return; m_hitPoints = value; NotifyInt(PropertyID.HitPoints, value); }
		}

		[SaveGameProperty("SpellPoints")]
		int m_spellPoints;
		public int SpellPoints
		{
			get { return m_spellPoints; }
			set { if (m_spellPoints == value) return; m_spellPoints = value; NotifyInt(PropertyID.SpellPoints, value); }
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
			set { if (m_assignment == value) return; m_assignment = value; NotifyObject(PropertyID.Assignment, value); }
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

		public override BaseGameObjectData Serialize()
		{
			var data = new LivingData()
			{
				ObjectID = this.ObjectID,
				Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID,
				Location = this.Location,

				CurrentAction = this.CurrentAction,
				ActionTicksLeft = this.ActionTicksLeft,
				ActionUserID = this.ActionUserID,

				Properties = SerializeProperties().Select(kvp => new Tuple<PropertyID, object>(kvp.Key, kvp.Value)).ToArray(),

				Skills = m_skillMap.Select(kvp => new Tuple<SkillID, byte>(kvp.Key, kvp.Value)).ToArray(),
			};

			return data;
		}

		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			props[PropertyID.HitPoints] = m_hitPoints;
			props[PropertyID.SpellPoints] = m_spellPoints;
			props[PropertyID.Strength] = m_strength;
			props[PropertyID.Dexterity] = m_dexterity;
			props[PropertyID.Constitution] = m_constitution;
			props[PropertyID.Intelligence] = m_intelligence;
			props[PropertyID.Wisdom] = m_wisdom;
			props[PropertyID.Charisma] = m_charisma;
			props[PropertyID.ArmorClass] = m_armorClass;
			props[PropertyID.VisionRange] = m_visionRange;
			props[PropertyID.FoodFullness] = m_foodFullness;
			props[PropertyID.WaterFullness] = m_waterFullness;
			props[PropertyID.Assignment] = m_assignment;
			return props;
		}

		public void SetAI(Jobs.IAI ai)
		{
			m_ai = ai;

			var aai = m_ai as Jobs.AssignmentAI;
			if (aai != null)
				aai.AssignmentChanged += OnAIAssignmentChanged;
		}

		public Grid2D<bool> VisionMap
		{
			get
			{
				Debug.Assert(this.Environment.VisibilityMode == VisibilityMode.LOS);
				UpdateLOS();
				return m_visionMap;
			}
		}

		void ReceiveDamage(int damage)
		{
			this.HitPoints -= damage;
			if (this.HitPoints <= 0)
			{
				Trace.TraceInformation("{0} dies", this);

				var builder = new ItemObjectBuilder(ItemID.Corpse, this.MaterialID);
				builder.Name = this.Name;
				var corpse = builder.Create(this.World);
				bool ok = corpse.MoveTo(this.Environment, this.Location);
				if (!ok)
					Trace.TraceWarning("Failed to move corpse");

				this.Destruct();
			}
		}

		// called during tick processing. the world state is not quite valid.
		public void PerformAction()
		{
			Debug.Assert(this.World.IsWritable);

			GameAction action = this.CurrentAction;
			// if action was cancelled just now, the actor misses the turn
			if (action == null)
			{
				D("PerformAction: skipping");
				return;
			}

			if (this.ActionTicksLeft == 0)
				throw new Exception();

			D("PerformAction: {0}", action);

			this.ActionTicksLeft -= 1;

			bool success = PerformAction(action);

			if (success == false)
				this.ActionTicksLeft = 0;


			if (this.ActionTicksLeft > 0)
			{
				var e = new ActionProgressChange(this)
				{
					ActionXXX = action,
					UserID = this.ActionUserID,
					TicksLeft = this.ActionTicksLeft,
				};

				this.ActionProgress(e);
				this.World.AddChange(e);
			}
			else
			{
				ActionState state = success ? ActionState.Done : ActionState.Fail;

				var e = new ActionDoneChange(this)
				{
					ActionXXX = action,
					UserID = this.ActionUserID,
					State = state,
					Error = success ? null : (m_actionError ?? "<no error str>"),
				};

				m_actionError = null;

				this.ActionDone(e);
				this.World.AddChange(e);
			}
		}

		string m_actionError;

		void SetActionError(string format, params object[] args)
		{
			SetActionError(String.Format(format, args));
		}

		void SetActionError(string error)
		{
			Trace.TraceWarning("{0} SetActionError({1})", this, error);
			m_actionError = error;
		}

		// Actor stuff
		[SaveGameProperty]
		public GameAction CurrentAction { get; private set; }
		public bool HasAction { get { return this.CurrentAction != null; } }

		[SaveGameProperty]
		public int ActionTicksLeft { get; private set; }
		[SaveGameProperty]
		public int ActionUserID { get; private set; }

		public void DoAction(GameAction action)
		{
			DoAction(action, 0);
		}

		public void DoAction(GameAction action, int userID)
		{
			D("DoAction: {0}, uid: {1}", action, userID);

			Debug.Assert(!this.HasAction);
			Debug.Assert(action.Priority != ActionPriority.Undefined);

			int ticks = InitializeAction(action);

			var c = new ActionStartedChange(this)
			{
				Action = action,
				UserID = userID,
				TicksLeft = ticks,
			};

			HandleActionStarted(c);

			this.World.AddChange(c);
		}

		public void CancelAction()
		{
			if (!this.HasAction)
				throw new Exception();

			D("CancelAction({0}, uid: {1})", this.CurrentAction, this.ActionUserID);

			var action = this.CurrentAction;

			var e = new ActionDoneChange(this)
			{
				ActionXXX = action,
				UserID = this.ActionUserID,
				State = ActionState.Abort,
			};

			this.ActionDone(e);

			this.World.AddChange(e);
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
					if (action != null && this.CurrentAction.Priority > action.Priority)
						throw new Exception();

					CancelAction();
				}

				if (action != null)
					DoAction(action);
			}
		}

		void HandleActionStarted(ActionStartedChange change)
		{
			Debug.Assert(!this.HasAction);

			this.CurrentAction = change.Action;
			this.ActionTicksLeft = change.TicksLeft;
			this.ActionUserID = change.UserID;

			if (m_ai != null)
				m_ai.ActionStarted(change);
		}

		void ActionProgress(ActionProgressChange e)
		{
			Debug.Assert(e.TicksLeft > 0);

			if (!this.HasAction)
				throw new Exception();

			var action = this.CurrentAction;

			this.ActionTicksLeft = e.TicksLeft;

			D("ActionProgress({0}, left: {1})", action, e.TicksLeft);

			if (m_ai != null)
				m_ai.ActionProgress(e);
		}

		void ActionDone(ActionDoneChange c)
		{
			if (!this.HasAction)
				throw new Exception();

			var action = this.CurrentAction;

			D("ActionDone({0}, {1})", action, c.State);

			if (m_ai != null)
				m_ai.ActionDone(c);

			this.CurrentAction = null;
			this.ActionTicksLeft = 0;
			this.ActionUserID = 0;
		}

		void OnAIAssignmentChanged(Jobs.IAssignment assignment)
		{
			if (assignment != null)
				this.Assignment = assignment.GetType().Name;
			else
				this.Assignment = null;
		}

		protected override void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv)
		{
			m_losMapVersion = 0;
		}

		void UpdateLOS()
		{
			if (this.Environment == null)
				return;

			if (this.Environment.VisibilityMode != VisibilityMode.LOS)
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
			s_losAlgo.Calculate(this.Location2D, this.VisionRange, m_visionMap, env.Bounds2D,
				l => !EnvironmentHelpers.CanEnter(env, new IntPoint3D(l, z))); // XXX

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		// does this living see location l in object ob
		public bool Sees(IGameObject ob, IntPoint3D l)
		{
			if (ob != this.Environment)
				return false;

			var env = ob as Environment;

			// if the ob is not Environment, and we're in it, we see everything there
			if (env == null)
				return true;

			if (env.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			if (env.VisibilityMode == VisibilityMode.GlobalFOV)
				return !env.GetHidden(l);

			IntVector3D dl = l - this.Location;

			if (dl.Z != 0)
				return false;

			if (Math.Abs(dl.X) > this.VisionRange ||
				Math.Abs(dl.Y) > this.VisionRange)
			{
				return false;
			}

			if (env.VisibilityMode == VisibilityMode.SimpleFOV)
				return true;

			if (this.VisionMap[new IntPoint(dl.X, dl.Y)] == false)
				return false;

			return true;
		}

		IEnumerable<IntPoint> GetVisibleLocationsSimpleFOV()
		{
			for (int y = this.Y - this.VisionRange; y <= this.Y + this.VisionRange; ++y)
			{
				for (int x = this.X - this.VisionRange; x <= this.X + this.VisionRange; ++x)
				{
					IntPoint loc = new IntPoint(x, y);
					if (!this.Environment.Bounds2D.Contains(loc))
						continue;

					yield return loc;
				}
			}
		}

		IEnumerable<IntPoint> GetVisibleLocationsLOS()
		{
			return this.VisionMap.
					Where(kvp => kvp.Value == true).
					Select(kvp => kvp.Key + new IntVector(this.X, this.Y));
		}

		public IEnumerable<IntPoint> GetVisibleLocations()
		{
			if (this.Environment.VisibilityMode == VisibilityMode.LOS)
				return GetVisibleLocationsLOS();
			else if (this.Environment.VisibilityMode == VisibilityMode.SimpleFOV)
				return GetVisibleLocationsSimpleFOV();
			else
				throw new Exception();
		}

		public override string ToString()
		{
			if (this.IsDestructed)
				return "<DestructedObject>";

			return String.Format("Living({0}/{1})", this.Name, this.ObjectID);
		}
	}


	public class LivingBuilder : ServerGameObjectBuilder
	{
		public int VisionRange { get; set; }
		public int FoodFullness { get; set; }
		public int WaterFullness { get; set; }
		public Dictionary<SkillID, byte> SkillMap { get; private set; }

		public LivingBuilder(string name)
		{
			this.Name = name;
			this.MaterialID = Dwarrowdelf.MaterialID.Flesh;
			this.VisionRange = 10;
			this.FoodFullness = 500;
			this.WaterFullness = 500;

			this.SkillMap = new Dictionary<SkillID, byte>();
		}

		public Living Create(World world)
		{
			return Living.Create(world, this);
		}

		public void SetSkillLevel(SkillID skill, byte level)
		{
			this.SkillMap[skill] = level;
		}
	}
}
