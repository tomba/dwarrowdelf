using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Dwarrowdelf.Client.UI
{
	class MapControlDragService
	{
		MasterMapControl m_mapControl;

		Point m_mapTile;

		bool m_enabled;

		public MapControlDragService(MasterMapControl mapControl)
		{
			m_mapControl = mapControl;
		}

		public bool IsEnabled
		{
			get { return m_enabled; }
			set
			{
				if (m_enabled == value)
					return;

				if (m_enabled)
				{
					if (m_mapControl.IsMouseCaptured)
						m_mapControl.ReleaseMouseCapture();

					m_mapControl.MouseDown -= OnMouseDown;
					m_mapControl.MouseUp -= OnMouseUp;
					m_mapControl.GotMouseCapture -= OnGotMouseCapture;
					m_mapControl.LostMouseCapture -= OnLostMouseCapture;
				}

				m_enabled = value;

				if (m_enabled)
				{
					m_mapControl.MouseDown += OnMouseDown;
					m_mapControl.MouseUp += OnMouseUp;
					m_mapControl.GotMouseCapture += OnGotMouseCapture;
					m_mapControl.LostMouseCapture += OnLostMouseCapture;
				}
			}
		}

		void OnMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left)
				return;

			var p = e.GetPosition(m_mapControl);
			m_mapTile = m_mapControl.MapControl.ScreenPointToMapTile(p);

			m_mapControl.CaptureMouse();
		}

		void OnMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left || !m_mapControl.IsMouseCaptured)
				return;

			m_mapControl.ReleaseMouseCapture();
		}

		void OnGotMouseCapture(object sender, MouseEventArgs e)
		{
			m_mapControl.MouseMove += OnMouseMove;
		}

		void OnLostMouseCapture(object sender, MouseEventArgs e)
		{
			m_mapControl.MouseMove -= OnMouseMove;
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			var pos = e.GetPosition(m_mapControl);

			var v = m_mapControl.MapControl.MapTileToScreenPoint(m_mapTile) - pos;

			var sp = m_mapControl.MapControl.MapTileToScreenPoint(m_mapControl.CenterPos) + v;

			var mt = m_mapControl.MapControl.ScreenPointToMapTile(sp);

			m_mapControl.CenterPos = mt;
		}
	}
}
