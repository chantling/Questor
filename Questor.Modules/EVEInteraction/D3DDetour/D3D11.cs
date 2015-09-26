using EasyHook;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace D3DDetour
{
    public class D3D11 : D3DHook
    {
        private Direct3D11Present _presentDelegate;
        private LocalHook Hook;

        public override unsafe void Initialize()
        {
            Form form = new Form();
            SwapChainDescription chainDescription = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription = new ModeDescription()
                {
                    Format = 28
                },
                Usage = 32,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription()
                {
                    Count = 1
                },
                IsWindowed = true
            };

            IntPtr ppSwapChain = IntPtr.Zero;
            IntPtr ppDevice = IntPtr.Zero;
            IntPtr ppImmediateContext = IntPtr.Zero;
            IntPtr pFeatureLevel = IntPtr.Zero;
            int ret = D3D11CreateDeviceAndSwapChain(IntPtr.Zero, 1, IntPtr.Zero, 0, IntPtr.Zero, 0, 7, &chainDescription, out ppSwapChain, out ppDevice, out pFeatureLevel, out ppImmediateContext);
            IntPtr ptr = Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppSwapChain), 32);
            D3DVirtVoid d3dChain = (D3DVirtVoid)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppSwapChain), 8), typeof(D3DVirtVoid));
            D3DVirtVoid d3dDevice = (D3DVirtVoid)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppDevice), 8), typeof(D3DVirtVoid));
            D3DVirtVoid d3dContext = (D3DVirtVoid)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(Marshal.ReadIntPtr(ppImmediateContext), 8), typeof(D3DVirtVoid));
            d3dChain(ppSwapChain);
            d3dDevice(ppDevice);
            d3dContext(ppImmediateContext);
            _presentDelegate = (Direct3D11Present)Marshal.GetDelegateForFunctionPointer(ptr, typeof(Direct3D11Present));
            Hook = LocalHook.Create(ptr, (Delegate)new Direct3D11Present(Callback), (object)this);
            Hook.ThreadACL.SetExclusiveACL(new int[1]);
        }

        private int Callback(IntPtr swapChainPtr, int syncInterval, int flags)
        {
            RaiseEvent();
            return _presentDelegate(swapChainPtr, syncInterval, flags);
        }

        public override void Remove()
        {
            Hook.Dispose();
        }

        [DllImport("d3d11.dll")]
        private static extern unsafe int D3D11CreateDeviceAndSwapChain(IntPtr pAdapter, int driverType, IntPtr Software,
        int flags, IntPtr pFeatureLevels,
        int FeatureLevels, int SDKVersion,
        void* pSwapChainDesc, [Out] out IntPtr ppSwapChain,
        [Out] out IntPtr ppDevice, [Out] out IntPtr pFeatureLevel,
        [Out] out IntPtr ppImmediateContext);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void D3DVirtVoid(IntPtr istance);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int Direct3D11Present(IntPtr swapChainPtr, int syncInterval, int flags);

        [StructLayout(LayoutKind.Sequential)]
        public struct SwapChainDescription
        {
            public ModeDescription ModeDescription;
            public SampleDescription SampleDescription;
            public int Usage;
            public int BufferCount;
            public IntPtr OutputHandle;
            [MarshalAs(UnmanagedType.Bool)]
            public bool IsWindowed;
            public int SwapEffect;
            public int Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rational
        {
            public int Numerator;
            public int Denominator;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ModeDescription
        {
            public int Width;
            public int Height;
            public Rational RefreshRate;
            public int Format;
            public int ScanlineOrdering;
            public int Scaling;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SampleDescription
        {
            public int Count;
            public int Quality;
        }
    }
}
