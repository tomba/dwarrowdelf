using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public partial class World
	{
		ProcessableList<ServerUser> m_users = new ProcessableList<ServerUser>();
		[GameProperty]
		ProcessableList<Living> m_livings = new ProcessableList<Living>();

		internal void AddUser(ServerUser user)
		{
			VerifyAccess();
			m_users.Add(user);
			SignalWorld();
		}

		internal void RemoveUser(ServerUser user)
		{
			VerifyAccess();
			m_users.Remove(user);
			SignalWorld();
		}


		internal void AddLiving(Living living)
		{
			VerifyAccess();
			m_livings.Add(living);
			SignalWorld();
		}

		internal void RemoveLiving(Living living)
		{
			VerifyAccess();
			m_livings.Remove(living);
			SignalWorld();
		}
	}
}
