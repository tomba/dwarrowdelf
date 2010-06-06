using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Windows.Media;

namespace MyGame.Client
{
    class D2DD3DImage: D3DImage, IDisposable
    {
        // Interop
        SurfaceQueueInteropHelper helper;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        protected override System.Windows.Freezable CreateInstanceCore()
        {
            return new D2DD3DImage();
        }

        public void SetPixelSize(uint pixelWidth, uint pixelHeight)
        {
            EnsureHelper();
            helper.SetPixelSize(pixelWidth, pixelHeight);
        }

        public IntPtr HWNDOwner
        {
            get { return (IntPtr)GetValue(HWNDOwnerProperty); }
            set { SetValue(HWNDOwnerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HWNDOwner.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HWNDOwnerProperty =
            DependencyProperty.Register("HWNDOwner", typeof(IntPtr), typeof(D2DD3DImage), new UIPropertyMetadata(IntPtr.Zero, HWNDOwnerChanged));

        public Action<IntPtr> OnRender
        {
            get { return (Action<IntPtr>)GetValue(OnRenderProperty); }
            set { SetValue(OnRenderProperty, value); }
        }

        // Using a DependencyProperty as the backing store for OnRender.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty OnRenderProperty =
            DependencyProperty.Register("OnRender", typeof(Action<IntPtr>), typeof(D2DD3DImage), new UIPropertyMetadata(null, RenderChanged));

        public void RequestRender()
        {
            EnsureHelper();

            // Don't bother with a call if there's no callback registered.
            if (null != this.OnRender)
            {
                helper.RequestRenderD2D();
            }
        }

        #region Helpers
        private void EnsureHelper()
        {
            if (helper == null)
            {
                helper = new SurfaceQueueInteropHelper();
                helper.HWND = this.HWNDOwner;
                helper.D3DImage = this;
                helper.RenderD2D = this.OnRender;
            }
        }
        #endregion Helpers

        #region Callbacks

        static void HWNDOwnerChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            D2DD3DImage image = sender as D2DD3DImage;

            if (image != null)
            {
                if (image.helper != null)
                {
                    image.helper.HWND = (IntPtr)args.NewValue;
                }
            }
        }

        static void RenderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            D2DD3DImage image = sender as D2DD3DImage;

            if (image != null)
            {
                if (image.helper != null)
                {
                    image.helper.RenderD2D = (Action<IntPtr>)args.NewValue;
                }
            }
        }

        #endregion Callbacks

        #region IDisposable Members

        public void Dispose()
        {
            if (helper != null)
            {
                helper.Dispose();
                helper = null;
            }
        }

        #endregion
    }
}
