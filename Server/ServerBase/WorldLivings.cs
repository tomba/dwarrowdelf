using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dwarrowdelf.Server
{
	public partial class World
	{
		ProcessableList<ServerConnection> m_connections = new ProcessableList<ServerConnection>();
		ProcessableList<User> m_users = new ProcessableList<User>();
		ProcessableList<Living> m_livings = new ProcessableList<Living>();

		// thread safe
		internal void AddConnection(ServerConnection connection)
		{
			lock (m_connections.AddList)
				m_connections.Add(connection);

			SignalWorld();
		}

		// thread safe
		internal void RemoveConnection(ServerConnection connection)
		{
			lock (m_connections.RemoveList)
				m_connections.Remove(connection);

			SignalWorld();
		}

		void ProcessConnectionAdds()
		{
			lock (m_connections.AddList)
				m_connections.ProcessAddItems();
		}

		void ProcessConnectionRemoves()
		{
			lock (m_connections.RemoveList)
				m_connections.ProcessRemoveItems();
		}


		internal void AddUser(User user)
		{
			VerifyAccess();
			m_users.Add(user);
			SignalWorld();
		}

		internal void RemoveUser(User user)
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
