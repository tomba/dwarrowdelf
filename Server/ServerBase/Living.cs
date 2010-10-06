using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	public partial class Living : ServerGameObject, ILiving
	{
		[System.Diagnostics.Conditional("DEBUG")]
		void D(string format, params object[] args)
		{
			//Debug.Print("[{0}]: {1}", this, String.Format(format, args));
		}

		static ILOSAlgo s_losAlgo = new LOSShadowCast1(); // XXX note: not re-entrant

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;
		Jobs.IAI m_ai;

		static readonly PropertyDefinition HitPointsProperty = RegisterProperty(typeof(Living), PropertyID.HitPoints, PropertyVisibility.Friendly, 1);
		static readonly PropertyDefinition SpellPointsProperty = RegisterProperty(typeof(Living), PropertyID.SpellPoints, PropertyVisibility.Friendly, 1);

		static readonly PropertyDefinition StrengthProperty = RegisterProperty(typeof(Living), PropertyID.Strength, PropertyVisibility.Friendly, 1);
		static readonly PropertyDefinition DexterityProperty = RegisterProperty(typeof(Living), PropertyID.Dexterity, PropertyVisibility.Friendly, 1);
		static readonly PropertyDefinition ConstitutionProperty = RegisterProperty(typeof(Living), PropertyID.Constitution, PropertyVisibility.Friendly, 1);
		static readonly PropertyDefinition IntelligenceProperty = RegisterProperty(typeof(Living), PropertyID.Intelligence, PropertyVisibility.Friendly, 1);
		static readonly PropertyDefinition WisdomProperty = RegisterProperty(typeof(Living), PropertyID.Wisdom, PropertyVisibility.Friendly, 1);
		static readonly PropertyDefinition CharismaProperty = RegisterProperty(typeof(Living), PropertyID.Charisma, PropertyVisibility.Friendly, 1);

		static readonly PropertyDefinition VisionRangeProperty = RegisterProperty(typeof(Living), PropertyID.VisionRange, PropertyVisibility.Friendly, 10, VisionRangeChanged);

		static readonly PropertyDefinition FoodFullnessProperty = RegisterProperty(typeof(Living), PropertyID.FoodFullness, PropertyVisibility.Friendly, 255);
		static readonly PropertyDefinition WaterFullnessProperty = RegisterProperty(typeof(Living), PropertyID.WaterFullness, PropertyVisibility.Friendly, 255);

		static readonly PropertyDefinition AssignmentProperty = RegisterProperty(typeof(Living), PropertyID.Assignment, PropertyVisibility.Friendly, "");

		static void VisionRangeChanged(PropertyDefinition property, object ob, object oldValue, object newValue)
		{
			Living l = (Living)ob;
			l.m_visionMap = null;
		}

		public Living(string name)
		{
			this.Name = name;
		}

		public override void Initialize(World world)
		{
			base.Initialize(world);
			world.AddLiving(this);
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
			this.World.RemoveLiving(this);
			base.Destruct();
		}

		public int HitPoints
		{
			get { return (int)GetValue(HitPointsProperty); }
			set { SetValue(HitPointsProperty, value); }
		}

		public int SpellPoints
		{
			get { return (int)GetValue(SpellPointsProperty); }
			set { SetValue(SpellPointsProperty, value); }
		}

		public int Strength
		{
			get { return (int)GetValue(StrengthProperty); }
			set { SetValue(StrengthProperty, value); }
		}

		public int Dexterity
		{
			get { return (int)GetValue(DexterityProperty); }
			set { SetValue(DexterityProperty, value); }
		}

		public int Constitution
		{
			get { return (int)GetValue(ConstitutionProperty); }
			set { SetValue(ConstitutionProperty, value); }
		}

		public int Intelligence
		{
			get { return (int)GetValue(IntelligenceProperty); }
			set { SetValue(IntelligenceProperty, value); }
		}

		public int Wisdom
		{
			get { return (int)GetValue(WisdomProperty); }
			set { SetValue(WisdomProperty, value); }
		}

		public int Charisma
		{
			get { return (int)GetValue(CharismaProperty); }
			set { SetValue(CharismaProperty, value); }
		}

		public int VisionRange
		{
			get { return (int)GetValue(VisionRangeProperty); }
			set { SetValue(VisionRangeProperty, value); }
		}

		public int FoodFullness
		{
			get { return (int)GetValue(FoodFullnessProperty); }
			set { SetValue(FoodFullnessProperty, value); }
		}

		public int WaterFullness
		{
			get { return (int)GetValue(WaterFullnessProperty); }
			set { SetValue(WaterFullnessProperty, value); }
		}

		public string Assignment
		{
			get { return (string)GetValue(AssignmentProperty); }
			set { SetValue(AssignmentProperty, value); }
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

			bool success = false;
			bool done = false;

			if (this.Parent != null)
			{
				var handled = this.Parent.HandleChildAction(this, action);
				if (handled)
				{
					done = true;
					success = true;
				}
			}

			if (!done)
			{
				Perform(action, out success);
			}

			ActionState state;

			if (success)
				state = this.ActionTicksLeft > 0 ? ActionState.Ok : ActionState.Done;
			else
				state = ActionState.Fail;

			if (success == false)
				this.ActionTicksLeft = 0;

			var e = new ActionProgressChange(this)
				{
					ActionXXX = action,
					UserID = this.ActionUserID,
					TicksLeft = this.ActionTicksLeft,
					State = state,
				};

			this.ActionProgress(e);

			this.World.AddChange(e);

			// is the action originator an user?
			//if (e.UserID != 0)
			//	this.World.SendEvent(this, e);
		}


		// Actor stuff
		public GameAction CurrentAction { get; private set; }
		public bool HasAction { get { return this.CurrentAction != null; } }

		public int ActionTicksLeft { get; private set; }
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

			int ticks;

			InitializeAction(action, out ticks);

			var c = new ActionStartedChange(this)
			{
				Action = action,
				UserID = userID,
				TicksLeft = ticks,
			};

			HandleActionStarted(c);

			this.World.AddChange(c);

			this.World.SignalWorld();
		}

		public void CancelAction()
		{
			if (!this.HasAction)
				throw new Exception();

			D("CancelAction({0}, uid: {1})", this.CurrentAction, this.ActionUserID);

			var action = this.CurrentAction;

			var e = new ActionProgressChange(this)
			{
				ActionXXX = action,
				UserID = this.ActionUserID,
				TicksLeft = 0,
				State = ActionState.Abort,
			};

			this.ActionProgress(e);

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
			if (action != null)
			{
				if (this.HasAction)
				{
					if (this.CurrentAction.Priority <= action.Priority)
						CancelAction();
					else
						throw new Exception();
				}

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
			if (!this.HasAction)
				throw new Exception();

			var action = this.CurrentAction;

			this.ActionTicksLeft = e.TicksLeft;

			D("ActionProgress({0}, {1})", action, e.State);

			if (m_ai != null)
				m_ai.ActionProgress(e);

			if (e.TicksLeft == 0)
			{
				D("ActionDone({0})", action);
				this.CurrentAction = null;
				this.ActionTicksLeft = 0;
				this.ActionUserID = 0;
			}
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
				l => !env.IsWalkable(new IntPoint3D(l, z)));

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
			else
				return GetVisibleLocationsSimpleFOV();
		}

		public override BaseGameObjectData Serialize()
		{
			var data = new LivingData();
			data.ObjectID = this.ObjectID;
			data.Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
			data.Properties = base.SerializeProperties();
			return data;
		}

		public override void SerializeTo(Action<Messages.ServerMessage> writer)
		{
			var msg = new Messages.ObjectDataMessage() { ObjectData = Serialize() };
			writer(msg);
		}

		public void SerializeInventoryTo(Action<Messages.ServerMessage> writer)
		{
			foreach (var item in this.Inventory)
				item.SerializeTo(writer);
		}

		public override string ToString()
		{
			return String.Format("Living({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
