using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using Dwarrowdelf.Messages;
using System.Windows;

namespace Dwarrowdelf.Client
{
	class ItemObject : ClientGameObject, IItemObject
	{
		public ItemObject(World world, ObjectID objectID)
			: base(world, objectID)
		{

		}

		public override string ToString()
		{
			return String.Format("Item({0})", this.ObjectID.Value);
		}

		object m_reservedBy;
		public object ReservedBy
		{
			get { return m_reservedBy; }
			set { m_reservedBy = value; Notify("ReservedBy"); }
		}

		public ItemInfo ItemInfo { get; private set; }
		public ItemClass ItemClass { get { return this.ItemInfo.ItemClass; } }
		public ItemID ItemID { get { return this.ItemInfo.ItemID; } }

		int m_nutritionalValue;
		public int NutritionalValue
		{
			get { return m_nutritionalValue; }
			private set { m_nutritionalValue = value; Notify("NutritionalValue"); }
		}

		int m_refreshmentValue;
		public int RefreshmentValue
		{
			get { return m_refreshmentValue; }
			private set { m_refreshmentValue = value; Notify("RefreshmentValue"); }
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.NutritionalValue:
					this.NutritionalValue = (int)value;
					break;

				case PropertyID.RefreshmentValue:
					this.RefreshmentValue = (int)value;
					break;

				default:
					base.SetProperty(propertyID, value);
					break;
			}
		}

		public override void Deserialize(BaseGameObjectData _data)
		{
			var data = (ItemData)_data;

			this.ItemInfo = Dwarrowdelf.Items.GetItem(data.ItemID);

			base.Deserialize(_data);

			var matInfo = this.Material;
			switch (this.ItemID)
			{
				case ItemID.UncutGem:
					this.Description = "Uncut " + matInfo.Name.ToLowerInvariant();
					break;

				case ItemID.Rock:
				case ItemID.Ore:
					if (matInfo.ID == MaterialID.NativeGold)
					{
						this.Description = matInfo.Adjective.Capitalize() + " nugget";
					}
					else
					{
						this.Description = matInfo.Adjective.Capitalize() + " rock";
					}
					break;

				case ItemID.Gem:
					this.Description = matInfo.Name.Capitalize();
					break;

				default:
					this.Description = matInfo.Adjective.Capitalize() + " " + this.ItemInfo.Name.ToLowerInvariant();
					break;
			}
		}
	}
}
