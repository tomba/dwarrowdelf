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

		public KeyboardHandler(SharpDXHost control, Camera camera, ViewGridProvider viewGridProvider)
		{
			m_control = control;
			m_camera = camera;
			m_viewGridProvider = viewGridProvider;

			control.KeyDown += OnKeyDown;
			control.KeyUp += OnKeyUp;
			control.LostKeyboardFocus += control_LostKeyboardFocus;
		}

		void control_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
		{
			m_keysDown.Clear();
		}

		HashSet<Key> m_keysDown = new HashSet<Key>();
		HashSet<Key> m_keysPressed = new HashSet<Key>();

		void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.System || e.Key == Key.Tab)
				return;

			e.Handled = true;

			m_keysDown.Add(e.Key);
			m_keysPressed.Add(e.Key);
		}

		void OnKeyUp(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.System || e.Key == Key.Tab)
				return;

			e.Handled = true;

			m_keysDown.Remove(e.Key);
		}

		public void Update(TimeSpan time)
		{
			HandleFpsKeyboard();
			HandleKeyPresses();

			m_keysPressed.Clear();
		}

		bool IsKeyDown(Key key)
		{
			return m_keysDown.Contains(key);
		}

		void HandleFpsKeyboard()
		{
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;
			float dTime = 0.0166f; // (float)gameTime.ElapsedGameTime.TotalSeconds;
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

		void HandleKeyPresses()
		{
			foreach (var key in m_keysPressed)
				HandleKeyPress(key);
		}

		void HandleKeyPress(Key key)
		{
			var viewGrid = m_viewGridProvider;

			var map = GameData.Data.Map;

			switch (key)
			{
#if asd
				case '>':
					viewGrid.ViewCorner2 = viewGrid.ViewCorner2 + Direction.Down;
					break;
				case '<':
					viewGrid.ViewCorner2 = viewGrid.ViewCorner2 + Direction.Up;
					break;
#endif
				case Key.D1:
					m_camera.LookAt(m_camera.Position,
						m_camera.Position + new Vector3(0, -1, -10),
						Vector3.UnitZ);
					break;
				case Key.D2:
					m_camera.LookAt(m_camera.Position,
						m_camera.Position + new Vector3(1, 1, -1),
						Vector3.UnitZ);
					break;

				case Key.R:
					if (map == null)
						break;
					{
						var p = Mouse.GetPosition(m_control);
						int px = MyMath.Round(p.X);
						int py = MyMath.Round(p.Y);

						// XXX not correct, should come from the surface
						var viewport = new ViewportF(0, 0, m_control.HostedWindowWidth, m_control.HostedWindowHeight);

						var ray = Ray.GetPickRay(px, py, viewport, m_camera.View * m_camera.Projection);

						VoxelRayCast.RunRayCast(ray.Position, ray.Direction, m_camera.FarZ,
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

#if asd
				case 'z':
					if (map == null)
						break;
					{
						var sel = this.Services.GetService<SelectionRenderer>();

						if (sel.SelectionVisible)
						{
							foreach (var p in sel.SelectionGrid.Range())
								map.SetTileData(p, TileData.EmptyTileData);
						}
						else if (sel.CursorVisible)
						{
							var p = sel.Position;
							map.SetTileData(p, TileData.EmptyTileData);
						}
					}
					break;

				case 'x':
					if (map == null)
						break;
					{
						var sel = this.Services.GetService<SelectionRenderer>();

						if (sel.SelectionVisible)
						{
							foreach (var p in sel.SelectionGrid.Range())
								map.SetTileData(p, TileData.GetNaturalWall(MaterialID.Granite));
						}
						else if (sel.CursorVisible)
						{
							var p = sel.Position;
							var d = sel.Direction;
							if (map.Size.Contains(p + d))
								map.SetTileData(p + d, TileData.GetNaturalWall(MaterialID.Granite));
						}
					}
					break;
#endif
			}
		}
#if asd





		void OnKeyDownForms(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			var viewGrid = m_viewGridProvider;

			bool handled = true;

			switch (e.KeyData)
			{
				case Forms.Keys.NumPad4:
					viewGrid.ViewCorner1 += Direction.West;
					break;

				case Forms.Keys.NumPad6:
					viewGrid.ViewCorner1 += Direction.East;
					break;

				case Forms.Keys.NumPad4 | Forms.Keys.Control:
					viewGrid.ViewCorner2 += Direction.West;
					break;

				case Forms.Keys.NumPad6 | Forms.Keys.Control:
					viewGrid.ViewCorner2 += Direction.East;
					break;

				case Forms.Keys.NumPad8:
					viewGrid.ViewCorner1 += Direction.North;
					break;

				case Forms.Keys.NumPad2:
					viewGrid.ViewCorner1 += Direction.South;
					break;

				case Forms.Keys.NumPad8 | Forms.Keys.Control:
					viewGrid.ViewCorner2 += Direction.North;
					break;

				case Forms.Keys.NumPad2 | Forms.Keys.Control:
					viewGrid.ViewCorner2 += Direction.South;
					break;

				default:
					handled = false;
					break;
			}

			e.Handled = handled;
			e.SuppressKeyPress = handled;
		}

		void OnKeyPressForms(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			var viewGrid = m_viewGridProvider;

			var map = GameData.Data.Map;

			switch (e.KeyChar)
			{
				case '>':
					viewGrid.ViewCorner2 = viewGrid.ViewCorner2 + Direction.Down;
					break;
				case '<':
					viewGrid.ViewCorner2 = viewGrid.ViewCorner2 + Direction.Up;
					break;
				case '1':
					m_cameraProvider.LookAt(m_cameraProvider.Position,
						m_cameraProvider.Position + new Vector3(0, -1, -10),
						Vector3.UnitZ);
					break;
				case '2':
					m_cameraProvider.LookAt(m_cameraProvider.Position,
						m_cameraProvider.Position + new Vector3(1, 1, -1),
						Vector3.UnitZ);
					break;

				case 'r':
					if (map == null)
						break;
					{
#if asd
						var ctrl = (System.Windows.Forms.Control)this.Game.Window.NativeWindow;
						var p = ctrl.PointToClient(System.Windows.Forms.Control.MousePosition);

						var camera = m_camera;

						var ray = Ray.GetPickRay(p.X, p.Y, this.GraphicsDevice.Viewport, camera.View * camera.Projection);

						VoxelRayCast.RunRayCast(ray.Position, ray.Direction, camera.FarZ,
							(x, y, z, dir) =>
							{
								var l = new IntVector3(x, y, z);

								if (map.Size.Contains(l) == false)
									return true;

								map.SetTileData(l, TileData.GetNaturalWall(MaterialID.Granite));

								return false;
							});
#endif
					}
					break;

				case 'z':
					if (map == null)
						break;
					{
#if asd
						var sel = this.Services.GetService<SelectionRenderer>();

						if (sel.SelectionVisible)
						{
							foreach (var p in sel.SelectionGrid.Range())
								map.SetTileData(p, TileData.EmptyTileData);
						}
						else if (sel.CursorVisible)
						{
							var p = sel.Position;
							map.SetTileData(p, TileData.EmptyTileData);
						}
#endif
					}
					break;

				case 'x':
					if (map == null)
						break;
					{
#if asd
						var sel = this.Services.GetService<SelectionRenderer>();

						if (sel.SelectionVisible)
						{
							foreach (var p in sel.SelectionGrid.Range())
								map.SetTileData(p, TileData.GetNaturalWall(MaterialID.Granite));
						}
						else if (sel.CursorVisible)
						{
							var p = sel.Position;
							var d = sel.Direction;
							if (map.Size.Contains(p + d))
								map.SetTileData(p + d, TileData.GetNaturalWall(MaterialID.Granite));
						}
#endif
					}
					break;
			}
		}

		public void Update(GameTime gameTime)
		{
			KeyboardState keyboardState = new KeyboardState();// m_keyboardManager.GetState();

			switch (GameData.Data.ControlMode)
			{
				case ControlMode.Fps:
					HandleFpsKeyboard(gameTime, keyboardState);
					break;

				case ControlMode.Rts:
					HandleRtsKeyboard(gameTime, keyboardState);
					break;

				default:
					throw new Exception();
			}
		}

		void HandleFpsKeyboard(GameTime gameTime, KeyboardState keyboardState)
		{
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;
			float dTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float mul = 1f;

			if (keyboardState.IsKeyDown(Keys.Shift))
				mul = 0.2f;

			if (keyboardState.IsKeyDown(Keys.W))
				m_cameraProvider.Walk(walkSpeek * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.S))
				m_cameraProvider.Walk(-walkSpeek * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.D))
				m_cameraProvider.Strafe(walkSpeek * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.A))
				m_cameraProvider.Strafe(-walkSpeek * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.E))
				m_cameraProvider.Climb(walkSpeek * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Q))
				m_cameraProvider.Climb(-walkSpeek * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.Up))
				m_cameraProvider.Pitch(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Down))
				m_cameraProvider.Pitch(rotSpeed * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.Left))
				m_cameraProvider.RotateZ(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Right))
				m_cameraProvider.RotateZ(rotSpeed * dTime * mul);
		}

		void HandleRtsKeyboard(GameTime gameTime, KeyboardState keyboardState)
		{
			float dTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
			float mul = 1f;
			const float walkSpeek = 40f;
			const float rotSpeed = MathUtil.PiOverTwo * 1.5f;

			if (keyboardState.IsKeyDown(Keys.Shift))
				mul = 0.2f;

			Vector3 v = new Vector3();

			if (keyboardState.IsKeyDown(Keys.E))
				v.Z = walkSpeek * dTime * mul;
			else if (keyboardState.IsKeyDown(Keys.Q))
				v.Z = -walkSpeek * dTime * mul;

			if (keyboardState.IsKeyDown(Keys.W))
				v.Y = walkSpeek * dTime * mul;
			else if (keyboardState.IsKeyDown(Keys.S))
				v.Y = -walkSpeek * dTime * mul;

			if (keyboardState.IsKeyDown(Keys.D))
				v.X = walkSpeek * dTime * mul;
			else if (keyboardState.IsKeyDown(Keys.A))
				v.X = -walkSpeek * dTime * mul;

			if (!v.IsZero)
			{
				m_cameraProvider.Move(v);

				if (GameData.Data.AlignViewGridToCamera && v.Z != 0)
				{
					var viewGrid = m_viewGridProvider;

					var c = viewGrid.ViewCorner2;
					c.Z = (int)m_cameraProvider.Position.Z - 32;
					viewGrid.ViewCorner2 = c;
				}
			}

			if (keyboardState.IsKeyDown(Keys.Up))
				m_cameraProvider.Pitch(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Down))
				m_cameraProvider.Pitch(rotSpeed * dTime * mul);

			if (keyboardState.IsKeyDown(Keys.Left))
				m_cameraProvider.RotateZ(-rotSpeed * dTime * mul);
			else if (keyboardState.IsKeyDown(Keys.Right))
				m_cameraProvider.RotateZ(rotSpeed * dTime * mul);
		}
#endif
	}
}
