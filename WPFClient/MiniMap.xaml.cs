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
using System.Windows.Shapes;

namespace MyGame
{
	/// <summary>
	/// Interaction logic for MiniMap.xaml
	/// </summary>
	public partial class MiniMap : Window
	{
		ClientGameObject m_followObject;

		public MiniMap()
		{
			InitializeComponent();
		}

		protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
		{
			this.FollowObject = null;
			base.OnClosing(e);
		}

		internal ClientGameObject FollowObject
		{
			get { return m_followObject; }

			set
			{
				if (m_followObject != null)
					m_followObject.ObjectMoved -= FollowedObjectMoved;

				m_followObject = value;

				if (m_followObject != null)
				{
					m_followObject.ObjectMoved += FollowedObjectMoved;
					FollowedObjectMoved(m_followObject.Environment, m_followObject.Location);
				}
			}
		}

		void FollowedObjectMoved(ClientGameObject e, IntPoint3D l)
		{
			Environment env = e as Environment;

			map.Environment = env;
			map.CenterPos = new IntPoint(l.X, l.Y);
		}
	}
}
