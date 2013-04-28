using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Server
{
	public partial class ItemObject
	{
		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();

			props[PropertyID.Quality] = m_quality;
			props[PropertyID.NutritionalValue] = m_nutritionalValue;
			props[PropertyID.RefreshmentValue] = m_refreshmentValue;
			props[PropertyID.ReservedByStr] = m_reservedByStr;
			props[PropertyID.IsInstalled] = m_isInstalled;
			props[PropertyID.IsClosed] = m_isClosed;
			props[PropertyID.IsEquipped] = m_isEquipped;

			return props;
		}

		[SaveGameProperty("ReservedBy")]
		object m_reservedBy;
		public object ReservedBy
		{
			get { return m_reservedBy; }
			set
			{
				Debug.Assert(value == null || m_reservedBy == null);
				m_reservedBy = value;
				if (value != null)
					this.ReservedByStr = value.ToString();
				else
					this.ReservedByStr = null;
			}
		}

		public bool IsReserved { get { return m_reservedBy != null; } }

		// String representation of ReservedBy, for client use
		[SaveGameProperty("ReservedByStr")]
		string m_reservedByStr;
		public string ReservedByStr
		{
			get { return m_reservedByStr; }
			set { if (m_reservedByStr == value) return; m_reservedByStr = value; NotifyString(PropertyID.ReservedByStr, value); }
		}

		[SaveGameProperty("Quality")]
		int m_quality;
		public int Quality
		{
			get { return m_quality; }
			set { if (m_quality == value) return; m_quality = value; NotifyInt(PropertyID.Quality, value); }
		}

		[SaveGameProperty("NutritionalValue")]
		int m_nutritionalValue;
		public int NutritionalValue
		{
			get { return m_nutritionalValue; }
			set { if (m_nutritionalValue == value) return; m_nutritionalValue = value; NotifyInt(PropertyID.NutritionalValue, value); }
		}

		[SaveGameProperty("RefreshmentValue")]
		int m_refreshmentValue;
		public int RefreshmentValue
		{
			get { return m_refreshmentValue; }
			set { if (m_refreshmentValue == value) return; m_refreshmentValue = value; NotifyInt(PropertyID.RefreshmentValue, value); }
		}

		[SaveGameProperty("IsInstalled")]
		bool m_isInstalled;
		public bool IsInstalled
		{
			get { return m_isInstalled; }
			set
			{
				Debug.Assert(this.Environment != null);
				Debug.Assert(this.ItemInfo.IsInstallable);
				if (m_isInstalled == value) return; m_isInstalled = value; NotifyBool(PropertyID.IsInstalled, value);
				CheckBlock();
			}
		}

		[SaveGameProperty("IsClosed")]
		bool m_isClosed;
		public bool IsClosed
		{
			get { return m_isClosed; }
			set
			{
				Debug.Assert(this.ItemID == Dwarrowdelf.ItemID.Door);
				if (m_isClosed == value) return; m_isClosed = value; NotifyBool(PropertyID.IsClosed, value);
				CheckBlock();
			}
		}

		[SaveGameProperty("IsEquipped")]
		bool m_isEquipped;
		public bool IsEquipped
		{
			get { return m_isEquipped; }

			internal set
			{
				Debug.Assert(this.IsWeapon || this.IsArmor);
				Debug.Assert(this.Parent is LivingObject);

				if (m_isEquipped == value)
					return;

				m_isEquipped = value;

				var p = (LivingObject)this.Parent;
				p.OnItemIsEquippedChanged(this, value);

				NotifyBool(PropertyID.IsEquipped, value);
			}
		}

		public LivingObject Equipper
		{
			get
			{
				if (this.IsEquipped)
					return (LivingObject)this.Parent;
				else
					return null;
			}
		}

		ILivingObject IItemObject.Equipper { get { return this.Equipper; } }
	}
}
