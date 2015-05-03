using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Forms = System.Windows.Forms;

namespace Client3D
{
	class KeyboardHandler : GameSystem
	{
		readonly KeyboardManager m_keyboardManager;

		CameraProvider m_cameraProvider;

		public KeyboardHandler(Game game)
			: base(game)
		{
			this.Enabled = true;

			m_keyboardManager = new KeyboardManager(game);

			m_cameraProvider = this.Services.GetService<CameraProvider>();

			game.GameSystems.Add(this);
		}

		public override void Initialize()
		{
			base.Initialize();

			if (Program.Mode == ThreeDMode.WinForms)
			{
				var form = (System.Windows.Forms.Form)this.Game.Window.NativeWindow;
				form.KeyPress += OnKeyPress;
				form.KeyDown += OnKeyDown;
			}
		}

		void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
		{
			var viewGrid = this.Services.GetService<ViewGridProvider>();

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

		void OnKeyPress(object sender, System.Windows.Forms.KeyPressEventArgs e)
		{
			var viewGrid = this.Services.GetService<ViewGridProvider>();

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
					{
						var form = (System.Windows.Forms.Form)this.Game.Window.NativeWindow;
						var p = form.PointToClient(System.Windows.Forms.Control.MousePosition);

						var camera = this.Services.GetService<CameraProvider>();

						var ray = Ray.GetPickRay(p.X, p.Y, this.GraphicsDevice.Viewport, camera.View * camera.Projection);

						VoxelRayCast.RunRayCast(ray.Position, ray.Direction, camera.FarZ,
							(x, y, z, dir) =>
							{
								var l = new IntVector3(x, y, z);

								if (GlobalData.Map.Size.Contains(l) == false)
									return true;

								GlobalData.Map.SetTileData(l, TileData.GetNaturalWall(MaterialID.Granite));

								return false;
							});
					}
					break;

				case 'z':
					{
						var sel = this.Services.GetService<SelectionRenderer>();

						if (sel.SelectionVisible)
						{
							foreach (var p in sel.SelectionGrid.Range())
								GlobalData.Map.SetTileData(p, TileData.EmptyTileData);
						}
						else if (sel.CursorVisible)
						{
							var p = sel.Position;
							GlobalData.Map.SetTileData(p, TileData.EmptyTileData);
						}
					}
					break;

				case 'x':
					{
						var sel = this.Services.GetService<SelectionRenderer>();

						if (sel.SelectionVisible)
						{
							foreach (var p in sel.SelectionGrid.Range())
								GlobalData.Map.SetTileData(p, TileData.GetNaturalWall(MaterialID.Granite));
						}
						else if (sel.CursorVisible)
						{
							var p = sel.Position;
							var d = sel.Direction;
							if (GlobalData.Map.Size.Contains(p + d))
								GlobalData.Map.SetTileData(p + d, TileData.GetNaturalWall(MaterialID.Granite));
						}
					}
					break;
			}
		}

		public override void Update(GameTime gameTime)
		{
			var keyboardState = m_keyboardManager.GetState();

			if (keyboardState.IsKeyDown(Keys.F4) && keyboardState.IsKeyDown(Keys.LeftAlt))
				this.Game.Exit();

			switch (GlobalData.ControlMode)
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

				if (GlobalData.AlignViewGridToCamera && v.Z != 0)
				{
					var viewGrid = this.Services.GetService<ViewGridProvider>();

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
	}
}
