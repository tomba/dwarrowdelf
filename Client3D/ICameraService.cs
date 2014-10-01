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

		float FarZ { get; }

		BoundingFrustum Frustum { get; }

		void LookAt(Vector3 pos, Vector3 target, Vector3 worldUp);
		void Move(Vector3 v);
		void Strafe(float d);
		void Walk(float d);
		void Climb(float d);
		void Pitch(float angle);
		void RotateZ(float angle);
	}
}
