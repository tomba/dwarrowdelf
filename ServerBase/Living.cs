using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame
{
	class Living : ServerGameObject, IActor
	{
		// XXX note: not re-entrant
		static LOSAlgo s_losAlgo = new LOSShadowCast1();

		public List<ItemObject> Inventory { get; private set; }

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

		int m_losTurn = -1;
		List<Location> m_newLocations;

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
			return;
			List<Location> newLocations = CalculateLOS();

			if (this.ClientCallback != null)
			{
				//FilterAndSendChanges(changes);
				SendNewTerrains(newLocations);
			}
		}

		void SendNewTerrains(List<Location> newLocations)
		{
			if (newLocations.Count == 0)
				return;

			var terrains = new ClientMsgs.MapData[newLocations.Count];
			int i = 0;
			foreach (Location l in newLocations)
			{
				ObjectID[] obs = null;
				if (this.Environment.GetContents(l) != null)
					obs = this.Environment.GetContents(l).Select<ServerGameObject, ObjectID>(o => o.ObjectID).ToArray();

				terrains[i++] = new ClientMsgs.MapData()
				{
					Location = l,
					Terrain = this.Environment.GetTerrain(l),
					Objects = obs 
				};
			}

			var msgs = new ClientMsgs.Message[] { new ClientMsgs.TerrainData() { MapDataList = terrains } };
			this.ClientCallback.DeliverMessage(msgs);
		}

		public List<Location> GetNewLocations()
		{
			UpdateLOS();
			return m_newLocations;
		}

		void UpdateLOS()
		{
			if (m_losTurn != this.World.TurnNumber)
			{
				m_newLocations = CalculateLOS();
				m_losTurn = this.World.TurnNumber;
			}
		}

		// calculate los and returns a list of new locations in sight
		List<Location> CalculateLOS()
		{
			LocationGrid<bool> oldVisionMap = this.VisionMap;
			TerrainInfo[] terrainInfo = this.Environment.Area.Terrains;
			LocationGrid<bool> newVisionMap = new LocationGrid<bool>(this.VisionRange * 2 + 1, this.VisionRange * 2 + 1,
				this.VisionRange, this.VisionRange);
			s_losAlgo.Calculate(this.Location, this.VisionRange, newVisionMap, this.Environment.Bounds,
				l => { return terrainInfo[this.Environment.GetTerrain(l)].IsWalkable == false; });

			List<Location> newLocations = new List<Location>();
			Location dl = this.Location - this.VisionLocation; // new/old location diff

			// xxx todo: optimize

			foreach (Location l in newVisionMap.GetLocations())
			{
				if (!this.Environment.Bounds.Contains(l + this.Location))
					continue;

				bool wasVisible = false;
				if (oldVisionMap != null)
				{
					Location oldLocation = l + dl;

					if (oldVisionMap.Bounds.Contains(oldLocation))
					{
						if (oldVisionMap[oldLocation] == true)
							wasVisible = true;
					}
				}

				bool isVisible = newVisionMap[l] == true;

				if (wasVisible == false && isVisible == true)
					newLocations.Add(l + this.Location);
			}

			this.VisionMap = newVisionMap;
			this.VisionLocation = this.Location;

			return newLocations;
		}

		// XXX move to somewhere generic
		public static ClientMsgs.Message ChangeToMessage(Change change)
		{
			if (change is ObjectEnvironmentChange)
			{
				ObjectEnvironmentChange ec = (ObjectEnvironmentChange)change;
				return ChangeToMessage(new ObjectLocationChange(ec.Target,
					ec.DestinationLocation, ec.DestinationLocation));
			}

			if (change is ObjectLocationChange)
			{
				ObjectLocationChange lc = (ObjectLocationChange)change;
				int symbol = ((ServerGameObject)lc.Target).SymbolID;
				return new ClientMsgs.ObjectMove(lc.Target, symbol, lc.SourceLocation, lc.TargetLocation);
			}

			if (change is MapChange)
			{
				MapChange mc = (MapChange)change;
				return new ClientMsgs.TerrainData()
				{
					MapDataList = new ClientMsgs.MapData[] {
						new ClientMsgs.MapData() { Location = mc.Location, Terrain = mc.TerrainType, Objects = null }
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

		bool ChangeFilter(Change change)
		{
			// send only changes that the player sees and needs to know

			if (change is ObjectEnvironmentChange)
			{
				// xxx what when srcmap == thismap
				ObjectEnvironmentChange ec = (ObjectEnvironmentChange)change;
				if (ec.DestinationMapID == this.Environment.ObjectID)
					return ChangeFilter(new ObjectLocationChange(this.World.FindObject(ec.ObjectID),
						ec.DestinationLocation, ec.DestinationLocation));
				else
					return false;
			}

			if (change is ObjectLocationChange)
			{
				ObjectLocationChange lc = (ObjectLocationChange)change;
				if (!Sees(lc.SourceLocation) && !Sees(lc.TargetLocation))
				{
					MyDebug.WriteLine("\tplr doesn't see ob at {0}, skipping change", lc.SourceLocation);
					return false;
				}
				else
				{
					return true;
				}
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

		void FilterAndSendChanges(Change[] changes)
		{
			MyDebug.WriteLine("Sending changes to plr id {0}", this.ObjectID);
			foreach (Change c in changes)
				MyDebug.WriteLine("\t" + c.ToString());

			ClientMsgs.Message[] arr = changes.
				Where(ChangeFilter).
				Where(c => c != null).
				Select<Change, ClientMsgs.Message>(ChangeToMessage).
				ToArray();

			this.ClientCallback.DeliverMessage(arr);
		}

		public void SendInventory()
		{
			if (this.ClientCallback != null)
			{
				var items = new List<ClientMsgs.ItemData>(this.Inventory.Count);
				foreach (ItemObject item in this.Inventory)
				{
					var data = new ClientMsgs.ItemData();
					data.ObjectID = item.ObjectID;
					data.Name = item.Name;
					data.SymbolID = item.SymbolID;
					items.Add(data);
				}

				var msgs = new ClientMsgs.Message[] { new ClientMsgs.ItemsData() { Items = items.ToArray() } };

				this.ClientCallback.DeliverMessage(msgs);
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
			Location dl = l - this.Location;

			if (Math.Abs(dl.X) > this.VisionRange ||
				Math.Abs(dl.Y) > this.VisionRange)
			{
				return false;
			}

			UpdateLOS();

			if (this.VisionMap[l - this.Location] == false)
				return false;

			return true;
		}

	}
}
