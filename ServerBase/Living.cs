using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame
{
	public class Living : ServerGameObject, IActor
	{
		// XXX note: not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		int m_visionRange = 3;
		Grid2D<bool> m_visionMap;

		IActor m_actorImpl;

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

			set
			{
				if (m_actorImpl != null)
					m_actorImpl.ActionQueuedEvent -= HandleActionQueued;

				m_actorImpl = value;

				if (m_actorImpl != null)
					m_actorImpl.ActionQueuedEvent += HandleActionQueued;
			}
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

		void HandleActionQueued()
		{
			if (this.ActionQueuedEvent != null)
				this.ActionQueuedEvent();
		}

		void PerformGet(GetAction action, out bool done, out bool success)
		{
			done = true;
			success = false;

			if (this.Environment == null)
				return;

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

		void PerformDrop(DropAction action, out bool done, out bool success)
		{
			done = true;
			success = false;

			if (this.Environment == null)
				return;

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

		// called during turn processing. the world state is not quite valid.
		public void PerformAction()
		{
			Debug.Assert(this.World.IsWriteable);

			GameAction action = GetCurrentAction();
			// if action was cancelled just now, the actor misses the turn
			if (action == null)
				return;

			MyDebug.WriteLine("PerformAction {0} : {1}", Name, action);

			Debug.Assert(action.ActorObjectID == this.ObjectID);

			bool done;
			bool success;

			if (action is MoveAction)
			{
				MoveAction ma = (MoveAction)action;
				success = MoveDir(ma.Direction);
				done = true;
			}
			else if (action is WaitAction)
			{
				WaitAction wa = (WaitAction)action;
				wa.Turns--;
				success = true;
				if (wa.Turns == 0)
					done = true;
				else
					done = false;
			}
			else if (action is GetAction)
			{
				PerformGet((GetAction)action, out done, out success);
			}
			else if (action is DropAction)
			{
				PerformDrop((DropAction)action, out done, out success);
			}
			else
			{
				throw new NotImplementedException();
			}

			ReportAction(done, success);

			if (done)
			{
				RemoveAction(action);
				
				// is the action originator an user?
				if (action.UserID != 0)
				{
					this.World.AddEvent(new ActionDoneEvent()
					{
						UserID = action.UserID,
						TransactionID = action.TransactionID
					});
				}
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
		public bool Sees(GameObject ob, IntPoint3D l)
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

		#region IActor Members

		public void RemoveAction(GameAction action)
		{
			this.Actor.RemoveAction(action);
		}

		public GameAction GetCurrentAction()
		{
			return this.Actor.GetCurrentAction();
		}

		public bool HasAction
		{
			get { return this.Actor.HasAction; }
		}

		public bool IsInteractive
		{
			get { return this.Actor.IsInteractive; }
		}

		public void ReportAction(bool done, bool success)
		{
			this.Actor.ReportAction(done, success);
		}

		public event Action ActionQueuedEvent;

		#endregion


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
			return String.Format("Living({0})", this.ObjectID);
		}
	}
}
