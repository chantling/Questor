using System;
using System.Threading;
using System.Windows.Forms;
using D3DDetour;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;

namespace Questor
{
    public static class BeforeLogin
    {
        private static Questor _questor;
        private static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;

        public static QuestorUI questorUI { get; set; }

        public static void Main(string[] args)
        {
            Logging.Log("Args:");
            foreach (var s in args)
            {
                Logging.Log(s);
            }

            if (args.Length != 2)
            {
                Environment.Exit(0);
                Environment.FailFast("");
            }

            Cache.Instance.CharName = args[0];
            Cache.Instance.PipeName = args[1];

            Cache.Instance.WCFClient.pipeName = Cache.Instance.PipeName;
            Cache.Instance.WCFClient.GetPipeProxy.Ping();

            if (Cache.Instance.EveAccount == null)
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

            Logging.Log("Loading DirectEve with " + Cache.D3DVersion);
            if (!Cache.LoadDirectEVEInstance(Cache.D3DVersion)) return;

            Time.Instance.LoginStarted_DateTime = DateTime.UtcNow;

            try
            {
                questorUI = new QuestorUI();
                //questorUI.Visible = (Cache.Instance.WCFClient.GetPipeProxy.IsMainFormMinimized() && Cache.Instance.WCFClient.GetPipeProxy.GetEVESettings().ToggleHideShowOnMinimize) || Cache.Instance.WCFClient.GetPipeProxy.GetEveAccount(Cache.Instance.EveAccount.CharacterName).Hidden;

                Logging.Log("Launching Questor");
                _questor = new Questor();


                Logging.Log("Launching QuestorUI");
                Application.Run(questorUI);


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