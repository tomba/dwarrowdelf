using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class VertexList<T>
	{
		public T[] Data { get; private set; }
		public int Count { get; private set; }

		public VertexList(int size)
		{
			this.Data = new T[size];
			this.Count = 0;
		}

		public void Add(T data)
		{
			this.Data[this.Count++] = data;
		}

		public void Clear()
		{
			this.Count = 0;
		}
	}
}
