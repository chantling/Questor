
namespace Questor.Modules.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Timers;
    using System.Xml.Linq;
    using DirectEve;
    using global::Questor.Modules.BackgroundTasks;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.States;
    public static class LoginToEVE
    {
        public static bool loggedInAndreadyToStartQuestorUI;
        public static bool useLoginOnFrameEvent;
        public static List<CharSchedule> CharSchedules { get; private set; }
        public static DateTime QuestorProgramLaunched = DateTime.UtcNow;
        
        public static DateTime QuestorSchedulerReadyToLogin = DateTime.UtcNow;
        public static DateTime EVEAccountLoginStarted = DateTime.UtcNow;
        public static DateTime NextSlotActivate = DateTime.UtcNow;
        public static bool _loginOnly;
        public static bool _showHelp;

        private static bool __chantlingScheduler;

        public static bool _chantlingScheduler
        {
            get
            {
                return __chantlingScheduler;
            }
            set
            {
                __chantlingScheduler = value;
                if (__chantlingScheduler == false && string.IsNullOrEmpty(Logging.MyCharacterName))
                {
                    Logging.Log("Startup", "We were told to use the scheduler but we are Missing the CharacterName to login with...", Logging.Debug);
                }
            }
        }

        private static bool __loginNowIgnoreScheduler;

        public static bool _loginNowIgnoreScheduler
        {
            get
            {
                return __loginNowIgnoreScheduler;
            }
            set
            {
                __loginNowIgnoreScheduler = value;
                _chantlingScheduler = false;
            }
        }

        public static bool _standaloneInstance
        {
            get
            {
                return !Logging.UseInnerspace;
            }
            set
            {
                //Logging.Log("Startup", "Setting: UseInnerspace = [" + !value + "]", Logging.White);
                Logging.UseInnerspace = !value;
            }

        }

        public static bool _loadAdaptEVE;
        private static bool? _readyToLoginEVEAccount;

        public static bool ReadyToLoginToEVEAccount
        {
            get
            {
                try
                {
                    return _readyToLoginEVEAccount ?? false;
                }
                catch (Exception ex)
                {
                    Logging.Log("ReadyToLoginToEVE", "Exception [" + ex + "]", Logging.Debug);
                    return false;
                }
            }

            set
            {
                _readyToLoginEVEAccount = value;
                if (value) //if true
                {
                    QuestorSchedulerReadyToLogin = DateTime.UtcNow;
                }
            }
        }
        public static bool _humanInterventionRequired;
        public static bool MissingEasyHookWarningGiven;
        public static readonly System.Timers.Timer Timer = new System.Timers.Timer();
        public const int RandStartDelay = 30; //Random startup delay in minutes
        public static readonly Random R = new Random();
        public static int ServerStatusCheck = 0;
        public static DateTime _nextPulse;
        public static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
        public static DateTime StartTime = DateTime.MaxValue;
        public static DateTime StopTime = DateTime.MinValue;
        public static DateTime DoneLoggingInToEVETimeStamp = DateTime.MaxValue;
        public static List<string> _QuestorParamaters;
        public static string PreLoginSettingsINI;

        public static bool LoadDirectEVEInstance()
        {
            #region Load DirectEVE

            //
            // Load DirectEVE
            //

            try
            {
                bool EasyHookExists = File.Exists(System.IO.Path.Combine(Logging.PathToCurrentDirectory, "EasyHook.dll"));
                if (!EasyHookExists && !LoginToEVE.MissingEasyHookWarningGiven)
                {
                    Logging.Log("Startup", "EasyHook DLL's are missing. Please copy them into the same directory as your questor.exe", Logging.Orange);
                    Logging.Log("Startup", "halting!", Logging.Orange);
                    LoginToEVE.MissingEasyHookWarningGiven = true;
                    return false;
                }

                int TryLoadingDirectVE = 0;
                while (Cache.Instance.DirectEve == null && TryLoadingDirectVE < 30)
                {
                    if (!Logging.UseInnerspace)
                    {
                        try
                        {
                            Logging.Log("Startup", "Starting Instance of DirectEVE using StandaloneFramework", Logging.Debug);
                            Cache.Instance.DirectEve = new DirectEve(new StandaloneFramework());
                            TryLoadingDirectVE++;
                            Logging.Log("Startup", "DirectEVE should now be active: see above for any messages from DirectEVE", Logging.Debug);
                            return true;
                        }
                        catch (Exception exception)
                        {
                            Logging.Log("Startup", "exception [" + exception + "]", Logging.Orange);
                            continue;
                        }
                    }
                    
                    try
                    {
                        Logging.Log("Startup", "Starting Instance of DirectEVE using Innerspace", Logging.Debug);
                        Cache.Instance.DirectEve = new DirectEve();
                        TryLoadingDirectVE++;
                        Logging.Log("Startup", "DirectEVE should now be active: see above for any messages from DirectEVE", Logging.Debug);
                        return true;
                    }
                    catch (Exception exception)
                    {
                        Logging.Log("Startup", "exception [" + exception + "]", Logging.Orange);
                        continue;
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Log("Startup", "exception [" + exception + "]", Logging.Orange);
                return false;
            }

            if (Cache.Instance.DirectEve == null)
            {
                try
                {
                    Logging.Log("Startup", "Error on Loading DirectEve, maybe server is down", Logging.Orange);
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cache.Instance.CloseQuestorEndProcess = true;
                    Cleanup.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
                    Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                    Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor, true);
                    return false;
                }
                catch (Exception exception)
                {
                    Logging.BasicLog("Startup", "Exception while logging exception, oh joy [" + exception + "]");
                    return false;
                }
            }

            return true;
            #endregion Load DirectEVE
        }

        public static int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }
    }
}
