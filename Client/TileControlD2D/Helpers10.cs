using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimDX;
using SlimDX.Direct3D10;
using SlimDX.Direct3D10_1;
using SlimDX.DXGI;
using SlimDX.Windows;
using SlimDX.D3DCompiler;
using Device = SlimDX.Direct3D10_1.Device1;
using DXGI = SlimDX.DXGI;

namespace Dwarrowdelf.Client.TileControl
{
	static class Helpers10
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

			var dxgiDevice = new SlimDX.DXGI.Device1(device);
			var adapter = dxgiDevice.GetParent<SlimDX.DXGI.Adapter1>();
			var factory = adapter.GetParent<Factory>();

			swapChain = new SwapChain(factory, device, swapChainDesc);

			factory.Dispose();
			adapter.Dispose();
			dxgiDevice.Dispose();

			// prevent DXGI handling of alt+enter, which doesn't work properly with Winforms
			using (var f = swapChain.GetParent<Factory>())
				f.SetWindowAssociation(windowHandle, WindowAssociationFlags.IgnoreAltEnter);

			renderTexture = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
		}
	}
}
