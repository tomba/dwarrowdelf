﻿using Dwarrowdelf.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	abstract class ChangeHandler
	{
		protected Player m_player;

		protected ChangeHandler(Player player)
		{
			m_player = player;
		}

		protected void Send(ClientMessage msg)
		{
			m_player.Send(msg);
		}

		protected void Send(IEnumerable<ClientMessage> msgs)
		{
			m_player.Send(msgs);
		}

		public abstract void HandleWorldChange(Change change);
	}

	sealed class AdminChangeHandler : ChangeHandler
	{
		public AdminChangeHandler(Player player)
			: base(player)
		{
		}

		public override void HandleWorldChange(Change change)
		{
			var changeMsg = new ChangeMessage() { ChangeData = change.ToChangeData() };

			Send(changeMsg);

			if (change is ObjectCreatedChange)
			{
				var c = (ObjectCreatedChange)change;
				var newObject = c.Object;
				newObject.SendTo(m_player, ObjectVisibility.All);
			}
		}
	}

	sealed class PlayerChangeHandler : ChangeHandler
	{
		// XXX should be saved
		bool m_turnStartSent;

		public PlayerChangeHandler(Player player)
			: base(player)
		{

		}

		public override void HandleWorldChange(Change change)
		{
			// can the player see the change?
			if (!CanSeeChange(change))
				return;

			// Send object data for an object that comes to the environment
			if (change is ObjectMoveChange)
			{
				var c = (ObjectMoveChange)change;
				var dst = c.Destination as EnvironmentObject;

				if (dst != null && c.Source != c.Destination &&
					(dst.VisibilityMode == VisibilityMode.AllVisible || dst.VisibilityMode == VisibilityMode.GlobalFOV))
				{
					var newObject = c.Object;
					var vis = m_player.GetObjectVisibility(newObject);
					Debug.Assert(vis != ObjectVisibility.None);
					newObject.SendTo(m_player, vis);
				}
			}
#if asf
			// When an armor is worn by a non-controllable, the armor isn't known to the client. Thus we need to send the data of the armor here.
			if (change is WearArmorChange)
			{
				var c = (WearArmorChange)change;

				if (m_player.IsController(c.Object) == false)
				{
					if (c.Wearable != null)
						c.Wearable.SendTo(m_player, ObjectVisibility.Public);
					// else it's being removed
				}
			}

			// The same for weapons
			if (change is WieldWeaponChange)
			{
				var c = (WieldWeaponChange)change;

				if (m_player.IsController(c.Object) == false)
				{
					if (c.Weapon != null)
						c.Weapon.SendTo(m_player, ObjectVisibility.Public);
					// else it's being removed
				}
			}
#endif
			var changeMsg = new ChangeMessage() { ChangeData = change.ToChangeData() };

			Send(changeMsg);
		}

		bool CanSeeChange(Change change)
		{
			if (change is TurnStartChange)
			{
				var c = (TurnStartChange)change;
				m_turnStartSent = c.Living == null || m_player.IsController(c.Living);
				return m_turnStartSent;
			}
			else if (change is TurnEndChange)
			{
				var b = m_turnStartSent;
				m_turnStartSent = false;
				return b;
			}
			else if (change is TickStartChange)
			{
				return true;
			}
			else if (change is GameDateChange)
			{
				return true;
			}
			else if (change is ObjectDestructedChange)
			{
				// XXX We should only send this if the player sees the object.
				// And the client should have a cleanup of some kind to remove old objects (which may or may not be destructed)
				return true;
			}
			else if (change is ObjectCreatedChange)
			{
				return false;
			}
			else if (change is ObjectMoveChange)
			{
				var c = (ObjectMoveChange)change;

				if (m_player.IsController(c.Object))
					return true;

				if (m_player.Sees(c.Source, c.SourceLocation))
					return true;

				if (m_player.Sees(c.Destination, c.DestinationLocation))
					return true;

				return false;
			}
			else if (change is ObjectMoveLocationChange)
			{
				var c = (ObjectMoveLocationChange)change;

				if (m_player.IsController(c.Object))
					return true;

				// XXX
				var env = ((MovableObject)c.Object).Parent;

				if (m_player.Sees(env, c.SourceLocation))
					return true;

				if (m_player.Sees(env, c.DestinationLocation))
					return true;

				return false;
			}
			else if (change is MapChange)
			{
				var c = (MapChange)change;
				return m_player.Sees(c.Environment, c.Location);
			}
			else if (change is PropertyChange)
			{
				var c = (PropertyChange)change;

				if (c.PropertyID == PropertyID.IsWorn || c.PropertyID == PropertyID.IsWielded)
				{
					Debugger.Break();
				
					var mo = (MovableObject)c.Object;

					for (MovableObject o = mo; o != null; o = o.Parent as MovableObject)
					{
						var ov = m_player.GetObjectVisibility(o);
						if ((ov & ObjectVisibility.Public) != 0)
							return true;
					}

					return false;
				}

				if (m_player.IsController(c.Object))
				{
					return true;
				}
				else
				{
					var vis = PropertyVisibilities.GetPropertyVisibility(c.PropertyID);
					var ov = m_player.GetObjectVisibility(c.Object);

					if ((ov & vis) == 0)
						return false;

					return true;
				}
			}
			else if (change is ActionStartedChange)
			{
				var c = (ActionStartedChange)change;
				return m_player.IsController(c.Object);
			}
			else if (change is ActionProgressChange)
			{
				var c = (ActionProgressChange)change;
				return m_player.IsController(c.Object);
			}
			else if (change is ActionDoneChange)
			{
				var c = (ActionDoneChange)change;
				return m_player.IsController(c.Object);
			}

			else if (change is ObjectChange)
			{
				var c = (ObjectChange)change;

				var vis = m_player.GetObjectVisibility(c.Object);

				return vis != ObjectVisibility.None;
			}
			else
			{
				throw new Exception();
			}
		}
	}
}
