using SharpDX;
using SharpDX.Toolkit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class CameraMoveService : IGameUpdatable
	{
		Camera m_camera;

		bool m_moving;

		Vector3 m_startPos;
		Vector3 m_endPos;

		Vector3 m_target;

		float m_step;

		public CameraMoveService(Camera camera)
		{
			m_camera = camera;
		}

		void IGameUpdatable.Update()
		{
			if (m_moving == false)
				return;

			Vector3 pos;

			Vector3.SmoothStep(ref m_startPos, ref m_endPos, m_step, out pos);

			m_camera.MoveTo(pos);
			m_step += 0.01f;

			if (m_step >= 1)
				m_moving = false;
		}

		public void Move(Vector3 eye, Vector3 target)
		{
			m_moving = true;
			m_step = 0;

			m_startPos = m_camera.Position;
			m_endPos = eye;

			m_target = target;
		}
	}
}
