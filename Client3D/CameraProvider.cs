using System;
using SharpDX;
using SharpDX.Toolkit;

namespace Client3D
{
	/// <summary>
	/// Component responsible for camera updates
	/// </summary>
	sealed class CameraProvider : ICameraService
	{
		// Camera coordinate system with coordinates relative to world space
		public Vector3 Position { get { return m_position; } }
		Vector3 m_position;
		Vector3 m_right;
		Vector3 m_up;
		Vector3 m_look;

		// Frustum
		float m_fovY, m_aspect, m_nearZ, m_farZ;
		float m_nearWindowHeight, m_farWindowHeight;

		Matrix m_view;
		bool m_viewDirty;
		public Matrix View { get { if (m_viewDirty) UpdateView(); return m_view; } }

		Matrix m_projection;
		bool m_projectionDirty;
		public Matrix Projection { get { if (m_projectionDirty) UpdateProjection(); return m_projection; } }

		BoundingFrustum m_frustum;
		bool m_frustumDirty;
		public BoundingFrustum Frustum { get { if (m_frustumDirty) UpdateFrustum(); return m_frustum; } }

		/// <summary>
		/// Initialize in constructor anything that doesn't depend on other services.
		/// </summary>
		/// <param name="game">The game where this system will be attached to.</param>
		public CameraProvider(Game game)
		{
			SetLens(MathUtil.PiOverFour, 1.0f, 1.0f, 1000.0f);

			LookAt(new Vector3(10, 10, 10), new Vector3(0, 0, 0), Vector3.UnitZ);

			// add this system in the list of services
			game.Services.AddService(typeof(ICameraService), this);
		}

		public void SetAspect(float aspect)
		{
			m_aspect = aspect;
			m_projectionDirty = true;
			m_frustumDirty = true;
		}

		public void SetLens(float fovY, float aspect, float zn, float zf)
		{
			m_fovY = fovY;
			m_aspect = aspect;
			m_nearZ = zn;
			m_farZ = zf;

			m_nearWindowHeight = 2.0f * m_nearZ * (float)Math.Tan(0.5f * m_fovY);
			m_farWindowHeight = 2.0f * m_farZ * (float)Math.Tan(0.5f * m_fovY);

			m_projectionDirty = true;
			m_frustumDirty = true;
		}

		public void LookAt(Vector3 pos, Vector3 target, Vector3 worldUp)
		{
			m_position = pos;
			m_look = Vector3.Normalize(target - pos);
			m_right = Vector3.Normalize(Vector3.Cross(worldUp, m_look));
			m_up = Vector3.Cross(m_look, m_right);
			m_viewDirty = true;
			m_frustumDirty = true;
		}

		public void Strafe(float d)
		{
			m_position += m_right * d;
			m_viewDirty = true;
			m_frustumDirty = true;
		}

		public void Walk(float d)
		{
			m_position += m_look * d;
			m_viewDirty = true;
			m_frustumDirty = true;
		}

		public void Climb(float d)
		{
			m_position += m_up * d;
			m_viewDirty = true;
			m_frustumDirty = true;
		}

		public void Pitch(float angle)
		{
			// Rotate up and look vector about the right vector.
			var rot = Matrix.RotationAxis(m_right, angle);
			m_up = Vector3.TransformNormal(m_up, rot);
			m_look = Vector3.TransformNormal(m_look, rot);
			m_viewDirty = true;
			m_frustumDirty = true;
		}

		public void RotateZ(float angle)
		{
			// Rotate the basis vectors about the world z-axis.
			var rot = Matrix.RotationZ(angle);
			m_right = Vector3.TransformNormal(m_right, rot);
			m_up = Vector3.TransformNormal(m_up, rot);
			m_look = Vector3.TransformNormal(m_look, rot);
			m_viewDirty = true;
			m_frustumDirty = true;
		}

		void UpdateProjection()
		{
			m_projection = Matrix.PerspectiveFovLH(m_fovY, m_aspect, m_nearZ, m_farZ);

			m_projectionDirty = false;
			m_frustumDirty = true;
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


			m_viewDirty = false;
		}

		void UpdateFrustum()
		{
			m_frustum = BoundingFrustum.FromCamera(m_position, m_look, m_up, m_fovY, m_nearZ, m_farZ, m_aspect);

			m_frustumDirty = false;
		}
	}
}
