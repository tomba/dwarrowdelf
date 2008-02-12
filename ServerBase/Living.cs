using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame
{
	class Living : ServerGameObject, IActor
	{
		public Living(World world)
			: base(world)
		{
			world.AddLiving(this);
		}

		public void Cleanup()
		{
			if (this.Environment != null)
				this.Environment.RemoveObject(this, this.Location);

			World.RemoveLiving(this);
		}

		public IClientCallback ClientCallback { get; set; }

		IActor m_actorImpl;

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

		public int VisionRange { get { return 3; } }
		public LocationGrid<bool> VisionMap { get; set; }
		public Location VisionLocation { get; set; }

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

			Debug.Assert(action.ObjectID == this.ObjectID);

			bool done;

			if (action is MoveAction)
			{
				MoveAction ma = (MoveAction)action;
				MoveDir(ma.Direction);
				done = true;
			}
			else if (action is WaitAction)
			{
				WaitAction wa = (WaitAction)action;
				wa.Turns--;
				if (wa.Turns == 0)
					done = true;
				else
					done = false;
			}
			else
				throw new NotImplementedException();

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
			List<Location> newLocations = CalculateLOS();

			if (this.ClientCallback != null)
			{
				FilterAndSendChanges(changes);
				SendNewTerrains(newLocations);
			}
		}

		void SendNewTerrains(List<Location> newLocations)
		{
			if (newLocations.Count == 0)
				return;

			MapLocationTerrain[] terrains = new MapLocationTerrain[newLocations.Count];
			int i = 0;
			foreach (Location l in newLocations)
			{
				ObjectID[] obs = null;
				if (this.Environment.GetContents(l) != null)
					obs = this.Environment.GetContents(l).Select<ServerGameObject, ObjectID>(o => o.ObjectID).ToArray();

				terrains[i++] = new MapLocationTerrain(l, this.Environment.GetTerrain(l), obs);
			}

			this.ClientCallback.DeliverMapTerrains(terrains);
		}

		// calculate los and returns a list of new locations in sight
		List<Location> CalculateLOS()
		{
			// xxx todo: optimize
			TerrainInfo[] terrainInfo = this.Environment.Area.Terrains;
			LocationGrid<bool> visionMap = new LocationGrid<bool>(this.VisionRange * 2 + 1, this.VisionRange * 2 + 1,
				this.VisionRange, this.VisionRange);
			LOSShadowCast1.CalculateLOS(this.Location, this.VisionRange, visionMap, this.Environment.Bounds,
				l => { return terrainInfo[this.Environment.GetTerrain(l)].IsWalkable == false; });

			List<Location> newLocations = new List<Location>();
			Location dl = this.Location - this.VisionLocation; // new/old location diff

			foreach (Location l in visionMap.GetLocations())
			{
				bool wasVisible = false;
				if (this.VisionMap != null)
				{
					Location oldLocation = l + dl;

					if (this.VisionMap.Bounds.Contains(oldLocation))
					{
						if (this.VisionMap[oldLocation] == true)
							wasVisible = true;
					}
				}

				bool isVisible = visionMap[l] == true;

				if (wasVisible == false && isVisible == true)
					newLocations.Add(l + this.Location);
			}

			this.VisionMap = visionMap;
			this.VisionLocation = this.Location;

			return newLocations;
		}

		Change ChangeSelector(Change change)
		{
			if (change is ObjectEnvironmentChange)
			{
				ObjectEnvironmentChange ec = (ObjectEnvironmentChange)change;
				if (ec.DestinationMapID == this.Environment.ObjectID)
					change = new ObjectLocationChange(this.World.FindObject(ec.ObjectID),
						ec.DestinationLocation, ec.DestinationLocation);
					// xxx what when srcmap == thismap
				else
					return null;
			}

			if (change is ObjectLocationChange)
			{
				ObjectLocationChange lc = (ObjectLocationChange)change;
				if (!Sees(lc.SourceLocation) && !Sees(lc.TargetLocation))
				{
					MyDebug.WriteLine("\tplr doesn't see ob at {0}, skipping change", lc.SourceLocation);
					return null;
				}
			}

			if (change is MapChange)
			{
				MapChange mc = (MapChange)change;
				if (!Sees(mc.Location))
				{
					MyDebug.WriteLine("\tplr doesn't see ob at {0}, skipping change", mc.Location);
					return null;
				}
			}
			// send only changes that the player sees and needs to know

			return change;
		}

		void FilterAndSendChanges(Change[] changes)
		{
			MyDebug.WriteLine("Sending changes to plr id {0}", this.ObjectID);
			foreach (Change c in changes)
				MyDebug.WriteLine("\t" + c.ToString());

			IEnumerable<Change> arr = changes.Select<Change, Change>(ChangeSelector).Where(c => { return c != null; });

			this.ClientCallback.DeliverChanges(arr.ToArray());
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

		public event Action ActionQueuedEvent;

		#endregion

		public bool Sees(Location l)
		{
			Location dl = l - this.Location;

			if (Math.Abs(dl.X) > this.VisionRange ||
				Math.Abs(dl.Y) > this.VisionRange)
			{
				return false;
			}

			if (this.VisionMap[l - this.Location] == false)
				return false;

			return true;
		}

	}
}
