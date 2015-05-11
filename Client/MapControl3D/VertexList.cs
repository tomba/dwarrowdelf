using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class VertexList<T>
	{
		public T[] Data { get; private set; }
		public int Count { get; private set; }
		int m_maxSize;

		public VertexList(int size)
		{
			m_maxSize = size;
		}

		public void Add(T data)
		{
			if (this.Data == null)
				this.Data = new T[m_maxSize];

			this.Data[this.Count++] = data;
		}

		public void Clear()
		{
			this.Count = 0;
		}
	}
}
