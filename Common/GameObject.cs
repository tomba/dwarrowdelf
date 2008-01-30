using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public interface IIdentifiable
	{
		ObjectID ObjectID { get; }
	}

	public abstract class GameObject : IIdentifiable
	{
		public ObjectID ObjectID { get; private set; }

		protected GameObject(ObjectID objectID)
		{
			this.ObjectID = objectID;
		}
	}
}
