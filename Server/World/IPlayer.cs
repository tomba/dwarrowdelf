using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public interface IPlayer
	{
		void Send(Dwarrowdelf.Messages.ClientMessage message);
		IVisionTracker GetVisionTracker(EnvironmentObject env);
		ObjectVisibility GetObjectVisibility(BaseObject ob);
	}

	public interface IVisionTracker
	{
		bool Sees(IntVector3 p);
	}
}
