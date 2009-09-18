using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Collections.Generic;

namespace MyGame
{
	public class ReadOnlyObservableKeyedCollection<K, T>
		: ReadOnlyCollection<T>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		ObservableKeyedCollection<K, T> m_collection;

		public ReadOnlyObservableKeyedCollection(ObservableKeyedCollection<K, T> collection)
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

		public T this[K key]
		{
			get { return m_collection[key]; }
		}

		public bool Contains(K key)
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

	public abstract class ObservableKeyedCollection<K, T> : KeyedCollection<K, T>,
		INotifyCollectionChanged, INotifyPropertyChanged
	{
		public ObservableKeyedCollection() : base(null, 0)
		{
		}

		protected override void InsertItem(int index, T item)
		{
			base.InsertItem(index, item);
			this.OnPropertyChanged("Count");
			this.OnPropertyChanged("Item[]");
			this.OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		}

		protected override void SetItem(int index, T item)
		{
			T oldItem = base[index];
			base.SetItem(index, item);
			this.OnPropertyChanged("Item[]");
			this.OnCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, item, index);
		}

		protected override void RemoveItem(int index)
		{
			T item = base[index];
			base.RemoveItem(index);
			this.OnPropertyChanged("Count");
			this.OnPropertyChanged("Item[]");
			this.OnCollectionChanged(NotifyCollectionChangedAction.Remove, item, index);
		}

		protected override void ClearItems()
		{
			base.ClearItems();
			this.OnPropertyChanged("Count");
			this.OnPropertyChanged("Item[]");
			this.OnCollectionReset();
		}

		protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, e);
		}

		void OnPropertyChanged(string propertyName)
		{
			this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
		}

		protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
		{
			if (this.CollectionChanged != null)
				this.CollectionChanged(this, e);
		}

		void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index)
		{
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index));
		}

		void OnCollectionChanged(NotifyCollectionChangedAction action, object item, int index, int oldIndex)
		{
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, item, index, oldIndex));
		}

		void OnCollectionChanged(NotifyCollectionChangedAction action, object oldItem, object newItem, int index)
		{
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, newItem, oldItem, index));
		}

		void OnCollectionReset()
		{
			this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}

		#region INotifyCollectionChanged Members

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		#endregion

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		#endregion
	}
}
