// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using Questor.Modules.Activities;

namespace Questor
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using DirectEve;
    using global::Questor.Behaviors;
    using global::Questor.Modules.Actions;
    using global::Questor.Modules.Caching;
    using global::Questor.Modules.Combat;
    using global::Questor.Modules.Logging;
    using global::Questor.Modules.Lookup;
    using global::Questor.Modules.Misc;
    using global::Questor.Modules.States;
    using global::Questor.Modules.BackgroundTasks;
    using LavishScriptAPI;

    public class Questor : IDisposable 
    {
        //private readonly Defense _defense;

        private DateTime _lastQuestorPulse;
        //private static DateTime _nextQuestorAction = DateTime.UtcNow.AddHours(-1);
        private readonly CombatMissionsBehavior _combatMissionsBehavior;
        //private readonly MissionSettings _combatMissionSettings;
        private readonly CombatHelperBehavior _combatHelperBehavior;
        private readonly DedicatedBookmarkSalvagerBehavior _dedicatedBookmarkSalvagerBehavior;
        private readonly DebugHangarsBehavior _debugHangarsBehavior;
        private readonly MiningBehavior _miningBehavior;

        private readonly InnerspaceCommands _innerspaceCommands;
        //private readonly Statistics _statistics;
        //private readonly BackgroundBehavior _backgroundbehavior;
        //private readonly Cleanup _cleanup;

        private bool _runOnceAfterStartupalreadyProcessed;
        private bool _runOnceInStationAfterStartupalreadyProcessed;


        private readonly Stopwatch _watch;

        public Questor()
        {
            //Logging.tryToLogToFile = true;
            _lastQuestorPulse = DateTime.UtcNow;

            //_defense = new Defense();
            _combatMissionsBehavior = new CombatMissionsBehavior();
            //_combatMissionSettings = new MissionSettings();
            _combatHelperBehavior = new CombatHelperBehavior();
            _dedicatedBookmarkSalvagerBehavior = new DedicatedBookmarkSalvagerBehavior();
            _debugHangarsBehavior = new DebugHangarsBehavior();
            _miningBehavior = new MiningBehavior();
            //_backgroundbehavior = new BackgroundBehavior();
            //_cleanup = new Cleanup();
            _watch = new Stopwatch();
            _innerspaceCommands = new InnerspaceCommands();
            //_statistics = new Statistics();

            Time.Instance.NextStartupAction = DateTime.UtcNow;
            // State fixed on ExecuteMission
            _States.CurrentQuestorState = QuestorState.Idle;

            if (Cache.Instance.DirectEve == null)
            {
                Logging.Log("Startup", "Error on Loading DirectEve, maybe server is down", Logging.Orange);
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cache.Instance.CloseQuestorEndProcess = true;
                Settings.Instance.AutoStart = true;
                Cleanup.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
                Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                return;
            }

            if (LoginToEVE.StartTime.AddMinutes(10) < LoginToEVE.StopTime)
            {
                Time.Instance.StopTime = LoginToEVE.StopTime;
                Logging.Log("Questor", "Schedule: setup correctly: stoptime is [" + Time.Instance.StopTime.ToShortTimeString() + "]", Logging.Orange);
            }
            else
            {
                Time.Instance.StopTime = DateTime.Now.AddHours(Time.Instance.QuestorScheduleNotUsed_Hours);
                Logging.Log("Questor", "Schedule: NOT setup correctly: stoptime  set to [" + Time.Instance.QuestorScheduleNotUsed_Hours + "] hours from now at [" + Time.Instance.StopTime.ToShortTimeString() + "]", Logging.Orange);
                Logging.Log("Questor", "You can correct this by editing schedules.xml to have an entry for this toon", Logging.Orange);
                Logging.Log("Questor", "Ex: <char user=\"" + Settings.Instance.CharacterName + "\" pw=\"MyPasswordForEVEHere\" name=\"MyLoginNameForEVEHere\" start=\"06:45\" stop=\"08:10\" start2=\"09:05\" stop2=\"14:20\"/>", Logging.Orange);
                Logging.Log("Questor", "make sure each toon has its own innerspace profile and specify the following startup program line:", Logging.Orange);
                Logging.Log("Questor", "dotnet questor questor -x -c \"MyEVECharacterName\"", Logging.Orange);
            }

            Time.Instance.StartTime = LoginToEVE.StartTime;
            Time.Instance.QuestorStarted_DateTime = DateTime.UtcNow;

            // get the current process
            Process currentProcess = System.Diagnostics.Process.GetCurrentProcess();

            // get the physical mem usage
            Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64 + 1 / 1024) / 1024);
            Logging.Log("Questor", "EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB", Logging.White);
            Statistics.SessionIskGenerated = 0;
            Statistics.SessionLootGenerated = 0;
            Statistics.SessionLPGenerated = 0;
            Settings.Instance.CharacterMode = "none";

            try
            {
                Logging.Log("Questor", "Register EVEOnFrame Event", Logging.White);
                //
                // setup the [ Cache.Instance.DirectEve.OnFrame ] Event triggered on every new frame to call EVEOnFrame()
                //
                Cache.Instance.DirectEve.OnFrame += EVEOnFrame;
            }
            catch (Exception ex)
            {
                Logging.Log("Questor", string.Format("DirectEVE.OnFrame: Exception {0}...", ex), Logging.White);
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cache.Instance.CloseQuestorEndProcess = true;
                Settings.Instance.AutoStart = true;
                Cleanup.ReasonToStopQuestor = "Error on DirectEve.OnFrame, maybe the DirectEVE license server is down";
                Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
            }

            Logging.Log("Questor", "Questor.", Logging.White);
        }

        public void RunOnceAfterStartup()
        {
            if (!_runOnceAfterStartupalreadyProcessed && DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15))
            {
                if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
                {
                    Cache.Instance.DirectEve.Skills.RefreshMySkills();
                    _runOnceAfterStartupalreadyProcessed = true;

                    Cache.Instance.IterateShipTargetValues("RunOnceAfterStartup");  // populates ship target values from an XML
                    //Cache.Instance.IterateInvTypes("RunOnceAfterStartup");          // populates the prices of items (cant we use prices from the game now?!)
                    Cache.Instance.IterateUnloadLootTheseItemsAreLootItems("RunOnceAfterStartup");       // populates the list of items we never want in our local cargo (used mainly in unloadloot)

                    MissionSettings.UpdateMissionName();
                    Logging.MaintainConsoleLogs();

                    if (Logging.UseInnerspace)
                    {
                        InnerspaceCommands.CreateLavishCommands();
                        
                        //enable windowtaskbar = on, so that minimized windows do not make us die in a fire.
                        Logging.Log("RunOnceAfterStartup", "Running Innerspace command: timedcommand 100 windowtaskbar on " + Settings.Instance.CharacterName, Logging.White);
                        LavishScript.ExecuteCommand("timedcommand 100 windowtaskbar on " + Settings.Instance.CharacterName);

                        if (Settings.Instance.EVEWindowXSize >= 100 && Settings.Instance.EVEWindowYSize >= 100)
                        {
                            Logging.Log("RunOnceAfterStartup", "Running Innerspace command: timedcommand 150 WindowCharacteristics -size " + Settings.Instance.EVEWindowXSize + "x" + Settings.Instance.EVEWindowYSize, Logging.White);
                            LavishScript.ExecuteCommand("timedcommand 150 WindowCharacteristics -size " + Settings.Instance.EVEWindowXSize + "x" + Settings.Instance.EVEWindowYSize);
                            Logging.Log("RunOnceAfterStartup", "Running Innerspace command: timedcommand 200 WindowCharacteristics -pos " + Settings.Instance.EVEWindowXPosition + "," + Settings.Instance.EVEWindowYPosition, Logging.White);
                            LavishScript.ExecuteCommand("timedcommand 200 WindowCharacteristics -pos " + Settings.Instance.EVEWindowXPosition + "," + Settings.Instance.EVEWindowYPosition);
                        }

                        if (Settings.Instance.MinimizeEveAfterStartingUp)
                        {
                            Logging.Log("RunOnceAfterStartup", "MinimizeEveAfterStartingUp is true: Minimizing EVE with: WindowCharacteristics -visibility minimize", Logging.White);
                            LavishScript.ExecuteCommand("WindowCharacteristics -visibility minimize");
                        }

                        if (Settings.Instance.LoginQuestorArbitraryOSCmd)
                        {
                            Logging.Log("RunOnceAfterStartup", "After Questor Login: executing LoginQuestorArbitraryOSCmd", Logging.White);
                            LavishScript.ExecuteCommand("Echo [${Time}] OSExecute " + Settings.Instance.LoginQuestorOSCmdContents.ToString(CultureInfo.InvariantCulture));
                            LavishScript.ExecuteCommand("OSExecute " + Settings.Instance.LoginQuestorOSCmdContents.ToString(CultureInfo.InvariantCulture));
                            Logging.Log("RunOnceAfterStartup", "Done: executing LoginQuestorArbitraryOSCmd", Logging.White);
                        }

                        if (Settings.Instance.LoginQuestorLavishScriptCmd)
                        {
                            Logging.Log("RunOnceAfterStartup", "After Questor Login: executing LoginQuestorLavishScriptCmd", Logging.White);
                            LavishScript.ExecuteCommand("Echo [${Time}] runscript " + Settings.Instance.LoginQuestorLavishScriptContents.ToString(CultureInfo.InvariantCulture));
                            LavishScript.ExecuteCommand("runscript " + Settings.Instance.LoginQuestorLavishScriptContents.ToString(CultureInfo.InvariantCulture));
                            Logging.Log("RunOnceAfterStartup", "Done: executing LoginQuestorLavishScriptCmd", Logging.White);
                        }
                    }
                }
                else
                {
                    Logging.Log("RunOnceAfterStartup", "Settings.Instance.CharacterName is still null", Logging.Orange);
                    Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                    _runOnceAfterStartupalreadyProcessed = false;
                    return;
                }
            }
        }

        public void RunOnceInStationAfterStartup()
        {
            if (!_runOnceInStationAfterStartupalreadyProcessed && DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15) && Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10))
            {
                if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName) || !string.IsNullOrEmpty(Settings.Instance.LootHangarTabName) && Cache.Instance.InStation)
                    {
                        Logging.Log("RunOnceAfterStartup", "Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);", Logging.Debug);
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);
                        Statistics.LogWindowActionToWindowLog("CorpHangar", "CorpHangar Opened");
                    }

                    _runOnceInStationAfterStartupalreadyProcessed = true;
                }
                else
                {
                    Logging.Log("RunOnceAfterStartup", "Settings.Instance.CharacterName is still null", Logging.Orange);
                    Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                    _runOnceInStationAfterStartupalreadyProcessed = false;
                    return;
                }
            }
        }

        public void DebugPerformanceClearandStartTimer()
        {
            _watch.Reset();
            _watch.Start();
        }

        public void DebugPerformanceStopandDisplayTimer(string whatWeAreTiming)
        {
            _watch.Stop();
            if (Logging.DebugPerformance)
                Logging.Log(whatWeAreTiming, " took " + _watch.ElapsedMilliseconds + "ms", Logging.White);
        }

        public static bool SkillQueueCheck()
        {
            if (!Cache.Instance.InSpace && Cache.Instance.InStation)
            {
                if (DateTime.UtcNow < Time.Instance.NextSkillTrainerAction)
                    return true;

                if (!Cache.Instance.DirectEve.Skills.AreMySkillsReady)
                {
                    if (Logging.DebugSkillTraining) Logging.Log("SkillQueueCheck", "if (!Cache.Instance.DirectEve.Skills.AreMySkillsReady) - this really should not happen (often?)", Logging.Debug);
                    return true;
                }

                if (Cache.Instance.DirectEve.HasSupportInstances() && Settings.Instance.ThisToonShouldBeTrainingSkills)
                {
                    if (Logging.DebugSkillTraining) Logging.Log("Questor.SkillQueueCheck", "Current Training Queue Length is [" + Cache.Instance.DirectEve.Skills.SkillQueueLength.ToString() + "]", Logging.White);
                    if (Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours < 24)
                    {
                        Logging.Log("Questor.SkillQueueCheck", "Training Queue currently has room. [" + Math.Round(24 - Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours, 2) + " hours free]", Logging.White);
                        //QuestorUI.lblCurrentMissionInfo.Text = "Training Queue currently has room. [" + Math.Round(24 - Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours, 2) + " hours free]";
                        _States.LavishEvent_SkillQueueHasRoom();
                        _States.CurrentQuestorState = QuestorState.SkillTrainer;
                        return false;
                    }

                    Logging.Log("Questor.SkillQueueCheck", "Training Queue is full. [" + Math.Round(Cache.Instance.DirectEve.Skills.SkillQueueLength.TotalHours, 2) + " is more than 24 hours]", Logging.White);
                    Time.Instance.NextSkillTrainerAction = DateTime.UtcNow.AddHours(3);
                    return true;
                }

                if (Logging.DebugSkillTraining) Logging.Log("Questor.SkillQueueCheck", "if (Cache.Instance.DirectEve.HasSupportInstances() && Settings.Instance.ThisToonShouldBeTrainingSkills)", Logging.White);
                if (Logging.DebugSkillTraining) Logging.Log("Questor.SkillQueueCheck", "Cache.Instance.DirectEve.HasSupportInstances() [" + Cache.Instance.DirectEve.HasSupportInstances() + "]", Logging.White);
                if (Logging.DebugSkillTraining) Logging.Log("Questor.SkillQueueCheck", "Settings.Instance.ThisToonShouldBeTrainingSkills [" + Settings.Instance.ThisToonShouldBeTrainingSkills + "]", Logging.White);
            }

            return true;
        }

        public static bool TimeCheck()
        {
            if (DateTime.UtcNow < Time.Instance.NextTimeCheckAction)
                return false;

            Time.Instance.NextTimeCheckAction = DateTime.UtcNow.AddSeconds(90);
            Logging.Log("Questor", "Checking: Current time [" + DateTime.Now.ToString(CultureInfo.InvariantCulture) +
                        "] StopTimeSpecified [" + Time.Instance.StopTimeSpecified +
                        "] StopTime [ " + Time.Instance.StopTime +
                        "] ManualStopTime = " + Time.Instance.ManualStopTime, Logging.White);

            if (DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalMinutes > Time.Instance.MaxRuntime)
            {
                // quit questor
                Logging.Log("Questor", "Maximum runtime exceeded.  Quitting...", Logging.White);
                Cleanup.ReasonToStopQuestor = "Maximum runtime specified and reached.";
                Settings.Instance.AutoStart = false;
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cleanup.SessionState = "Exiting";
                Cleanup.BeginClosingQuestor();
                return true;
            }

            if (Time.Instance.StopTimeSpecified)
            {
                if (DateTime.Now >= Time.Instance.StopTime)
                {
                    Logging.Log("Questor", "Time to stop. StopTimeSpecified and reached. Quitting game.", Logging.White);
                    Cleanup.ReasonToStopQuestor = "StopTimeSpecified and reached.";
                    Settings.Instance.AutoStart = false;
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cleanup.SessionState = "Exiting";
                    Cleanup.BeginClosingQuestor();
                    return true;
                }
            }

            if (DateTime.Now >= Time.Instance.ManualRestartTime)
            {
                Logging.Log("Questor", "Time to stop. ManualRestartTime reached. Quitting game.", Logging.White);
                Cleanup.ReasonToStopQuestor = "ManualRestartTime reached.";
                Settings.Instance.AutoStart = true;
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cleanup.SessionState = "Exiting";
                Cleanup.BeginClosingQuestor();
                return true;
            }

            if (DateTime.Now >= Time.Instance.ManualStopTime)
            {
                Logging.Log("Questor", "Time to stop. ManualStopTime reached. Quitting game.", Logging.White);
                Cleanup.ReasonToStopQuestor = "ManualStopTime reached.";
                Settings.Instance.AutoStart = false;
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cleanup.SessionState = "Exiting";
                Cleanup.BeginClosingQuestor();
                return true;
            }

            if (Cache.Instance.ExitWhenIdle)
            {
                Logging.Log("Questor", "ExitWhenIdle set to true.  Quitting game.", Logging.White);
                Cleanup.ReasonToStopQuestor = "ExitWhenIdle set to true";
                Settings.Instance.AutoStart = false;
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cleanup.SessionState = "Exiting";
                Cleanup.BeginClosingQuestor();
                return true;
            }

            if (Statistics.MissionsThisSession > MissionSettings.StopSessionAfterMissionNumber)
            {
                Logging.Log("Questor", "MissionsThisSession [" + Statistics.MissionsThisSession + "] is greater than StopSessionAfterMissionNumber [" + MissionSettings.StopSessionAfterMissionNumber + "].  Quitting game.", Logging.White);
                Cleanup.ReasonToStopQuestor = "MissionsThisSession > StopSessionAfterMissionNumber";
                Settings.Instance.AutoStart = false;
                Cache.Instance.CloseQuestorCMDLogoff = false;
                Cache.Instance.CloseQuestorCMDExitGame = true;
                Cleanup.SessionState = "Exiting";
                Cleanup.BeginClosingQuestor();
                return true;
            }
            return false;
        }

        public static void WalletCheck()
        {
            if (_States.CurrentQuestorState == QuestorState.Mining ||
                _States.CurrentQuestorState == QuestorState.CombatHelperBehavior ||
                _States.CurrentQuestorState == QuestorState.DedicatedBookmarkSalvagerBehavior)
            //_States.CurrentQuestorState == QuestorState.BackgroundBehavior)
            {
                if (Logging.DebugWalletBalance) Logging.Log("Questor.WalletCheck", "QuestorState is [" + _States.CurrentQuestorState.ToString() + "] which does not use WalletCheck", Logging.White);
                return;
            }

            Time.Instance.LastWalletCheck = DateTime.UtcNow;

            //Logging.Log("[Questor] Wallet Balance Debug Info: LastKnownGoodConnectedTime = " + Settings.Instance.lastKnownGoodConnectedTime);
            //Logging.Log("[Questor] Wallet Balance Debug Info: DateTime.UtcNow - LastKnownGoodConnectedTime = " + DateTime.UtcNow.Subtract(Settings.Instance.LastKnownGoodConnectedTime).TotalSeconds);
            if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 1)
            {
                Logging.Log("Questor.WalletCheck", String.Format("Wallet Balance Has Not Changed in [ {0} ] minutes.", Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)), Logging.White);
            }

            if (Logging.DebugWalletBalance)
            {
                Logging.Log("Questor.WalletCheck", String.Format("DEBUG: Wallet Balance [ {0} ] has been checked.", Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)), Logging.Yellow);

            }

            //Settings.Instance.WalletBalanceChangeLogOffDelay = 2;  //used for debugging purposes
            //Logging.Log("Time.Instance.lastKnownGoodConnectedTime is currently: " + Time.Instance.LastKnownGoodConnectedTime);
            if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) < Settings.Instance.WalletBalanceChangeLogOffDelay)
            {
                try
                {
                    if ((long)Cache.Instance.MyWalletBalance != (long)Cache.Instance.DirectEve.Me.Wealth)
                    {
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    }
                }
                catch (Exception exception)
                {
                    Logging.Log("Questor.WalletCheck", "Checking my wallet balance caused an exception [" + exception + "]", Logging.White);
                }
            }
            else if (Settings.Instance.WalletBalanceChangeLogOffDelay != 0)
            {
                if ((Cache.Instance.InStation) || (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) > Settings.Instance.WalletBalanceChangeLogOffDelay + 5))
                {
                    Logging.Log("Questor", String.Format("Questor: Wallet Balance Has Not Changed in [ {0} ] minutes. Switching to QuestorState.CloseQuestor", Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)), Logging.White);
                    Cleanup.ReasonToStopQuestor = "Wallet Balance did not change for over " + Settings.Instance.WalletBalanceChangeLogOffDelay + "min";
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cleanup.SessionState = "Exiting";
                    Cleanup.BeginClosingQuestor();
                    return;
                }

                //
                // it is assumed if you got this far that you are in space. If you are 'stuck' in a session change then you'll be stuck another 5 min until the timeout above.
                //
                _States.CurrentDedicatedBookmarkSalvagerBehaviorState = DedicatedBookmarkSalvagerBehaviorState.GotoBase;
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                _States.CurrentCombatHelperBehaviorState = CombatHelperBehaviorState.GotoBase;
                return;
            }
        }

        public bool OnframeProcessEveryPulse()
        {
            try
            {
                // New frame, invalidate old cache
                Cache.Instance.InvalidateCache();

                Time.Instance.LastFrame = DateTime.UtcNow;

                int pulseDelay = 400;
                if (Cache.Instance.InSpace) pulseDelay = Time.Instance.QuestorPulseInSpace_milliseconds;
                if (Cache.Instance.InStation) pulseDelay = Time.Instance.QuestorPulseInStation_milliseconds;

                if (DateTime.UtcNow.Subtract(_lastQuestorPulse).TotalMilliseconds < pulseDelay)
                {
                    return false;
                }

                _lastQuestorPulse = DateTime.UtcNow;

                //if (Cache.Instance.DirectEve.Login.AtLogin)
                //{
                //    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (Cache.Instance.DirectEve.Login.AtLogin)", Logging.Debug);
                //    //if we somehow manage to get the questor GUI running on the login screen, do nothing.
                //    return false;
                //}

                if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))
                {
                    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))", Logging.Debug);
                    return false;
                }

                if (!Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
                {
                    // Update settings (settings only load if character name changed)
                    if (!Settings.Instance.DefaultSettingsLoaded)
                    {
                        Settings.Instance.LoadSettings();
                    }
                }

                //if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.OnframeProcessEveryPulse", "return;", Logging.Debug);
                //return false;
                
                if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
                {
                    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (!Cache.Instance.InSpace && !Cache.Instance.InStation)", Logging.Debug);
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(30))
                {
                    Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                }

                // Start _cleanup.ProcessState
                // Description: Closes Windows, and eventually other things considered 'cleanup' useful to more than just Questor(Missions) but also Anomalies, Mining, etc
                //

                if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "Cleanup.ProcessState();", Logging.Debug);
                Cleanup.ProcessState();
                if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "Statistics.ProcessState();", Logging.Debug);
                Statistics.ProcessState();
                if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "_innerspaceCommands.ProcessState();", Logging.Debug);
                _innerspaceCommands.ProcessState();

                // Done
                // Cleanup State: ProcessState

                // Session is not ready yet, do not continue
                if (!Cache.Instance.DirectEve.Session.IsReady)
                {
                    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (!Cache.Instance.DirectEve.Session.IsReady)", Logging.Debug);
                    return false;
                }

                if (Cache.Instance.DirectEve.Session.IsReady)
                {
                    Time.Instance.LastSessionIsReady = DateTime.UtcNow;
                }

                if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)
                {
                    if (Cache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                    {
                        Logging.Log("Panic", "We are in a pod. Don't wait for the session wait timer to expire!", Logging.Red);
                        Time.Instance.NextInSpaceorInStation = DateTime.UtcNow;
                        return true;
                    }

                    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)", Logging.Debug);
                    return false;
                }

                // Check 3D rendering
                if (Cache.Instance.DirectEve.Session.IsInSpace && Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
                {
                    Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;
                }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastUpdateOfSessionRunningTime).TotalSeconds < Time.Instance.SessionRunningTimeUpdate_seconds)
                {
                    Statistics.SessionRunningTime = (int)DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalMinutes;
                    Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log("Questor.OnframeProcessEveryPulse", "Exception [" + ex + "]", Logging.Debug);
                return false;
            }
        }

        private void EVEOnFrame(object sender, EventArgs e)
        {
            try
            {
                if (!OnframeProcessEveryPulse()) return;
                if (Logging.DebugOnframe) Logging.Log("Questor", "OnFrame: this is Questor.cs [" + DateTime.UtcNow + "] by default the next InSpace pulse will be in [" + Time.Instance.QuestorPulseInSpace_milliseconds + "]milliseconds", Logging.Teal);

                if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.EVEOnFrame", "return;", Logging.Debug);
                return;
                RunOnceAfterStartup();
                RunOnceInStationAfterStartup();

                if (!Cache.Instance.Paused)
                {
                    if (DateTime.UtcNow.Subtract(Time.Instance.LastWalletCheck).TotalMinutes > Time.Instance.WalletCheck_minutes && !Settings.Instance.DefaultSettingsLoaded)
                    {
                        WalletCheck();
                    }
                }

                // We always check our defense state if we're in space, regardless of questor state
                // We also always check panic
                Defense.ProcessState();

                if (Cache.Instance.Paused) //|| DateTime.UtcNow < _nextQuestorAction)
                {
                    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.ProcessState", "if (Cache.Instance.Paused)", Logging.Debug);
                    Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                    Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    Cache.Instance.GotoBaseNow = false;
                    Cleanup.SessionState = string.Empty;
                    return;
                }

                if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
                {
                    if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
                    {
                        _States.CurrentQuestorState = QuestorState.CloseQuestor;
                        Cleanup.BeginClosingQuestor();
                    }
                }

                // When in warp there's nothing we can do, so ignore everything
                if (Cache.Instance.InSpace && Cache.Instance.InWarp)
                {
                    if (Logging.DebugQuestorEVEOnFrame) Logging.Log("Questor.EVEOnFrame", "if (Cache.Instance.InSpace && Cache.Instance.InWarp)", Logging.Debug);
                    return;
                }

                switch (_States.CurrentQuestorState)
                {
                    case QuestorState.Idle:
                        if (TimeCheck()) return; //Should we close questor due to stoptime or runtime?

                        if (!SkillQueueCheck()) return; //if we need to train skills we return here, on the next pass we will be _States.CurrentQuestorState = QuestorSate.SkillTrainer

                        if (Cache.Instance.StopBot)
                        {
                            if (Logging.DebugIdle) Logging.Log("Questor", "Cache.Instance.StopBot = true - this is set by the LocalWatch code so that we stay in station when local is unsafe", Logging.Orange);
                            return;
                        }

                        if (_States.CurrentQuestorState == QuestorState.Idle && Settings.Instance.CharacterMode != "none" && Settings.Instance.CharacterName != null)
                        {
                            _States.CurrentQuestorState = QuestorState.Start;
                            return;
                        }

                        Logging.Log("Questor", "Settings.Instance.CharacterMode = [" + Settings.Instance.CharacterMode + "]", Logging.Orange);
                        _States.CurrentQuestorState = QuestorState.Error;
                        break;

                    case QuestorState.CombatMissionsBehavior:

                        //
                        // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                        //
                        _combatMissionsBehavior.ProcessState();
                        break;

                    case QuestorState.SkillTrainer:

                        //
                        // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                        //
                        SkillTrainerClass.ProcessState();
                        break;

                    case QuestorState.CombatHelperBehavior:

                        //
                        // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                        //
                        _combatHelperBehavior.ProcessState();
                        break;

                    case QuestorState.DedicatedBookmarkSalvagerBehavior:

                        //
                        // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                        //
                        _dedicatedBookmarkSalvagerBehavior.ProcessState();
                        break;

                    case QuestorState.DebugHangarsBehavior:

                        //
                        // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                        //
                        _debugHangarsBehavior.ProcessState();
                        break;

                    case QuestorState.DebugReloadAll:
                        if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distances.OnGridWithMe))) return;
                        _States.CurrentQuestorState = QuestorState.Start;
                        break;

                    case QuestorState.Mining:
                        _miningBehavior.ProcessState();
                        break;

                    case QuestorState.Start:
                        switch (Settings.Instance.CharacterMode.ToLower())
                        {
                            case "combat missions":
                            case "combat_missions":
                            case "dps":
                                Logging.Log("Questor", "Start Mission Behavior", Logging.White);
                                _States.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
                                break;

                            case "salvage":
                                Logging.Log("Questor", "Start Salvaging Behavior", Logging.White);
                                _States.CurrentQuestorState = QuestorState.DedicatedBookmarkSalvagerBehavior;
                                break;

                            case "mining":
                                Logging.Log("Questor", "Start Mining Behavior", Logging.White);
                                _States.CurrentQuestorState = QuestorState.Mining;
                                _States.CurrentMiningState = MiningState.Start;
                                break;

                            case "combat helper":
                            case "combat_helper":
                            case "combathelper":
                                Logging.Log("Questor", "Start CombatHelper Behavior", Logging.White);
                                _States.CurrentQuestorState = QuestorState.CombatHelperBehavior;
                                break;

                            case "custom":
                                Logging.Log("Questor", "Start Custom Behavior", Logging.White);
                                //_States.CurrentQuestorState = QuestorState.BackgroundBehavior;
                                break;

                            case "directionalscanner":
                                Logging.Log("Questor", "Start DirectionalScanner Behavior", Logging.White);
                                _States.CurrentQuestorState = QuestorState.DirectionalScannerBehavior;
                                break;
                        }
                        break;

                    case QuestorState.CloseQuestor:
                        if (Cleanup.ReasonToStopQuestor == string.Empty)
                        {
                            Cleanup.ReasonToStopQuestor = "case QuestorState.CloseQuestor:";
                        }

                        Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                        return;

                    case QuestorState.DebugCloseQuestor:

                        //Logging.Log("ISBoxerCharacterSet: " + Settings.Instance.Lavish_ISBoxerCharacterSet);
                        //Logging.Log("Profile: " + Settings.Instance.Lavish_InnerspaceProfile);
                        //Logging.Log("Game: " + Settings.Instance.Lavish_Game);
                        Logging.Log("Questor", "CloseQuestorCMDUplinkInnerspaceProfile: " + Settings.Instance.CloseQuestorCMDUplinkInnerspaceProfile, Logging.White);
                        Logging.Log("Questor", "CloseQuestorCMDUplinkISboxerCharacterSet: " + Settings.Instance.CloseQuestorCMDUplinkIsboxerCharacterSet, Logging.White);
                        Logging.Log("Questor", "CloseQuestorArbitraryOSCmd" + Settings.Instance.CloseQuestorArbitraryOSCmd, Logging.White);
                        Logging.Log("Questor", "CloseQuestorOSCmdContents" + Settings.Instance.CloseQuestorOSCmdContents, Logging.White);
                        Logging.Log("Questor", "WalletBalanceChangeLogOffDelay: " + Settings.Instance.WalletBalanceChangeLogOffDelay, Logging.White);
                        Logging.Log("Questor", "WalletBalanceChangeLogOffDelayLogoffOrExit: " + Settings.Instance.WalletBalanceChangeLogOffDelayLogoffOrExit, Logging.White);
                        Logging.Log("Questor", "EVEProcessMemoryCeiling: " + Settings.Instance.EVEProcessMemoryCeiling, Logging.White);
                        Logging.Log("Questor", "Cache.Instance.CloseQuestorCMDExitGame: " + Cache.Instance.CloseQuestorCMDExitGame, Logging.White);
                        Logging.Log("Questor", "Cache.Instance.CloseQuestorCMDLogoff: " + Cache.Instance.CloseQuestorCMDLogoff, Logging.White);
                        Logging.Log("Questor", "Cache.Instance.CloseQuestorEndProcess: " + Cache.Instance.CloseQuestorEndProcess, Logging.White);
                        Logging.Log("Questor", "Time.EnteredCloseQuestor_DateTime: " + Time.EnteredCloseQuestor_DateTime.ToShortTimeString(), Logging.White);
                        _States.CurrentQuestorState = QuestorState.Error;
                        return;

                    case QuestorState.DebugWindows:
                        List<DirectWindow> windows = Cache.Instance.Windows;

                        if (windows != null && windows.Any())
                        {
                            foreach (DirectWindow window in windows)
                            {
                                Logging.Log("Questor", "--------------------------------------------------", Logging.Orange);
                                Logging.Log("Questor", "Debug_Window.Name: [" + window.Name + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.Caption: [" + window.Caption + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.Type: [" + window.Type + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.IsModal: [" + window.IsModal + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.IsDialog: [" + window.IsDialog + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.Id: [" + window.Id + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.IsKillable: [" + window.IsKillable + "]", Logging.White);
                                Logging.Log("Questor", "Debug_Window.Html: [" + window.Html + "]", Logging.White);
                            }

                            //Logging.Log("Questor", "Debug_InventoryWindows", Logging.White);
                            //foreach (DirectWindow window in windows)
                            //{
                            //    if (window.Type.Contains("inventory"))
                            //    {
                            //        Logging.Log("Questor", "Debug_Window.Name: [" + window.Name + "]", Logging.White);
                            //        Logging.Log("Questor", "Debug_Window.Type: [" + window.Type + "]", Logging.White);
                            //        Logging.Log("Questor", "Debug_Window.Caption: [" + window.Caption + "]", Logging.White);
                            //        //Logging.Log("Questor", "Debug_Window.Type: [" + window. + "]", Logging.White);
                            //    }
                            //}
                        }
                        else
                        {
                            Logging.Log("Questor", "DebugWindows: No Windows Found", Logging.White);
                        }
                        _States.CurrentQuestorState = QuestorState.Error;
                        return;

                    case QuestorState.DebugInventoryTree:

                        if (Cache.Instance.PrimaryInventoryWindow.ExpandCorpHangarView())
                        {
                            Logging.Log("DebugInventoryTree", "ExpandCorpHangar executed", Logging.Teal);
                        }
                        Logging.Log("DebugInventoryTree", "--------------------------------------------------", Logging.Orange);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.Name: [" + Cache.Instance.PrimaryInventoryWindow.Name + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.Caption: [" + Cache.Instance.PrimaryInventoryWindow.Caption + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.Type: [" + Cache.Instance.PrimaryInventoryWindow.Type + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.IsModal: [" + Cache.Instance.PrimaryInventoryWindow.IsModal + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.IsDialog: [" + Cache.Instance.PrimaryInventoryWindow.IsDialog + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.Id: [" + Cache.Instance.PrimaryInventoryWindow.Id + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.IsKillable: [" + Cache.Instance.PrimaryInventoryWindow.IsKillable + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.IsReady: [" + Cache.Instance.PrimaryInventoryWindow.IsReady + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.LocationFlag: [" + Cache.Instance.PrimaryInventoryWindow.LocationFlag + "]", Logging.White);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.currInvIdName: " + Cache.Instance.PrimaryInventoryWindow.currInvIdName, Logging.Red);
                        Logging.Log("DebugInventoryTree", "InventoryWindow.currInvIdName: " + Cache.Instance.PrimaryInventoryWindow.currInvIdItem, Logging.Red);

                        foreach (Int64 itemInTree in Cache.Instance.IDsinInventoryTree)
                        {
                            if (Cache.Instance.PrimaryInventoryWindow.GetIdsFromTree(false).Contains(itemInTree))
                            {
                                Cache.Instance.PrimaryInventoryWindow.SelectTreeEntryByID(itemInTree);
                                Cache.Instance.IDsinInventoryTree.Remove(itemInTree);
                                break;
                            }
                        }
                        break;

                    //case QuestorState.BackgroundBehavior:

                    //
                    // QuestorState will stay here until changed externally by the behavior we just kicked into starting
                    //
                    //_backgroundbehavior.ProcessState();
                    //break;
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Questor.EVEOnFrame", "Exception [" + ex + "]", Logging.Debug);
                return;
            }
        }

        #region IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
 
        private bool m_Disposed = false;
 
        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    //
                    // Close any open files here...
                    //

                }
 
                // Unmanaged resources are released here.
 
                m_Disposed = true;
            }
        }
 
        ~Questor()    
        {        
            Dispose(false);
        }
 
        #endregion
    }
}