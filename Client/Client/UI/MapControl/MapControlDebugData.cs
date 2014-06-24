using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Dwarrowdelf.Client.UI
{
	public class MapControlDebugData : INotifyPropertyChanged
	{
		public static MapControlDebugData Data { get; private set; }

		static MapControlDebugData()
		{
			Data = new MapControlDebugData();
		}

		public Point ScreenPos { get; set; }
		public Point ScreenTile { get; set; }
		public Point MapTile { get; set; }
		public IntVector3 MapLocation { get; set; }

		public event PropertyChangedEventHandler PropertyChanged;

		public void Update()
		{
			if (this.PropertyChanged != null)
				this.PropertyChanged(this, new PropertyChangedEventArgs(""));
		}
	}
}
