using SharpDX.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class FPSCounter : IGameUpdatable
	{
		GameCore m_game;
		int m_frameCount;
		TimeSpan m_fpsPrev;

		public FPSCounter(GameCore game)
		{
			m_game = game;
		}

		public void Update()
		{
			m_frameCount++;

			var time = m_game.Time.TotalTime;

			var diff = time - m_fpsPrev;

			if (diff.TotalMilliseconds >= 1000)
			{
				var fps = m_frameCount / diff.TotalSeconds;

				App.Current.MainWindow.Title = string.Format("{0} frames in {1:F2} ms = {2:F2} fps", m_frameCount, diff.TotalMilliseconds, fps);

				m_frameCount = 0;
				m_fpsPrev = time;
			}
		}
	}
}
