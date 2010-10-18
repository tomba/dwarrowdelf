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

		public ItemClass ItemClass { get; private set; }

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

			this.ItemClass = data.ItemClass;

			base.Deserialize(_data);
		}
	}
}
