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
	public interface ITileSet
	{
		byte[] GetRawBitmap();
		int RawBitmapWidth { get; }

		int GetTileXOffset(int tileSize);
		int GetTileYOffset(SymbolID symbolID);
	}

	static class Helpers11
	{
		public const int MAX_MIPMAP_TILE_SIZE = 64;

		/// <summary>
		/// Create a Texture2D array which contains mipmapped versions of all symbol drawings
		/// </summary>
		public static Texture2D CreateTextures11(Device device, ITileSet tileSet)
		{
			var numDistinctBitmaps = EnumHelpers.GetEnumMax<SymbolID>() + 1;

			const int bytesPerPixel = 4;
			int maxTileSize = MAX_MIPMAP_TILE_SIZE;
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

			var bmpRaw = tileSet.GetRawBitmap();
			int bmpWidth = tileSet.RawBitmapWidth;

			using (var dataStream = DataStream.Create(bmpRaw, true, false))
			{
				for (int mipLevel = 0; mipLevel < mipLevels; ++mipLevel)
				{
					int tileSize = maxTileSize >> mipLevel;

					for (int i = 0; i < numDistinctBitmaps; ++i)
					{
						SymbolID sid = (SymbolID)i;

						int xOffset = tileSet.GetTileXOffset(tileSize);
						int yOffset = tileSet.GetTileYOffset(sid);

						dataStream.Position = yOffset * bmpWidth * bytesPerPixel + xOffset * bytesPerPixel;

						var box = new DataBox(dataStream.PositionPointer, bmpWidth * bytesPerPixel, 0);

						device.ImmediateContext.UpdateSubresource(box, atlasTexture,
							Texture2D.CalculateSubResourceIndex(mipLevel, i, mipLevels));
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
				var rgb = GameColorRGB.FromGameColor(gc);
				arr[i] = rgb.R | (rgb.G << 8) | (rgb.B << 16);
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
