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

		public GameCore Game { get; private set; }

		public GraphicsDevice GraphicsDevice { get { return this.Game.GraphicsDevice; } }
		public ContentPool Content { get { return this.Game.Content; } }

		protected GameComponent(GameCore game)
		{
			this.Game = game;
		}
	}
}
