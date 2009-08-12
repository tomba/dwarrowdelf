using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	delegate void ObjectMoved(ServerGameObject o, Environment e, IntPoint l);

	abstract class ServerGameObject : GameObject
	{
		public int SymbolID { get; set; }

		public string Name { get; set; }
		public Environment Environment { get; private set; }
		public IntPoint Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public World World { get; protected set; }

		internal ServerGameObject(World world)
			: base(world.GetNewObjectID())
		{
			this.World = world;
			this.World.AddGameObject(this);
		}

		public bool MoveTo(Environment env, IntPoint l)
		{
			if (l.X < 0 || l.Y < 0 || l.X >= env.Width || l.Y >= env.Height)
				return false;

			if (!env.Area.Terrains[env.GetTerrain(l)].IsWalkable)
				return false;

			if (this.Environment != null)
				this.Environment.RemoveObject(this, this.Location);

			bool envChanged = this.Environment != env;

			Environment oldEnv = this.Environment;
			ObjectID oldMapID = ObjectID.NullObjectID;
			if (this.Environment != null)
				oldMapID = this.Environment.ObjectID;
			IntPoint oldLocation = this.Location;

			this.Environment = env;
			this.Location = l;
			env.AddObject(this, l);

			if (envChanged)
			{
				OnEnvironmentChanged(oldEnv, env);
				this.World.AddChange(new ObjectEnvironmentChange(this, oldMapID, oldLocation,
					this.Environment.ObjectID, l));
			}
			else
			{
				this.World.AddChange(new ObjectLocationChange(this, oldLocation, l));
			}

			return true;
		}

		protected virtual void OnEnvironmentChanged(Environment oldEnv, Environment newEnv) { }

		public bool MoveDir(Direction dir)
		{
			int x = 0;
			int y = 0;

			switch (dir)
			{
				case Direction.North: y = -1; break;
				case Direction.South: y = 1; break;
				case Direction.West: x = -1; break;
				case Direction.East: x = 1; break;
				case Direction.NorthWest: x = -1; y = -1; break;
				case Direction.SouthWest: x = -1; y = 1; break;
				case Direction.NorthEast: x = 1; y = -1; break;
				case Direction.SouthEast: x = 1; y = 1; break;
				default:
					throw new Exception();
			}

			IntPoint l = this.Location;
			l.X += x;
			l.Y += y;
			return MoveTo(this.Environment, l);
		}

	}
}
