using System;
using System.Diagnostics;
using System.Linq;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.BackgroundTasks
{
    public static class Cleanup
    {
        private static DateTime _lastCleanupAction;
        private static DateTime _lastCleanupProcessState;
        private static int _droneBayClosingAttempts;

        public static bool CloseQuestorFlag = true;
        private static bool FoundDuelInvitation;
        private static DateTime FoundDuelInvitationTime = DateTime.UtcNow.AddDays(-1);
        private static DateTime CloseQuestorDelay { get; set; }
        public static string ReasonToStopQuestor { get; set; }
        public static string SessionState { get; set; }
        public static bool SignalToQuitQuestorAndEVEAndRestartInAMoment { get; set; }
        public static bool SignalToQuitQuestor { get; set; }

        public static void BeginClosingQuestor()
        {
            Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
            SessionState = "BeginClosingQuestor";
            SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
            _States.CurrentQuestorState = QuestorState.CloseQuestor;
        }

        public static void DirecteveDispose()
        {
            Logging.Logging.Log("started calling DirectEve.Dispose()");
            Cache.Instance.DirectEve.Dispose(); //could this hang?
            Logging.Logging.Log("finished calling DirectEve.Dispose()");
            SignalToQuitQuestor = true;
        }

        public static bool CloseQuestor(string Reason, bool restart = false)
        {
            SessionState = "Quitting!!";
            if (!Cache.Instance.CloseQuestorCMDLogoff && !Cache.Instance.CloseQuestorCMDExitGame)
            {
                Cache.Instance.CloseQuestorCMDExitGame = true;
            }

            Logging.Logging.Log("Closing with: Process.GetCurrentProcess().Kill()");

            DirecteveDispose();

            Process.GetCurrentProcess().Kill();
            Environment.Exit(0);
            Environment.FailFast("exit");

            return false;
        }

        public static bool CloseInventoryWindows()
        {
            try
            {
                if (DateTime.UtcNow < _lastCleanupAction.AddMilliseconds(500))
                    return false;

                if (!Cache.Instance.Windows.Any())
                {
                    return false;
                }

                _lastCleanupAction = DateTime.UtcNow;

                //
                // go through *every* window
                //
                foreach (var window in Cache.Instance.Windows)
                {
                    if (window.Name.Contains("_ShipDroneBay_") && window.Caption.Contains("Drone Bay") && window.Type.Contains("Inventory"))
                    {
                        Logging.Logging.Log("CloseInventoryWindows: Closing Drone Bay Window");
                        window.Close();
                        _lastCleanupAction = DateTime.UtcNow;
                        return false;
                    }

                    if (window.Name.Contains("_ShipCargo_") && window.Caption.Contains("active ship") && window.Type.Contains("Inventory"))
                    {
                        Logging.Logging.Log("CloseInventoryWindows: Closing Cargo Bay Window");
                        window.Close();
                        _lastCleanupAction = DateTime.UtcNow;
                        return false;
                    }

                    if (window.Name.Contains("_StationItems_") && window.Caption.Contains("Item hangar") && window.Type.Contains("Inventory"))
                    {
                        Logging.Logging.Log("CloseInventoryWindows: Closing Item Hangar Window");
                        window.Close();
                        _lastCleanupAction = DateTime.UtcNow;
                        return false;
                    }

                    if (window.Name.Contains("_StationShips_") && window.Caption.Contains("Ship hangar") && window.Type.Contains("Inventory"))
                    {
                        Logging.Logging.Log("CloseInventoryWindows: Closing Ship Hangar Window");
                        window.Close();
                        _lastCleanupAction = DateTime.UtcNow;
                        return false;
                    }

                    if (window.Type.Contains("Inventory"))
                    {
                        Logging.Logging.Log("CloseInventoryWindows: Closing other Inventory Window named [ " + window.Name + "]");
                        window.Close();
                        _lastCleanupAction = DateTime.UtcNow;
                        return false;
                    }

                    //
                    // add ship hangar, items hangar, corp hangar, etc... as at least come of those may be open in space (pos?) or may someday be bugged by ccp.
                    // add repairship, lpstore, marketwindow, etc
                    //
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }

        public static void CheckEVEStatus()
        {
            try
            {
                // get the current process
                var currentProcess = Process.GetCurrentProcess();

                // get the physical mem usage (this only runs between missions)
                if (currentProcess.WorkingSet64 != 0)
                {
                    Cache.Instance.TotalMegaBytesOfMemoryUsed = ((currentProcess.WorkingSet64/1024)/1024);
                }
                Logging.Logging.Log("EVE instance: totalMegaBytesOfMemoryUsed - " + Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");

                if (Cache.Instance.TotalMegaBytesOfMemoryUsed > (Settings.Instance.EVEProcessMemoryCeiling - 50))
                {
                    Logging.Logging.Log("Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                        Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB");
                    ReasonToStopQuestor = "Memory usage is above the EVEProcessMemoryCeiling threshold. EVE instance: totalMegaBytesOfMemoryUsed - " +
                                          Cache.Instance.TotalMegaBytesOfMemoryUsed + " MB";
                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    SessionState = "Exiting";
                    BeginClosingQuestor();
                    return;
                }

                SessionState = "Running";
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }
        }

        public static void ProcessState()
        {
            if (DateTime.UtcNow < _lastCleanupProcessState.AddMilliseconds(100) || Logging.Logging.DebugDisableCleanup)
                //if it has not been 100ms since the last time we ran this ProcessState return. We can't do anything that close together anyway
                return;

            _lastCleanupProcessState = DateTime.UtcNow;

            if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(10))
            {
                if (Logging.Logging.DebugCleanup)
                    Logging.Logging.Log("last session change was at [" + Time.Instance.LastSessionChange + "] waiting until 20 sec have passed");
                return;
            }

            if (Cache.Instance.InSpace)
            {
                // When in warp there's nothing we can do, so ignore everything
                if (Cache.Instance.InWarp)
                {
                    if (Logging.Logging.DebugCleanup) Logging.Logging.Log("Processstate: we are in warp: do nothing");
                    _States.CurrentSalvageState = SalvageState.Idle;
                    return;
                }

                if (Logging.Logging.DebugCleanup) Logging.Logging.Log("Processstate: we are in space");
                if (DateTime.UtcNow < Time.Instance.LastInStation.AddSeconds(10))
                {
                    if (Logging.Logging.DebugCleanup)
                        Logging.Logging.Log("Processstate: last in station time is [" + Time.Instance.LastInStation + " waiting until 10 seconds have passed");
                    return;
                }
            }

            if (Cache.Instance.InStation)
            {
                if (Logging.Logging.DebugCleanup) Logging.Logging.Log("Processstate: we are in station");
                if (DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(10))
                {
                    if (Logging.Logging.DebugCleanup)
                        Logging.Logging.Log("Processstate: last in space time is [" + Time.Instance.LastInSpace + " waiting until 10 seconds have passed");
                    return;
                }
            }

            switch (_States.CurrentCleanupState)
            {
                case CleanupState.Idle:

                    //Cleanup State should only run every 4 seconds
                    if (DateTime.UtcNow.Subtract(_lastCleanupAction).TotalSeconds < 4)
                        return;
                    _States.CurrentCleanupState = CleanupState.CheckModalWindows;
                    break;

                case CleanupState.CheckModalWindows:

                    //
                    // go through *every* window
                    //
                    if (!Cache.Instance.InSpace && !Cache.Instance.InStation && Settings.Instance.CharacterName != "AtLoginScreenNoCharactersLoggedInYet")
                    {
                        if (Logging.Logging.DebugCleanup)
                            Logging.Logging.Log("CheckModalWindows: We are in a session change, waiting 4 seconds");
                        _lastCleanupAction = DateTime.UtcNow;
                        _States.CurrentCleanupState = CleanupState.Idle;
                        return;
                    }

                    if (Settings.Instance.CharacterName == "AtLoginScreenNoCharactersLoggedInYet" && Time.Instance.LastInStation.AddHours(1) > DateTime.UtcNow)
                    {
                        ReasonToStopQuestor = "we are no longer in a valid session (not logged in) and we had been logged in. restarting";
                        Logging.Logging.Log(ReasonToStopQuestor);
                        SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                        CloseQuestor(ReasonToStopQuestor);
                        return;
                    }

                    if (Cache.Instance.Windows == null || !Cache.Instance.Windows.Any())
                    {
                        if (Logging.Logging.DebugCleanup)
                            Logging.Logging.Log("CheckModalWindows: Cache.Instance.Windows returned null or empty");
                        _lastCleanupAction = DateTime.UtcNow;
                        _States.CurrentCleanupState = CleanupState.Idle;
                        return;
                    }
                    if (Logging.Logging.DebugCleanup) Logging.Logging.Log("Checking Each window in Cache.Instance.Windows");

                    foreach (var window in Cache.Instance.Windows)
                    {
                        // Telecom messages are generally mission info messages: close them
                        if (window.Name == "telecom" && !Logging.Logging.DebugDoNotCloseTelcomWindows)
                        {
                            Logging.Logging.Log("Closing telecom message...");
                            Logging.Logging.Log("Content of telecom window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") + "]");
                            window.Close();
                        }

                        // Modal windows must be closed
                        // But lets only close known modal windows
                        if (window.IsModal)
                        {
                            var close = false;
                            var restart = false;
                            var restartHarsh = false;
                            var gotoBaseNow = false;
                            var sayYes = false;
                            var sayOk = false;
                            var pause = false;
                            var stackHangars = false;
                            var clearPocket = false;
                            //bool sayno = false;
                            if (!string.IsNullOrEmpty(window.Html))
                            {
                                // Server going down /unscheduled/ potentially very soon!
                                // CCP does not reboot in the middle of the day because the server is behaving
                                // dock now to avoid problems
                                gotoBaseNow |= window.Html.Contains("for a short unscheduled reboot");

                                //fitting window errors - DO NOT undock if this happens! people should fix the fits they load to not move more modules than necessary as that causes problems and requires extra modules

                                //if (_States.CurrentQuestorState == QuestorState.BackgroundBehavior)
                                //{
                                //
                                // we do not care about fitting errors when using the BackgroundBehavior
                                //
                                //    sayOk |= window.Html.Contains("Not all the items could be fitted");
                                //}
                                //else
                                //{
                                pause |= window.Html.Contains("Not all the items could be fitted");
                                //}

                                pause |= window.Html.Contains("Cannot move");

                                if (window.Type == "form.MessageBox" && window.IsDialog && window.IsModal && window.IsKillable)
                                {
                                    sayOk |=
                                        window.Html.Contains(
                                            "If you decline of fail a mission from an agent he/she might become displeased and lower your standing towards him/her. You can decline a mission every four hours without penalty");
                                        //4 hours without penalty
                                }

                                // quitting eve?
                                close |= window.Html.Contains("Do you really want to quit now?");

                                // Server going down
                                close |= window.Html.Contains("Please make sure your characters are out of harm");
                                close |= window.Html.Contains("the servers are down for 30 minutes each day for maintenance and updates");

                                // In space "shit"
                                close |= window.Html.Contains("Item cannot be moved back to a loot container.");
                                close |= window.Html.Contains("you do not have the cargo space");
                                close |= window.Html.Contains("cargo units would be required to complete this operation.");
                                close |= window.Html.Contains("You are too far away from the acceleration gate to activate it!");
                                close |= window.Html.Contains("maximum distance is 2500 meters");
                                // agent mission decline warning (ok button)
                                close |=
                                    window.Html.Contains(
                                        "If you decline of fail a mission from an agent he/she might become displeased and lower your standing towards him/her. You can decline a mission every four hours without penalty");
                                    //4 hours without penalty
                                // Stupid warning, lets see if we can find it
                                close |= window.Html.Contains("Do you wish to proceed with this dangerous action?");
                                // Yes we know the mission is not complete, Questor will just redo the mission
                                close |= window.Html.Contains("weapons in that group are already full");
                                //close |= window.Html.Contains("You have to be at the drop off location to deliver the items in person");

                                //fitting window message(s)
                                close |= window.Html.Contains("No rigs were added to or removed from the ship");
                                //In station - Flying Between Hangars
                                close |= window.Html.Contains("You can't fly your active ship into someone else's hangar");
                                close |= window.Html.Contains("You can't do this quite so fast");
                                // Lag :/

                                clearPocket |= window.Html.Contains("This gate is locked!");


                                close |= window.Html.Contains("The Zbikoki's Hacker Card");
                                close |= window.Html.Contains(" units free.");
                                close |= window.Html.Contains("already full");
                                //windows that can be disabled, but may not yet be disabled
                                //why are we reloading an already full weapon?
                                close |= window.Html.Contains("All the weapons in this group are already full");
                                //trial account
                                close |=
                                    window.Html.Contains(
                                        "At any time you can log in to the account management page and change your trial account to a paying account");

                                restartHarsh |= window.Html.Contains("The user's connection has been usurped on the proxy");
                                restartHarsh |= window.Html.Contains("The connection to the server was closed"); //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("server was closed"); //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("The socket was closed"); //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("The connection was closed"); //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("Connection to server lost"); //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("The user connection has been usurped on the proxy"); //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("The transport has not yet been connected, or authentication was not successful");
                                    //CONNECTION LOST
                                restartHarsh |= window.Html.Contains("Your client has waited");
                                    //SOUL-CRUSHING LAG - Your client has waited x minutes for a remote call to complete.
                                restartHarsh |= window.Html.Contains("This could mean the server is very loaded");
                                    //SOUL-CRUSHING LAG - Your client has waited x minutes for a remote call to complete.

                                //
                                // restart the client if these are encountered
                                //
                                restart |= window.Html.Contains("Local cache is corrupt");
                                restart |= window.Html.Contains("Local session information is corrupt");
                                //
                                // Modal Dialogs the need "yes" pressed
                                //
                                sayYes |= window.Html.Contains("objectives requiring a total capacity");
                                sayYes |= window.Html.Contains("your ship only has space for");
                                sayYes |= window.Html.Contains("Are you sure you want to remove location");

                                //
                                // Accept fleet invites from this specific character
                                //
                                sayYes |= window.Html.Contains(Settings.Instance.CharacterToAcceptInvitesFrom + " wants you to join their fleet");

                                //sayyes |= window.Html.Contains("Repairing these items will cost");
                                sayYes |= window.Html.Contains("Are you sure you would like to decline this mission");
                                //sayyes |= window.Html.Contains("You can decline a mission every four hours without penalty");
                                sayYes |= window.Html.Contains("has no other missions to offer right now. Are you sure you want to decline");

                                //
                                // LP Store "Accept offer" dialog
                                //
                                sayOk |= window.Html.Contains("Are you sure you want to accept this offer?");

                                sayOk |= window.Html.Contains("You do not have an outstanding invitation to this fleet.");
                                sayOk |= window.Html.Contains("You have already selected a character for this session.");
                                sayOk |= window.Html.Contains("If you decline or fail a mission from an agent");

                                //
                                // Not Enough Shelf Space
                                // "You can't add the Militants as there are simply too many items here already."
                                //
                                stackHangars |= window.Html.Contains("as there are simply too many items here already");

                                //
                                // Modal Dialogs the need "no" pressed
                                //
                                //sayno |= window.Html.Contains("Do you wish to proceed with this dangerous action
                            }

                            // Unfortunately, it now seems that the html content of payment confirmation windows during drone repair is empty,
                            // therefore the following check is necessary to press Ok in that window
                            if (_States.CurrentArmState == ArmState.RepairShop || _States.CurrentPanicState == PanicState.Panic)
                            {
                                sayOk |= window.Type.Contains("form.HybridWindow") && window.Caption.Contains("Set Quantity");
                            }


                            if (restartHarsh)
                            {
                                Logging.Logging.Log("Restarting eve...");
                                Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                Cache.Instance.CloseQuestorCMDLogoff = false;
                                Cache.Instance.CloseQuestorCMDExitGame = true;
                                Cache.Instance.CloseQuestorEndProcess = true;
                                ReasonToStopQuestor = "A message from ccp indicated we were disconnected";
                                SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                                CloseQuestor(ReasonToStopQuestor);
                                return;
                            }

                            if (restart)
                            {
                                Logging.Logging.Log("Restarting eve...");
                                Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                Cache.Instance.CloseQuestorCMDLogoff = false;
                                Cache.Instance.CloseQuestorCMDExitGame = true;
                                Cache.Instance.CloseQuestorEndProcess = false;
                                ReasonToStopQuestor = "A message from ccp indicated we were should restart";
                                SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                                window.Close();
                                CloseQuestor(ReasonToStopQuestor);
                                return;
                            }

                            if (sayYes)
                            {
                                Logging.Logging.Log("Found a window that needs 'yes' chosen...");
                                Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                window.AnswerModal("Yes");
                                continue;
                            }

                            if (sayOk)
                            {
                                Logging.Logging.Log("Found a window that needs 'ok' chosen...");

                                if (window.Html == null)
                                {
                                    Logging.Logging.Log("WINDOW HTML == NULL");
                                    continue;
                                }
                                else
                                {
                                    // TODO: fix check if .html is still functional
                                    Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");

                                    if (window.Html.Contains("Repairing these items will cost"))
                                    {
                                        Cache.Instance.doneUsingRepairWindow = true;
                                    }
                                    window.AnswerModal("OK");
                                }


                                continue;
                            }

                            if (stackHangars)
                            {
                                if (!Cache.Instance.StackAmmoHangar("Cleanup")) return;
                                if (!Cache.Instance.StackLootHangar("Cleanup")) return;
                                //if (!Cache.Instance.StackItemhangar("Cleanup")) return;
                                continue;
                            }

                            if (gotoBaseNow)
                            {
                                Logging.Logging.Log("Evidently the cluster is dieing... and CCP is restarting the server");
                                Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                Cache.Instance.GotoBaseNow = true;
                                Settings.Instance.AutoStart = false;

                                //
                                // do not close eve, let the shutdown of the server do that
                                //
                                //Cache.Instance.CloseQuestorCMDLogoff = false;
                                //Cache.Instance.CloseQuestorCMDExitGame = true;
                                //Cleanup.ReasonToStopQuestor = "A message from ccp indicated we were disconnected";
                                //Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                                window.Close();
                                continue;
                            }

                            if (pause)
                            {
                                Logging.Logging.Log("This window indicates an error fitting the ship. pausing");
                                Cache.Instance.Paused = true;
                            }

                            if (close)
                            {
                                Logging.Logging.Log("Closing modal window...");
                                Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                window.Close();
                                continue;
                            }

                            if (clearPocket)
                            {
                                Logging.Logging.Log("Closing modal window...");
                                Logging.Logging.Log("Content of modal window (HTML): [" + (window.Html).Replace("\n", "").Replace("\r", "") + "]");
                                window.Close();
                                //
                                //  queue up a clearpocket action;
                                //
                                CombatMissionCtrl.ReplaceMissionsActions();
                                continue;
                            }
                        }

                        if (Cache.Instance.InSpace)
                        {
                            if (FoundDuelInvitation && window.IsDialog && window.IsModal && window.Caption == "Duel Invitation")
                            {
                                if (DateTime.UtcNow > FoundDuelInvitationTime.AddSeconds(Cache.Instance.RandomNumber(4, 25)))
                                {
                                    //window.AnswerModal("yes");
                                    //window.Close();
                                    FoundDuelInvitation = true;
                                }
                            }

                            if (window.IsDialog && window.IsModal && window.Caption == "Duel Invitation")
                            {
                                FoundDuelInvitation = true;
                                FoundDuelInvitationTime = DateTime.UtcNow;
                            }

                            if (window.Name.Contains("_ShipDroneBay_") && window.Caption == "Drone Bay")
                            {
                                if (Drones.UseDrones &&
                                    (Cache.Instance.ActiveShip.GroupId != (int) Group.Shuttle &&
                                     Cache.Instance.ActiveShip.GroupId != (int) Group.Industrial &&
                                     Cache.Instance.ActiveShip.GroupId != (int) Group.TransportShip &&
                                     _droneBayClosingAttempts <= 1))
                                {
                                    _lastCleanupAction = DateTime.UtcNow;
                                    _droneBayClosingAttempts++;

                                    // Close the drone bay, its not required in space.
                                    window.Close();
                                }
                            }
                            else
                            {
                                _droneBayClosingAttempts = 0;
                            }
                        }
                    }

                    _States.CurrentCleanupState = CleanupState.CleanupTasks;
                    break;

                case CleanupState.CleanupTasks:

                    if (DateTime.UtcNow > Time.Instance.LastSessionChange.AddSeconds(30) && (
                        _States.CurrentQuestorState == QuestorState.CombatMissionsBehavior ||
                        _States.CurrentQuestorState == QuestorState.Idle ||
                        _States.CurrentQuestorState == QuestorState.Cleanup) &&
                        string.Compare(Logging.Logging.FilterPath(Settings.Instance.CharacterName).ToUpperInvariant(),
                            Logging.Logging.FilterPath(Cache.Instance.DirectEve.Me.Name).ToUpperInvariant(), StringComparison.OrdinalIgnoreCase) == 1
                        )
                    {
                        Logging.Logging.Log("DebugInfo:  Settings.Instance.CharacterName [" + Settings.Instance.CharacterName + "]");
                        Logging.Logging.Log("DebugInfo: Cache.Instance.DirectEve.Me.Name [" + Cache.Instance.DirectEve.Me.Name + "]");
                        ReasonToStopQuestor =
                            "CharacterName not defined! - Are we still logged in? Did we lose connection to eve? Questor should be restarting here.";
                        Logging.Logging.Log("CharacterName not defined! - Are we still logged in? Did we lose connection to eve? Questor should be restarting here.");
                        Settings.Instance.CharacterName = "NoCharactersLoggedInAnymore";
                        Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
                        SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                        _States.CurrentQuestorState = QuestorState.CloseQuestor;
                        CloseQuestor(ReasonToStopQuestor);
                        return;
                    }

                    _lastCleanupAction = DateTime.UtcNow;
                    _States.CurrentCleanupState = CleanupState.Idle;
                    break;

                default:

                    // Next state
                    _States.CurrentCleanupState = CleanupState.Idle;
                    break;
            }
        }
    }
}