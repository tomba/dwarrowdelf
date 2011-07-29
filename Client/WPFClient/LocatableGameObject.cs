using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace Dwarrowdelf.Client
{
	abstract class LocatabletGameObject : ClientGameObject, ILocatableGameObject
	{
		static LocatabletGameObject()
		{
			GameData.Data.SymbolDrawingCache.DrawingsChanged += OnSymbolDrawingCacheChanged;
		}

		static void OnSymbolDrawingCacheChanged()
		{
			foreach (var ob in GameData.Data.World.Objects.OfType<LocatabletGameObject>())
				ob.ReloadDrawing();
		}

		public LocatabletGameObject(World world, ObjectID objectID)
			: base(world, objectID)
		{
		}

		public override void SetProperty(PropertyID propertyID, object value)
		{
			switch (propertyID)
			{
				case PropertyID.Name:
					this.Name = (string)value;
					break;

				case PropertyID.Color:
					this.Color = (GameColor)value;
					break;

				case PropertyID.SymbolID:
					this.SymbolID = (SymbolID)value;
					break;

				case PropertyID.MaterialID:
					this.MaterialID = (MaterialID)value;
					break;

				default:
					throw new Exception(String.Format("Unknown property {0} in {1}", propertyID, this.GetType().FullName));
			}
		}

		string m_name;
		public string Name
		{
			get { return m_name; }
			private set { m_name = value; Notify("Name"); }
		}

		GameColor m_color;
		public GameColor Color
		{
			get { return m_color; }
			private set
			{
				m_color = value;

				m_drawing = new DrawingImage(GameData.Data.SymbolDrawingCache.GetDrawing(this.SymbolID, this.Color));
				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("Color");
				Notify("Drawing");
			}
		}

		SymbolID m_symbolID;
		public SymbolID SymbolID
		{
			get { return m_symbolID; }
			private set
			{
				m_symbolID = value;

				m_drawing = new DrawingImage(GameData.Data.SymbolDrawingCache.GetDrawing(this.SymbolID, this.Color));
				if (this.Environment != null)
					this.Environment.OnObjectVisualChanged(this);

				Notify("SymbolID");
				Notify("Drawing");
			}
		}

		DrawingImage m_drawing;
		public DrawingImage Drawing
		{
			get { return m_drawing; }
		}

		void ReloadDrawing()
		{
			m_drawing = new DrawingImage(GameData.Data.SymbolDrawingCache.GetDrawing(this.SymbolID, this.Color));
			if (this.Environment != null)
				this.Environment.OnObjectVisualChanged(this);

			Notify("Drawing");
		}

		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			private set
			{
				m_materialID = value;
				m_materialInfo = Materials.GetMaterial(this.MaterialID);
				Notify("MaterialID");
				Notify("Material");
			}
		}

		MaterialInfo m_materialInfo;
		public MaterialInfo Material
		{
			get { return m_materialInfo; }
		}

		public MaterialClass MaterialClass { get { return m_materialInfo.MaterialClass; } } // XXX

		string m_desc;
		public string Description
		{
			get { return m_desc; }
			protected set
			{
				m_desc = value;
				Notify("Description");
			}
		}

		public override string ToString()
		{
			return String.Format("Object({0}/{1})", this.Name, this.ObjectID);
		}
	}
}
