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
	sealed class MapControlDragService
	{
		MasterMapControl m_mapControl;

		Point m_contentTile;

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
					m_mapControl.DragStarted -= OnDragStarted;
					m_mapControl.DragEnded -= OnDragEnded;
					m_mapControl.Dragging -= OnDragging;
					m_mapControl.DragAborted -= OnDragAborted;
				}

				m_enabled = value;

				if (m_enabled)
				{
					m_mapControl.DragStarted += OnDragStarted;
					m_mapControl.DragEnded += OnDragEnded;
					m_mapControl.Dragging += OnDragging;
					m_mapControl.DragAborted += OnDragAborted;
				}
			}
		}

		void OnDragStarted(Point pos)
		{
			m_contentTile = m_mapControl.ScreenPointToContentTile(pos);
			m_mapControl.Cursor = Cursors.ScrollAll;
		}

		void OnDragEnded(Point pos)
		{
			m_mapControl.ClearValue(UserControl.CursorProperty);
		}

		void OnDragging(Point pos)
		{
			var v = m_mapControl.ContentTileToScreenPoint(m_contentTile) - pos;

			var sp = m_mapControl.ContentTileToScreenPoint(m_mapControl.CenterPos) + v;

			var ct = m_mapControl.ScreenPointToContentTile(sp);

			m_mapControl.CenterPos = ct;
		}

		void OnDragAborted()
		{
			m_mapControl.ClearValue(UserControl.CursorProperty);
		}
	}
}
