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
				if (m_enabled)
				{
					m_mapControl.MapControl.DragStarted -= OnDragStarted;
					m_mapControl.MapControl.DragEnded -= OnDragEnded;
					m_mapControl.MapControl.Dragging -= OnDragging;
					m_mapControl.MapControl.DragAborted -= OnDragAborted;
				}

				m_enabled = value;

				if (m_enabled)
				{
					m_mapControl.MapControl.DragStarted += OnDragStarted;
					m_mapControl.MapControl.DragEnded += OnDragEnded;
					m_mapControl.MapControl.Dragging += OnDragging;
					m_mapControl.MapControl.DragAborted += OnDragAborted;
				}
			}
		}

		void OnDragStarted(Point pos)
		{
			m_mapTile = m_mapControl.MapControl.ScreenPointToMapTile(pos);
			m_mapControl.Cursor = Cursors.ScrollAll;
		}

		void OnDragEnded(Point pos)
		{
			m_mapControl.ClearValue(UserControl.CursorProperty);
		}

		void OnDragging(Point pos)
		{
			var v = m_mapControl.MapControl.MapTileToScreenPoint(m_mapTile) - pos;

			var sp = m_mapControl.MapControl.MapTileToScreenPoint(m_mapControl.CenterPos) + v;

			var mt = m_mapControl.MapControl.ScreenPointToMapTile(sp);

			m_mapControl.CenterPos = mt;
		}

		void OnDragAborted()
		{
			m_mapControl.ClearValue(UserControl.CursorProperty);
		}
	}
}
