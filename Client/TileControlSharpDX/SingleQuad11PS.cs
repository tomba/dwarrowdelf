using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using Device = SharpDX.Direct3D11.Device;
using System.Runtime.InteropServices;

namespace Dwarrowdelf.Client.TileControl
{
	class SingleQuad11PS : Component
	{
		Device m_device;
		PixelShader m_pixelShader;

		[StructLayout(LayoutKind.Sequential, Size = 16)]
		struct ShaderData
		{
			public int SimpleTint;
		}

		[StructLayout(LayoutKind.Sequential, Size = 32)]
		struct ShaderDataPerFrame
		{
			public Vector2 ColRow;	/* columns, rows */
			public Vector2 RenderOffset;
			public float TileSize;
		}

		ShaderDataPerFrame m_shaderDataPerFrame;
		Buffer m_shaderDataBufferPerFrame;

		Buffer m_shaderDataBuffer;
		ShaderResourceView m_colorBufferView;
		SamplerState m_sampler;

		Buffer m_tileBuffer;
		ShaderResourceView m_tileBufferView;

		ShaderResourceView m_tileTextureView;

		public SingleQuad11PS(Device device)
		{
			m_device = device;

			var ass = System.Reflection.Assembly.GetCallingAssembly();

			// fxc /T ps_4_0 /E PS /Fo SingleQuad11.pso SingleQuad11.ps

			using (var stream = ass.GetManifestResourceStream("Dwarrowdelf.Client.TileControl.SingleQuad11PS.hlslo"))
			{
				var bytecode = ShaderBytecode.FromStream(stream);
				Create(bytecode);
			}
		}

		void Create(ShaderBytecode bytecode)
		{
			m_pixelShader = ToDispose(new PixelShader(m_device, bytecode));

			/* Constant buffer */
			ShaderData shaderData = new ShaderData()
			{
				SimpleTint = 1,
			};

			m_shaderDataBuffer = Buffer.Create(m_device, BindFlags.ConstantBuffer, ref shaderData);

			/* Constant buffer per frame */
			m_shaderDataBufferPerFrame = ToDispose(new Buffer(m_device, Utilities.SizeOf<ShaderDataPerFrame>(),
				ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0));

			/* color buffer */
			var colorBuffer = ToDispose(Helpers11.CreateGameColorBuffer(m_device));

			m_colorBufferView = ToDispose(new ShaderResourceView(m_device, colorBuffer, new ShaderResourceViewDescription()
			{
				Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
				Dimension = ShaderResourceViewDimension.Buffer,
				Buffer = new ShaderResourceViewDescription.BufferResource()
				{
					ElementWidth = colorBuffer.Description.SizeInBytes / sizeof(uint),
					ElementOffset = 0,
				},
			}));

			/* Texture sampler */
			m_sampler = ToDispose(new SamplerState(m_device, new SamplerStateDescription()
			{
				/* Use point filtering as linear filtering causes artifacts */
				Filter = Filter.MinMagMipPoint,
				AddressU = TextureAddressMode.Wrap,
				AddressV = TextureAddressMode.Wrap,
				AddressW = TextureAddressMode.Wrap,
				BorderColor = new Color4(0),
				ComparisonFunction = Comparison.Never,
				MipLodBias = 0,
				MinimumLod = 0,
				MaximumLod = float.MaxValue,
			}));
		}

		public void SetupTileBuffer(IntSize2 gridSize)
		{
			RemoveAndDispose(ref m_tileBuffer);
			RemoveAndDispose(ref m_tileBufferView);

			m_tileBuffer = ToDispose(new SharpDX.Direct3D11.Buffer(m_device, new BufferDescription()
			{
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.Write,
				OptionFlags = ResourceOptionFlags.BufferStructured,
				SizeInBytes = gridSize.Area * Marshal.SizeOf(typeof(RenderTile)),
				StructureByteStride = Marshal.SizeOf(typeof(RenderTile)),
				Usage = ResourceUsage.Dynamic,
			}));

			m_tileBufferView = ToDispose(new ShaderResourceView(m_device, m_tileBuffer, new ShaderResourceViewDescription()
			{
				Format = SharpDX.DXGI.Format.Unknown,
				Dimension = ShaderResourceViewDimension.Buffer,
				Buffer = new ShaderResourceViewDescription.BufferResource()
				{
					ElementWidth = gridSize.Area,
					ElementOffset = 0,
				},
			}));
		}

		public void SetTileTextures(Texture2D textureArray)
		{
			RemoveAndDispose(ref m_tileTextureView);

			m_tileTextureView = ToDispose(new ShaderResourceView(m_device, textureArray, new ShaderResourceViewDescription()
			{
				Format = textureArray.Description.Format,
				Dimension = ShaderResourceViewDimension.Texture2DArray,
				Texture2DArray = new ShaderResourceViewDescription.Texture2DArrayResource()
				{
					MipLevels = textureArray.Description.MipLevels,
					MostDetailedMip = 0,
					ArraySize = textureArray.Description.ArraySize,
				},
			}));
		}

		public void SendMapData(RenderTile[] mapData, int columns, int rows)
		{
			m_shaderDataPerFrame.ColRow = new Vector2(columns, rows);

			DataStream stream;
			var box = m_device.ImmediateContext.MapSubresource(m_tileBuffer, MapMode.WriteDiscard, SharpDX.Direct3D11.MapFlags.None, out stream);
			stream.WriteRange(mapData, 0, columns * rows);
			m_device.ImmediateContext.UnmapSubresource(m_tileBuffer, 0);
			stream.Dispose();
		}

		public void SetTileSize(float tileSize)
		{
			m_shaderDataPerFrame.TileSize = tileSize;
		}

		public void SetRenderOffset(Vector2 renderOffset)
		{
			m_shaderDataPerFrame.RenderOffset = renderOffset;
		}

		public void Update()
		{
			var context = m_device.ImmediateContext;

			context.UpdateSubresource(ref m_shaderDataPerFrame, m_shaderDataBufferPerFrame);

			context.PixelShader.Set(m_pixelShader);
			context.PixelShader.SetConstantBuffer(0, m_shaderDataBuffer);
			context.PixelShader.SetConstantBuffer(1, m_shaderDataBufferPerFrame);
			context.PixelShader.SetShaderResource(0, m_tileTextureView);
			context.PixelShader.SetShaderResource(1, m_colorBufferView);
			context.PixelShader.SetShaderResource(2, m_tileBufferView);
			context.PixelShader.SetSampler(0, m_sampler);
		}
	}
}
