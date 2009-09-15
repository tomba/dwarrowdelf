using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	public struct EnvPos
	{
		public EnvPos(GameObject env, IntPoint3D p)
		{
			this.Environment = env;
			this.Position = p;
		}

		public GameObject Environment;
		public IntPoint3D Position;
	}
}
