using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace Dwarrowdelf.Client
{
	public interface ISelectable
	{
		bool? IsSelected { get; set; }
		event Action IsSelectedChanged;
	}

	public interface ISelectable<TValue> : ISelectable
	{
		TValue Value { get; }
	}

	public class SelectableValue<TValue> : ISelectable<TValue>, INotifyPropertyChanged
	{
		public TValue Value { get; private set; }
		public event Action IsSelectedChanged;

		public SelectableValue(TValue value)
		{
			this.Value = value;
			m_isSelected = false;
		}

		public SelectableValue(TValue value, bool selected)
		{
			this.Value = value;
			m_isSelected = selected;
		}

		bool? m_isSelected;

		public bool? IsSelected
		{
			get
			{
				return m_isSelected;
			}

			set
			{
				if (value == m_isSelected)
					return;

				m_isSelected = value;
				Notify("IsSelected");
				if (this.IsSelectedChanged != null)
					this.IsSelectedChanged();
			}
		}

		#region INotifyPropertyChanged Members

		public event PropertyChangedEventHandler PropertyChanged;

		void Notify(string property)
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(property));
		}

		#endregion
	}

	public class SelectableCollection<TValue, TItem> : SelectableValue<TValue> where TItem : ISelectable
	{
		bool m_ignoreChildEvents;

		public ObservableCollection<TItem> Items { get; private set; }

		public SelectableCollection(TValue value)
			: base(value)
		{
			this.Items = new ObservableCollection<TItem>();
			this.IsSelectedChanged += OnIsSelectedChanged;
			this.Items.CollectionChanged += Items_CollectionChanged;
		}

		public SelectableCollection(TValue value, IEnumerable<TItem> items)
			: base(value)
		{
			this.Items = new ObservableCollection<TItem>(items);
			this.IsSelectedChanged += OnIsSelectedChanged;
			this.Items.CollectionChanged += Items_CollectionChanged;

			foreach (var item in this.Items)
				item.IsSelectedChanged += ItemIsSelectedChanged;
		}

		void OnIsSelectedChanged()
		{
			if (this.IsSelected.HasValue)
			{
				m_ignoreChildEvents = true;
				foreach (var m in this.Items)
					m.IsSelected = this.IsSelected.Value;
				m_ignoreChildEvents = false;
			}
		}

		void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (ISelectable item in e.NewItems)
						item.IsSelectedChanged += ItemIsSelectedChanged;

					break;

				case NotifyCollectionChangedAction.Remove:
					foreach (ISelectable item in e.OldItems)
						item.IsSelectedChanged -= ItemIsSelectedChanged;

					break;

				default:
					throw new Exception();
			}

			CheckCollection();
		}

		void ItemIsSelectedChanged()
		{
			if (m_ignoreChildEvents)
				return;

			CheckCollection();
		}

		void CheckCollection()
		{
			int undetermined = 0;
			int selected = 0;
			int unselected = 0;

			foreach (var item in this.Items)
			{
				if (item.IsSelected.HasValue == false)
				{
					undetermined++;
					break;
				}
				else if (item.IsSelected.Value == true)
				{
					selected++;
					if (unselected > 0)
						break;
				}
				else
				{
					unselected++;
					if (selected > 0)
						break;
				}
			}

			if (undetermined > 0 || (selected > 0 && unselected > 0))
				this.IsSelected = null;
			else if (selected == this.Items.Count)
				this.IsSelected = true;
			else
			{
				Debug.Assert(unselected == this.Items.Count);
				this.IsSelected = false;
			}
		}
	}
}
