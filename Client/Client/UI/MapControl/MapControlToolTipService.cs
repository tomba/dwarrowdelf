using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	sealed class MapControlToolTipService
	{
		MapControl m_mapControl;

		IntPoint3D? m_tooltipMapLocation;
		ToolTip m_toolTip;
		bool m_isToolTipEnabled;

		public MapControlToolTipService(MapControl mapControl)
		{
			m_mapControl = mapControl;

			CreateToolTip();
		}

		public bool IsToolTipEnabled
		{
			get { return m_isToolTipEnabled; }

			set
			{
				if (value == m_isToolTipEnabled)
					return;

				if (value == true)
				{
					m_mapControl.MouseLeave += OnMouseLeave;
					m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
					m_mapControl.MouseMove += OnMouseMove;
					m_mapControl.ZChanged += OnZChanged;
				}
				else
				{
					m_mapControl.MouseLeave -= OnMouseLeave;
					m_mapControl.TileLayoutChanged -= OnTileLayoutChanged;
					m_mapControl.MouseMove -= OnMouseMove;
					m_mapControl.ZChanged -= OnZChanged;

					CloseToolTip();
				}

				m_isToolTipEnabled = value;
			}
		}

		void OnZChanged(int z)
		{
			Point pos = Mouse.GetPosition(m_mapControl);
			UpdateToolTip(pos, false);
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!m_isToolTipEnabled)
				return;

			UpdateToolTip(e.GetPosition(m_mapControl), true);
		}

		void OnTileLayoutChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			if (!m_isToolTipEnabled)
				return;

			var pos = Mouse.GetPosition(m_mapControl);
			UpdateToolTip(pos, false);
		}

		void OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (!m_isToolTipEnabled)
				return;

			CloseToolTip();
		}

		void CreateToolTip()
		{
			var tt = new ToolTip();
			tt.Content = new ObjectInfoControl();
			tt.IsOpen = false;
			tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
			tt.PlacementTarget = m_mapControl;
			m_toolTip = tt;
		}

		void UpdateToolTip(Point mousePos, bool isMouseMove)
		{
			if (m_mapControl.Environment == null)
			{
				CloseToolTip();
				return;
			}

			if (!m_mapControl.IsMouseOver)
			{
				CloseToolTip();
				return;
			}

			var ml = m_mapControl.ScreenPointToMapLocation(mousePos);

			object ob;

			ob = m_mapControl.Environment.GetFirstObject(ml);

			if (ob == null)
				ob = m_mapControl.Environment.GetElementAt(ml);

			if (ob != null)
			{
				if (!m_tooltipMapLocation.HasValue || m_tooltipMapLocation != ml || ob != m_toolTip.DataContext)
				{
					// open a new tooltip only if the user has moved the mouse to this location
					if (isMouseMove)
					{
						m_toolTip.DataContext = ob;

						var rect = m_mapControl.MapRectToScreenPointRect(new IntRect(ml.ToIntPoint(), new IntSize(1, 1)));
						m_toolTip.PlacementRectangle = rect;

						m_toolTip.IsOpen = true;
						m_tooltipMapLocation = ml;
					}
					else
					{
						CloseToolTip();
					}
				}
			}
			else
			{
				CloseToolTip();
			}
		}

		void CloseToolTip()
		{
			m_toolTip.IsOpen = false;
			m_tooltipMapLocation = null;
		}
	}
}
