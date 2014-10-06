using SharpDX.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class FPSCounterSystem : GameSystem
	{
		Action<string> m_cb;

		TimeSpan m_lastUpdate;
		int m_lastFrameCount;
		double m_min, m_max;

		public FPSCounterSystem(Game game, Action<string> cb)
			: base(game)
		{
			m_cb = cb;

			this.Enabled = true;

			game.GameSystems.Add(this);
		}

		public override void Update(GameTime gameTime)
		{
			if (gameTime.FrameCount == 0)
			{
				m_lastUpdate = gameTime.TotalGameTime;
				m_lastFrameCount = 0;
				m_min = Double.MaxValue;
				m_max = 0;
			}

			double tot = gameTime.ElapsedGameTime.TotalMilliseconds;

			if (tot < m_min)
				m_min = tot;

			if (tot > m_max)
				m_max = tot;

			if (gameTime.TotalGameTime > m_lastUpdate + TimeSpan.FromSeconds(1))
			{
				TimeSpan span = gameTime.TotalGameTime - m_lastUpdate;
				int frames = gameTime.FrameCount - m_lastFrameCount;

				var fpsText = string.Format("{0:F2} / {1:F2} / {2:F2}, {3:F2} FPS",
					m_min, span.TotalMilliseconds / frames, m_max,
					1.0 / (span.TotalSeconds / frames));

				m_lastUpdate = gameTime.TotalGameTime;
				m_lastFrameCount = gameTime.FrameCount;
				m_min = Double.MaxValue;
				m_max = 0;

				m_cb(fpsText);
			}
		}
	}
}
