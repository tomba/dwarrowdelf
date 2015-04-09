using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	[SaveGameObject]
	public abstract class ConcreteObject : MovableObject, IConcreteObject
	{
		// Contents of type IItemObject
		IEnumerable<IItemObject> IConcreteObject.Inventory { get { return this.Contents.OfType<IItemObject>(); } }
		// Contents of type ItemObject
		public IEnumerable<ItemObject> Inventory { get { return this.Contents.OfType<ItemObject>(); } }

		protected ConcreteObject(ObjectType objectType, ConcreteObjectBuilder builder)
			: base(objectType, builder)
		{
			m_name = builder.Name;
			m_color = builder.Color;
			m_materialID = builder.MaterialID;
		}

		protected ConcreteObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
		}

		[SaveGameProperty("Name")]
		string m_name;
		public string Name
		{
			get { return m_name; }
			set { if (m_name == value) return; m_name = value; NotifyString(PropertyID.Name, value); }
		}

		[SaveGameProperty("MaterialID")]
		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			set { if (m_materialID == value) return; m_materialID = value; NotifyValue(PropertyID.MaterialID, value); }
		}

		[SaveGameProperty("Color")]
		GameColor m_color;
		public GameColor Color
		{
			get { return m_color; }
			set { if (m_color == value) return; m_color = value; NotifyValue(PropertyID.Color, value); }
		}

		public MaterialInfo Material { get { return Materials.GetMaterial(this.MaterialID); } }
		public MaterialCategory MaterialCategory { get { return this.Material.Category; } }


		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			props[PropertyID.Name] = m_name;
			props[PropertyID.MaterialID] = m_materialID;
			props[PropertyID.Color] = m_color;
			return props;
		}
	}

	public abstract class ConcreteObjectBuilder : MovableObjectBuilder
	{
		public string Name { get; set; }
		public MaterialID MaterialID { get; set; }
		public GameColor Color { get; set; }
	}
}
