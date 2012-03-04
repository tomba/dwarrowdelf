using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using D3D9 = SharpDX.Direct3D9;
using D3D11 = SharpDX.Direct3D11;
using SharpDX.Direct3D11;

namespace Dwarrowdelf.Client.TileControl
{
	sealed class D3DImageSharpDX : D3DImage, IDisposable
	{
		[DllImport("user32.dll", SetLastError = false)]
		static extern IntPtr GetDesktopWindow();

		static int s_numActiveImages = 0;
		static D3D9.Direct3DEx s_context;
		static D3D9.DeviceEx s_device;

		D3D9.Texture m_sharedTexture;

		public D3DImageSharpDX()
		{
			InitD3D9();
		}

		public void Dispose()
		{
			SetBackBufferSlimDX(null);

			ShutdownD3D9();
		}

		public void InvalidateD3DImage()
		{
			if (m_sharedTexture != null)
			{
				Lock();
				AddDirtyRect(new Int32Rect(0, 0, PixelWidth, PixelHeight));
				Unlock();
			}
		}

		public void SetBackBufferSlimDX(D3D11.Texture2D texture)
		{
			if (m_sharedTexture != null)
			{
				m_sharedTexture.Dispose();
				m_sharedTexture = null;
			}

			if (texture == null)
			{
				Lock();
				SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
				Unlock();
			}
			else if (IsShareable(texture))
			{
				D3D9.Format format = TranslateFormat(texture);
				if (format == D3D9.Format.Unknown)
					throw new ArgumentException("Texture format is not compatible with OpenSharedResource");

				IntPtr handle = GetSharedHandle(texture);
				if (handle == IntPtr.Zero)
					throw new ArgumentNullException("Handle");

				m_sharedTexture = new D3D9.Texture(s_device, texture.Description.Width, texture.Description.Height, 1,
					D3D9.Usage.RenderTarget, format, D3D9.Pool.Default, ref handle);

				using (D3D9.Surface surface = m_sharedTexture.GetSurfaceLevel(0))
				{
					Lock();
					SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
					Unlock();
				}
			}
			else
			{
				throw new ArgumentException("Texture must be created with ResourceOptionFlags.Shared");
			}
		}

		static void InitD3D9()
		{
			if (s_numActiveImages == 0)
			{
				s_context = new D3D9.Direct3DEx();

				var presentparams = new D3D9.PresentParameters()
				{
					Windowed = true,
					SwapEffect = D3D9.SwapEffect.Discard,
					DeviceWindowHandle = GetDesktopWindow(),
					PresentationInterval = D3D9.PresentInterval.Immediate,
				};

				s_device = new D3D9.DeviceEx(s_context, 0, D3D9.DeviceType.Hardware, IntPtr.Zero,
					D3D9.CreateFlags.HardwareVertexProcessing | D3D9.CreateFlags.Multithreaded | D3D9.CreateFlags.FpuPreserve, presentparams);
			}

			s_numActiveImages++;
		}

		static void ShutdownD3D9()
		{
			s_numActiveImages--;

			if (s_numActiveImages == 0)
			{
				if (s_device != null)
				{
					s_device.Dispose();
					s_device = null;
				}

				if (s_context != null)
				{
					s_context.Dispose();
					s_context = null;
				}
			}
		}

		static IntPtr GetSharedHandle(D3D11.Texture2D texture)
		{
			var resource = texture.QueryInterface<SharpDX.DXGI.Resource>();
			IntPtr result = resource.SharedHandle;
			resource.Dispose();
			return result;
		}

		static D3D9.Format TranslateFormat(D3D11.Texture2D texture)
		{
			switch (texture.Description.Format)
			{
				case SharpDX.DXGI.Format.R10G10B10A2_UNorm:
					return SharpDX.Direct3D9.Format.A2B10G10R10;

				case SharpDX.DXGI.Format.R16G16B16A16_Float:
					return SharpDX.Direct3D9.Format.A16B16G16R16F;

				case SharpDX.DXGI.Format.B8G8R8A8_UNorm:
					return SharpDX.Direct3D9.Format.A8R8G8B8;

				default:
					return SharpDX.Direct3D9.Format.Unknown;
			}
		}

		static bool IsShareable(D3D11.Texture2D texture)
		{
			return (texture.Description.OptionFlags & D3D11.ResourceOptionFlags.Shared) != 0;
		}
	}
}
