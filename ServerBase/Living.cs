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

//			World.AddChange(new ObjectMoveChange(this, this.Environment.ObjectID, this.Location,
//				ObjectID.NullObjectID, new IntPoint()));
		}

		public IClientCallback ClientCallback { get; set; }

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
				if(this.ClientCallback != null)
					this.ClientCallback.TransactionDone(action.TransactionID);
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
			s_losAlgo.Calculate(this.Location2D, this.VisionRange, m_visionMap, this.Environment.Bounds2D,
				l => !this.Environment.IsWalkable(new IntPoint3D(l, z)));

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		// XXX move to somewhere generic
		public static ClientMsgs.Message ChangeToMessage(Change change)
		{
			if (change is ObjectMoveChange)
			{
				ObjectMoveChange mc = (ObjectMoveChange)change;
				return new ClientMsgs.ObjectMove(mc.Object, mc.SourceMapID, mc.SourceLocation,
					mc.DestinationMapID, mc.DestinationLocation);
			}

			if (change is MapChange)
			{
				MapChange mc = (MapChange)change;
				return new ClientMsgs.TerrainData()
				{
					Environment = mc.MapID,
					MapDataList = new ClientMsgs.MapTileData[] {
						new ClientMsgs.MapTileData() { Location = mc.Location, TerrainID = mc.TerrainType }
					}
				};
			}

			if (change is TurnChange)
			{
				return new ClientMsgs.TurnChange() { TurnNumber = ((TurnChange)change).TurnNumber };
			}

			Debug.Assert(false);

			return null;
		}

		// send only changes that the player sees and needs to know
		public bool ChangeFilter(Change change)
		{
			if (this.Environment.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			if (change is ObjectMoveChange)
			{
				ObjectMoveChange ec = (ObjectMoveChange)change;

				if (ec.Source == this.Environment && Sees(ec.SourceLocation))
					return true;

				if (ec.Destination == this.Environment && Sees(ec.DestinationLocation))
					return true;

				MyDebug.WriteLine("\tplr doesn't see ob moving {0}->{1}, skipping change",
					ec.SourceLocation, ec.DestinationLocation);
				return false;
			}

			if (change is MapChange)
			{
				MapChange mc = (MapChange)change;
				if (!Sees(mc.Location))
				{
					MyDebug.WriteLine("\tplr doesn't see ob at {0}, skipping change", mc.Location);
					return false;
				}
				else
				{
					return true;
				}
			}

			if (change is TurnChange)
			{
				return true;
			}

			Debug.Assert(false);

			return false;
		}

		public void SendInventory()
		{
			if (this.ClientCallback != null)
			{
				var items = new List<ClientMsgs.Message>(this.Inventory.Count);
				foreach (ItemObject item in this.Inventory)
				{
					var data = item.Serialize();
					items.Add(data);
				}

				this.ClientCallback.DeliverMessages(items);
			}
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

		public bool Sees(IntPoint3D l)
		{
			if (this.Environment.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			IntVector3D dl = l - this.Location;

			if (dl.Z != 0)
				return false;

			if (Math.Abs(dl.X) > this.VisionRange ||
				Math.Abs(dl.Y) > this.VisionRange)
			{
				return false;
			}

			if (this.Environment.VisibilityMode == VisibilityMode.SimpleFOV)
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
