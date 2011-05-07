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
		[GameProperty]
		public ServerGameObject Parent { get; private set; }
		public Environment Environment { get { return this.Parent as Environment; } }
		IEnvironment IGameObject.Environment { get { return this.Parent as IEnvironment; } }
		[GameProperty("Inventory")]
		KeyedObjectCollection m_children;
		public ReadOnlyCollection<ServerGameObject> Inventory { get; private set; }

		[GameProperty]
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

		[OnGameDeserialized]
		void OnDeserialized()
		{
			this.Inventory = new ReadOnlyCollection<ServerGameObject>(m_children);
		}

		public override void Destruct()
		{
			this.MoveTo(null);
			base.Destruct();
		}

		static readonly PropertyDefinition NameProperty = RegisterProperty(typeof(ServerGameObject), typeof(string), PropertyID.Name, PropertyVisibility.Public, null);
		public string Name
		{
			get { return (string)GetValue(NameProperty); }
			set { SetValue(NameProperty, value); }
		}

		static readonly PropertyDefinition ColorProperty = RegisterProperty(typeof(ServerGameObject), typeof(GameColor), PropertyID.Color, PropertyVisibility.Public, new GameColor());
		public GameColor Color
		{
			get { return (GameColor)GetValue(ColorProperty); }
			set { SetValue(ColorProperty, value); }
		}

		static readonly PropertyDefinition SymbolIDProperty = RegisterProperty(typeof(ServerGameObject), typeof(SymbolID), PropertyID.SymbolID, PropertyVisibility.Public, SymbolID.Undefined);
		public SymbolID SymbolID
		{
			get { return (SymbolID)GetValue(SymbolIDProperty); }
			set { SetValue(SymbolIDProperty, value); }
		}

		static readonly PropertyDefinition MaterialIDProperty = RegisterProperty(typeof(ServerGameObject), typeof(MaterialID), PropertyID.MaterialID, PropertyVisibility.Public, MaterialID.Undefined);
		public MaterialID MaterialID
		{
			get { return (MaterialID)GetValue(MaterialIDProperty); }
			set { SetValue(MaterialIDProperty, value); this.Color = Materials.GetMaterial(value).Color; }
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
}
