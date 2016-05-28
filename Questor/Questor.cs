﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using DirectEve;
using Questor.Behaviors;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor
{
    public class Questor : IDisposable
    {
        private static DateTime _nextPulse = DateTime.MinValue;
        private static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
        private static DateTime _lastSessionNotReady = DateTime.MinValue;
        private static Random _random = new Random();
        public static Questor questor = null;
        private readonly CombatMissionsBehavior _combatMissionsBehavior;

        private readonly Stopwatch _watch;
        //private readonly Defense _defense;

        private DateTime _lastQuestorPulse;

        private bool _runOnceAfterStartupalreadyProcessed;
        private bool _runOnceInStationAfterStartupalreadyProcessed;

        private int pulseDelay = 800;

        public Questor()
        {
            _lastQuestorPulse = DateTime.UtcNow;
            _combatMissionsBehavior = new CombatMissionsBehavior();
            _watch = new Stopwatch();
            Time.Instance.NextStartupAction = DateTime.UtcNow;
            _States.CurrentQuestorState = QuestorState.Idle;
            Time.Instance.StartTime = DateTime.UtcNow;
            Time.Instance.QuestorStarted_DateTime = DateTime.UtcNow;
            Settings.Instance.CharacterMode = "none";

            try
            {
                Logging.Log("Register EVEOnFrame Event");
                Cache.Instance.DirectEve.OnFrame += EVEOnFrame;
            }
            catch (Exception ex)
            {
                Logging.Log(string.Format("DirectEVE.OnFrame: Exception {0}...", ex));
            }

            Logging.Log("Questor.");
            questor = this;
        }

        public void RunOnceAfterStartup()
        {
            if (!_runOnceAfterStartupalreadyProcessed &&
                DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(15) &&
                Cache.Instance.DirectEve.Session.CharacterId != null && Cache.Instance.DirectEve.Session.CharacterId > 0)
            {
                if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
                {
                    _runOnceAfterStartupalreadyProcessed = true;

                    Cache.Instance.IterateShipTargetValues("RunOnceAfterStartup");
                    // populates ship target values from an XML
                    Cache.Instance.IterateUnloadLootTheseItemsAreLootItems("RunOnceAfterStartup");
                    // populates the list of items we never want in our local cargo (used mainly in unloadloot)

                    MissionSettings.UpdateMissionName();
                    Logging.MaintainConsoleLogs();
                }
                else
                {
                    Logging.Log("Settings.Instance.CharacterName is still null");
                    Time.Instance.NextStartupAction = DateTime.UtcNow.AddSeconds(10);
                    _runOnceAfterStartupalreadyProcessed = false;
                    return;
                }
            }
        }

        public void RunOnceInStationAfterStartup()
        {
            if (!_runOnceInStationAfterStartupalreadyProcessed &&
                DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddSeconds(20) && Cache.Instance.InStation &&
                DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10))
            {
                if (Settings.Instance.CharacterXMLExists && DateTime.UtcNow > Time.Instance.NextStartupAction)
                {
                    if (!string.IsNullOrEmpty(Settings.Instance.AmmoHangarTabName) ||
                        !string.IsNullOrEmpty(Settings.Instance.LootHangarTabName) && Cache.Instance.InStation)
                    {
                        Logging.Log("Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCorpHangar);
                        Statistics.LogWindowActionToWindowLog("CorpHangar", "CorpHangar Opened");
                    }


                    _runOnceInStationAfterStartupalreadyProcessed = true;
                }
                else
                {
                    Logging.Log("Settings.Instance.CharacterName is still null");
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
                Logging.Log(" took " + _watch.ElapsedMilliseconds + "ms");
        }

        protected static int GetRandom(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        protected DateTime UTCNowAddDelay(int minDelayInSeconds, int maxDelayInSeconds)
        {
            return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInSeconds*1000, maxDelayInSeconds*1000));
        }

        public static bool TimeCheck()
        {
            if (DateTime.UtcNow < Time.Instance.NextTimeCheckAction)
                return false;

            Time.Instance.NextTimeCheckAction = DateTime.UtcNow.AddSeconds(90);

            if (Cache.Instance.ExitWhenIdle)
            {
                Logging.Log("ExitWhenIdle set to true.  Quitting game.");
                Cleanup.ReasonToStopQuestor = "ExitWhenIdle set to true";
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
            Time.Instance.LastWalletCheck = DateTime.UtcNow;

            //Logging.Log("[Questor] Wallet Balance Debug Info: LastKnownGoodConnectedTime = " + Settings.Instance.lastKnownGoodConnectedTime);
            //Logging.Log("[Questor] Wallet Balance Debug Info: DateTime.UtcNow - LastKnownGoodConnectedTime = " + DateTime.UtcNow.Subtract(Settings.Instance.LastKnownGoodConnectedTime).TotalSeconds);
            if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) > 1)
            {
                Logging.Log(String.Format("Wallet Balance Has Not Changed in [ {0} ] minutes.",
                    Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
            }

            if (Logging.DebugWalletBalance)
            {
                Logging.Log(String.Format("DEBUG: Wallet Balance [ {0} ] has been checked.",
                    Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes, 0)));
            }

            //Settings.Instance.WalletBalanceChangeLogOffDelay = 2;  //used for debugging purposes
            //Logging.Log("Time.Instance.lastKnownGoodConnectedTime is currently: " + Time.Instance.LastKnownGoodConnectedTime);
            if (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) <
                Settings.Instance.WalletBalanceChangeLogOffDelay)
            {
                try
                {
                    if ((long) Cache.Instance.MyWalletBalance != (long) Cache.Instance.DirectEve.Me.Wealth)
                    {
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                    }
                }
                catch (Exception exception)
                {
                    Logging.Log("Checking my wallet balance caused an exception [" + exception + "]");
                }
            }
            else if (Settings.Instance.WalletBalanceChangeLogOffDelay != 0)
            {
                if ((Cache.Instance.InStation) ||
                    (Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes) >
                     Settings.Instance.WalletBalanceChangeLogOffDelay + 5))
                {
                    Logging.Log(String.Format(
                            "Questor: Wallet Balance Has Not Changed in [ {0} ] minutes. Switching to QuestorState.CloseQuestor",
                        Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastKnownGoodConnectedTime).TotalMinutes,
                            0)));
                    Cleanup.ReasonToStopQuestor = "Wallet Balance did not change for over " +
                                                  Settings.Instance.WalletBalanceChangeLogOffDelay + "min";
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cleanup.SessionState = "Exiting";
                    Cleanup.BeginClosingQuestor();
                    return;
                }

                //
                // it is assumed if you got this far that you are in space. If you are 'stuck' in a session change then you'll be stuck another 5 min until the timeout above.
                //
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

                return;
            }
        }

        public bool OnframeProcessEveryPulse()
        {
            try
            {
                // New frame, invalidate old cache
                Cache.Instance.InvalidateCache();

                if (DateTime.UtcNow <
                    Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))
                {
                    if (Logging.DebugQuestorEVEOnFrame)
                        Logging.Log("if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(Cache.Instance.RandomNumber(1, 4)))");
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

                if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
                {
                    if (Logging.DebugQuestorEVEOnFrame)
                        Logging.Log("if (!Cache.Instance.InSpace && !Cache.Instance.InStation)");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.QuestorStarted_DateTime.AddSeconds(30))
                {
                    Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                }

                // Session is not ready yet, do not continue
                if (!Cache.Instance.DirectEve.Session.IsReady)
                {
                    Logging.Log("if (!Cache.Instance.DirectEve.Session.IsReady)");
                    return false;
                }

                if (Logging.DebugQuestorEVEOnFrame)
                    Logging.Log("Cleanup.ProcessState();");
                Cleanup.ProcessState();
                if (Logging.DebugQuestorEVEOnFrame)
                    Logging.Log("Statistics.ProcessState();");
                Statistics.ProcessState();

                if (Cache.Instance.DirectEve.Session.IsReady)
                {
                    Time.Instance.LastSessionIsReady = DateTime.UtcNow;
                }

                if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)
                {
                    if (Cache.Instance.ActiveShip.GroupId == (int) Group.Capsule)
                    {
                        Logging.Log("We are in a pod. Don't wait for the session wait timer to expire!");
                        Time.Instance.NextInSpaceorInStation = DateTime.UtcNow;
                        return true;
                    }

                    if (Logging.DebugQuestorEVEOnFrame)
                        Logging.Log("if (DateTime.UtcNow < Time.Instance.NextInSpaceorInStation)");
                    return false;
                }

                // Check 3D rendering
                if (Cache.Instance.DirectEve.Session.IsInSpace &&
                    Cache.Instance.DirectEve.Rendering3D != !Settings.Instance.Disable3D)
                {
                    Cache.Instance.DirectEve.Rendering3D = !Settings.Instance.Disable3D;
                }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastUpdateOfSessionRunningTime).TotalSeconds <
                    Time.Instance.SessionRunningTimeUpdate_seconds)
                {
                    Statistics.SessionRunningTime =
                        (int) DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalMinutes;
                    Time.Instance.LastUpdateOfSessionRunningTime = DateTime.UtcNow;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }

        public void EVEOnFrame(object sender, EventArgs e)
        {
            try
            {
                if (_nextPulse > DateTime.UtcNow)
                {
                    return;
                }

                Time.Instance.LastFrame = DateTime.UtcNow;

                if (Cache.Instance.Paused)
                {
                    // Chant - 05/02/2016 - Reset our timeouts so we don't exit every time we're paused for more than a few seconds
                    Time.Instance.LastSessionIsReady = DateTime.UtcNow;
                    Time.Instance.LastFrame = DateTime.UtcNow;
                    Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                    NavigateOnGrid.AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                    Cache.Instance.CanSafelyCloseQuestorWindow = true;
                    return;
                }

                Cache.Instance.CanSafelyCloseQuestorWindow = false;

                if (!BeforeLogin.questorUI.tabControlMain.SelectedTab.Text.ToLower().Equals("questor"))
                {
                    _nextPulse = DateTime.UtcNow.AddSeconds(2);
                    return;
                }

                if (DateTime.UtcNow < Time.Instance.LastDockAction.AddSeconds(5))
                {
                    // temorarily fix
                    //Logging.Log("LoginOnFrame", "if(DateTime.UtcNow < Time.Instance.LastDockAction.AddSeconds(8)", Logging.White);
                    _nextPulse = UTCNowAddDelay(1, 1);
                    return;
                }

                if (DateTime.UtcNow.Subtract(_lastQuestorPulse).TotalMilliseconds < pulseDelay)
                {
                    return;
                }
                _lastQuestorPulse = DateTime.UtcNow;

                if (Cache.Instance.IsLoadingSettings)
                {
                    _nextPulse = UTCNowAddDelay(1, 1);
                    return;
                }

                if (DateTime.UtcNow < _lastSessionNotReady)
                {
                    _nextPulse = UTCNowAddDelay(1, 2);
                    //Logging.Log("Questor.ProcessState", "if(GetNowAddDelay(8,10) > _lastSessionNotReady)", Logging.White);
                    return;
                }


                if (!Cache.Instance.DirectEve.Session.IsReady && !Cache.Instance.DirectEve.Login.AtLogin &&
                    !Cache.Instance.DirectEve.Login.AtCharacterSelection)
                {
                    _lastSessionNotReady = UTCNowAddDelay(7, 8);
                    return;
                }

                Cache.Instance.WCFClient.GetPipeProxy.SetEveAccountAttributeValue(Cache.Instance.CharName,
                    "LastQuestorSessionReady", DateTime.UtcNow);


                if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
                {
                    if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
                    {
                        _States.CurrentQuestorState = QuestorState.CloseQuestor;
                        Cleanup.BeginClosingQuestor();
                    }
                }

                #region LOGIN

                if (Cache.Instance.DirectEve.Login.AtLogin || Cache.Instance.DirectEve.Login.AtCharacterSelection ||
                    Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)
                {
                    if (Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)
                    {
                        Logging.Log("if(Cache.Instance.DirectEve.Login.IsConnecting || Cache.Instance.DirectEve.Login.IsLoading)");
                        _nextPulse = UTCNowAddDelay(2, 4);
                        return;
                    }

                    //Time.Instance.LastSessionIsReady = DateTime.UtcNow;

                    if (DateTime.UtcNow < _lastServerStatusCheckWasNotOK.AddSeconds(Cache.Instance.RandomNumber(4, 7)))
                    {
                        Logging.Log("lastServerStatusCheckWasNotOK = [" + _lastServerStatusCheckWasNotOK.ToShortTimeString() +
                            "] waiting 10 to 20 seconds.");
                        return;
                    }

                    _lastServerStatusCheckWasNotOK = DateTime.UtcNow.AddDays(-1);
                    //reset this so we never hit this twice in a row w/o another server status check not being OK.

                    if (DateTime.UtcNow < _nextPulse)
                    {
                        if (Logging.DebugOnframe)
                            Logging.Log("if (DateTime.UtcNow < _nextPulse)");
                        return;
                    }

                    if (Logging.DebugOnframe) Logging.Log("Pulse...");


                    _nextPulse = DateTime.UtcNow.AddMilliseconds(Time.Instance.QuestorBeforeLoginPulseDelay_milliseconds);

                    if (DateTime.UtcNow < Cache.QuestorProgramLaunched.AddSeconds(5))
                    {
                        //
                        // do not login for the first 7 seconds, wait...
                        //
                        return;
                    }

                    if (Cache._humanInterventionRequired)
                    {
                        Logging.Log("OnFrame: _humanInterventionRequired is true (this will spam every second or so)");
                        _nextPulse = _nextPulse.AddMinutes(2);
                        return;
                    }

                    if (Logging.DebugOnframe)
                        Logging.Log("before: if (Cache.Instance.DirectEve.Windows.Count != 0)");

                    // We should not get any windows
                    if (Cache.Instance.DirectEve.Windows.Count != 0)
                    {
                        foreach (var window in Cache.Instance.DirectEve.Windows)
                        {
                            if (string.IsNullOrEmpty(window.Html))
                                continue;
                            Logging.Log("WindowTitles:" + window.Name + "::" + window.Html);

                            //
                            // Close these windows and continue
                            //
                            if (window.Name == "telecom" && !Logging.DebugDoNotCloseTelcomWindows)
                            {
                                Logging.Log("Closing telecom message...");
                                Logging.Log("Content of telecom window (HTML): [" +
                                    (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                window.Close();
                                continue;
                            }

                            // Modal windows must be closed
                            // But lets only close known modal windows
                            if (window.IsModal)
                            {
                                var close = false;
                                var restart = false;
                                var needHumanIntervention = false;
                                var sayYes = false;
                                var sayOk = false;
                                var quit = false;

                                //bool update = false;

                                if (!string.IsNullOrEmpty(window.Html))
                                {
                                    //errors that are repeatable and unavoidable even after a restart of eve/questor
                                    needHumanIntervention = window.Html.Contains("reason: Account subscription expired");

                                    //update |= window.Html.Contains("The update has been downloaded");

                                    // Server going down
                                    //Logging.Log("[Startup] (1) close is: " + close);
                                    close |=
                                        window.Html.ToLower()
                                            .Contains("please make sure your characters are out of harms way");
                                    close |= window.Html.ToLower().Contains("accepting connections");
                                    close |= window.Html.ToLower().Contains("could not connect");
                                    close |= window.Html.ToLower().Contains("the connection to the server was closed");
                                    close |= window.Html.ToLower().Contains("server was closed");
                                    close |= window.Html.ToLower().Contains("make sure your characters are out of harm");
                                    close |= window.Html.ToLower().Contains("connection to server lost");
                                    close |= window.Html.ToLower().Contains("the socket was closed");
                                    close |= window.Html.ToLower().Contains("the specified proxy or server node");
                                    close |= window.Html.ToLower().Contains("starting up");
                                    close |= window.Html.ToLower().Contains("unable to connect to the selected server");
                                    close |= window.Html.ToLower()
                                        .Contains("could not connect to the specified address");
                                    close |= window.Html.ToLower().Contains("connection timeout");
                                    close |=
                                        window.Html.ToLower()
                                            .Contains("the cluster is not currently accepting connections");
                                    close |= window.Html.ToLower().Contains("your character is located within");
                                    close |= window.Html.ToLower().Contains("the transport has not yet been connected");
                                    close |= window.Html.ToLower().Contains("the user's connection has been usurped");
                                    close |=
                                        window.Html.ToLower()
                                            .Contains("the EVE cluster has reached its maximum user limit");
                                    close |= window.Html.ToLower().Contains("the connection to the server was closed");
                                    close |= window.Html.ToLower()
                                        .Contains("client is already connecting to the server");

                                    //close |= window.Html.Contains("A client update is available and will now be installed");
                                    //
                                    // eventually it would be nice to hit ok on this one and let it update
                                    //
                                    close |=
                                        window.Html.ToLower()
                                            .Contains("client update is available and will now be installed");
                                    close |=
                                        window.Html.ToLower().Contains("change your trial account to a paying account");

                                    //
                                    // these windows require a restart of eve all together
                                    //
                                    restart |= window.Html.ToLower().Contains("the connection was closed");
                                    restart |= window.Html.ToLower().Contains("connection to server lost.");
                                    //INFORMATION
                                    restart |= window.Html.ToLower().Contains("local cache is corrupt");
                                    sayOk |= window.Html.ToLower().Contains("local session information is corrupt");
                                    restart |= window.Html.ToLower().Contains("The client's local session");
                                    // information is corrupt");
                                    restart |= window.Html.ToLower().Contains("restart the client prior to logging in");

                                    //
                                    // these windows require a quit of eve all together
                                    //
                                    quit |= window.Html.ToLower().Contains("the socket was closed");

                                    //
                                    // Modal Dialogs the need "yes" pressed
                                    //
                                    //sayYes |= window.Html.Contains("There is a new build available. Would you like to download it now");
                                    //sayOk |= window.Html.Contains("The update has been downloaded. The client will now close and the update process begin");
                                    sayOk |=
                                        window.Html.Contains(
                                            "The transport has not yet been connected, or authentication was not successful");

                                    //Logging.Log("[Startup] (2) close is: " + close);
                                    //Logging.Log("[Startup] (1) window.Html is: " + window.Html);
                                }

                                if (sayYes)
                                {
                                    Logging.Log("Found a window that needs 'yes' chosen...");
                                    Logging.Log("Content of modal window (HTML): [" +
                                        (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    window.AnswerModal("Yes");
                                    continue;
                                }

                                if (sayOk)
                                {
                                    Logging.Log("Found a window that needs 'ok' chosen...");
                                    Logging.Log("Content of modal window (HTML): [" +
                                        (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    window.AnswerModal("OK");
                                    if (
                                        window.Html.Contains(
                                            "The update has been downloaded. The client will now close and the update process begin"))
                                    {
                                        //
                                        // schedule the closing of launcher.exe via a timedcommand (10 min?) in the uplink...
                                        //
                                    }
                                    continue;
                                }

                                if (quit)
                                {
                                    Logging.Log("Restarting eve...");
                                    Logging.Log("Content of modal window (HTML): [" +
                                        (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    window.AnswerModal("quit");

                                    //_directEve.ExecuteCommand(DirectCmd.CmdQuitGame);
                                }

                                if (restart)
                                {
                                    Logging.Log("Restarting eve...");
                                    Logging.Log("Content of modal window (HTML): [" +
                                        (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    window.AnswerModal("restart");
                                    continue;
                                }

                                if (close)
                                {
                                    Logging.Log("Closing modal window...");
                                    Logging.Log("Content of modal window (HTML): [" +
                                        (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                    window.Close();
                                    continue;
                                }

                                if (needHumanIntervention)
                                {
                                    Logging.Log("ERROR! - Human Intervention is required in this case: halting all login attempts - ERROR!");
                                    Logging.Log("window.Name is: " + window.Name);
                                    Logging.Log("window.Html is: " + window.Html);
                                    Logging.Log("window.Caption is: " + window.Caption);
                                    Logging.Log("window.Type is: " + window.Type);
                                    Logging.Log("window.ID is: " + window.Id);
                                    Logging.Log("window.IsDialog is: " + window.IsDialog);
                                    Logging.Log("window.IsKillable is: " + window.IsKillable);
                                    Logging.Log("window.Viewmode is: " + window.ViewMode);
                                    Logging.Log("ERROR! - Human Intervention is required in this case: halting all login attempts - ERROR!");
                                    Cache._humanInterventionRequired = true;
                                    return;
                                }
                            }

                            if (string.IsNullOrEmpty(window.Html))
                                continue;

                            if (window.Name == "telecom")
                                continue;
                            Logging.Log("We have an unexpected window, auto login halted.");
                            Logging.Log("window.Name is: " + window.Name);
                            Logging.Log("window.Html is: " + window.Html);
                            Logging.Log("window.Caption is: " + window.Caption);
                            Logging.Log("window.Type is: " + window.Type);
                            Logging.Log("window.ID is: " + window.Id);
                            Logging.Log("window.IsDialog is: " + window.IsDialog);
                            Logging.Log("window.IsKillable is: " + window.IsKillable);
                            Logging.Log("window.Viewmode is: " + window.ViewMode);
                            Logging.Log("We have got an unexpected window, auto login halted.");
                            return;
                        }
                        return;
                    }

                    if (Cache.Instance.DirectEve.Login.AtLogin &&
                        Cache.Instance.DirectEve.Login.ServerStatus != "Status: OK")
                    {
                        if (Cache.ServerStatusCheck <= 20) // at 10 sec a piece this would be 200+ seconds
                        {
                            Logging.Log("Server status[" + Cache.Instance.DirectEve.Login.ServerStatus + "] != [OK] try later");
                            Cache.ServerStatusCheck++;
                            //retry the server status check twice (with 1 sec delay between each) before kicking in a larger delay
                            if (Cache.ServerStatusCheck > 2)
                            {
                                _lastServerStatusCheckWasNotOK = DateTime.UtcNow;
                            }

                            return;
                        }

                        Cache.ServerStatusCheck = 0;
                        Cleanup.ReasonToStopQuestor =
                            "Server Status Check shows server still not ready after more than 3 min. Restarting Questor. ServerStatusCheck is [" +
                            Cache.ServerStatusCheck + "]";
                        Logging.Log(Cleanup.ReasonToStopQuestor);
                        Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
                        Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor, true);
                        return;
                    }

                    if (Cache.Instance.DirectEve.Login.AtLogin && !Cache.Instance.DirectEve.Login.IsLoading &&
                        !Cache.Instance.DirectEve.Login.IsConnecting)
                    {
                        if (DateTime.UtcNow.Subtract(Cache.QuestorSchedulerReadyToLogin).TotalMilliseconds >
                            Cache.Instance.RandomNumber(Time.Instance.EVEAccountLoginDelayMinimum_seconds*1000,
                                Time.Instance.EVEAccountLoginDelayMaximum_seconds*1000))
                        {
                            Logging.Log("Login account [" + Logging.EVELoginUserName + "]");
                            Cache.Instance.DirectEve.Login.Login(Logging.EVELoginUserName, Logging.EVELoginPassword);
                            _nextPulse = UTCNowAddDelay(10, 12);
                            Logging.Log("Waiting for Character Selection Screen");
                            return;
                        }
                    }

                    if (Cache.Instance.DirectEve.Login.AtCharacterSelection &&
                        Cache.Instance.DirectEve.Login.IsCharacterSelectionReady &&
                        !Cache.Instance.DirectEve.Login.IsConnecting && !Cache.Instance.DirectEve.Login.IsLoading)
                    {
                        if (DateTime.UtcNow.Subtract(Cache.EVEAccountLoginStarted).TotalMilliseconds >
                            Cache.Instance.RandomNumber(Time.Instance.CharacterSelectionDelayMinimum_seconds*1000,
                                Time.Instance.CharacterSelectionDelayMaximum_seconds*1000) &&
                            DateTime.UtcNow > Cache.NextSlotActivate)
                        {
                            foreach (var slot in Cache.Instance.DirectEve.Login.CharacterSlots)
                            {
                                if (slot.CharId.ToString(CultureInfo.InvariantCulture) != Logging.MyCharacterName &&
                                    String.Compare(slot.CharName, Logging.MyCharacterName,
                                        StringComparison.OrdinalIgnoreCase) != 0)
                                {
                                    continue;
                                }

                                Logging.Log("Activating character [" + slot.CharName + "]");
                                Cache.NextSlotActivate = DateTime.UtcNow.AddSeconds(5);
                                slot.Activate();
                                _nextPulse = UTCNowAddDelay(12, 14);
                                return;
                            }

                            Logging.Log("Character id/name [" + Logging.MyCharacterName + "] not found, retrying in 10 seconds");
                        }
                    }

                    return;
                }

                #endregion

                if (Cache.Instance.InSpace) pulseDelay = Time.Instance.QuestorPulseInSpace_milliseconds;
                if (Cache.Instance.InStation) pulseDelay = Time.Instance.QuestorPulseInStation_milliseconds;

                if (!OnframeProcessEveryPulse()) return;

                RunOnceAfterStartup();

                RunOnceInStationAfterStartup();


                Defense.ProcessState();

                if (Cache.Instance.Paused)
                {
                    if (Logging.DebugQuestorEVEOnFrame)
                        Logging.Log("if (Cache.Instance.Paused)");
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
                    if (Logging.DebugQuestorEVEOnFrame)
                        Logging.Log("if (Cache.Instance.InSpace && Cache.Instance.InWarp)");
                    return;
                }

                switch (_States.CurrentQuestorState)
                {
                    case QuestorState.Idle:
                        if (TimeCheck()) return; //Should we close questor due to stoptime or runtime?

                        if (Cache.Instance.StopBot)
                        {
                            if (Logging.DebugIdle)
                                Logging.Log("Cache.Instance.StopBot = true - this is set by the LocalWatch code so that we stay in station when local is unsafe");
                            return;
                        }

                        if (_States.CurrentQuestorState == QuestorState.Idle &&
                            Settings.Instance.CharacterMode != "none" && Settings.Instance.CharacterName != null)
                        {
                            _States.CurrentQuestorState = QuestorState.Start;
                            return;
                        }

                        Logging.Log("Settings.Instance.CharacterMode = [" + Settings.Instance.CharacterMode + "]");
                        _States.CurrentQuestorState = QuestorState.Error;
                        break;

                    case QuestorState.CombatMissionsBehavior:

                        _combatMissionsBehavior.ProcessState();
                        break;


                    case QuestorState.Start:
                        switch (Settings.Instance.CharacterMode.ToLower())
                        {
                            case "combat missions":
                            case "combat_missions":
                            case "dps":
                                Logging.Log("Start Mission Behavior");
                                _States.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
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
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Exception [" + ex + "]");
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
                }
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