
using global::Questor.Modules.Lookup;

namespace Questor
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Forms;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Timers;
	using System.Xml.Linq;
	using global::Questor.Modules.Activities;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Caching;
	using global::Questor.Modules.Logging;
	using global::Questor.Modules.States;
	using global::Questor.Modules.Lookup;
	using DirectEve;
	

	public static class BeforeLogin
	{

		private static Questor _questor;
		
		private static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
		
		public static QuestorUI questorUI { get; set; }
		
		public static void Main(string[] args)
		{
			
			Logging.Log("Startup", "Args:", Logging.Teal);
			foreach(string s in args) {
				Logging.Log("Startup ", s, Logging.Teal);
			}
			
			if(args.Length != 2)
			{
				Environment.Exit(0);
				Environment.FailFast("");
			}
			
			Cache.Instance.CharName = args[0];
			Cache.Instance.PipeName = args[1];
			
			Cache.Instance.WcfClient.pipeName = Cache.Instance.PipeName;
            Cache.Instance.WcfClient.GetPipeProxy.Ping();

            if (Cache.Instance.EveAccount == null)
            {
                Environment.Exit(0);
                Environment.FailFast("");
            }

            Logging.EVELoginUserName = Cache.Instance.EveAccount.AccountName;
            Logging.EVELoginPassword = Cache.Instance.EveAccount.Password;
            Logging.MyCharacterName = Cache.Instance.EveAccount.CharacterName;

            Cache.D3DVersion = Cache.Instance.EveAccount.DX11 ? D3DDetour.D3DVersion.Direct3D11 : D3DDetour.D3DVersion.Direct3D9;

            if (!string.IsNullOrEmpty(Logging.EVELoginUserName) && !string.IsNullOrEmpty(Logging.EVELoginPassword) && !string.IsNullOrEmpty(Logging.MyCharacterName))
			{
				LoginToEVE.ReadyToLoginToEVEAccount = true;
			}
			
			Logging.Log("Startup", "Loading DirectEve with " + Cache.D3DVersion, Logging.Teal);
			if (!LoginToEVE.LoadDirectEVEInstance(Cache.D3DVersion)) return;
			
			Time.Instance.LoginStarted_DateTime = DateTime.UtcNow;
			
			try
			{
				
				//new SubModules.Debug();
				
				questorUI = new QuestorUI();
				
				Logging.Log("Startup", "Launching Questor", Logging.Teal);
				_questor = new Questor();
				
				
				Logging.Log("Startup", "Launching QuestorUI", Logging.Teal);
				Application.Run(questorUI);
				

				while (!Cleanup.SignalToQuitQuestor)
				{
					System.Threading.Thread.Sleep(50);
				}

				Logging.Log("Startup", "Exiting Questor", Logging.Teal);

			}
			catch (Exception ex)
			{
				Logging.Log("Startup", "Exception [" + ex + "]", Logging.Teal);
			}
			finally
			{
				Cleanup.DirecteveDispose();
				AppDomain.Unload(AppDomain.CurrentDomain);
			}
			
		}
	}
}