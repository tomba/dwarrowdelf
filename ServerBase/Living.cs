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

		public List<ItemObject> Inventory { get; private set; }

		uint m_losMapVersion;
		IntPoint m_losLocation;
		int m_visionRange = 3;
		LocationGrid<bool> m_visionMap;

		IActor m_actorImpl;

		public Living(World world)
			: base(world)
		{
			world.AddLiving(this);
			this.Inventory = new List<ItemObject>();
		}

		public void Cleanup()
		{
			if (this.Environment != null)
				this.Environment.RemoveObject(this, this.Location);

			World.RemoveLiving(this);

			World.AddChange(new ObjectMoveChange(this, this.Environment.ObjectID, this.Location,
				ObjectID.NullObjectID, new IntPoint()));
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

		public LocationGrid<bool> VisionMap
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

		// called during turn processing. the world state is not quite valid.
		public void PerformAction()
		{
			GameAction action = GetCurrentAction();
			// if action was cancelled just now, the actor misses the turn
			if (action == null)
				return;

			MyDebug.WriteLine("PerformAction {0} : {1}", Name, action);

			Debug.Assert(action.ObjectID == this.ObjectID);

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
			else
				throw new NotImplementedException();

			ReportAction(done, success);

			if (done)
			{
				RemoveAction(action);
				if(this.ClientCallback != null)
					this.ClientCallback.TransactionDone(action.TransactionID);
			}
		}

		// called after turn. world state is valid.
		public void ProcessChanges(Change[] changes)
		{
		}

		protected override void OnEnvironmentChanged(Environment oldEnv, Environment newEnv)
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
				m_visionMap = new LocationGrid<bool>(this.VisionRange * 2 + 1, this.VisionRange * 2 + 1,
					this.VisionRange, this.VisionRange);
				m_losMapVersion = 0;
			}

			var terrains = this.Environment.World.AreaData.Terrains;
			s_losAlgo.Calculate(this.Location, this.VisionRange, m_visionMap, this.Environment.Bounds,
				l => !this.Environment.IsWalkable(l));

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

		// XXX move to somewhere generic
		public static ClientMsgs.Message ChangeToMessage(Change change)
		{
			if (change is ObjectMoveChange)
			{
				ObjectMoveChange mc = (ObjectMoveChange)change;
				return new ClientMsgs.ObjectMove(mc.Target, mc.SourceMapID, mc.SourceLocation,
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

				if (ec.SourceMapID == this.Environment.ObjectID && Sees(ec.SourceLocation))
					return true;

				if (ec.DestinationMapID == this.Environment.ObjectID && Sees(ec.DestinationLocation))
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
				var items = new List<ClientMsgs.ItemData>(this.Inventory.Count);
				foreach (ItemObject item in this.Inventory)
				{
					var data = item.Serialize();
					items.Add(data);
				}

				var msgs = new ClientMsgs.Message[] { new ClientMsgs.ItemsData() { Items = items.ToArray() } };

				this.ClientCallback.DeliverMessages(msgs);
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

		public bool Sees(IntPoint l)
		{
			if (this.Environment.VisibilityMode == VisibilityMode.AllVisible)
				return true;

			IntVector dl = l - this.Location;

			if (Math.Abs(dl.X) > this.VisionRange ||
				Math.Abs(dl.Y) > this.VisionRange)
			{
				return false;
			}

			if (this.Environment.VisibilityMode == VisibilityMode.SimpleFOV)
				return true;

			if (this.VisionMap[l - (IntVector)this.Location] == false)
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
					if (!this.Environment.Bounds.Contains(loc))
						continue;

					yield return loc;
				}
			}
		}

		IEnumerable<IntPoint> GetVisibleLocationsLOS()
		{
			return this.VisionMap.
					Where(kvp => kvp.Value == true).
					Select(kvp => kvp.Key + (IntVector)this.Location);
		}

		public IEnumerable<IntPoint> GetVisibleLocations()
		{
			if (this.Environment.VisibilityMode == VisibilityMode.LOS)
				return GetVisibleLocationsLOS();
			else
				return GetVisibleLocationsSimpleFOV();
		}


	}
}
