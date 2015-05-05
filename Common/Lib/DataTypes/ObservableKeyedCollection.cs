using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Dwarrowdelf
{
	public abstract class ObservableKeyedCollection<TKey, TValue> : KeyedCollection<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
	{
		protected ObservableKeyedCollection()
			: base(null, 20)
		{
		}

		protected override void InsertItem(int index, TValue item)
		{
			base.InsertItem(index, item);
			this.OnPropertyChanged("Count");
			this.OnPropertyChanged("Item[]");
			this.OnCollectionChanged(NotifyCollectionChangedAction.Add, item, index);
		}

		protected override void SetItem(int index, TValue item)
		{
			TValue oldItem = base[index];
			base.SetItem(index, item);
			this.OnPropertyChanged("Item[]");
			this.OnCollectionChanged(NotifyCollectionChangedAction.Replace, oldItem, item, index);
		}

		protected override void RemoveItem(int index)
		{
			TValue item = base[index];
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
