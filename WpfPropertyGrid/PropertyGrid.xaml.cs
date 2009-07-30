using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace WpfPropertyGrid
{
	public partial class PropertyGrid : UserControl
	{
		PropertyList m_pl;
		object m_instance;

		public PropertyGrid()
		{
			InitializeComponent();

			m_pl = new PropertyList();
			m_listView.ItemsSource = m_pl;
		}

		public object Instance
		{
			get { return m_instance; }
			set
			{
				m_instance = value;
				RecreateList();
			}
		}

		void RecreateList()
		{
			m_pl.Clear();

			if (m_instance == null)
				return;

			foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(m_instance))
			{
				if (!property.IsBrowsable)
					continue; //could also check for browsableattribute, but this one's shorter

				PropertyItem p = new PropertyItem(m_instance, property);
				m_pl.Add(p);
			}
		}

		private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			TextBox tb = (TextBox)sender;
			PropertyItem pi = (PropertyItem)tb.Tag;
			string text = tb.Text;

			if (pi.IsReadOnly)
				return;

			if (tb.Text == pi.Value)
				return;
			
			object value;
			Type t = pi.PropertyDescriptor.PropertyType;

			try
			{
				if (pi.Converter != null)
					value = pi.Converter.ConvertFromString(text);
				else
					value = Convert.ChangeType(text, t);

				pi.PropertyDescriptor.SetValue(pi.Instance, value);
			}
			catch (Exception ex)
			{
				tb.Text = pi.Value;
				MessageBox.Show(ex.Message);
			}
		}

		private void TextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				TextBox tb = (TextBox)sender;
				PropertyItem pi = (PropertyItem)tb.Tag;

				tb.Text = pi.Value;
				e.Handled = true;
			}
		}
	}
}
