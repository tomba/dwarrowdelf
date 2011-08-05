using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace Dwarrowdelf.Client.UI
{
	class HoverTileInfo : INotifyPropertyChanged
	{
		MapControl m_mapControl;

		public Point MousePos { get; private set; }
		public IntPoint3D MapLocation { get; private set; }
		public IntPoint ScreenLocation { get; private set; }

		public HoverTileInfo(MapControl mapControl)
		{
			m_mapControl = mapControl;

			m_mapControl.TileLayoutChanged += OnTileLayoutChanged;
			m_mapControl.MouseMove += OnMouseMove;
			m_mapControl.ZChanged += OnZChanged;
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			var p = e.GetPosition(m_mapControl);
			UpdateHoverTileInfo(p);
		}

		void OnTileLayoutChanged(IntSize gridSize, double tileSize, Point centerPos)
		{
			var p = Mouse.GetPosition(m_mapControl);
			UpdateHoverTileInfo(p);
		}

		void OnZChanged(int z)
		{
			var p = Mouse.GetPosition(m_mapControl);
			UpdateHoverTileInfo(p);
		}

		void UpdateHoverTileInfo(Point p)
		{
			var sl = m_mapControl.ScreenPointToIntScreenLocation(p);
			var ml = m_mapControl.ScreenPointToMapLocation3D(p);

			if (p != this.MousePos)
			{
				this.MousePos = p;
				Notify("MousePos");
			}

			if (sl != this.ScreenLocation)
			{
				this.ScreenLocation = sl;
				Notify("ScreenLocation");
			}

			if (ml != this.MapLocation)
			{
				this.MapLocation = ml;
				Notify("MapLocation");
			}
		}

		void Notify(string name)
		{
			if (PropertyChanged != null)
				PropertyChanged(this, new PropertyChangedEventArgs(name));
		}

		public event PropertyChangedEventHandler PropertyChanged;
	}
}
