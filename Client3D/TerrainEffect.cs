using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class DirectionalLight
	{
		public Vector4 AmbientColor;
		public Vector4 DiffuseColor;
		public Vector4 SpecularColor;
		public Vector3 LightDirection;
	}

	class TerrainEffect : Effect
	{
		EffectConstantBuffer m_perObConstBuf;
		EffectParameter m_objectWorldMatrixParam;

			public TerrainEffect(GraphicsDevice device, EffectData effectData)
			: base(device, effectData)
		{

		}

		protected override void Initialize()
		{
			base.Initialize();
			this.Parameters["blockSampler"].SetResource(this.GraphicsDevice.SamplerStates.LinearClamp);

			m_perObConstBuf = this.ConstantBuffers["PerObjectBuffer"];
			m_objectWorldMatrixParam = m_perObConstBuf.Parameters["worldMatrix"];
		}

		protected override EffectPass OnApply(EffectPass pass)
		{
			return base.OnApply(pass);
		}

		public void SetPerObjectConstBuf(ref Matrix worldMatrix)
		{
			m_objectWorldMatrixParam.SetValue(worldMatrix);
			m_perObConstBuf.Update();
		}

		public Texture2D TileTextures
		{
			set { this.Parameters["blockTextures"].SetResource(value); }
		}

		public void SetDirectionalLight(DirectionalLight m_directionalLight)
		{
			this.Parameters["ambientColor"].SetValue(m_directionalLight.AmbientColor);
			this.Parameters["diffuseColor"].SetValue(m_directionalLight.DiffuseColor);
			this.Parameters["specularColor"].SetValue(m_directionalLight.SpecularColor);
			this.Parameters["lightDirection"].SetValue(m_directionalLight.LightDirection);
		}

		public Matrix ViewProjection
		{
			set { this.Parameters["g_viewProjMatrix"].SetValue(ref value); }
		}

		public Vector3 EyePos
		{
			set { this.Parameters["g_eyePos"].SetValue(ref value); }
		}

		public bool ShowBorders
		{
			set { this.Parameters["g_showBorders"].SetValue(value); }
		}

		public bool DisableLight
		{
			set { this.Parameters["g_disableLight"].SetValue(value); }
		}

		public bool DisableOcclusion
		{
			set { this.Parameters["g_disableOcclusion"].SetValue(value); }
		}
	}
}
