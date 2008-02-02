using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;


namespace MyGame
{
	delegate void ObjectMoved(ServerGameObject o, MapLevel e, Location l);

	interface IActor
	{
		void EnqueueAction(GameAction action);
		GameAction DequeueAction();
		GameAction PeekAction();

		event Action ActionQueuedEvent;
	}

	class ServerGameObject : GameObject
	{
		public int SymbolID { get; protected set; }

		public MapLevel Environment { get; private set; }
		public Location Location { get; private set; }
		public int X { get { return this.Location.X; } }
		public int Y { get { return this.Location.Y; } }

		public event ObjectMoved ObjectMoved;

		IActor m_actorImpl;

		public ServerGameObject()
			: base(World.CurrentWorld.GetNewObjectID())
		{
			this.SymbolID = 3;
			World.CurrentWorld.AddGameObject(this);
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
				World.CurrentWorld.AddChange(new EnvironmentChange(this, this.Environment.ObjectID, l));
			else
				World.CurrentWorld.AddChange(new LocationChange(this, l));

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
			if (Math.Abs(l.X - this.X) > this.ViewRange || Math.Abs(l.Y - this.Y) > this.ViewRange)
				return false;

			double dx = this.Location.X - l.X;
			double dy = this.Location.Y - l.Y;

			if (dx == 0 && dy == 0)
				return true;

			if (Math.Abs(dx) > Math.Abs(dy))
			{
				double k = dy / dx;

				int x1 = Math.Min(this.Location.X, l.X);
				int x2 = Math.Max(this.Location.X, l.X);
				for (int x = x1; x <= x2; x++)
				{
					int y = (int)((x-this.Location.X) * k + this.Location.Y);

					Debug.Assert(x >= 0 && y >= 0 && x < this.Environment.Width && y < this.Environment.Height);

					int terrain = this.Environment.GetTerrain(new Location(x, y));
					if (!this.Environment.Area.Terrains[terrain].IsWalkable)
					{
						if (x == x2)
							return true;
						else
							return false;
					}					
				}
			}
			else
			{
				double k = dx / dy;

				int y1 = Math.Min(this.Location.Y, l.Y);
				int y2 = Math.Max(this.Location.Y, l.Y);
				for (int y = y1; y <= y2; y++)
				{
					int x = (int)((y - this.Location.Y) * k + this.Location.X);

					Debug.Assert(x >= 0 && y >= 0 && x < this.Environment.Width && y < this.Environment.Height);

					int terrain = this.Environment.GetTerrain(new Location(x, y));
					if (!this.Environment.Area.Terrains[terrain].IsWalkable)
					{
						if (y == y2)
							return true;
						else
							return false;
					}
				}
			}

			return true;
		}
	}
}
