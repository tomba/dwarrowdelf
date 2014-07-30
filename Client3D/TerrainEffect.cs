using SharpDX;
using SharpDX.Toolkit.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Dwarrowdelf;
using Dwarrowdelf.Client;

namespace Client3D
{
	class DirectionalLight
	{
		public Vector3 AmbientColor;
		public Vector3 DiffuseColor;
		public Vector3 SpecularColor;
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

			if (this.Parameters.Contains("blockSampler"))
				this.Parameters["blockSampler"].SetResource(this.GraphicsDevice.SamplerStates.LinearClamp);

			m_perObConstBuf = this.ConstantBuffers["PerObjectBuffer"];
			m_objectWorldMatrixParam = m_perObConstBuf.Parameters["g_chunkOffset"];

			CreateGameColorBuffer();
		}

		/// <summary>
		/// Create a buffer containing all GameColors
		/// </summary>
		public void CreateGameColorBuffer()
		{
			var arr = new int[GameColorRGB.NUMCOLORS];
			for (int i = 0; i < arr.Length; ++i)
			{
				var gc = (GameColor)i;
				var rgb = GameColorRGB.FromGameColor(gc);
				arr[i] = rgb.R | (rgb.G << 8) | (rgb.B << 16);
			}

			var buf = Buffer<int>.New(this.GraphicsDevice, arr, BufferFlags.ShaderResource, SharpDX.DXGI.Format.R8G8B8A8_UNorm,
				SharpDX.Direct3D11.ResourceUsage.Immutable);

			this.Parameters["g_colorBuffer"].SetResource(buf);
		}

		protected override EffectPass OnApply(EffectPass pass)
		{
			var device = this.GraphicsDevice;

			device.SetVertexInputLayout(VertexInputLayout.New<TerrainVertex>(0));

			return base.OnApply(pass);
		}

		public void SetPerObjectConstBuf(IntVector3 offset)
		{
			m_objectWorldMatrixParam.SetValue(offset.ToVector3());
			m_perObConstBuf.Update();
		}

		public Texture2D TileTextures
		{
			set
			{
				if (this.Parameters.Contains("blockTextures"))
					this.Parameters["blockTextures"].SetResource(value);
			}
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

		public bool DisableBorders
		{
			set { this.Parameters["g_disableBorders"].SetValue(value); }
		}

		public bool DisableLight
		{
			set { this.Parameters["g_disableLight"].SetValue(value); }
		}

		public bool DisableOcclusion
		{
			set { this.Parameters["g_disableOcclusion"].SetValue(value); }
		}

		public bool DisableTexture
		{
			set { this.Parameters["g_disableTexture"].SetValue(value); }
		}
	}

	struct FaceTexture
	{
		//public SymbolID Symbol0;
		public SymbolID Symbol1;
		public SymbolID Symbol2;
		public GameColor Color0;
		public GameColor Color1;
		public GameColor Color2;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct TerrainVertex
	{
		[VertexElement("POSITION", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 Position;
		[VertexElement("OCCLUSION")]
		public int Occlusion;	/* xxx pack into some other field */
		[VertexElement("TEX", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 TexPack;
		[VertexElement("COLOR", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 ColorPack;

		public TerrainVertex(IntVector3 pos, int occlusion, FaceTexture tex)
		{
			this.Position = new Byte4(pos.X, pos.Y, pos.Z, 0);
			this.Occlusion = occlusion;
			this.TexPack = new Byte4((byte)0, (byte)tex.Symbol1, (byte)tex.Symbol2, (byte)0);
			this.ColorPack = new Byte4((byte)tex.Color0, (byte)tex.Color1, (byte)tex.Color2, (byte)0);
		}
	}
}
