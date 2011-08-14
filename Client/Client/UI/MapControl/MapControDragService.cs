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

		DragHelper m_dragHelper;

		public MapControlDragService(MasterMapControl mapControl)
		{
			m_mapControl = mapControl;

			m_dragHelper = new DragHelper(m_mapControl);
			m_dragHelper.DragStarted += OnDragStarted;
			m_dragHelper.DragEnded += OnDragEnded;
			m_dragHelper.Dragging += OnDragging;
			m_dragHelper.DragAborted += OnDragAborted;
		}

		public bool IsEnabled
		{
			get { return m_dragHelper.IsEnabled; }
			set { m_dragHelper.IsEnabled = value; }
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
