using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;

namespace MyGame.Client
{
	class LivingCollection : ObservableCollection<Living> { }

	class Living : ClientGameObject
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		int m_visionRange;
		Grid2D<bool> m_visionMap;

		public int VisionRange
		{
			get { return m_visionRange; }
			set { m_visionRange = value; m_visionMap = null; }
		}

		public AI AI { get; private set; }

		public Living(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.AI = new AI(this);
			this.IsLiving = true;
		}

		public void EnqueueAction(GameAction action)
		{
			action.ActorObjectID = this.ObjectID;
			MyDebug.WriteLine("DoAction({0}: {1})", this, action);
			GameData.Data.ActionCollection.Add(action);
			GameData.Data.Connection.EnqueueAction(action);
		}

		public void ActionDone(GameAction action)
		{
			MyDebug.WriteLine("ActionDone({0}: {1})", this, action);
			GameData.Data.ActionCollection.Remove(action);
		}

		public Grid2D<bool> VisionMap
		{
			get
			{
				UpdateLOS();
				return m_visionMap;
			}
		}

		void UpdateLOS()
		{
			Debug.Assert(this.Environment.VisibilityMode == VisibilityMode.LOS);

			if (this.Environment == null)
				return;

			if (m_losLocation == this.Location && m_losMapVersion == this.Environment.Version)
				return;

			if (m_visionMap == null)
			{
				m_visionMap = new Grid2D<bool>(m_visionRange * 2 + 1, m_visionRange * 2 + 1,
					m_visionRange, m_visionRange);
				m_losMapVersion = 0;
			}

			var terrains = this.Environment.World.AreaData.Terrains;
			var level = this.Environment.GetLevel(this.Location.Z);

			s_losAlgo.Calculate(this.Location2D, m_visionRange,
				m_visionMap, level.Bounds,
				l => terrains.GetInteriorInfo(level.GetInteriorID(l)).Blocker);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

	}
}
