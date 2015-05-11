using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Dwarrowdelf.Client
{
	public class MapControl3D : SharpDXHost
	{
		MyGame m_game;

		public MapControl3D()
		{
		}

		public void Start()
		{
			m_game = new MyGame(this);
			m_game.Start();

			var dbg = new DebugWindow();
			dbg.Owner = System.Windows.Window.GetWindow(this);
			dbg.SetGame(m_game);
			dbg.Show();
		}

		public void Stop()
		{
			m_game.Stop();
			m_game.Dispose();
		}

		EnvironmentObject m_env;
		public EnvironmentObject Environment
		{
			get { return m_env; }
			set
			{
				m_env = value;
				m_game.Environment = value;
			}
		}

		public void GoTo(LivingObject ob)
		{
			var env = ob.Environment;

			this.Environment = env;

			m_game.GoTo(ob);
		}
	}
}
