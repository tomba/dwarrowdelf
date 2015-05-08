using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;

namespace Dwarrowdelf.Client
{
	interface IGameUpdatable
	{
		void Update(TimeSpan time);
	}

	interface IGameDrawable
	{
		void Draw(Camera camera);
	}

	abstract class GameComponent : Component, IGameUpdatable, IGameDrawable
	{
		public abstract void Update(TimeSpan time);
		public abstract void Draw(Camera camera);

		public GraphicsDevice GraphicsDevice { get; private set; }

		protected GameComponent(GraphicsDevice device)
		{
			this.GraphicsDevice = device;
		}
	}
}
