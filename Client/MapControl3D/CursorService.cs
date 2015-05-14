using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dwarrowdelf.Client
{
	class CursorService
	{
		MyGame m_game;
		SharpDXHost m_control;

		public CursorService(MyGame game, SharpDXHost control)
		{
			m_game = game;
			m_control = control;
		}

		bool m_enabled;

		public bool IsEnabled
		{
			get { return m_enabled; }

			set
			{
				if (m_enabled == value)
					return;

				m_enabled = value;

				if (m_enabled)
				{
					m_game.MousePositionService.MouseLocationChanged += OnMouseLocationChanged;
					m_game.KeyboardHandler.KeyDown += OnKeyDown;

					SetInitialCursorPos();
				}
				else
				{
					m_game.MousePositionService.MouseLocationChanged -= OnMouseLocationChanged;
					m_game.KeyboardHandler.KeyDown -= OnKeyDown;

					this.Location = null;
				}
			}
		}

		void OnKeyDown(KeyEventArgs e)
		{
			if (this.Location.HasValue == false)
				return;

			Key key = e.Key;

			if (KeyHelpers.KeyIsDir(key) == false)
				return;

			//m_control.ScrollStop();

			var dir = KeyHelpers.KeyToDir(key);

			IntVector3 v;

			if ((e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0)
			{
				if (dir == Direction.North)
					dir = Direction.Up;
				else if (dir == Direction.South)
					dir = Direction.Down;
				else
					return;

				v = dir.ToIntVector3();
			}
			else
			{
				var v2 = m_game.Camera.PlanarAdjust(dir.ToIntVector2());
				v = new IntVector3(v2, 0);
			}

			int m = 1;

			if ((e.KeyboardDevice.Modifiers & ModifierKeys.Shift) != 0)
				m = 5;

			this.Location += v * m;

			//m_control.KeepOnScreen(this.CursorPosition);

			e.Handled = true;
		}

		void SetInitialCursorPos()
		{
			// first try to set the cursor where the mouse is
			var loc = m_game.MousePositionService.MapLocation;

			// then try the voxel in the center of the screen
			if (loc.HasValue == false)
			{
				IntVector3 pos;
				Direction face;

				var view = m_game.Surfaces[0].Views[0];

				var center = new IntVector2((int)view.ViewPort.Width / 2, (int)view.ViewPort.Height / 2);

				bool hit = MousePositionService.PickVoxel(m_game, center, out pos, out face);

				if (hit)
					loc = pos;
				else
					loc = null;
			}

			this.Location = loc;
		}

		void OnMouseLocationChanged()
		{
			var loc = m_game.MousePositionService.MapLocation;

			if (loc.HasValue == false)
				return;

			this.Location = loc.Value;
		}

		public event Action<IntVector3?> LocationChanged;

		IntVector3? m_location;

		public IntVector3? Location
		{
			get { return m_location; }

			private set
			{
				if (value == m_location)
					return;

				m_location = value;

				if (this.LocationChanged != null)
					this.LocationChanged(value);
			}
		}
	}
}
