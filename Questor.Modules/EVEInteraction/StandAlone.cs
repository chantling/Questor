namespace DirectEve
{
	using System;
	using D3DDetour;
	using System.Diagnostics;
	using System.Runtime.InteropServices;

	public class StandaloneFramework : IFramework
	{
		private EventHandler<EventArgs> _frameHook = null;
		
		[DllImport("kernel32.dll", CharSet = CharSet.Auto)]
		public static extern IntPtr GetModuleHandle(string lpModuleName);

		public void RegisterFrameHook(EventHandler<EventArgs> frameHook)
		{
			
			
			IntPtr d3D9Loaded = IntPtr.Zero;
			IntPtr d3D11Loaded = IntPtr.Zero;

			d3D9Loaded = GetModuleHandle("d3d9.dll");
			d3D11Loaded = GetModuleHandle("d3d11.dll");

			if (d3D11Loaded != IntPtr.Zero)
			{
				Pulse.Initialize(D3DVersion.Direct3D11);
			}
			else
			{
				Pulse.Initialize(D3DVersion.Direct3D9);
			}
			
			_frameHook = frameHook;
			D3DHook.OnFrame += _frameHook;
		}

		public void RegisterLogger(EventHandler<EventArgs> logger)
		{
		}

		public void Log(string msg)
		{
			Debugger.Log(0, "", msg);
		}

		#region IDisposable Members
		public void Dispose()
		{
			D3DHook.OnFrame -= _frameHook;
			Pulse.Shutdown();
		}
		#endregion
	}
}