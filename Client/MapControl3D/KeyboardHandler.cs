using Dwarrowdelf;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dwarrowdelf.Client
{
	class KeyboardHandler : IGameUpdatable
	{
		ViewGridProvider m_viewGridProvider;

		Camera m_camera;

		SharpDXHost m_control;

		MyGame m_game;

		enum ControlMode
		{
			Fps,
			Rts,
		}

		ControlMode m_controlMode = ControlMode.Fps;
		bool AlignViewGridToCamera { get; set; }

		public Action<KeyEventArgs> KeyDown;

		public KeyboardHandler(MyGame game, SharpDXHost control, Camera camera, ViewGridProvider viewGridProvider)
		{
			this.AlignViewGridToCamera = true;

			m_game = game;
			m_control = control;
			m_camera = camera;
			m_viewGridProvider = viewGridProvider;

			control.TextInput += OnTextInput;
			control.KeyDown += OnKeyDown;
			control.KeyUp += OnKeyUp;
			control.LostKeyboardFocus += OnLostKeyboardFocus;
		}

		void OnTextInput(object sender, TextCompositionEventArgs e)
		{
			if (e.Text.Length != 1)
				return;

			var viewGrid = m_viewGridProvider;
			var map = m_game.Environment;

			char key = e.Text[0];

			e.Handled = true;

			switch (key)
			{
				case '>':
					viewGrid.ViewCorner2 = viewGrid.ViewCorner2 + Direction.Down;
					break;

				case '<':
					viewGrid.ViewCorner2 = viewGrid.ViewCorner2 + Direction.Up;
					break;

				case '1':
					m_camera.LookAt(m_camera.Position,
						m_camera.Position + new Vector3(0, -1, -10),
						Vector3.UnitZ);
					break;

				case '2':
					m_camera.LookAt(m_camera.Position,
						m_camera.Position + new Vector3(1, 1, -1),
						Vector3.UnitZ);
					break;

				case 'r':
					if (map == null)
						break;
					{
						var p = Mouse.GetPosition(m_control);
						int px = MyMath.Round(p.X);
						int py = MyMath.Round(p.Y);

						// XXX not correct, should come from the surface
						var viewport = new ViewportF(0, 0, m_control.HostedWindowWidth, m_control.HostedWindowHeight);

						var ray = Ray.GetPickRay(px, py, viewport, m_camera.View * m_camera.Projection);

						VoxelRayCast.RunRayCast(m_game.Environment.Size, ray.Position, ray.Direction, m_camera.FarZ,
							(x, y, z, dir) =>
							{
								var l = new IntVector3(x, y, z);

								if (map.Size.Contains(l) == false)
									return true;

								map.SetTileData(l, TileData.GetNaturalWall(MaterialID.Granite));

								return false;
							});
					}
					break;

				case 'z':
					if (map == null)
						break;
					{
						var sel = m_game.SelectionService.Selection;

						if (sel.IsSelectionValid)
						{
							foreach (var p in sel.SelectionBox.Range())
								map.SetTileData(p, TileData.EmptyTileData);
						}
						else if (m_game.MousePositionService.MapLocation.HasValue)
						{
							var p = m_game.MousePositionService.MapLocation.Value;
							map.SetTileData(p, TileData.EmptyTileData);
						}
					}
					break;

				case 'x':
					if (map == null)
						break;
					{
						var sel = m_game.SelectionService.Selection;

						if (sel.IsSelectionValid)
						{
							foreach (var p in sel.SelectionBox.Range())
								map.SetTileData(p, TileData.GetNaturalWall(MaterialID.Granite));
						}
						else if (m_game.MousePositionService.MapLocation.HasValue)
						{
							var p = m_game.MousePositionService.MapLocation.Value;
							var d = m_game.MousePositionService.Face;
							if (map.Size.Contains(p + d))
								map.SetTileData(p + d, TileData.GetNaturalWall(MaterialID.Granite));
						}
					}
					break;

				default:
					e.Handled = false;
					break;
			}
		}

		HashSet<Key> m_keysDown = new HashSet<Key>();

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (this.KeyDown != null)
			{
				this.KeyDown(e);

				if (e.Handled)
					return;
			}

			Key key = e.Key;

			if (key == Key.System || key == Key.Tab)
				return;

			var viewGrid = m_viewGridProvider;
			bool ctrl = (e.KeyboardDevice.Modifiers & ModifierKeys.Control) != 0;

			e.Handled = true;

			switch (key)
			{
				case Key.NumPad4:
					if (!ctrl)
						viewGrid.ViewCorner1 += Direction.West;
					else
						viewGrid.ViewCorner2 += Direction.West;
					return;

				case Key.NumPad6:
					if (!ctrl)
						viewGrid.ViewCorner1 += Direction.East;
					else
						viewGrid.ViewCorner2 += Direction.East;
					return;

				case Key.NumPad8:
					if (!ctrl)
						viewGrid.ViewCorner1 += Direction.North;
					else
						viewGrid.ViewCorner2 += Direction.North;
					return;

				case Key.NumPad2:
					if (!ctrl)
						viewGrid.ViewCorner1 += Direction.South;
					else
						viewGrid.ViewCorner2 += Direction.South;
					return;

				case Key.NumPad5:
					viewGrid.ResetGrid();
					return;

				default:
					e.Handled = false;
					break;
			}

			m_keysDown.Add(key);

			// mark the keys as handled which have side effects (moving focus etc)
			HandleRtsFspKeyHandled(e);
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			Key key = e.Key;

			if (key == Key.System || key == Key.Tab)
				return;

			m_keysDown.Remove(key);

			// mark the keys as handled which have side effects (moving focus etc)
			HandleRtsFspKeyHandled(e);
		}

		void HandleRtsFspKeyHandled(KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Up:
				case Key.Down:
				case Key.Left:
				case Key.Right:
				case Key.W:
				case Key.A:
				case Key.S:
				case Key.D:
				case Key.Q:
				case Key.E:
					e.Handled = true;
					break;
				default:
					e.Handled = false;
					break;
			}
		}

		void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			m_keysDown.Clear();
		}

		public void Update()
		{
			switch (m_controlMode)
			{
				case ControlMode.Fps:
					HandleFpsKeyboard();
					break;

				case ControlMode.Rts:
					HandleRtsKeyboard();
					break;

				default:
					throw new Exception();
			}
		}

		bool IsKeyDown(Key key)
		{
			return m_keysDown.Contains(key);
		}

		void HandleFpsKeyboard()
		{
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;
			float dTime = (float)m_game.Time.FrameTime.TotalSeconds;
			float mul = 1f;

			if (IsKeyDown(Key.LeftShift) || IsKeyDown(Key.RightShift))
				mul = 0.2f;

			if (IsKeyDown(Key.W))
				m_camera.Walk(walkSpeek * dTime * mul);
			else if (IsKeyDown(Key.S))
				m_camera.Walk(-walkSpeek * dTime * mul);

			if (IsKeyDown(Key.D))
				m_camera.Strafe(walkSpeek * dTime * mul);
			else if (IsKeyDown(Key.A))
				m_camera.Strafe(-walkSpeek * dTime * mul);

			if (IsKeyDown(Key.E))
				m_camera.Climb(walkSpeek * dTime * mul);
			else if (IsKeyDown(Key.Q))
				m_camera.Climb(-walkSpeek * dTime * mul);

			if (IsKeyDown(Key.Up))
				m_camera.Pitch(-rotSpeed * dTime * mul);
			else if (IsKeyDown(Key.Down))
				m_camera.Pitch(rotSpeed * dTime * mul);

			if (IsKeyDown(Key.Left))
				m_camera.RotateZ(-rotSpeed * dTime * mul);
			else if (IsKeyDown(Key.Right))
				m_camera.RotateZ(rotSpeed * dTime * mul);
		}

		void HandleRtsKeyboard()
		{
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;
			float dTime = (float)m_game.Time.FrameTime.TotalSeconds;
			float mul = 1f;

			if (IsKeyDown(Key.LeftShift) || IsKeyDown(Key.RightShift))
				mul = 0.2f;

			Vector3 v = new Vector3();

			if (IsKeyDown(Key.E))
				v.Z = walkSpeek * dTime * mul;
			else if (IsKeyDown(Key.Q))
				v.Z = -walkSpeek * dTime * mul;

			if (IsKeyDown(Key.W))
				v.Y = walkSpeek * dTime * mul;
			else if (IsKeyDown(Key.S))
				v.Y = -walkSpeek * dTime * mul;

			if (IsKeyDown(Key.D))
				v.X = walkSpeek * dTime * mul;
			else if (IsKeyDown(Key.A))
				v.X = -walkSpeek * dTime * mul;

			if (!v.IsZero)
			{
				m_camera.Move(v);

				if (this.AlignViewGridToCamera && v.Z != 0)
				{
					var viewGrid = m_viewGridProvider;

					var c = viewGrid.ViewCorner2;
					c.Z = (int)m_camera.Position.Z - 32;
					viewGrid.ViewCorner2 = c;
				}
			}

			if (IsKeyDown(Key.Up))
				m_camera.Pitch(-rotSpeed * dTime * mul);
			else if (IsKeyDown(Key.Down))
				m_camera.Pitch(rotSpeed * dTime * mul);

			if (IsKeyDown(Key.Left))
				m_camera.RotateZ(-rotSpeed * dTime * mul);
			else if (IsKeyDown(Key.Right))
				m_camera.RotateZ(rotSpeed * dTime * mul);
		}
	}
}
