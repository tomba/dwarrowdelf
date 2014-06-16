using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.Direct3D;

namespace TexGen
{
	class Program
	{
		static void Main(string[] args)
		{
			var factory = new SharpDX.DXGI.Factory();

			using (var adapter = factory.GetAdapter(0))
			{
				var device = new Device(adapter, DeviceCreationFlags.None, FeatureLevel.Level_10_0);

				CreateTextures(device);
			}
		}

		static void CreateTextures(Device device)
		{
			string[] texFiles = System.IO.Directory.GetFiles("../../Data/BlockTextures");
			int numTextures = texFiles.Length;

			var format = SharpDX.DXGI.Format.B8G8R8A8_UNorm;
			var filter = FilterFlags.Linear;
			var mipFilter = FilterFlags.Linear;

			int w = 256;
			int h = 256;
			int mipLevels = 8;

			var textureArray = new Texture2D(device, new Texture2DDescription()
			{
				Width = w,
				Height = h,
				Format = format,
				MipLevels = mipLevels,
				ArraySize = numTextures,
				SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
				Usage = ResourceUsage.Default,
				BindFlags = BindFlags.ShaderResource,
				CpuAccessFlags = CpuAccessFlags.None,
			});

			var loadInfo = new ImageLoadInformation()
			{
				Width = w,
				Height = h,
				Depth = 0,
				FirstMipLevel = 0,
				MipLevels = mipLevels,
				Usage = ResourceUsage.Staging,
				BindFlags = BindFlags.None,
				CpuAccessFlags = CpuAccessFlags.Read | CpuAccessFlags.Write,
				Format = format,
				Filter = filter,
				MipFilter = mipFilter,
			};

			for (int texNum = 0; texNum < texFiles.Length; ++texNum)
			{
				using (var tex = Texture2D.FromFile<Texture2D>(device, texFiles[texNum], loadInfo))
				{
					for (int i = 0; i < mipLevels; ++i)
					{
						device.ImmediateContext.CopySubresourceRegion(tex, Resource.CalculateSubResourceIndex(i, 0, mipLevels), null,
							textureArray, Resource.CalculateSubResourceIndex(i, texNum, mipLevels), 0, 0, 0);

					}
				}
			}

			Texture2D.ToFile(device.ImmediateContext, textureArray, ImageFileFormat.Dds, "TileTextureArray.dds");
		}
	}
}
