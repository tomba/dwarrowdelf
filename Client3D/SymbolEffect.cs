using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client3D
{
	class SymbolEffect : Effect
	{
		public SymbolEffect(GraphicsDevice device, EffectData effectData)
			: base(device, effectData)
		{

		}

		protected override void Initialize()
		{
			base.Initialize();

			this.Parameters["g_sampler"].SetResource(this.GraphicsDevice.SamplerStates.LinearClamp);
		}

		public Texture2D SymbolTextures
		{
			set { this.Parameters["g_texture"].SetResource(value); }
		}

		public Matrix WorldViewProjection
		{
			set { this.Parameters["gWorldViewProj"].SetValue(ref value); }
		}

		public Vector3 EyePos
		{
			set { this.Parameters["gEyePosW"].SetValue(ref value); }
		}

		public int Mode
		{
			set { this.Parameters["g_mode"].SetValue(value); }
		}
	}
}
