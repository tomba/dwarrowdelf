using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class BufferPool<T> where T : class
	{
		ConcurrentStack<T> m_stack;
		Func<T> m_constr;

		public BufferPool(Func<T> constr)
		{
			m_stack = new ConcurrentStack<T>();
			m_constr = constr;
		}

		public T Take()
		{
			T val;

			if (m_stack.TryPop(out val) == false)
				return m_constr();
			else
				return val;
		}

		public void Return(T ob)
		{
			m_stack.Push(ob);
		}
	}

	class BlockingBufferPool<T> where T : class
	{
		BlockingCollection<T> m_stack;

		public BlockingBufferPool(int count, Func<T> constr)
		{
			var stack = new ConcurrentStack<T>();

			for (int i = 0; i < count; ++i)
				stack.Push(constr());

			m_stack = new BlockingCollection<T>(stack);
		}

		public T Take()
		{
			return m_stack.Take();
		}

		public void Return(T ob)
		{
			m_stack.Add(ob);
		}

		public int Count { get { return m_stack.Count; } }
	}
}
