using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	[SaveGameObjectByRef]
	public abstract class ConcreteObject : MovableObject, IConcreteObject
	{
		protected ConcreteObject(ObjectType objectType, LocatableGameObjectBuilder builder)
			: base(objectType, builder)
		{
			m_name = builder.Name;
			m_color = builder.Color;
			m_materialID = builder.MaterialID;
			if (m_color == GameColor.None)
				m_color = Materials.GetMaterial(m_materialID).Color;
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
			set { if (m_name == value) return; m_name = value; NotifyObject(PropertyID.Name, value); }
		}

		[SaveGameProperty("MaterialID")]
		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			set { if (m_materialID == value) return; m_materialID = value; NotifyObject(PropertyID.MaterialID, value); this.Color = Materials.GetMaterial(value).Color; } // XXX sets color?
		}

		[SaveGameProperty("Color")]
		GameColor m_color;
		public GameColor Color
		{
			get { return m_color; }
			set { if (m_color == value) return; m_color = value; NotifyObject(PropertyID.Color, value); }
		}

		public MaterialCategory MaterialCategory { get { return Materials.GetMaterial(this.MaterialID).Category; } } // XXX


		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			props[PropertyID.Name] = m_name;
			props[PropertyID.MaterialID] = m_materialID;
			props[PropertyID.Color] = m_color;
			return props;
		}
	}

	public abstract class LocatableGameObjectBuilder : ServerGameObjectBuilder
	{
		public string Name { get; set; }
		public MaterialID MaterialID { get; set; }
		public GameColor Color { get; set; }
	}
}
