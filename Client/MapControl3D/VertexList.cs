using System;

namespace Dwarrowdelf.Client
{
	/// <summary>
	/// A very simple List<T> which gives direct access to the underlying array
	/// </summary>
	class VertexList<T>
	{
		public T[] Data { get; private set; }
		public int Count { get; private set; }
		public int Capacity { get; private set; }

		public VertexList(int capacity)
		{
			this.Capacity = capacity;
			this.Data = new T[this.Capacity];
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
