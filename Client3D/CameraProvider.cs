using System;
using SharpDX;
using SharpDX.Toolkit;

namespace Client3D
{
	/// <summary>
	/// Component responsible for camera updates
	/// </summary>
	sealed class CameraProvider : GameSystem, ICameraService
	{
		private Matrix m_view;
		private Matrix m_projection;

		/// <summary>
		/// Initialize in constructor anything that doesn't depend on other services.
		/// </summary>
		/// <param name="game">The game where this system will be attached to.</param>
		public CameraProvider(Game game)
			: base(game)
		{
			this.Enabled = true; // enable camera updates

			// add the system itself to the systems list, so that it will get initialized and processed properly
			// this can be done after game initialization - the Game class supports adding and removing of game systems dynamically
			game.GameSystems.Add(this);

			// add this system in the list of services
			game.Services.AddService(typeof(ICameraService), this);
		}

		/// <summary>
		/// The camera's view matrix
		/// </summary>
		public Matrix View { get { return m_view; } }

		/// <summary>
		/// The camera's projection matrix
		/// </summary>
		public Matrix Projection { get { return m_projection; } }

		/// <summary>
		/// Update the camera logic.
		/// </summary>
		/// <param name="gameTime">Structure containing information about elapsed game time.</param>
		public override void Update(GameTime gameTime)
		{
			base.Update(gameTime);

			// animate camera rotation based on total elapsed game time
			var viewRotationAngle = (float)(gameTime.TotalGameTime.TotalSeconds * 0.2f);

			// compute the camera eye position by rotating the initial position around the "up" vector (Y axis)
			var eyePosition = Vector3.Transform(new Vector3(0, 2, 5), Quaternion.RotationAxis(Vector3.UnitY, viewRotationAngle));

			// update the view with new data
			m_view = Matrix.LookAtRH(eyePosition, new Vector3(0, 0, 0), Vector3.UnitY);

			// recompute the projection matrix in case if graphics device changed
			m_projection = Matrix.PerspectiveFovRH(MathUtil.PiOverFour, (float)GraphicsDevice.BackBuffer.Width / GraphicsDevice.BackBuffer.Height, 0.1f, 200.0f);
		}
	}
}
