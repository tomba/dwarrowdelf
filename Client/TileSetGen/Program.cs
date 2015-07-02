using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace Dwarrowdelf.Client
{
	class Program
	{
		static int Main(string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("Bad args");
				return 1;
			}

			if (Directory.Exists(args[0]) == false)
			{
				Console.WriteLine("Source dir {0} doesn't exist", args[0]);
				return 1;
			}

			if (Directory.Exists(args[1]) == false)
			{
				Console.WriteLine("Source dir {0} doesn't exist", args[1]);
				return 1;
			}

			string srcPath = args[0];
			string dstPath = args[1];

			var loader = new TileSetLoader(srcPath);
			loader.Load();

			CreatePng(loader, dstPath);

			using (var factory = new SharpDX.DXGI.Factory())
			using (var adapter = factory.GetAdapter(0))
			using (var device = new Device(adapter, DeviceCreationFlags.None, FeatureLevel.Level_10_0))
			{
				CreateDds(device, loader, dstPath);
			}

			return 0;
		}

		static void CreatePng(TileSetLoader loader, string dstPath)
		{
			int numSymbols = EnumHelpers.GetEnumMax<SymbolID>() + 1;

			int[] tileSizes = new int[] {
				8, 10, 12,
				16, 20, 24,
				32, 40, 48,
				64, 80, 96
			};

			int maxTileSize = tileSizes.Max();

			WriteableBitmap target = new WriteableBitmap(tileSizes.Sum(), maxTileSize * numSymbols, 96, 96,
				PixelFormats.Bgra32, null);

			target.Lock();

			// leave the first one (Undefined) empty
			for (int i = 1; i < numSymbols; ++i)
			{
				int xOffset = 0;

				foreach (int tileSize in tileSizes)
				{
					var source = loader.GetTileBitmap((SymbolID)i, tileSize);

					int stride = source.PixelWidth * (source.Format.BitsPerPixel / 8);
					byte[] data = new byte[stride * source.PixelHeight];

					source.CopyPixels(data, stride, 0);

					target.WritePixels(new Int32Rect(xOffset, i * maxTileSize, tileSize, tileSize), data, stride, 0);

					xOffset += tileSize;
				}
			}

			target.Unlock();

			string tileSizesStr = string.Join(",", tileSizes.Select(i => i.ToString()));
			var pngEncoder = new PngBitmapEncoder();
			var metadata = new BitmapMetadata("png");
			metadata.SetQuery("/tEXt/Software", "Dwarrowdelf");
			metadata.SetQuery("/tEXt/tilesizes", tileSizesStr);
			var frame = BitmapFrame.Create(target, null, metadata, null);
			pngEncoder.Frames = new BitmapFrame[] { frame };

			string path = Path.Combine(dstPath, "TileSet.png");

			using (var stream = File.OpenWrite(path))
				pngEncoder.Save(stream);

			Console.WriteLine("Generated TileSet to {0}", path);
		}

		static void CreateDds(Device device, TileSetLoader loader, string dstPath)
		{
			int numDistinctBitmaps = EnumHelpers.GetEnumMax<SymbolID>() + 1;

			const int bytesPerPixel = 4;
			int maxTileSize = 64;
			int mipLevels = 6; // 64, 32, 16, 8, 4, 2

			var atlasTexture = new Texture2D(device, new Texture2DDescription()
			{
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,

				Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
				Width = maxTileSize,
				Height = maxTileSize,
				SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),

				ArraySize = numDistinctBitmaps,
				MipLevels = mipLevels,
			});

			//int autoGenMipLevel = 0;

			for (int mipLevel = 0; mipLevel < mipLevels; ++mipLevel)
			{
				int tileSize = maxTileSize >> mipLevel;

				/*
				if (tileSet.HasTileSize(tileSize) == false)
				{
					autoGenMipLevel = mipLevel - 1;
					break;
				}
				*/

				// leave the first one (Undefined) empty
				for (int i = 1; i < numDistinctBitmaps; ++i)
				{
					var bmp = loader.GetTileBitmap((SymbolID)i, tileSize);

					int pitch = tileSize * bytesPerPixel;

					var arr = new uint[tileSize * tileSize];

					bmp.CopyPixels(arr, pitch, 0);

					device.ImmediateContext.UpdateSubresource(arr, atlasTexture,
						Texture2D.CalculateSubResourceIndex(mipLevel, i, mipLevels), pitch);
				}
			}

			// Generate mipmaps for the smallest tiles
			//atlasTexture.FilterTexture(device.ImmediateContext, autoGenMipLevel, FilterFlags.Triangle);

			string path = Path.Combine(dstPath, "TileSet.dds");
			Texture2D.ToFile(device.ImmediateContext, atlasTexture, ImageFileFormat.Dds, path);
			Console.WriteLine("Generated TileSet to {0}", path);
		}
	}
}
