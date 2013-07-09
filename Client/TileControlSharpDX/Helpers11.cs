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
		/// <summary>
		/// Create a Texture2D array which contains mipmapped versions of all symbol drawings
		/// </summary>
		public static Texture2D CreateTextures11(Device device, TileSet tileSet)
		{
			var numDistinctBitmaps = EnumHelpers.GetEnumMax<SymbolID>() + 1;

			const int bytesPerPixel = 4;
			int maxTileSize = 64;
			int mipLevels = 4; // 64, 32, 16, 8

			var atlasTexture = new Texture2D(device, new Texture2DDescription()
			{
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,

				Format = Format.B8G8R8A8_UNorm,
				Width = maxTileSize,
				Height = maxTileSize,
				SampleDescription = new SampleDescription(1, 0),

				ArraySize = numDistinctBitmaps,
				MipLevels = mipLevels,
			});

			var bmp = tileSet.Atlas;

			for (int mipLevel = 0; mipLevel < mipLevels; ++mipLevel)
			{
				int tileSize = maxTileSize >> mipLevel;

				var pixelArray = new byte[tileSize * tileSize * bytesPerPixel];

				for (int i = 0; i < numDistinctBitmaps; ++i)
				{
					SymbolID sid = (SymbolID)i;
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
#elif TEST
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
#else
					int xOffset = tileSet.GetTileXOffset(tileSize);
					int yOffset = tileSet.GetTileYOffset(sid);

					var srcRect = new System.Windows.Int32Rect(xOffset, yOffset, tileSize, tileSize);

					bmp.CopyPixels(srcRect, pixelArray, tileSize * bytesPerPixel, 0);
#endif

					using (var dataStream = DataStream.Create(pixelArray, true, false))
					{
						var box = new DataBox(dataStream.DataPointer, tileSize * bytesPerPixel, 0);
						device.ImmediateContext.UpdateSubresource(box, atlasTexture, Texture2D.CalculateSubResourceIndex(mipLevel, i, mipLevels));
					}
				}
			}

			return atlasTexture;
		}

		/// <summary>
		/// Create a buffer containing all GameColors
		/// </summary>
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
