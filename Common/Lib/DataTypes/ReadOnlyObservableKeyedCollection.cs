using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Dwarrowdelf
{
	public abstract class ReadOnlyObservableKeyedCollection<TKey, TValue> : ReadOnlyCollection<TValue>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		ObservableKeyedCollection<TKey, TValue> m_collection;

		protected ReadOnlyObservableKeyedCollection(ObservableKeyedCollection<TKey, TValue> collection)
			: base(collection)
		{
			m_collection = collection;
			m_collection.CollectionChanged += new NotifyCollectionChangedEventHandler(m_collection_CollectionChanged);
			m_collection.PropertyChanged += new PropertyChangedEventHandler(m_collection_PropertyChanged);
		}

		void m_collection_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(sender, e);
		}

		void m_collection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (this.CollectionChanged != null)
				this.CollectionChanged(sender, e);
		}

		public TValue this[TKey key]
		{
			get { return m_collection[key]; }
		}

		public bool Contains(TKey key)
		{
			return m_collection.Contains(key);
		}

		#region INotifyCollectionChanged Members

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
