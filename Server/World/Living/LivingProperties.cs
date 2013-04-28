using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	public sealed partial class LivingObject
	{
		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();

			props[PropertyID.HitPoints] = m_hitPoints;
			props[PropertyID.MaxHitPoints] = m_maxHitPoints;
			props[PropertyID.SpellPoints] = m_spellPoints;
			props[PropertyID.MaxSpellPoints] = m_maxSpellPoints;
			props[PropertyID.Strength] = m_strength;
			props[PropertyID.Dexterity] = m_dexterity;
			props[PropertyID.Constitution] = m_constitution;
			props[PropertyID.Intelligence] = m_intelligence;
			props[PropertyID.Wisdom] = m_wisdom;
			props[PropertyID.Charisma] = m_charisma;
			props[PropertyID.Size] = m_size;
			props[PropertyID.NaturalArmorClass] = m_naturalArmorClass;
			props[PropertyID.VisionRange] = m_visionRange;
			props[PropertyID.Hunger] = m_hunger;
			props[PropertyID.Thirst] = m_thirst;
			props[PropertyID.Exhaustion] = m_exhaustion;
			props[PropertyID.Assignment] = m_assignment;
			props[PropertyID.Gender] = m_gender;

			return props;
		}

		[SaveGameProperty("HitPoints")]
		int m_hitPoints;
		public int HitPoints
		{
			get { return m_hitPoints; }
			set { if (m_hitPoints == value) return; m_hitPoints = value; NotifyInt(PropertyID.HitPoints, value); }
		}

		[SaveGameProperty("MaxHitPoints")]
		int m_maxHitPoints;
		public int MaxHitPoints
		{
			get { return m_maxHitPoints; }
			set { if (m_maxHitPoints == value) return; m_maxHitPoints = value; NotifyInt(PropertyID.MaxHitPoints, value); }
		}

		[SaveGameProperty("SpellPoints")]
		int m_spellPoints;
		public int SpellPoints
		{
			get { return m_spellPoints; }
			set { if (m_spellPoints == value) return; m_spellPoints = value; NotifyInt(PropertyID.SpellPoints, value); }
		}

		[SaveGameProperty("MaxSpellPoints")]
		int m_maxSpellPoints;
		public int MaxSpellPoints
		{
			get { return m_maxSpellPoints; }
			set { if (m_maxSpellPoints == value) return; m_maxSpellPoints = value; NotifyInt(PropertyID.MaxSpellPoints, value); }
		}

		[SaveGameProperty("Strength")]
		int m_strength;
		public int Strength
		{
			get { return m_strength; }
			set { if (m_strength == value) return; m_strength = value; NotifyInt(PropertyID.Strength, value); }
		}

		[SaveGameProperty("Dexterity")]
		int m_dexterity;
		public int Dexterity
		{
			get { return m_dexterity; }
			set { if (m_dexterity == value) return; m_dexterity = value; NotifyInt(PropertyID.Dexterity, value); }
		}

		[SaveGameProperty("Constitution")]
		int m_constitution;
		public int Constitution
		{
			get { return m_constitution; }
			set { if (m_constitution == value) return; m_constitution = value; NotifyInt(PropertyID.Constitution, value); }
		}

		[SaveGameProperty("Intelligence")]
		int m_intelligence;
		public int Intelligence
		{
			get { return m_intelligence; }
			set { if (m_intelligence == value) return; m_intelligence = value; NotifyInt(PropertyID.Intelligence, value); }
		}

		[SaveGameProperty("Wisdom")]
		int m_wisdom;
		public int Wisdom
		{
			get { return m_wisdom; }
			set { if (m_wisdom == value) return; m_wisdom = value; NotifyInt(PropertyID.Wisdom, value); }
		}

		[SaveGameProperty("Charisma")]
		int m_charisma;
		public int Charisma
		{
			get { return m_charisma; }
			set { if (m_charisma == value) return; m_charisma = value; NotifyInt(PropertyID.Charisma, value); }
		}

		[SaveGameProperty("Size")]
		int m_size;
		public int Size
		{
			get { return m_size; }
			set { if (m_size == value) return; m_size = value; NotifyInt(PropertyID.Size, value); }
		}

		[SaveGameProperty("NaturalArmorClass")]
		int m_naturalArmorClass;
		public int NaturalArmorClass
		{
			get { return m_naturalArmorClass; }
			set { if (m_naturalArmorClass == value) return; m_naturalArmorClass = value; NotifyInt(PropertyID.NaturalArmorClass, value); }
		}

		[SaveGameProperty("VisionRange")]
		int m_visionRange;
		public int VisionRange
		{
			get { return m_visionRange; }
			set { if (m_visionRange == value) return; m_visionRange = value; NotifyInt(PropertyID.VisionRange, value); m_visionMap = null; }
		}

		[SaveGameProperty("Hunger")]
		int m_hunger;
		public int Hunger
		{
			get { return m_hunger; }
			set { if (m_hunger == value) return; m_hunger = value; NotifyInt(PropertyID.Hunger, value); }
		}

		[SaveGameProperty("Thirst")]
		int m_thirst;
		public int Thirst
		{
			get { return m_thirst; }
			set { if (m_thirst == value) return; m_thirst = value; NotifyInt(PropertyID.Thirst, value); }
		}

		[SaveGameProperty("Exhaustion")]
		int m_exhaustion;
		public int Exhaustion
		{
			get { return m_exhaustion; }
			set { if (m_exhaustion == value) return; m_exhaustion = value; NotifyInt(PropertyID.Exhaustion, value); }
		}

		// String representation of assignment, for client use
		[SaveGameProperty("Assignment")]
		string m_assignment;
		public string Assignment
		{
			get { return m_assignment; }
			set { if (m_assignment == value) return; m_assignment = value; NotifyString(PropertyID.Assignment, value); }
		}

		[SaveGameProperty("Gender")]
		LivingGender m_gender;
		public LivingGender Gender
		{
			get { return m_gender; }
			set { if (m_gender == value) return; m_gender = value; NotifyValue(PropertyID.Gender, value); }
		}

	}
}
