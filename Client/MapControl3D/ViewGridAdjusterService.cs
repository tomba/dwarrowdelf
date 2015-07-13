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

		public int Height = 32;

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

			var viewGrid = m_game.ViewGridProvider;

			var c = viewGrid.ViewCorner2;
			c.Z = camZ - this.Height;
			viewGrid.ViewCorner2 = c;
		}
	}
}
