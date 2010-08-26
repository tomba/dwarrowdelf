using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.Server
{
	public class Living : ServerGameObject
	{
		static ILOSAlgo s_losAlgo = new LOSShadowCast1(); // XXX note: not re-entrant

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;

		IActor m_actorImpl;

		static readonly PropertyDefinition HitPointsProperty = RegisterProperty(typeof(Living), PropertyID.HitPoints, PropertyVisibility.Friendly, 0);
		static readonly PropertyDefinition SpellPointsProperty = RegisterProperty(typeof(Living), PropertyID.SpellPoints, PropertyVisibility.Friendly, 0);

		static readonly PropertyDefinition StrengthProperty = RegisterProperty(typeof(Living), PropertyID.Strength, PropertyVisibility.Friendly, 0);
		static readonly PropertyDefinition DexterityProperty = RegisterProperty(typeof(Living), PropertyID.Dexterity, PropertyVisibility.Friendly, 0);
		static readonly PropertyDefinition ConstitutionProperty = RegisterProperty(typeof(Living), PropertyID.Constitution, PropertyVisibility.Friendly, 0);
		static readonly PropertyDefinition IntelligenceProperty = RegisterProperty(typeof(Living), PropertyID.Intelligence, PropertyVisibility.Friendly, 0);
		static readonly PropertyDefinition WisdomProperty = RegisterProperty(typeof(Living), PropertyID.Wisdom, PropertyVisibility.Friendly, 0);
		static readonly PropertyDefinition CharismaProperty = RegisterProperty(typeof(Living), PropertyID.Charisma, PropertyVisibility.Friendly, 0);

		static readonly PropertyDefinition VisionRangeProperty = RegisterProperty(typeof(Living), PropertyID.VisionRange, PropertyVisibility.Friendly, 10, VisionRangeChanged);

		static readonly PropertyDefinition FoodFullnessProperty = RegisterProperty(typeof(Living), PropertyID.FoodFullness, PropertyVisibility.Friendly, 255);
		static readonly PropertyDefinition WaterFullnessProperty = RegisterProperty(typeof(Living), PropertyID.WaterFullness, PropertyVisibility.Friendly, 255);

		static void VisionRangeChanged(PropertyDefinition property, object ob, object oldValue, object newValue)
		{
			Living l = (Living)ob;
			l.m_visionMap = null;
		}

		public Living(World world, string name)
			: base(world)
		{
			this.Name = name;
			world.AddLiving(this);
		}

		public void Cleanup()
		{
			this.Actor = null;
			World.RemoveLiving(this);
			this.Destruct();
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

		public IActor Actor
		{
			get { return m_actorImpl; }
			set { m_actorImpl = value; }
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

		void PerformGet(GetAction action, out bool success)
		{
			success = false;

			if (this.Environment == null)
				return;

			if (action.TicksLeft > 0)
			{
				success = true;
				return;
			}

			var list = this.Environment.GetContents(this.Location);
			if (list == null)
				return;

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var item = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (item == null)
					throw new Exception();

				if (item.MoveTo(this) == false)
					throw new Exception();
			}

			success = true;
		}

		void PerformDrop(DropAction action, out bool success)
		{
			success = false;

			if (this.Environment == null)
				return;

			if (action.TicksLeft > 0)
			{
				success = true;
				return;
			}

			var list = this.Inventory;
			if (list == null)
				throw new Exception();

			foreach (var itemID in action.ItemObjectIDs)
			{
				if (itemID == this.ObjectID)
					throw new Exception();

				var ob = list.FirstOrDefault(o => o.ObjectID == itemID);
				if (ob == null)
					throw new Exception();

				if (ob.MoveTo(this.Environment, this.Location) == false)
					throw new Exception();
			}

			success = true;
		}

		void PerformMove(MoveAction action, out bool success)
		{
			// this should check if movement is blocked, even when TicksLeft > 0
			if (action.TicksLeft == 0)
				success = MoveDir(action.Direction);
			else
				success = true;
		}

		void PerformMine(MineAction action, out bool success)
		{
			IntPoint3D p = this.Location + new IntVector3D(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id == InteriorID.Wall)
			{
				if (action.TicksLeft == 0)
					this.Environment.SetInteriorID(p, InteriorID.Empty);
				success = true;
			}
			else
			{
				success = false;
			}
		}

		void PerformWait(WaitAction action, out bool success)
		{
			success = true;
		}

		void PerformBuildItem(BuildItemAction action, out bool success)
		{
			var building = this.Environment.GetBuildingAt(this.Location);

			if (building == null)
			{
				success = false;
				return;
			}

			if (action.TicksLeft != 0)
			{
				success = building.VerifyBuildItem(this, action.SourceObjectIDs);
			}
			else
			{
				success = building.PerformBuildItem(this, action.SourceObjectIDs);
			}
		}

		// called during tick processing. the world state is not quite valid.
		public void PerformAction()
		{
			Debug.Assert(this.World.IsWritable);

			GameAction action = GetAction();
			// if action was cancelled just now, the actor misses the turn
			if (action == null)
			{
				MyDebug.WriteLine("PerformAction {0} : skipping", this);
				return;
			}

			Debug.Assert(action.ActorObjectID == this.ObjectID);

			// new action?
			if (action.TicksLeft == 0)
			{
				// The action should be initialized somewhere
				if (action is WaitAction)
				{
					action.TicksLeft = ((WaitAction)action).WaitTicks;
				}
				else if (action is MineAction)
				{
					action.TicksLeft = 3;
				}
				else if (action is MoveAction)
				{
					action.TicksLeft = 1;
				}
				else if (action is BuildItemAction)
				{
					action.TicksLeft = 8;
				}
				else
				{
					action.TicksLeft = 1;
				}
			}

			MyDebug.WriteLine("PerformAction {0} : {1}", this, action);

			action.TicksLeft -= 1;

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
				if (action is NopAction)
				{
					success = true;
				}
				else if (action is MoveAction)
				{
					PerformMove((MoveAction)action, out success);
				}
				else if (action is WaitAction)
				{
					PerformWait((WaitAction)action, out success);
				}
				else if (action is GetAction)
				{
					PerformGet((GetAction)action, out success);
				}
				else if (action is DropAction)
				{
					PerformDrop((DropAction)action, out success);
				}
				else if (action is MineAction)
				{
					PerformMine((MineAction)action, out success);
				}
				else if (action is BuildItemAction)
				{
					PerformBuildItem((BuildItemAction)action, out success);
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (success == false)
				action.TicksLeft = 0;

			if (action.TicksLeft == 0)
				CancelAction();

			// is the action originator an user?
			if (action.UserID != 0)
			{
				this.World.AddEvent(new ActionProgressEvent()
				{
					UserID = action.UserID,
					TransactionID = action.TransactionID,
					TicksLeft = action.TicksLeft,
					Success = success,
				});
			}
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
		public bool Sees(IIdentifiable ob, IntPoint3D l)
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


		// Actor stuff
		GameAction m_action;

		public void SetAction(GameAction action)
		{
			if (m_action != null)
				throw new Exception();

			action.ActorObjectID = this.ObjectID;
			m_action = action;
			this.World.SignalWorld();
		}

		public void CancelAction()
		{
			m_action = null;
		}

		public GameAction GetAction()
		{
			return m_action;
		}

		public bool HasAction
		{
			get
			{
				return m_action != null;
			}
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
			var msg = new Messages.ObjectDataMessage() { Object = Serialize() };
			writer(msg);
		}

		public override string ToString()
		{
			return String.Format("Living({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
