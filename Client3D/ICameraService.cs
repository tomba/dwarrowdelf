using SharpDX;

namespace Client3D
{
	interface ICameraService
	{
		Vector3 Position { get; }

		Matrix View { get; }

		Matrix Projection { get; }

		BoundingFrustum Frustum { get; }
	}
}
