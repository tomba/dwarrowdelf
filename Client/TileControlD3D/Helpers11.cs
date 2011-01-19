using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D11;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using Device = SlimDX.Direct3D11.Device;

namespace Dwarrowdelf.Client.TileControl
{
	static class Helpers11
	{
		public static Texture2D CreateTextures11(Device device, ISymbolDrawingCache symbolDrawingCache)
		{
			var symbolIDArr = (SymbolID[])Enum.GetValues(typeof(SymbolID));
			var numDistinctBitmaps = (int)symbolIDArr.Max() + 1;
			int numTiles = numDistinctBitmaps;

			Texture2D atlasTexture = null;
			int maxTileSize = 64;
			int mipLevels = (int)Math.Log(maxTileSize, 2);

			SymbolBitmapCache sbc;

			for (int mipLevel = 0; mipLevel < mipLevels; ++mipLevel)
			{
				int tileSize = maxTileSize >> mipLevel;

				var bitmaps = Drawings.GetBitmaps(symbolDrawingCache, tileSize);
				sbc = new SymbolBitmapCache(symbolDrawingCache, tileSize);

				var pixelArray = new byte[tileSize * tileSize * 4];
				var dataRectangle = new DataRectangle(tileSize * 4, new DataStream(pixelArray, true, true));

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
					//bitmaps[i].CopyPixels(pixelArray, tileSize * 4, 0);
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


					dataRectangle.Data.Position = 0;

					using (var tileTex = new Texture2D(device, texDesc, dataRectangle))
					using (var tileSurface = tileTex.AsSurface())
					{
						var tileDataRect = tileSurface.Map(SlimDX.DXGI.MapFlags.Read);

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

						var dataBox = new DataBox(tileDataRect.Pitch, 0, tileDataRect.Data);
						device.ImmediateContext.UpdateSubresource(dataBox, atlasTexture, Texture2D.CalculateSubresourceIndex(mipLevel, i, mipLevels));

						tileSurface.Unmap();
					}
				}
			}

			return atlasTexture;
		}

		public static SlimDX.Direct3D11.Buffer CreateGameColorBuffer(Device device)
		{
			int numcolors = (int)GameColor.NumColors;

			var arr = new int[numcolors];
			for (int i = 0; i < numcolors; ++i)
			{
				var gc = (GameColor)i;
				arr[i] = GameColorRGB.FromGameColor(gc).ToInt32();
			}

			SlimDX.Direct3D11.Buffer colorBuffer;

			using (var stream = new DataStream(arr, true, false))
			{
				colorBuffer = new SlimDX.Direct3D11.Buffer(device, stream, new BufferDescription()
				{
					BindFlags = BindFlags.ShaderResource,
					CpuAccessFlags = CpuAccessFlags.Write,
					OptionFlags = ResourceOptionFlags.None,
					SizeInBytes = sizeof(int) * arr.Length,
					Usage = ResourceUsage.Dynamic,
				});
			}

			return colorBuffer;
		}
	}
}
