using Dwarrowdelf;
using Dwarrowdelf.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class MovableObject
	{
		public static event Action<MovableObject> MovableMoved;

		public IntVector3 Position { get; private set; }
		public SymbolID SymbolID { get; private set; }
		public GameColor Color { get; private set; }

		public MovableObject(SymbolID symbol, GameColor color)
		{
			this.SymbolID = symbol;
			this.Color = color;
		}

		public void Move(IntVector3 p)
		{
			this.Position = p;
			if (MovableObject.MovableMoved != null)
				MovableObject.MovableMoved(this);
		}
	}
}
