using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	class ChangeHandler
	{
		static Dictionary<Type, Action<ChangeHandler, Change>> s_changeHandlerMap;

		static ChangeHandler()
		{
			var changeTypes = Helpers.GetNonabstractSubclasses(typeof(Change));

			s_changeHandlerMap = new Dictionary<Type, Action<ChangeHandler, Change>>(changeTypes.Count());

			foreach (var type in changeTypes)
			{
				var method = WrapperGenerator.CreateActionWrapper<ChangeHandler, Change>("HandleChange", type);
				if (method == null)
					throw new NotImplementedException(String.Format("No HandleChange method found for {0}", type.Name));
				s_changeHandlerMap[type] = method;
			}
		}

		World m_world;
		MyTraceSource trace = new MyTraceSource("Dwarrowdelf.Connection", "ChangeHandler");

		public event Action TurnEnded;

		public ChangeHandler(World world)
		{
			m_world = world;
		}

		public void HandleChangeMessage(Dwarrowdelf.Messages.ChangeMessage msg)
		{
			var change = msg.Change;
			var method = s_changeHandlerMap[change.GetType()];
			method(this, change);
		}

		void HandleChange(ObjectCreatedChange change)
		{
			var ob = m_world.CreateObject(change.ObjectID);
		}

		// XXX check if this is needed
		void HandleChange(FullObjectChange change)
		{
			var ob = m_world.GetObject<BaseObject>(change.ObjectID);

			ob.Deserialize(change.ObjectData);
		}

		void HandleChange(ObjectMoveChange change)
		{
			var ob = m_world.FindObject<MovableObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			Debug.Assert(ob.IsInitialized);

			ContainerObject env = null;
			if (change.DestinationID != ObjectID.NullObjectID)
				env = m_world.GetObject<ContainerObject>(change.DestinationID);

			ob.MoveTo(env, change.DestinationLocation);
		}

		void HandleChange(ObjectMoveLocationChange change)
		{
			var ob = m_world.FindObject<MovableObject>(change.ObjectID);

			if (ob == null)
			{
				/* There's a special case where we don't get objectinfo, but we do get
				 * ObjectMove: If the object moves from tile, that just came visible to us, 
				 * to a tile that we cannot see. So let's not throw exception, but exit
				 * silently */
				// XXX is this still valid?
				return;
			}

			Debug.Assert(ob.IsInitialized);

			ob.MoveTo(change.DestinationLocation);
		}

		void HandlePropertyChange(ObjectID objectID, PropertyID propertyID, object value)
		{
			var ob = m_world.GetObject<BaseObject>(objectID);

			Debug.Assert(ob.IsInitialized);

			ob.SetProperty(propertyID, value);
		}

		void HandleChange(PropertyValueChange change)
		{
			HandlePropertyChange(change.ObjectID, change.PropertyID, change.Value);
		}

		void HandleChange(PropertyIntChange change)
		{
			HandlePropertyChange(change.ObjectID, change.PropertyID, change.Value);
		}

		void HandleChange(PropertyStringChange change)
		{
			HandlePropertyChange(change.ObjectID, change.PropertyID, change.Value);
		}

		void HandleChange(SkillChange change)
		{
			var ob = m_world.GetObject<LivingObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			ob.SetSkillLevel(change.SkillID, change.Level);
		}

		void HandleChange(WearChange change)
		{
			var ob = m_world.GetObject<LivingObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			if (change.WearableID != ObjectID.NullObjectID)
			{
				var wearable = m_world.FindOrCreateObject<ItemObject>(change.WearableID);
				ob.WearArmor(change.Slot, wearable);
			}
			else
			{
				ob.RemoveArmor(change.Slot);
			}
		}


		void HandleChange(WieldChange change)
		{
			var ob = m_world.GetObject<LivingObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			if (change.WeaponID != ObjectID.NullObjectID)
			{
				var weapon = m_world.FindOrCreateObject<ItemObject>(change.WeaponID);
				ob.WieldWeapon(weapon);
			}
			else
			{
				ob.RemoveWeapon();
			}
		}

		void HandleChange(ObjectDestructedChange change)
		{
			var ob = m_world.GetObject<BaseObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			ob.Destruct();
		}

		void HandleChange(MapChange change)
		{
			var env = m_world.GetObject<EnvironmentObject>(change.EnvironmentID);

			Debug.Assert(env.IsInitialized);

			env.SetTileData(change.Location, change.TileData);
		}

		void HandleChange(TickStartChange change)
		{
			m_world.HandleChange(change);
		}

		void HandleChange(TurnStartSimultaneousChange change)
		{
		}

		void HandleChange(TurnStartSequentialChange change)
		{
		}

		void HandleChange(TurnEndSimultaneousChange change)
		{
			if (TurnEnded != null)
				TurnEnded();
		}

		void HandleChange(TurnEndSequentialChange change)
		{
			if (TurnEnded != null)
				TurnEnded();
		}

		void HandleChange(ActionStartedChange change)
		{
			//Debug.WriteLine("ActionStartedChange({0})", change.ObjectID);

			var ob = m_world.GetObject<LivingObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			ob.HandleActionStarted(change);
		}

		void HandleChange(ActionProgressChange change)
		{
			var ob = m_world.GetObject<LivingObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			ob.HandleActionProgress(change);
		}

		void HandleChange(ActionDoneChange change)
		{
			var ob = m_world.GetObject<LivingObject>(change.ObjectID);

			Debug.Assert(ob.IsInitialized);

			ob.HandleActionDone(change);
		}
	}
}
