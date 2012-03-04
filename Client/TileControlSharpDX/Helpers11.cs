using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Device = SharpDX.Direct3D11.Device;
using DXGI = SharpDX.DXGI;

namespace Dwarrowdelf.Client.TileControl
{
	static class Helpers11
	{
		public static Device CreateDevice()
		{
			return new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport, FeatureLevel.Level_10_0);
		}

		public static Texture2D CreateTextureRenderSurface(Device device, int width, int height)
		{
			var texDesc = new Texture2DDescription()
			{
				BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
				Format = DXGI.Format.B8G8R8A8_UNorm,
				Width = width,
				Height = height,
				MipLevels = 1,
				SampleDescription = new DXGI.SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				OptionFlags = ResourceOptionFlags.Shared,
				CpuAccessFlags = CpuAccessFlags.None,
				ArraySize = 1,
			};

			return new Texture2D(device, texDesc);
		}


		public static void CreateHwndRenderSurface(IntPtr windowHandle, Device device, int width, int height, out Texture2D renderTexture, out SwapChain swapChain)
		{
			var swapChainDesc = new SwapChainDescription()
			{
				BufferCount = 1,
				ModeDescription = new ModeDescription(width, height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
				IsWindowed = true,
				OutputHandle = windowHandle,
				SampleDescription = new SampleDescription(1, 0),
				SwapEffect = SwapEffect.Discard,
				Usage = Usage.RenderTargetOutput
			};

			using (var dxgiDevice = new SharpDX.DXGI.Device1(device.NativePointer))
			using (var adapter = dxgiDevice.GetParent<SharpDX.DXGI.Adapter1>())
			using (var factory = adapter.GetParent<Factory>())
			{
				swapChain = new SwapChain(factory, device, swapChainDesc);
			}

			renderTexture = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
		}

		public static Texture2D CreateTextures11(Device device, ISymbolDrawingCache symbolDrawingCache)
		{
			var numDistinctBitmaps = EnumHelpers.GetEnumMax<SymbolID>() + 1;

			Texture2D atlasTexture = null;
			int maxTileSize = 64;
			int mipLevels = (int)Math.Log(maxTileSize, 2);

			SymbolBitmapCache sbc;

			for (int mipLevel = 0; mipLevel < mipLevels; ++mipLevel)
			{
				int tileSize = maxTileSize >> mipLevel;

				sbc = new SymbolBitmapCache(symbolDrawingCache, tileSize);

				var pixelArray = new byte[tileSize * tileSize * 4];

				var texDesc = new Texture2DDescription()
				{
					CpuAccessFlags = CpuAccessFlags.Read,
					Format = Format.B8G8R8A8_UNorm,
					Width = tileSize,
					Height = tileSize,
					Usage = ResourceUsage.Staging,
					MipLevels = 1,
					BindFlags = BindFlags.None,
					ArraySize = 1,
					SampleDescription = new SampleDescription(1, 0),
				};

				for (int i = 0; i < numDistinctBitmaps; ++i)
				{
					var bmp = sbc.GetBitmap((SymbolID)i, GameColor.None);
					bmp.CopyPixels(pixelArray, tileSize * 4, 0);
#if COLORMIPMAPS
					byte r, g, b;
					switch (mipLevel)
					{
						case 0: r = 255; g = 0; b = 0; break;
						case 1: r = 0; g = 255; b = 0; break;
						case 2: r = 0; g = 0; b = 255; break;
						case 3: r = 255; g = 255; b = 0; break;
						case 4: r = 255; g = 0; b = 255; break;
						case 5: r = 0; g = 255; b = 255; break;

						default: throw new Exception();
					}

					for (int y = 0; y < tileSize; ++y)
					{
						for (int x = 0; x < tileSize; ++x)
						{
							pixelArray[y * tileSize * 4 + x * 4 + 0] = b;
							pixelArray[y * tileSize * 4 + x * 4 + 1] = g;
							pixelArray[y * tileSize * 4 + x * 4 + 2] = r;
							pixelArray[y * tileSize * 4 + x * 4 + 3] = 255;
						}
					}
#endif

#if TEST
					for (int y = 0; y < tileSize; ++y)
					{
						for (int x = 0; x < tileSize; ++x)
						{
							if (x == y)
							{
								pixelArray[y * tileSize * 4 + x * 4 + 0] = 255;
								pixelArray[y * tileSize * 4 + x * 4 + 1] = (byte)y;
								pixelArray[y * tileSize * 4 + x * 4 + 2] = (byte)(x + y);
								pixelArray[y * tileSize * 4 + x * 4 + 3] = 255;
							}
						}
					}
#endif

					using (var dataStream = DataStream.Create(pixelArray, true, true))
					{
						var dataRectangle = new DataRectangle(dataStream.DataPointer, tileSize * 4);

						using (var tileTex = new Texture2D(device, texDesc, dataRectangle))
						using (var tileSurface = new Surface(tileTex.NativePointer))
						{
							if (atlasTexture == null)
							{
								Texture2DDescription desc = tileTex.Description;
								desc.ArraySize = numDistinctBitmaps;
								desc.Usage = ResourceUsage.Default;
								desc.BindFlags = BindFlags.ShaderResource;
								desc.CpuAccessFlags = CpuAccessFlags.None;
								desc.MipLevels = mipLevels;
								atlasTexture = new Texture2D(device, desc);
							}

							device.ImmediateContext.CopySubresourceRegion(tileTex, 0, null, atlasTexture, Texture2D.CalculateSubResourceIndex(mipLevel, i, mipLevels), 0, 0, 0);
						}
					}
				}
			}

			return atlasTexture;
		}

		public static SharpDX.Direct3D11.Buffer CreateGameColorBuffer(Device device)
		{
			int numcolors = GameColorRGB.NUMCOLORS;

			var arr = new int[numcolors];
			for (int i = 0; i < numcolors; ++i)
			{
				var gc = (GameColor)i;
				arr[i] = GameColorRGB.FromGameColor(gc).ToInt32();
			}

			SharpDX.Direct3D11.Buffer colorBuffer;


			using (var stream = DataStream.Create(arr, true, false))
			{
				colorBuffer = new SharpDX.Direct3D11.Buffer(device, stream, new BufferDescription()
				{
					BindFlags = BindFlags.ShaderResource,
					CpuAccessFlags = CpuAccessFlags.None,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = sizeof(int) * arr.Length,
					Usage = ResourceUsage.Immutable,
				});
			}

			return colorBuffer;
		}
	}
}
