using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client.TileControl
{
	public class SceneList : IScene
	{
		List<IScene> m_sceneList = new List<IScene>();

		public SceneList(IEnumerable<IScene> scenes)
		{
			m_sceneList.AddRange(scenes);
		}

		public void Attach(ISceneHost host)
		{
			foreach (var scene in m_sceneList)
				scene.Attach(host);
		}

		public void Detach()
		{
			foreach (var scene in m_sceneList)
				scene.Detach();
		}

		public void OnRenderSizeChanged(IntSize2 renderSize)
		{
			foreach (var scene in m_sceneList)
				scene.OnRenderSizeChanged(renderSize);
		}

		public void Update(TimeSpan timeSpan)
		{
			foreach (var scene in m_sceneList)
				scene.Update(timeSpan);
		}

		public void Render()
		{
			foreach (var scene in m_sceneList)
				scene.Render();
		}
	}
}
