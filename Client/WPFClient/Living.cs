using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows;

namespace MyGame.Client
{
	class Living : ClientGameObject, ILiving
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;

		public Jobs.IAI AI { get; private set; }

		public Living(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.AI = new Jobs.ClientAI(this, this.World.JobManager);
			this.IsLiving = true;
		}

		public static readonly DependencyProperty HitPointsProperty =
			RegisterGameProperty(PropertyID.HitPoints, "HitPoints", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty SpellPointsProperty =
			RegisterGameProperty(PropertyID.SpellPoints, "SpellPoints", typeof(int), typeof(Living), new UIPropertyMetadata(0));

		public static readonly DependencyProperty StrengthProperty =
			RegisterGameProperty(PropertyID.Strength, "Strength", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty DexterityProperty =
			RegisterGameProperty(PropertyID.Dexterity, "Dexterity", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty ConstitutionProperty =
			RegisterGameProperty(PropertyID.Constitution, "Constitution", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty IntelligenceProperty =
			RegisterGameProperty(PropertyID.Intelligence, "Intelligence", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty WisdomProperty =
			RegisterGameProperty(PropertyID.Wisdom, "Wisdom", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty CharismaProperty =
			RegisterGameProperty(PropertyID.Charisma, "Charisma", typeof(int), typeof(Living), new UIPropertyMetadata(0));

		public static readonly DependencyProperty VisionRangeProperty =
			RegisterGameProperty(PropertyID.VisionRange, "VisionRange", typeof(int), typeof(Living), new UIPropertyMetadata(VisionRangeChanged));
		public static readonly DependencyProperty FoodFullnessProperty =
			RegisterGameProperty(PropertyID.FoodFullness, "FoodFullness", typeof(int), typeof(Living));
		public static readonly DependencyProperty WaterFullnessProperty =
			RegisterGameProperty(PropertyID.WaterFullness, "WaterFullness", typeof(int), typeof(Living));

		public int HitPoints
		{
			get { return (int)GetValue(HitPointsProperty); }
			set { SetValue(HitPointsProperty, value); }
		}

		public int SpellPoints
		{
			get { return (int)GetValue(SpellPointsProperty); }
			set { SetValue(SpellPointsProperty, value); }
		}

		public int Strength
		{
			get { return (int)GetValue(StrengthProperty); }
			set { SetValue(StrengthProperty, value); }
		}

		public int Dexterity
		{
			get { return (int)GetValue(DexterityProperty); }
			set { SetValue(DexterityProperty, value); }
		}

		public int Constitution
		{
			get { return (int)GetValue(ConstitutionProperty); }
			set { SetValue(ConstitutionProperty, value); }
		}

		public int Intelligence
		{
			get { return (int)GetValue(IntelligenceProperty); }
			set { SetValue(IntelligenceProperty, value); }
		}

		public int Wisdom
		{
			get { return (int)GetValue(WisdomProperty); }
			set { SetValue(WisdomProperty, value); }
		}

		public int Charisma
		{
			get { return (int)GetValue(CharismaProperty); }
			set { SetValue(CharismaProperty, value); }
		}

		public int VisionRange
		{
			get { return (int)GetValue(VisionRangeProperty); }
			set { SetValue(VisionRangeProperty, value); }
		}

		static void VisionRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			Living l = (Living)d;
			l.m_visionMap = null;
		}

		public static readonly DependencyProperty CurrentActionProperty = DependencyProperty.Register("CurrentAction", typeof(GameAction), typeof(Living));
		public GameAction CurrentAction
		{
			get { return (GameAction)this.GetValue(CurrentActionProperty); }
			private set { this.SetValue(CurrentActionProperty, value); }
		}

		public bool HasAction { get { return this.CurrentAction != null; } }

		public static readonly DependencyProperty ActionTicksLeftProperty = DependencyProperty.Register("ActionTicksLeft", typeof(int), typeof(Living));
		public int ActionTicksLeft
		{
			get { return (int)this.GetValue(ActionTicksLeftProperty); }
			private set { this.SetValue(ActionTicksLeftProperty, value); }
		}

		public void HandleActionStarted(ActionStartedChange change)
		{
			if (this.HasAction)
				throw new Exception();

			this.CurrentAction = change.Action;
			this.ActionTicksLeft = change.TicksLeft;
		}

		public void DoAction(GameAction action)
		{
			throw new Exception();
			if (this.HasAction)
				throw new Exception();

			MyDebug.WriteLine("DoAction({0}: {1})", this, action);

			this.CurrentAction = action;

			//GameData.Data.Connection.Send(new Messages.DoActionMessage() { Action = action });
		}

		public void CancelAction()
		{
			//throw new Exception();
		}

		public void ActionProgress(ActionProgressChange e)
		{
			if (!this.HasAction)
				return;

			var action = this.CurrentAction;

			this.ActionTicksLeft = e.TicksLeft;

			if (e.TicksLeft == 0)
			{
				MyDebug.WriteLine("ActionDone({0}: {1})", this, action);
				this.CurrentAction = null;
			}
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

			if (m_losLocation == this.Location && m_losMapVersion == this.Environment.Version && m_visionMap != null)
				return;

			int visionRange = this.VisionRange;

			if (m_visionMap == null)
			{
				m_visionMap = new Grid2D<bool>(visionRange * 2 + 1, visionRange * 2 + 1,
					visionRange, visionRange);
				m_losMapVersion = 0;
			}

			var env = this.Environment;
			var z = this.Location.Z;

			s_losAlgo.Calculate(this.Location.ToIntPoint(), visionRange,
				m_visionMap, env.Bounds.Plane,
				l => env.GetInterior(new IntPoint3D(l, z)).Blocker);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

	}
}
