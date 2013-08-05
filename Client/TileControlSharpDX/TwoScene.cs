using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client.TileControl
{
	public class TwoScene : IScene
	{
		IScene m_scene1;
		IScene m_scene2;

		public TwoScene(IScene scene1, IScene scene2)
		{
			m_scene1 = scene1;
			m_scene2 = scene2;
		}

		public void Attach(ISceneHost host)
		{
			m_scene1.Attach(host);
			m_scene2.Attach(host);
		}

		public void Detach()
		{
		}

		public void Update(TimeSpan timeSpan)
		{
			m_scene1.Update(timeSpan);
			m_scene2.Update(timeSpan);
		}

		public void Render()
		{
			m_scene1.Render();
			m_scene2.Render();
		}

		public void Dispose()
		{
		}
	}
}
