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

namespace Dwarrowdelf.Client.UI
{
	sealed partial class ObjectInfoControl : ContentControl
	{
		public ObjectInfoControl()
		{
			InitializeComponent();
		}
	}

	sealed class GameObjectTemplateSelector : DataTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item == null)
				return null;

			var c = (ContentPresenter)container;

			if (item is ItemObject)
				return c.FindResource("itemTemplate") as DataTemplate;

			if (item is LivingObject)
				return c.FindResource("livingTemplate") as DataTemplate;

			if (item is Stockpile)
				return c.FindResource("stockpileTemplate") as DataTemplate;

			throw new Exception();
			/*
			string numberStr = item as string;

			if (numberStr != null)
			{
				int num;
				Window win = Application.Current.MainWindow;

				try
				{
					num = Convert.ToInt32(numberStr);
				}
				catch
				{
					return null;
				}

				// Select one of the DataTemplate objects, based on the 
				// value of the selected item in the ComboBox.
				if (num < 5)
				{
					return win.FindResource("numberTemplate") as DataTemplate;
				}
				else
				{
					return win.FindResource("largeNumberTemplate") as DataTemplate;

				}
			}
			 */
		}
	}
}
