using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class GameTime
	{
		public TimeSpan TotalTime { get; private set; }
		public TimeSpan FrameTime { get; private set; }
		public int FrameCount { get; private set; }

		public void Update(TimeSpan totalTime, TimeSpan frameTime)
		{
			this.TotalTime = totalTime;
			this.FrameTime = frameTime;
			FrameCount++;
		}
	}
}
