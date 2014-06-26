using SharpDX;

namespace Client3D
{
	interface ICameraService
	{
		Vector3 Position { get; }
		Vector3 Right { get; }
		Vector3 Up { get; }
		Vector3 Look { get; }

		Matrix View { get; }
		Matrix Projection { get; }

		BoundingFrustum Frustum { get; }
	}
}
