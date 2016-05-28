using System;
using System.Threading;
using System.Windows.Forms;
using D3DDetour;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Controllers;

namespace Questor
{
	public static class Program
	{
		public  static QuestorUI QuestorUIInstance { get; set; }
		public static QuestorControllerManager QuestorControllerManagerInstance { get; set; }

		public static void Main(string[] args)
		{
			Logging.Log("Args:");
			foreach (var s in args)
			{
				Logging.Log(s);
			}
			
			Cache.Instance.CharName = args[0];
			Cache.Instance.PipeName = args[1];

			Cache.Instance.WCFClient.pipeName = Cache.Instance.PipeName;
			Cache.Instance.WCFClient.GetPipeProxy.Ping();

			if (Cache.Instance.EveAccount == null || args.Length != 2)
			{
				Environment.Exit(0);
				Environment.FailFast("");
			}

			Logging.EVELoginUserName = Cache.Instance.EveAccount.AccountName;
			Logging.EVELoginPassword = Cache.Instance.EveAccount.Password;
			Logging.MyCharacterName = Cache.Instance.EveAccount.CharacterName;

			Cache.D3DVersion = Cache.Instance.EveAccount.DX11 ? D3DVersion.Direct3D11 : D3DVersion.Direct3D9;

			if (string.IsNullOrEmpty(Logging.EVELoginUserName) || string.IsNullOrEmpty(Logging.EVELoginPassword) ||
			    string.IsNullOrEmpty(Logging.MyCharacterName))
			{
				return;
			}

			Time.Instance.LoginStarted_DateTime = DateTime.UtcNow;

			try
			{
				Logging.Log("Launching QuestorControllerManager");
				
				QuestorControllerManagerInstance = new QuestorControllerManager();
				QuestorControllerManagerInstance.AddController(new LoginController());
				QuestorControllerManagerInstance.AddController(new QuestorController());

				QuestorUIInstance = new QuestorUI();
				Logging.Log("Launching QuestorControllerUI");
				Application.Run(QuestorUIInstance);


				while (!Cleanup.SignalToQuitQuestor)
				{
					Thread.Sleep(50);
				}

				Logging.Log("Exiting Questor");
			}
			catch (Exception ex)
			{
				Logging.Log("Exception [" + ex + "]");
			}
			finally
			{
				Cleanup.DirecteveDispose();
				AppDomain.Unload(AppDomain.CurrentDomain);
			}
		}
	}
}