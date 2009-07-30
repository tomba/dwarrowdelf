using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace WpfPropertyGrid
{
	class PropertyList : ObservableCollection<PropertyItem> { }

	class PropertyItem : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public object Instance { get; set; }
		public PropertyDescriptor PropertyDescriptor { get; set; }
		public TypeConverter Converter { get; set; }

		string m_name;
		string m_value;

		public PropertyItem(object instance, PropertyDescriptor property)
		{
			this.Instance = instance;
			this.PropertyDescriptor = property;

			this.Converter = TypeDescriptor.GetConverter(property.PropertyType);

			m_name = property.Name;

			object value = property.GetValue(instance);

			if (value == null)
				m_value = "null";
			else if (Converter != null)
				m_value = Converter.ConvertToString(value);
			else
				m_value = value.ToString();

			this.IsReadOnly = property.IsReadOnly;
		}

		public bool IsReadOnly
		{
			get;
			set;
		}

		protected void Notify(string propName)
		{
			if (this.PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(propName));
		}

		public string Name
		{
			get { return m_name; }
			set
			{
				if (m_name == value)
					return;
				this.m_name = value;
				Notify("Name");
			}
		}

		public string Value
		{
			get { return m_value; }
			set
			{
				if (m_value == value)
					return;
				this.m_value = value;
				Notify("Value");
			}
		}
	}
}
