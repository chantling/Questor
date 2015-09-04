
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
        private static bool _questorScheduleSaysWeShouldLoginNow;
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

        private static double _minutesToStart;
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

        public static void WaitToLoginUntilSchedulerSaysWeShould()
        {
            string path = Logging.PathToCurrentDirectory;
            Logging.MyCharacterName = Logging.MyCharacterName.Replace("\"", ""); // strip quotation marks if any are present


            CharSchedules = new List<CharSchedule>();
            if (path != null)
            {
                //
                // we should add a check for a missing schedules.xml here and log to the user if it is missing
                //
                XDocument values = XDocument.Load(Path.Combine(path, "Schedules.xml"));
                if (values.Root != null)
                {
                    foreach (XElement value in values.Root.Elements("char"))
                    {
                        CharSchedules.Add(new CharSchedule(value));
                    }
                }
            }

            //
            // chantling scheduler
            //
            CharSchedule schedule = CharSchedules.FirstOrDefault(v => v.ScheduleCharacterName == Logging.MyCharacterName);
            if (schedule == null)
            {
                Logging.Log("Startup", "Error - character [" + Logging.MyCharacterName + "] not found in Schedules.xml!", Logging.Red);
                return;
            }

            if (schedule.LoginUserName == null || schedule.LoginPassWord == null)
            {
                Logging.Log("Startup", "Error - Login details not specified in Schedules.xml!", Logging.Red);
                return;
            }

            Logging.EVELoginUserName = schedule.LoginUserName;
            Logging.EVELoginPassword = schedule.LoginPassWord;
            Logging.Log("Startup", "User: " + schedule.LoginUserName + " Name: " + schedule.ScheduleCharacterName, Logging.White);

            if (schedule.StartTimeSpecified)
            {
                if (schedule.Start1 > schedule.Stop1) schedule.Stop1 = schedule.Stop1.AddDays(1);
                if (DateTime.Now.AddHours(2) > schedule.Start1 && DateTime.Now < schedule.Stop1)
                {
                    StartTime = schedule.Start1;
                    StopTime = schedule.Stop1;
                    Time.Instance.StopTimeSpecified = true;
                    Logging.Log("Startup", "Schedule1: Start1: " + schedule.Start1 + " Stop1: " + schedule.Stop1, Logging.White);
                }
            }

            if (schedule.StartTime2Specified)
            {
                if (schedule.Start2 > schedule.Stop2) schedule.Stop2 = schedule.Stop2.AddDays(1);
                if (DateTime.Now.AddHours(2) > schedule.Start2 && DateTime.Now < schedule.Stop2)
                {
                    StartTime = schedule.Start2;
                    StopTime = schedule.Stop2;
                    Time.Instance.StopTimeSpecified = true;
                    Logging.Log("Startup", "Schedule2: Start2: " + schedule.Start2 + " Stop2: " + schedule.Stop2, Logging.White);
                }
            }

            if (schedule.StartTime3Specified)
            {
                if (schedule.Start3 > schedule.Stop3) schedule.Stop3 = schedule.Stop3.AddDays(1);
                if (DateTime.Now.AddHours(2) > schedule.Start3 && DateTime.Now < schedule.Stop3)
                {
                    StartTime = schedule.Start3;
                    StopTime = schedule.Stop3;
                    Time.Instance.StopTimeSpecified = true;
                    Logging.Log("Startup", "Schedule3: Start3: " + schedule.Start3 + " Stop3: " + schedule.Stop3, Logging.White);
                }
            }

            //
            // if we have not found a workable schedule yet assume schedule 1 is correct. what we want.
            //
            if (schedule.StartTimeSpecified && StartTime == DateTime.MaxValue)
            {
                StartTime = schedule.Start1;
                StopTime = schedule.Stop1;
                Logging.Log("Startup", "Forcing Schedule 1 because none of the schedules started within 2 hours", Logging.White);
                Logging.Log("Startup", "Schedule 1: Start1: " + schedule.Start1 + " Stop1: " + schedule.Stop1, Logging.White);
            }

            if (schedule.StartTimeSpecified || schedule.StartTime2Specified || schedule.StartTime3Specified)
            {
                StartTime = StartTime.AddSeconds(R.Next(0, (RandStartDelay * 60)));
            }

            if ((DateTime.Now > StartTime))
            {
                if ((DateTime.Now.Subtract(StartTime).TotalMinutes < 1200)) //if we're less than x hours past start time, start now
                {
                    StartTime = DateTime.Now;
                    _questorScheduleSaysWeShouldLoginNow = true;
                }
                else
                {
                    StartTime = StartTime.AddDays(1); //otherwise, start tomorrow at start time
                }
            }
            else if ((StartTime.Subtract(DateTime.Now).TotalMinutes > 1200)) //if we're more than x hours shy of start time, start now
            {
                StartTime = DateTime.Now;
                _questorScheduleSaysWeShouldLoginNow = true;
            }

            if (StopTime < StartTime)
            {
                StopTime = StopTime.AddDays(1);
            }

            //if (schedule.RunTime > 0) //if runtime is specified, overrides stop time
            //    StopTime = StartTime.AddMinutes(schedule.RunTime); //minutes of runtime

            //if (schedule.RunTime < 18 && schedule.RunTime > 0)     //if runtime is 10 or less, assume they meant hours
            //    StopTime = StartTime.AddHours(schedule.RunTime);   //hours of runtime

            if (_loginNowIgnoreScheduler)
            {
                _questorScheduleSaysWeShouldLoginNow = true;
            }
            else
            {
                Logging.Log("Startup", " Start Time: " + StartTime + " - Stop Time: " + StopTime, Logging.White);
            }

            if (!_questorScheduleSaysWeShouldLoginNow)
            {
                _minutesToStart = StartTime.Subtract(DateTime.Now).TotalMinutes;
                Logging.Log("Startup", "Starting at " + StartTime + ". " + String.Format("{0:0.##}", _minutesToStart) + " minutes to go.", Logging.Yellow);
                Timer.Elapsed += new ElapsedEventHandler(TimerEventProcessor);
                if (_minutesToStart > 0)
                {
                    Timer.Interval = (int)(_minutesToStart * 60000);
                }
                else
                {
                    Timer.Interval = 1000;
                }

                Timer.Enabled = true;
                Timer.Start();
            }
            else
            {
                ReadyToLoginToEVEAccount = true;
                Logging.Log("Startup", "Already passed start time.  Starting in 15 seconds.", Logging.White);
                System.Threading.Thread.Sleep(15000);
            }

            //
            // chantling scheduler (above)
            //
        }

        private static void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            Timer.Stop();
            Logging.Log("Startup", "Timer elapsed.  Starting now.", Logging.White);
            ReadyToLoginToEVEAccount = true;
            _questorScheduleSaysWeShouldLoginNow = true;
        }

        public static int RandomNumber(int min, int max)
        {
            Random random = new Random();
            return random.Next(min, max);
        }

        public static IEnumerable<string> SplitArguments(string commandLine)
        {
            var parmChars = commandLine.ToCharArray();
            var inSingleQuote = false;
            var inDoubleQuote = false;
            for (var index = 0; index < parmChars.Length; index++)
            {
                if (parmChars[index] == '"' && !inSingleQuote)
                {
                    inDoubleQuote = !inDoubleQuote;
                    parmChars[index] = '\n';
                }
                if (parmChars[index] == '\'' && !inDoubleQuote)
                {
                    inSingleQuote = !inSingleQuote;
                    parmChars[index] = '\n';
                }
                if (!inSingleQuote && !inDoubleQuote && parmChars[index] == ' ')
                    parmChars[index] = '\n';
            }
            return (new string(parmChars)).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static bool PreLoginSettings(string iniFile)
        {
            if (string.IsNullOrEmpty(iniFile))
            {
                Logging.Log("PreLoginSettings", "iniFile was not passed to PreLoginSettings", Logging.Debug);
                return false;
            }

            try
            {
                if (!File.Exists(iniFile))
                {
                    Logging.Log("PreLoginSettings", "Could not find inifile named [" + iniFile + "]", Logging.Debug);
                }
                else
                {
                    Logging.Log("PreLoginSettings", "found a inifile named [" + Path.GetFileName(iniFile).Substring(0, 4) + "_MyINIFileRedacted_" + "]", Logging.Debug);
                }

                int index = 0;
                foreach (string line in File.ReadAllLines(iniFile))
                {
                    index++;
                    if (line.StartsWith(";"))
                    {
                        //Logging.Log("PreLoginSettings.Comment", line, Logging.Debug);
                        continue;
                    }

                    if (line.StartsWith("["))
                    {
                        //Logging.Log("PreLoginSettings.Section", line, Logging.Debug);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(line))
                    {
                        //Logging.Log("PreLoginSettings.RawIniData-StartsWithWhitespaceSpace", line, Logging.Debug);
                        continue;
                    }

                    if (string.IsNullOrEmpty(line))
                    {
                        //Logging.Log("PreLoginSettings.RawIniData-IsNullOrEmpty", line, Logging.Debug);
                        continue;
                    }

                    //Logging.Log("PreLoginSettings.RawIniData", line, Logging.Debug);
                    
                    string[] sLine = line.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    //Logging.Log("PreLoginSettings", "Processing line: [" + index + "] of [" + Path.GetFileName(iniFile).Substring(0, 4) + "_MyINIFileRedacted_] Found Var: [" + sLine[0] + "] Found Value: [" + sLine[1] + "]", Logging.Debug);
                    //if (sLine.Count() != 2 && !sLine[0].Equals(ProxyUsername) && !sLine[0].Equals(ProxyPassword) )
                    if (sLine.Count() != 2)
                    {
                        Logging.Log("PreLoginSettings", "IniFile not right format at line: [" + index + "]", Logging.Debug);
                    }

                    switch (sLine[0].ToLower())
                    {
                        case "gameloginusername":
                            Logging.EVELoginUserName = sLine[1];
                            Logging.Log("PreLoginSettings", "EVELoginUserName [" + Logging.EVELoginUserName + "]", Logging.Debug);
                            break;

                        case "gameloginpassword":
                            Logging.EVELoginPassword = sLine[1];
                            Logging.Log("PreLoginSettings", "EVELoginPassword [" + Logging.EVELoginPassword + "]", Logging.Debug);
                            break;

                        case "eveloginusername":
                            Logging.EVELoginUserName = sLine[1];
                            Logging.Log("PreLoginSettings", "EVELoginUserName [" + Logging.EVELoginUserName + "]", Logging.Debug);
                            break;

                        case "eveloginpassword":
                            Logging.EVELoginPassword = sLine[1];
                            Logging.Log("PreLoginSettings", "EVELoginPassword [" + Logging.EVELoginPassword + "]", Logging.Debug);
                            break;

                        case "characternametologin":
                            try
                            {
                                Logging.MyCharacterName = sLine[1];
                                //Logging.MyCharacterName = Logging.MyCharacterName.Replace("_", " ");
                                Logging.Log("PreLoginSettings", "MyCharacterName [" + Logging.MyCharacterName + "]", Logging.Debug);
                            }
                            catch (Exception ex)
                            {
                                Logging.Log("PreLoginSettings.characternametologin", "Exception [" + ex + "]", Logging.Debug);
                            }
                            
                            break;

                        case "questorloginonly":
                            _loginOnly = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "_loginOnly [" + _loginOnly + "]", Logging.Debug);
                            break;

                        case "questorusescheduler":
                            _chantlingScheduler = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "_chantlingScheduler [" + _chantlingScheduler + "]", Logging.Debug);
                            break;

                        case "standaloneinstance":
                            _standaloneInstance = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "_standaloneInstance [" + _standaloneInstance + "]", Logging.Debug);
                            break;

                        case "enablevisualstyles":
                            Logging.EnableVisualStyles = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "EnableVisualStyles [" + Logging.EnableVisualStyles + "]", Logging.Debug);
                            break;

                        case "debugbeforelogin":
                            Logging.DebugBeforeLogin = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "DebugBeforeLogin [" + Logging.DebugBeforeLogin + "]", Logging.Debug);
                            break;

                        case "debugdisableautologin":
                            Logging.DebugDisableAutoLogin = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "DebugDisableAutoLogin [" + Logging.DebugDisableAutoLogin + "]", Logging.Debug);
                            break;

                        case "debugonframe":
                            Logging.DebugOnframe = bool.Parse(sLine[1]);
                            Logging.Log("PreLoginSettings", "DebugOnframe: [" + Logging.DebugOnframe + "]", Logging.Debug);
                            break;
                    }
                }

                if (Logging.EVELoginUserName == null)
                {
                    Logging.Log("PreLoginSettings", "Missing: EVELoginUserName in [" + Path.GetFileName(iniFile).Substring(0, 4) + "_MyINIFileRedacted_" + "]: questor cant possibly AutoLogin without the EVE Login UserName", Logging.Debug);
                }

                if (Logging.EVELoginPassword == null)
                {
                    Logging.Log("PreLoginSettings", "Missing: EVELoginPassword in [" + Path.GetFileName(iniFile).Substring(0, 4) + "_MyINIFileRedacted_" + "]: questor cant possibly AutoLogin without the EVE Login Password!", Logging.Debug);
                }

                if (Logging.MyCharacterName == null)
                {
                    Logging.Log("PreLoginSettings", "Missing: CharacterNameToLogin in [" + Path.GetFileName(iniFile).Substring(0, 4) + "_MyINIFileRedacted_" + "]: questor cant possibly AutoLogin without the EVE CharacterName to choose", Logging.Debug);
                }

                Logging.Log("PreLoginSettings", "Done reading ini", Logging.Debug);
                return true;
            }
            catch (Exception exception)
            {
                Logging.Log("Startup.PreLoginSettings", "Exception [" + exception + "]", Logging.Debug);
                return false;
            }
        }
    }
}
