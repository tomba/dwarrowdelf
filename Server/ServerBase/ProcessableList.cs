using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Dwarrowdelf.Server
{
	class ProcessableList<T>
	{
		List<T> m_list = new List<T>();
		List<T> m_addList = new List<T>();
		List<T> m_removeList = new List<T>();

		public ReadOnlyCollection<T> List { get; private set; }
		public ReadOnlyCollection<T> AddList { get; private set; }
		public ReadOnlyCollection<T> RemoveList { get; private set; }

		public ProcessableList()
		{
			this.List = m_list.AsReadOnly();
			this.AddList = m_addList.AsReadOnly();
			this.RemoveList = m_removeList.AsReadOnly();
		}

		public void Add(T item)
		{
			Debug.Assert(!m_list.Contains(item));
			m_addList.Add(item);
		}

		public void Remove(T item)
		{
			Debug.Assert(m_list.Contains(item) || m_addList.Contains(item));

			if (m_addList.Contains(item))
				m_addList.Remove(item);
			else
				m_removeList.Add(item);
		}

		public void Process()
		{
			ProcessRemoveItems();
			ProcessAddItems();
		}

		public void ProcessAddItems()
		{
			foreach (var item in m_addList)
			{
				Debug.Assert(!m_list.Contains(item));
				m_list.Add(item);
			}

			m_addList.Clear();
		}

		public void ProcessRemoveItems()
		{
			foreach (var item in m_removeList)
			{
				bool removed = m_list.Remove(item);
				Debug.Assert(removed);
			}

			m_removeList.Clear();
		}
	}
}
