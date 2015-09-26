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
		
		private D3DVersion version { get; set; }

		public void RegisterFrameHook(EventHandler<EventArgs> frameHook)
		{
			
			Pulse.Initialize(version);
			
			_frameHook = frameHook;
			D3DHook.OnFrame += _frameHook;
		}
		
		private StandaloneFramework() {
			
		}
		
		public StandaloneFramework(D3DVersion version) {
			this.version = version;	
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