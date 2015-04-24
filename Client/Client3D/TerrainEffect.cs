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
		VertexInputLayout m_vertexInputLayout = VertexInputLayout.New<TerrainVertex>(0);

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

			var buf = ToDispose(Buffer<int>.New(this.GraphicsDevice, arr, BufferFlags.ShaderResource, SharpDX.DXGI.Format.R8G8B8A8_UNorm,
				SharpDX.Direct3D11.ResourceUsage.Immutable));

			this.Parameters["g_colorBuffer"].SetResource(buf);
		}

		protected override EffectPass OnApply(EffectPass pass)
		{
			var device = this.GraphicsDevice;

			device.SetVertexInputLayout(m_vertexInputLayout);

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
		// Background Color
		public GameColor Color0;
		// Symbol1 Color
		public GameColor Color1;
		// Symbol2 Color
		public GameColor Color2;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	struct TerrainVertex
	{
		[VertexElement("POSITION0", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 Position0;
		[VertexElement("POSITION1", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 Position1;
		[VertexElement("POSITION2", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 Position2;
		[VertexElement("POSITION3", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 Position3;
		[VertexElement("OCCLUSION", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 Occlusion;
		[VertexElement("TEX", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 TexPack;
		[VertexElement("COLOR", SharpDX.DXGI.Format.R8G8B8A8_UInt)]
		public Byte4 ColorPack;

		// vertices in order: top right, bottom right, bottom left, top left
		public TerrainVertex(IntVector3 p0, IntVector3 p1, IntVector3 p2, IntVector3 p3,
			int occ0, int occ1, int occ2, int occ3, FaceTexture tex)
		{
			// last bytes of positions are unused
			this.Position0 = new Byte4(p3.X, p3.Y, p3.Z, 0);
			this.Position1 = new Byte4(p0.X, p0.Y, p0.Z, 0);
			this.Position2 = new Byte4(p2.X, p2.Y, p2.Z, 0);
			this.Position3 = new Byte4(p1.X, p1.Y, p1.Z, 0);
			this.Occlusion = new Byte4(occ3, occ0, occ2, occ1);
			this.TexPack = new Byte4((byte)0, (byte)tex.Symbol1, (byte)tex.Symbol2, (byte)0);
			this.ColorPack = new Byte4((byte)tex.Color0, (byte)tex.Color1, (byte)tex.Color2, (byte)0);
		}
	}
}
