using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace Dwarrowdelf
{
	public class ReadOnlyArray<T> : IEnumerable<T>, IEnumerable
	{
		T[] m_array;

		public ReadOnlyArray(T[] array)
		{
			m_array = array;
		}

		public T this[int idx]
		{
			get { return m_array[idx]; }
			set { m_array[idx] = value; }
		}

		public Enumerator GetEnumerator()
		{
			return new Enumerator(m_array);
		}

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
		{
			T[] m_array;
			int m_index;
			T m_current;

			internal Enumerator(T[] array)
			{
				m_array = array;
				m_index = 0;
				m_current = default(T);
			}

			public void Dispose()
			{
			}

			public T Current
			{
				get { return m_current; }
			}

			object IEnumerator.Current
			{
				get
				{
					if (m_index == 0 || m_index == m_array.Length + 1)
						throw new InvalidOperationException();

					return this.Current;
				}
			}

			public bool MoveNext()
			{
				if (m_index < m_array.Length)
				{
					m_current = m_array[m_index++];
					return true;
				}

				return MoveNextRare();
			}

			private bool MoveNextRare()
			{
				m_index = m_array.Length + 1;
				m_current = default(T);
				return false;
			}

			void IEnumerator.Reset()
			{
				m_index = 0;
				m_current = default(T);
			}
		}
	}
}
