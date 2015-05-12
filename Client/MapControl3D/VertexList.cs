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
		public int Capacity { get; private set; }

		public VertexList(int capacity)
		{
			this.Capacity = capacity;
		}

		public void Add(T data)
		{
			if (this.Data == null)
				this.Data = new T[this.Capacity];

			this.Data[this.Count++] = data;
		}

		public void Clear()
		{
			this.Count = 0;
		}
	}
}
