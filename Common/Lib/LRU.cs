using System;
using System.Collections.Generic;

namespace Dwarrowdelf
{
	public sealed class LRUCache<TKey, TValue>
	{
		int m_capacity;
		Dictionary<TKey, LinkedListNode<LRUCacheItem>> m_cacheMap;
		LinkedList<LRUCacheItem> m_lruList;

		public LRUCache(int capacity)
		{
			m_capacity = capacity;
			m_cacheMap = new Dictionary<TKey, LinkedListNode<LRUCacheItem>>(capacity);
			m_lruList = new LinkedList<LRUCacheItem>();
		}

		public bool TryGet(TKey key, out TValue value)
		{
			LinkedListNode<LRUCacheItem> node;

			if (m_cacheMap.TryGetValue(key, out node))
			{
				value = node.Value.Value;
				m_lruList.Remove(node);
				m_lruList.AddLast(node);
				return true;
			}
			else
			{
				value = default(TValue);
				return false;
			}
		}

		public void Add(TKey key, TValue val)
		{
			if (m_cacheMap.Count >= m_capacity)
			{
				var first = m_lruList.First;
				m_lruList.RemoveFirst();
				m_cacheMap.Remove(first.Value.Key);
			}

			var cacheItem = new LRUCacheItem(key, val);
			var node = new LinkedListNode<LRUCacheItem>(cacheItem);
			m_lruList.AddLast(node);
			m_cacheMap.Add(key, node);
		}

		class LRUCacheItem
		{
			public TKey Key;
			public TValue Value;

			public LRUCacheItem(TKey k, TValue v)
			{
				this.Key = k;
				this.Value = v;
			}
		}
	}
}