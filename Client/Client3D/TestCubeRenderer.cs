using SharpDX;
using SharpDX.Toolkit;
using SharpDX.Toolkit.Graphics;
using System;

namespace Dwarrowdelf.Client
{
	sealed class TestCubeRenderer : GameComponent
	{
		GeometricPrimitive m_cube;
		Texture2D m_cubeTexture;
		Matrix m_cubeTransform;

		BasicEffect m_basicEffect;

		Camera m_camera;
		public TestCubeRenderer(MyGame game, Camera camera)
			: base(game)
		{
			m_camera = camera;
			LoadContent();
		}

		void LoadContent()
		{
			m_basicEffect = ToDispose(new BasicEffect(this.GraphicsDevice));

			m_basicEffect.EnableDefaultLighting(); // enable default lightning, useful for quick prototyping
			m_basicEffect.TextureEnabled = true;   // enable texture drawing

			LoadCube();
		}

		public override void Update(TimeSpan gameTime)
		{
			var time = (float)gameTime.TotalSeconds;

			m_cubeTransform = Matrix.RotationX(time) * Matrix.RotationY(time * 2f) * Matrix.RotationZ(time * .7f) *
				Matrix.Translation(m_camera.Position + m_camera.Look * 10);
		}

		public override void Draw(Camera camera)
		{
			m_basicEffect.View = camera.View;
			m_basicEffect.Projection = camera.Projection;

			m_basicEffect.Texture = m_cubeTexture;
			m_basicEffect.World = m_cubeTransform;
			m_cube.Draw(m_basicEffect);
		}

		void LoadCube()
		{
			m_cube = ToDispose(GeometricPrimitive.Cube.New(this.GraphicsDevice, 1, toLeftHanded: true));

			m_cubeTexture = this.Content.Load<Texture2D>("logo_large");

			m_cubeTransform = Matrix.Identity;
		}
	}
}
