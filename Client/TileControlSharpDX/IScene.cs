using System;
using SharpDX.Direct3D11;

namespace Dwarrowdelf.Client.TileControl
{
	public interface IScene
	{
		void Attach(ISceneHost host);
		void Detach();
		void OnRenderSizeChanged(IntSize2 renderSize);
		void Update(TimeSpan timeSpan);
		void Render();
	}

	public interface ISceneHost
	{
		Device Device { get; }
	}
}
