using System;
using SharpDX;
using SharpDX.Toolkit;

namespace Client3D
{
	/// <summary>
	/// Component responsible for camera updates
	/// </summary>
	sealed class CameraProvider
	{
		// Camera coordinate system with coordinates relative to world space
		public Vector3 Position { get { return m_position; } }
		public Vector3 Right { get { return m_right; } }
		public Vector3 Up { get { return m_up; } }
		public Vector3 Look { get { return m_look; } }

		Vector3 m_position;
		Vector3 m_right;
		Vector3 m_up;
		Vector3 m_look;

		public float FarZ { get { return m_farZ; } }

		// Frustum
		float m_fovY, m_aspect, m_nearZ, m_farZ;
		float m_nearWindowHeight, m_farWindowHeight;

		Matrix? m_view;
		public Matrix View { get { if (!m_view.HasValue) UpdateView(); return m_view.Value; } }

		Matrix? m_projection;
		public Matrix Projection { get { if (!m_projection.HasValue) UpdateProjection(); return m_projection.Value; } }

		BoundingFrustum? m_frustum;
		public BoundingFrustum Frustum { get { if (!m_frustum.HasValue) UpdateFrustum(); return m_frustum.Value; } }

		public CameraProvider(Game game)
		{
			SetLens(MathUtil.PiOverFour, 1.0f, 1.0f, 200.0f);

			game.Services.AddService(typeof(CameraProvider), this);
		}

		public void SetAspect(float aspect)
		{
			m_aspect = aspect;
			m_projection = null;
			m_frustum = null;
		}

		public void SetLens(float fovY, float aspect, float nearZ, float farZ)
		{
			m_fovY = fovY;
			m_aspect = aspect;
			m_nearZ = nearZ;
			m_farZ = farZ;

			m_nearWindowHeight = 2.0f * m_nearZ * (float)Math.Tan(0.5f * m_fovY);
			m_farWindowHeight = 2.0f * m_farZ * (float)Math.Tan(0.5f * m_fovY);

			m_projection = null;
			m_frustum = null;
		}

		public void LookAt(Vector3 pos, Vector3 target, Vector3 worldUp)
		{
			m_position = pos;
			m_look = Vector3.Normalize(target - pos);
			m_right = Vector3.Normalize(Vector3.Cross(worldUp, m_look));
			m_up = Vector3.Cross(m_look, m_right);
			m_view = null;
			m_frustum = null;
		}

		public void Move(Vector3 v)
		{
			var vy = m_look;
			vy.Z = 0;
			vy.Normalize();

			var vx = m_right;
			vx.Z = 0;
			vx.Normalize();

			var vz = new Vector3(0, 0, 1);

			m_position += v.X * vx + v.Y * vy + v.Z * vz;
			m_view = null;
			m_frustum = null;
		}

		public void Strafe(float d)
		{
			m_position += m_right * d;
			m_view = null;
			m_frustum = null;
		}

		public void Walk(float d)
		{
			m_position += m_look * d;
			m_view = null;
			m_frustum = null;
		}

		public void Climb(float d)
		{
			m_position += m_up * d;
			m_view = null;
			m_frustum = null;
		}

		public void Pitch(float angle)
		{
			// Rotate up and look vector about the right vector.
			var rot = Matrix.RotationAxis(m_right, angle);
			m_up = Vector3.TransformNormal(m_up, rot);
			m_look = Vector3.TransformNormal(m_look, rot);
			m_view = null;
			m_frustum = null;
		}

		public void RotateZ(float angle)
		{
			// Rotate the basis vectors about the world z-axis.
			var rot = Matrix.RotationZ(angle);
			m_right = Vector3.TransformNormal(m_right, rot);
			m_up = Vector3.TransformNormal(m_up, rot);
			m_look = Vector3.TransformNormal(m_look, rot);
			m_view = null;
			m_frustum = null;
		}

		void UpdateProjection()
		{
			m_projection = Matrix.PerspectiveFovLH(m_fovY, m_aspect, m_nearZ, m_farZ);
		}

		void UpdateView()
		{
			// Keep camera's axes orthogonal to each other and of unit length.
			m_look = Vector3.Normalize(m_look);
			m_up = Vector3.Normalize(Vector3.Cross(m_look, m_right));

			// U, L already ortho-normal, so no need to normalize cross product.
			m_right = Vector3.Cross(m_up, m_look);

			// Fill in the view matrix entries.
			float x = -Vector3.Dot(m_position, m_right);
			float y = -Vector3.Dot(m_position, m_up);
			float z = -Vector3.Dot(m_position, m_look);

			m_view = new Matrix()
			{
				Column1 = new Vector4(m_right, x),
				Column2 = new Vector4(m_up, y),
				Column3 = new Vector4(m_look, z),
				Column4 = new Vector4(0, 0, 0, 1),
			};
		}

		void UpdateFrustum()
		{
			m_frustum = BoundingFrustum.FromCamera(m_position, m_look, m_up, m_fovY, m_nearZ, m_farZ, m_aspect);
		}
	}
}
