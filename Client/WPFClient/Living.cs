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

		public GameAction CurrentAction { get; private set; }

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

		public bool HasAction { get { return this.CurrentAction != null; } }
		static int s_transactionNumber;

		public void DoAction(GameAction action)
		{
			if (this.HasAction)
				throw new Exception();

			action.ActorObjectID = this.ObjectID;
			if (action.Priority == ActionPriority.Undefined)
				action.Priority = ActionPriority.User;

			MyDebug.WriteLine("DoAction({0}: {1})", this, action);

			this.CurrentAction = action;
			GameData.Data.ActionCollection.Add(action);

			int tid = System.Threading.Interlocked.Increment(ref s_transactionNumber);
			action.TransactionID = tid;
			GameData.Data.Connection.Send(new Messages.DoActionMessage() { Action = action });
		}

		public void CancelAction()
		{
			if (!this.HasAction)
				throw new Exception();

			this.CurrentAction = null;
		}

		public void ActionProgress(ActionProgressEvent e)
		{
			if (!this.HasAction || e.TransactionID != this.CurrentAction.TransactionID)
				throw new Exception();

			var action = this.CurrentAction;

			action.TicksLeft = e.TicksLeft;

			if (e.TicksLeft == 0)
			{
				MyDebug.WriteLine("ActionDone({0}: {1})", this, action);

				GameData.Data.ActionCollection.Remove(action);
				this.CurrentAction = null;
			}

			// XXX GameAction doesn't have INotifyProperty changed, so we have to update manually
			var itemsView = System.Windows.Data.CollectionViewSource.GetDefaultView(App.MainWindow.actionList.ItemsSource);
			itemsView.Refresh();
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
