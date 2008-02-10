using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	delegate void ObjectMoved(ServerGameObject o, MapLevel e, Location l);

	class ServerGameObject : GameObject
	{
		public int SymbolID { get; protected set; }

		public MapLevel Environment { get; private set; }
		public Location Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		public World World { get; protected set; }

		internal ServerGameObject(World world)
			: base(world.GetNewObjectID())
		{
			this.World = world;
			this.World.AddGameObject(this);
			this.SymbolID = 3;
		}

		public bool MoveTo(MapLevel level, Location l)
		{
			if (l.X < 0 || l.Y < 0 || l.X >= level.Width || l.Y >= level.Height)
				return false;

			if (!level.Area.Terrains[level.GetTerrain(l)].IsWalkable)
				return false;

			if (this.Environment != null)
				this.Environment.RemoveObject(this, this.Location);

			bool envChanged = this.Environment != level;

			Location oldLocation = this.Location;

			this.Environment = level;
			this.Location = l;
			level.AddObject(this, l);

			if(envChanged)
				this.World.AddChange(new EnvironmentChange(this, this.Environment.ObjectID, l));
			else
				this.World.AddChange(new LocationChange(this, oldLocation, l));

			if (ObjectMoved != null)
				ObjectMoved(this, level, l);

			return true;
		}

		public bool MoveDir(Direction dir)
		{
			int x = 0;
			int y = 0;

			switch (dir)
			{
				case Direction.Up: y = -1; break;
				case Direction.Down: y = 1; break;
				case Direction.Left: x = -1; break;
				case Direction.Right: x = 1; break;
				case Direction.UpLeft: x = -1; y = -1; break;
				case Direction.DownLeft: x = -1; y = 1; break;
				case Direction.UpRight: x = 1; y = -1; break;
				case Direction.DownRight: x = 1; y = 1; break;
				default:
					throw new Exception();
			}

			Location l = this.Location;
			l.X += x;
			l.Y += y;
			return MoveTo(this.Environment, l);
		}

	}
}
