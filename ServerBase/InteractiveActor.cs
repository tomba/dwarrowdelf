using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyGame
{
	class InteractiveActor : IActor
	{
		public InteractiveActor()
		{
		}

		#region IActor Members

		public bool IsInteractive
		{
			get { return true; }
		}

		public void DetermineAction()
		{
		}

		#endregion
	}
}
