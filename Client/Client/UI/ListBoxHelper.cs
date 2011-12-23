using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace Dwarrowdelf.Client
{
	public sealed class ListBoxHelper
	{
		#region SelectedItems

		public static readonly DependencyProperty SelectedItemsProperty =
			DependencyProperty.RegisterAttached("SelectedItems", typeof(IList), typeof(ListBoxHelper),
				new FrameworkPropertyMetadata((IList)null, new PropertyChangedCallback(OnSelectedItemsChanged)));

		public static IList GetSelectedItems(DependencyObject d)
		{
			return (IList)d.GetValue(SelectedItemsProperty);
		}

		public static void SetSelectedItems(DependencyObject d, IList value)
		{
			d.SetValue(SelectedItemsProperty, value);
		}

		static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var listBox = (ListBox)d;

			listBox.SelectionChanged -= ReSetSelectedItems;

			IList selectedItems = GetSelectedItems(listBox);
			listBox.UnselectAll();
			if (selectedItems != null)
			{
				foreach (var item in selectedItems)
					listBox.SelectedItems.Add(item);
			}

			listBox.SelectionChanged += ReSetSelectedItems;
		}

		#endregion

		static void ReSetSelectedItems(object sender, SelectionChangedEventArgs e)
		{
			var listBox = (ListBox)sender;
			IList selectedItems = GetSelectedItems(listBox);

			if (selectedItems == null)
				return;

			selectedItems.Clear();
			if (listBox.SelectedItems != null)
			{
				foreach (var item in listBox.SelectedItems)
					selectedItems.Add(item);
			}
		}
	}
}
