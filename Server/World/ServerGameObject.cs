using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	class KeyedObjectCollection : KeyedCollection<ObjectID, ServerGameObject>
	{
		public KeyedObjectCollection() : base(null, 10) { }

		protected override ObjectID GetKeyForItem(ServerGameObject item)
		{
			return item.ObjectID;
		}
	}

	/* Game object that has inventory, location */
	abstract public class ServerGameObject : BaseGameObject, IGameObject
	{
		[SaveGameProperty]
		public ServerGameObject Parent { get; private set; }
		IBaseGameObject IGameObject.Parent { get { return this.Parent; } }
		public Environment Environment { get { return this.Parent as Environment; } }
		IEnvironment IGameObject.Environment { get { return this.Parent as IEnvironment; } }
		[SaveGameProperty("Inventory")]
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<ServerGameObject> Inventory { get; private set; }

		[SaveGameProperty]
		public IntPoint3D Location { get; private set; }
		public IntPoint Location2D { get { return new IntPoint(this.Location.X, this.Location.Y); } }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }
		public int Z { get { return this.Location.Z; } }

		protected ServerGameObject(ObjectType objectType)
			: base(objectType)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);
		}

		protected ServerGameObject(ObjectType objectType, ServerGameObjectBuilder builder)
			: base(objectType)
		{
			m_children = new KeyedObjectCollection();
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);

			m_name = builder.Name;
			m_color = builder.Color;
			m_symbolID = builder.SymbolID;
			m_materialID = builder.MaterialID;
			if (m_color == GameColor.None)
				m_color = Materials.GetMaterial(m_materialID).Color;
		}

		protected ServerGameObject(SaveGameContext ctx, ObjectType objectType)
			: base(ctx, objectType)
		{
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);
		}

		public override void Destruct()
		{
			this.MoveTo(null);
			base.Destruct();
		}

		public override void SerializeTo(Action<Messages.ClientMessage> writer)
		{
			base.SerializeTo(writer);
			foreach (var o in this.Inventory)
				o.SerializeTo(writer);
		}

		[SaveGameProperty("Name")]
		string m_name;
		public string Name
		{
			get { return m_name; }
			set { if (m_name == value) return; m_name = value; NotifyObject(PropertyID.Name, value); }
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

		[SaveGameProperty("MaterialID")]
		MaterialID m_materialID;
		public MaterialID MaterialID
		{
			get { return m_materialID; }
			set { if (m_materialID == value) return; m_materialID = value; NotifyObject(PropertyID.MaterialID, value); this.Color = Materials.GetMaterial(value).Color; } // XXX sets color?
		}

		protected override Dictionary<PropertyID, object> SerializeProperties()
		{
			var props = base.SerializeProperties();
			props[PropertyID.Name] = m_name;
			props[PropertyID.Color] = m_color;
			props[PropertyID.SymbolID] = m_symbolID;
			props[PropertyID.MaterialID] = m_materialID;
			return props;
		}

		public MaterialClass MaterialClass { get { return Materials.GetMaterial(this.MaterialID).MaterialClass; } } // XXX

		public virtual bool HandleChildAction(ServerGameObject child, GameAction action) { return false; }

		protected virtual bool OkToAddChild(ServerGameObject ob, IntPoint3D dstLoc) { return true; }
		protected virtual bool OkToMoveChild(ServerGameObject ob, Direction dir, IntPoint3D dstLoc) { return true; }

		protected virtual void OnChildAdded(ServerGameObject child) { }
		protected virtual void OnChildRemoved(ServerGameObject child) { }
		protected virtual void OnChildMoved(ServerGameObject child, IntPoint3D srcLoc, IntPoint3D dstLoc) { }

		protected virtual void OnEnvironmentChanged(ServerGameObject oldEnv, ServerGameObject newEnv) { }

		public bool MoveTo(ServerGameObject parent)
		{
			return MoveTo(parent, new IntPoint3D());
		}

		public bool MoveTo(ServerGameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.World.IsWritable);

			if (dst != null && !dst.OkToAddChild(this, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		public bool MoveTo(int x, int y, int z)
		{
			var p = new IntPoint3D(x, y, z);
			return MoveTo(this.Environment, p);
		}

		public bool MoveDir(Direction dir)
		{
			Debug.Assert(this.World.IsWritable);

			if (this.Environment == null)
				throw new Exception();

			var dst = this.Environment;
			var dstLoc = this.Location + dir;

			if (!dst.OkToMoveChild(this, dir, dstLoc))
				return false;

			MoveToLow(dst, dstLoc);

			return true;
		}

		void MoveToLow(ServerGameObject dst, IntPoint3D dstLoc)
		{
			Debug.Assert(this.IsInitialized);
			Debug.Assert(!this.IsDestructed);

			var src = this.Parent;
			var srcLoc = this.Location;

			if (src != dst)
			{
				if (src != null)
				{
					src.OnChildRemoved(this);
					src.m_children.Remove(this);
				}

				this.Parent = dst;
			}

			if (this.Location != dstLoc)
			{
				this.Location = dstLoc;
				if (dst != null && src == dst)
					dst.OnChildMoved(this, srcLoc, dstLoc);
			}

			if (src != dst)
			{
				if (dst != null)
				{
					dst.m_children.Add(this);
					dst.OnChildAdded(this);
				}
			}

			if (src != dst)
				OnEnvironmentChanged(src, dst);

			this.World.AddChange(new ObjectMoveChange(this, src, srcLoc, dst, dstLoc));
		}

		public override string ToString()
		{
			return String.Format("ServerGameObject({0})", this.ObjectID);
		}
	}

	public abstract class ServerGameObjectBuilder
	{
		public string Name { get; set; }
		public GameColor Color { get; set; }
		public SymbolID SymbolID { get; set; }
		public MaterialID MaterialID { get; set; }
	}
}
