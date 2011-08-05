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
	class MapControlToolTipService
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
			UpdateToolTip(pos);
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (!m_isToolTipEnabled)
				return;

			UpdateToolTip(e.GetPosition(m_mapControl));
		}

		void OnTileLayoutChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			if (!m_isToolTipEnabled)
				return;

			var pos = Mouse.GetPosition(m_mapControl);
			UpdateToolTip(pos);
		}

		void OnMouseLeave(object sender, MouseEventArgs e)
		{
			if (!m_isToolTipEnabled)
				return;
			
			CloseToolTip();
		}

		void CreateToolTip()
		{
			var toolTipContent = new UI.ObjectInfoControl();
			var tt = new ToolTip();
			tt.Content = toolTipContent;
			tt.IsOpen = false;
			tt.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
			tt.PlacementTarget = m_mapControl;
			tt.DataContext = null;
			m_toolTip = tt;
		}

		void UpdateToolTip(Point mousePos)
		{
			if (m_mapControl.Environment == null)
			{
				CloseToolTip();
				return;
			}

			var ml = m_mapControl.ScreenPointToMapLocation3D(mousePos);

			var ob = m_mapControl.Environment.GetFirstObject(ml);

			if (ob != null)
			{
				if (!m_tooltipMapLocation.HasValue || m_tooltipMapLocation != ml || ob != m_toolTip.DataContext)
				{
					m_toolTip.DataContext = ob;

					var rect = m_mapControl.MapRectToScreenPointRect(new IntRect(ml.ToIntPoint(), new IntSize(1, 1)));
					m_toolTip.PlacementRectangle = rect;

					m_toolTip.IsOpen = true;
					m_tooltipMapLocation = ml;
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
