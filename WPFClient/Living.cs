using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows;

namespace MyGame.Client
{
	class LivingCollection : ObservableCollection<Living> { }

	class Living : ClientGameObject
	{
		// XXX not re-entrant
		static ILOSAlgo s_losAlgo = new LOSShadowCast1();

		uint m_losMapVersion;
		IntPoint3D m_losLocation;
		Grid2D<bool> m_visionMap;

		public AI AI { get; private set; }

		public Living(World world, ObjectID objectID)
			: base(world, objectID)
		{
			this.AI = new AI(this);
			this.IsLiving = true;
		}

		static Living()
		{
			AddPropertyMapping(PropertyID.HitPoints, HitPointsProperty);
			AddPropertyMapping(PropertyID.SpellPoints, SpellPointsProperty);

			AddPropertyMapping(PropertyID.Strength, StrengthProperty);
			AddPropertyMapping(PropertyID.Dexterity, DexterityProperty);
			AddPropertyMapping(PropertyID.Constitution, ConstitutionProperty);
			AddPropertyMapping(PropertyID.Intelligence, IntelligenceProperty);
			AddPropertyMapping(PropertyID.Wisdom, WisdomProperty);
			AddPropertyMapping(PropertyID.Charisma, CharismaProperty);

			AddPropertyMapping(PropertyID.VisionRange, VisionRangeProperty);
			AddPropertyMapping(PropertyID.FoodFullness, FoodFullnessProperty);
			AddPropertyMapping(PropertyID.WaterFullness, WaterFullnessProperty);
		}


		public static readonly DependencyProperty HitPointsProperty =
			DependencyProperty.Register("HitPoints", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty SpellPointsProperty =
			DependencyProperty.Register("SpellPoints", typeof(int), typeof(Living), new UIPropertyMetadata(0));

		public static readonly DependencyProperty StrengthProperty =
			DependencyProperty.Register("Strength", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty DexterityProperty =
			DependencyProperty.Register("Dexterity", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty ConstitutionProperty =
			DependencyProperty.Register("Constitution", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty IntelligenceProperty =
			DependencyProperty.Register("Intelligence", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty WisdomProperty =
			DependencyProperty.Register("Wisdom", typeof(int), typeof(Living), new UIPropertyMetadata(0));
		public static readonly DependencyProperty CharismaProperty =
			DependencyProperty.Register("Charisma", typeof(int), typeof(Living), new UIPropertyMetadata(0));

		public static readonly DependencyProperty VisionRangeProperty =
			DependencyProperty.Register("VisionRange", typeof(int), typeof(Living), new UIPropertyMetadata(VisionRangeChanged));
		public static readonly DependencyProperty FoodFullnessProperty =
			DependencyProperty.Register("FoodFullness", typeof(int), typeof(Living));
		public static readonly DependencyProperty WaterFullnessProperty =
			DependencyProperty.Register("WaterFullness", typeof(int), typeof(Living));

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

			if (m_losLocation == this.Location && m_losMapVersion == this.Environment.Version && m_visionMap != null)
				return;

			int visionRange = this.VisionRange;

			if (m_visionMap == null)
			{
				m_visionMap = new Grid2D<bool>(visionRange * 2 + 1, visionRange * 2 + 1,
					visionRange, visionRange);
				m_losMapVersion = 0;
			}

			var level = this.Environment.GetLevel(this.Location.Z);

			s_losAlgo.Calculate(this.Location.ToIntPoint(), visionRange,
				m_visionMap, level.Bounds,
				l => Interiors.GetInterior(level.GetInteriorID(l)).Blocker);

			m_losMapVersion = this.Environment.Version;
			m_losLocation = this.Location;
		}

	}
}
