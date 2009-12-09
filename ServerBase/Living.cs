using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame
{
	public class Living : ServerGameObject
	{
		// XXX note: not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		int m_visionRange = 10;
		Grid2D<bool> m_visionMap;

		IActor m_actorImpl;

		public GameColor Color { get; set; }
		public MaterialID MaterialID { get; set; }

		public Living(World world)
			: base(world)
		{
			world.AddLiving(this);
		}

		public void Cleanup()
		{
			this.MoveTo(null, new IntPoint3D());
			World.RemoveLiving(this);
		}

		public IActor Actor
		{
			get { return m_actorImpl; }
			set { m_actorImpl = value; }
		}

		public int VisionRange
		{
			get { return m_visionRange; }
			set { m_visionRange = value; m_visionMap = null; }
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
			IntPoint3D p = this.Location + IntVector3D.FromDirection(action.Direction);

			var id = this.Environment.GetInteriorID(p);

			if (id == InteriorID.NaturalWall)
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

		// called during tick processing. the world state is not quite valid.
		public void PerformAction()
		{
			Debug.Assert(this.World.IsWriteable);

			GameAction action = GetCurrentAction();
			// if action was cancelled just now, the actor misses the turn
			if (action == null)
				return;

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
					action.TicksLeft = 4;
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
				if (action is MoveAction)
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
				else
				{
					throw new NotImplementedException();
				}
			}

			if (success == false)
				action.TicksLeft = 0;

			if (action.TicksLeft == 0)
				RemoveAction(action);

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

			if (m_losLocation == this.Location &&
				m_losMapVersion == this.Environment.Version)
				return;

			if (this.Environment.VisibilityMode != VisibilityMode.LOS)
				throw new Exception();

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


		// pass changes that this living sees
		public bool ChangeFilter(Change change)
		{
			if (change is ObjectMoveChange)
			{
				ObjectMoveChange ec = (ObjectMoveChange)change;

				if (Sees(ec.Source, ec.SourceLocation))
					return true;

				if (Sees(ec.Destination, ec.DestinationLocation))
					return true;

				return false;
			}
			else if (change is MapChange)
			{
				MapChange mc = (MapChange)change;

				return Sees(mc.Map, mc.Location);
			}

			throw new Exception();
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


		public ClientMsgs.Message SerializeInventory()
		{
			var items = this.Inventory.Select(o => o.Serialize()).ToArray();
			return new ClientMsgs.CompoundMessage() { Messages = items };
		}

		// Actor stuff
		Queue<GameAction> m_actionQueue = new Queue<GameAction>();

		public void EnqueueAction(GameAction action)
		{
			action.ActorObjectID = this.ObjectID;

			lock (m_actionQueue)
				m_actionQueue.Enqueue(action);

			this.World.SignalWorld();
		}

		public void RemoveAction(GameAction action)
		{
			lock (m_actionQueue)
			{
				GameAction topAction = m_actionQueue.Peek();

				if (topAction == action)
					m_actionQueue.Dequeue();
			}
		}

		public GameAction GetCurrentAction()
		{
			lock (m_actionQueue)
			{
				if (m_actionQueue.Count == 0)
					return null;

				return m_actionQueue.Peek();
			}
		}

		public bool HasAction
		{
			get
			{
				lock (m_actionQueue)
					return m_actionQueue.Count > 0;
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

		public override ClientMsgs.Message Serialize()
		{
			var data = new ClientMsgs.LivingData();
			data.ObjectID = this.ObjectID;
			data.Name = this.Name;
			data.SymbolID = this.SymbolID;
			data.Environment = this.Parent != null ? this.Parent.ObjectID : ObjectID.NullObjectID;
			data.Location = this.Location;
			data.Color = this.Color;
			data.VisionRange = this.VisionRange;
			return data;
		}

		public override string ToString()
		{
			return String.Format("Living({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
