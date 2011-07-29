using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public class LocatableGameObject : ServerGameObject
	{
		protected LocatableGameObject(ObjectType objectType, LocatableGameObjectBuilder builder)
			: base(objectType, builder)
		{
			m_name = builder.Name;
			m_color = builder.Color;
			m_symbolID = builder.SymbolID;
			m_materialID = builder.MaterialID;
			if (m_color == GameColor.None)
				m_color = Materials.GetMaterial(m_materialID).Color;
		}

		protected LocatableGameObject(SaveGameContext ctx, ObjectType objectType)
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

		[SaveGameProperty("SymbolID")]
		SymbolID m_symbolID;
		public SymbolID SymbolID
		{
			get { return m_symbolID; }
			set { if (m_symbolID == value) return; m_symbolID = value; NotifyObject(PropertyID.SymbolID, value); }
		}

		public MaterialClass MaterialClass { get { return Materials.GetMaterial(this.MaterialID).MaterialClass; } } // XXX


		protected override Dictionary<PropertyID, object> SerializeProperties(ObjectVisibility visibility)
		{
			var props = base.SerializeProperties(visibility);
			props[PropertyID.Name] = m_name;
			props[PropertyID.MaterialID] = m_materialID;
			props[PropertyID.Color] = m_color;
			props[PropertyID.SymbolID] = m_symbolID;
			return props;
		}
	}

	public abstract class LocatableGameObjectBuilder : ServerGameObjectBuilder
	{
		public string Name { get; set; }
		public MaterialID MaterialID { get; set; }
		public GameColor Color { get; set; }
		public SymbolID SymbolID { get; set; }
	}
}
