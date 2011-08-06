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
	public partial class ObjectInfoControl : UserControl
	{
		LivingInfoControl m_livingInfoControl;
		ItemInfoControl m_itemInfoControl;
		BuildingInfoControl m_buildingInfoControl;

		public ObjectInfoControl()
		{
			this.DataContextChanged += new DependencyPropertyChangedEventHandler(ObjectInfoControl_DataContextChanged);
		}

		void ObjectInfoControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			Control content;

			if (e.NewValue is Living)
			{
				if (m_livingInfoControl == null)
					m_livingInfoControl = new LivingInfoControl();

				content = m_livingInfoControl;
			}
			else if (e.NewValue is ItemObject)
			{
				if (m_itemInfoControl == null)
					m_itemInfoControl = new ItemInfoControl();

				content = m_itemInfoControl;
			}
			else if (e.NewValue is BuildingObject)
			{
				if (m_buildingInfoControl == null)
					m_buildingInfoControl = new BuildingInfoControl();

				content = m_buildingInfoControl;
			}
			else
			{
				content = null;
			}

			this.Content = content;
		}
	}
}
