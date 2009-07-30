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
		public int SymbolID { get; set; }

		public string Name { get; set; }
		public MapLevel Environment { get; private set; }
		public Location Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public World World { get; protected set; }

		internal ServerGameObject(World world)
			: base(world.GetNewObjectID())
		{
			this.World = world;
			this.World.AddGameObject(this);
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

			ObjectID oldMapID = ObjectID.NullObjectID;
			if (this.Environment != null)
				oldMapID = this.Environment.ObjectID;
			Location oldLocation = this.Location;

			this.Environment = level;
			this.Location = l;
			level.AddObject(this, l);

			if(envChanged)
				this.World.AddChange(new ObjectEnvironmentChange(this, oldMapID, oldLocation,
					this.Environment.ObjectID, l));
			else
				this.World.AddChange(new ObjectLocationChange(this, oldLocation, l));

			return true;
		}

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

			Location l = this.Location;
			l.X += x;
			l.Y += y;
			return MoveTo(this.Environment, l);
		}

	}
}
