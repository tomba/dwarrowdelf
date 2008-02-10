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
		public void PostTurn()
		{
			// xxx todo: optimize
			TerrainInfo[] terrainInfo = this.Environment.Area.Terrains;
			LocationGrid<bool> visionMap = new LocationGrid<bool>(this.VisionRange * 2 + 1, this.VisionRange * 2 + 1,
				this.VisionRange, this.VisionRange);
			LOSShadowCast1.CalculateLOS(this.Location, this.VisionRange, visionMap,
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

			if (this.ClientCallback != null)
			{
				MapLocationTerrain[] terrains = new MapLocationTerrain[newLocations.Count];
				int i = 0;
				foreach (Location l in newLocations)
				{
					ObjectID[] obs = null;
					if(this.Environment.GetContents(l) != null)
						obs = this.Environment.GetContents(l).Select<ServerGameObject, ObjectID>(o => o.ObjectID).ToArray();

					terrains[i++] = new MapLocationTerrain(l, this.Environment.GetTerrain(l), obs);
				}

				this.ClientCallback.DeliverMapTerrains(terrains);
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

		public event Action ActionQueuedEvent;

		#endregion




		public bool Sees(Location l)
		{
			if (Math.Abs(l.X - this.Location.X) <= this.VisionRange &&
				Math.Abs(l.Y - this.Location.Y) <= this.VisionRange)
			{
				return true;
			}

			return false;
		}

	}
}
