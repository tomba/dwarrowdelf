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

		IActor m_actorImpl;

		World m_world;

		internal ServerGameObject(World world)
			: base(world.GetNewObjectID())
		{
			m_world = world;
			m_world.AddGameObject(this);
			this.SymbolID = 3;
		}

		public void SetActor(IActor actor)
		{
			m_actorImpl = actor;
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

			this.Environment = level;
			this.Location = l;
			level.AddObject(this, l);

			if(envChanged)
				m_world.AddChange(new EnvironmentChange(this, this.Environment.ObjectID, l));
			else
				m_world.AddChange(new LocationChange(this, l));

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

		public int ViewRange { get { return 6; } }

		public bool Sees(Location l)
		{
			return true;
		}
	}
}
