using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace MyGame.Server
{
	public partial class World
	{
		List<ServerConnection> m_userList = new List<ServerConnection>();

		List<Living> m_livingList = new List<Living>();

		List<Living> m_addLivingList = new List<Living>();
		List<Living> m_removeLivingList = new List<Living>();

		// thread safe
		internal void AddUser(ServerConnection user)
		{
			lock (m_userList)
				m_userList.Add(user);

			SignalWorld();
		}

		// thread safe
		internal void RemoveUser(ServerConnection user)
		{
			lock (m_userList)
				m_userList.Remove(user);

			SignalWorld();
		}

		bool HasUsers
		{
			get
			{
				lock (m_userList)
					return m_userList.Count > 0;
			}
		}



		// thread safe
		internal void AddLiving(Living living)
		{
			lock (m_addLivingList)
				m_addLivingList.Add(living);

			SignalWorld();
		}

		bool HasAddLivings
		{
			get
			{
				lock (m_addLivingList)
					return m_addLivingList.Count > 0;
			}
		}

		void ProcessAddLivingList()
		{
			VerifyAccess();

			lock (m_addLivingList)
			{
				if (m_addLivingList.Count > 0)
					MyDebug.WriteLine("Processing {0} add livings", m_addLivingList.Count);
				foreach (var living in m_addLivingList)
				{
					Debug.Assert(!m_livingList.Contains(living));
					m_livingList.Add(living);
				}

				m_addLivingList.Clear();
			}
		}

		// thread safe
		internal void RemoveLiving(Living living)
		{
			lock (m_removeLivingList)
				m_removeLivingList.Add(living);

			SignalWorld();
		}

		bool HasRemoveLivings
		{
			get
			{
				lock (m_removeLivingList)
					return m_removeLivingList.Count > 0;
			}
		}

		bool RemoveLivingListContains(Living living)
		{
			lock (m_removeLivingList)
			{
				return m_removeLivingList.Contains(living);
			}
		}

		void ProcessRemoveLivingList()
		{
			VerifyAccess();

			lock (m_removeLivingList)
			{
				if (m_removeLivingList.Count > 0)
					MyDebug.WriteLine("Processing {0} remove livings", m_removeLivingList.Count);
				foreach (var living in m_removeLivingList)
				{
					bool removed = m_livingList.Remove(living);
					Debug.Assert(removed);
				}

				m_removeLivingList.Clear();
			}
		}
	}
}
