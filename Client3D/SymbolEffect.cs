using Dwarrowdelf;
using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class SymbolEffect : Effect
	{
		EffectConstantBuffer m_perObConstBuf;
		EffectParameter m_objectWorldMatrixParam;

		public SymbolEffect(GraphicsDevice device, EffectData effectData)
			: base(device, effectData)
		{

		}

		protected override void Initialize()
		{
			base.Initialize();

			this.Parameters["g_sampler"].SetResource(this.GraphicsDevice.SamplerStates.LinearClamp);

			m_perObConstBuf = this.ConstantBuffers["PerObjectBuffer"];
			m_objectWorldMatrixParam = m_perObConstBuf.Parameters["g_chunkOffset"];
		}

		protected override EffectPass OnApply(EffectPass pass)
		{
			var device = this.GraphicsDevice;

			device.SetRasterizerState(device.RasterizerStates.CullNone);
			device.SetVertexInputLayout(VertexInputLayout.New<SceneryVertex>(0));

			return base.OnApply(pass);
		}

		public Texture2D SymbolTextures
		{
			set { this.Parameters["g_texture"].SetResource(value); }
		}

		public Matrix ViewProjection
		{
			set { this.Parameters["g_viewProjMatrix"].SetValue(ref value); }
		}

		public void SetPerObjectConstBuf(IntVector3 offset)
		{
			m_objectWorldMatrixParam.SetValue(offset.ToVector3());
			m_perObConstBuf.Update();
		}

		public Vector3 EyePos
		{
			set { this.Parameters["gEyePosW"].SetValue(ref value); }
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct SceneryVertex
	{
		[VertexElement("POSITION")]
		public Vector3 Position;
		[VertexElement("COLOR")]
		public Color Color;
		[VertexElement("TEXIDX")]
		public uint TexIdx;

		public SceneryVertex(Vector3 pos, Color color, uint texIdx)
		{
			this.Position = pos;
			this.Color = color;
			this.TexIdx = texIdx;
		}
	}
}
