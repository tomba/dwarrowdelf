using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dwarrowdelf.Client
{
	class ViewGridAdjusterService : IGameUpdatable
	{
		MyGame m_game;
		SharpDXHost m_control;

		int m_currentZ = -1;

		public ViewGridAdjusterService(MyGame game, SharpDXHost control)
		{
			m_game = game;
			m_control = control;
		}

		void IGameUpdatable.Update()
		{
			var controlMode = ((MapControl3D)m_control).Config.ControlMode;
			if (controlMode != MapControlMode.Rts)
				return;

			if (m_game.Environment == null)
				return;

			int camZ = (int)m_game.Camera.Position.Z;
			if (camZ == m_currentZ)
				return;

			m_currentZ = camZ;

			var viewGrid = m_game.ViewGridProvider;

			var c = viewGrid.ViewCorner2;
			c.Z = camZ - 32;
			viewGrid.ViewCorner2 = c;
		}
	}
}
