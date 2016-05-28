// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using Questor.Actions;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;
using Questor.Storylines;

//using SlimDX.Direct2D;

namespace Questor.Behaviors
{
    public class CombatMissionsBehavior
    {
        private static bool _firstStart = true;
        private readonly CombatMissionCtrl _combatMissionCtrl;
        private readonly Random _random;
        private readonly Storyline _storyline;
        private DateTime _lastCMBIdle = DateTime.UtcNow.AddHours(-1);


        private DateTime _lastPulse;
        private DateTime _lastSalvageTrip = DateTime.MinValue;
        private DateTime _lastValidSettingsCheck = DateTime.UtcNow.AddHours(-1);

        private double _lastX;
        private double _lastY;
        private double _lastZ;

        private DateTime _nextBookmarkRefreshCheck = DateTime.MinValue;
        private DateTime _nextBookmarksrefresh = DateTime.MinValue;
        private int _randomDelay;
        public bool PanicStateReset; //false;

        public CombatMissionsBehavior()
        {
            _lastPulse = DateTime.MinValue;
            _random = new Random();

            _combatMissionCtrl = new CombatMissionCtrl();
            _storyline = new Storyline();
            Cache.storyline = _storyline;

            //
            // this is combat mission specific and needs to be generalized
            //
            Settings.Instance.SettingsLoaded += SettingsLoaded;

            // States.CurrentCombatMissionBehaviorState fixed on ExecuteMission
            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;
            _States.CurrentTravelerState = TravelerState.AtDestination;
        }

        private bool ValidSettings { get; set; }

        public void SettingsLoaded(object sender, EventArgs e)
        {
            Logging.Log("called.");
            ValidateCombatMissionSettings();
        }

        public bool ValidateCombatMissionSettings()
        {
            try
            {
                ValidSettings = true;
                if (Combat.Ammo.Any())
                {
                    if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)
                    {
                        if (Combat.Ammo.All(a => a.DamageType != DamageType.EM)) Logging.Log(": Missing EM damage type!");
                        if (Combat.Ammo.All(a => a.DamageType != DamageType.Thermal)) Logging.Log("Missing Thermal damage type!");
                        if (Combat.Ammo.All(a => a.DamageType != DamageType.Kinetic)) Logging.Log("Missing Kinetic damage type!");
                        if (Combat.Ammo.All(a => a.DamageType != DamageType.Explosive))
                            Logging.Log("Missing Explosive damage type!");

                        Logging.Log("You are required to specify all 4 damage types in your settings xml file!");
                        ValidSettings = false;
                        return false;
                    }
                }
                else
                {
                    Combat.Ammo = new List<Ammo>();
                    ValidSettings = false;
                    return false;
                }

                if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                {
                    if (Cache.Instance.Agent != null)
                    {
                        Logging.Log("if (Cache.Instance.Agent != null) - AgentInteraction.AgentId = (long)Cache.Instance.Agent.AgentId");

                        if (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid)
                        {
                            Logging.Log("AgentInteraction.Agent == null || !AgentInteraction.Agent.IsValid");
                            ValidSettings = false;
                            return false;
                        }
                    }
                    else
                    {
                        Logging.Log("Cache.Instance.Agent == null");
                        Logging.Log("Unable to locate agent 2  [" + Cache.Instance.CurrentAgent + "]");
                        ValidSettings = false;
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log("Exception: " + ex);
                return false;
            }
        }

        public bool ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState _CMBStateToSet, string LogMessage = null)
        {
            try
            {
                Logging.Log("New state [" + _CMBStateToSet.ToString() + "]");

                //
                // if _ArmStateToSet matches also do this stuff...
                //
                switch (_CMBStateToSet)
                {
                    case CombatMissionsBehaviorState.Idle:
                        break;

                    case CombatMissionsBehaviorState.Panic:
                        break;

                    case CombatMissionsBehaviorState.Error:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Exception [" + ex + "]");
                return false;
            }
            finally
            {
                //if (WaitAMomentbeforeNextAction) Time.Instance.next = DateTime.UtcNow;
                //CombatMissionsBehavior.ClearDataBetweenStates();
                if (_States.CurrentCombatMissionBehaviorState != _CMBStateToSet)
                {
                    _States.CurrentCombatMissionBehaviorState = _CMBStateToSet;
                    // ProcessState(); why are we calling this again here :/
                }
            }

            return true;
        }

        private void BeginClosingQuestor()
        {
            Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
            _States.CurrentQuestorState = QuestorState.CloseQuestor;
        }

        private bool WeShouldBeInSpaceORInStationAndOutOfSessionChange()
        {
            if (!Cache.Instance.InSpace && !Cache.Instance.InStation)
            {
                if (Logging.DebugWeShouldBeInSpaceORInStationAndOutOfSessionChange)
                    Logging.Log("if (!Cache.Instance.InSpace && !Cache.Instance.InStation)");
                //wait... we must be in a session change
                //return false;
            }

            if ((Cache.Instance.InStation && DateTime.UtcNow < Time.Instance.LastInSpace.AddSeconds(16)) ||
                //if we are in Station and are still in session change
                (Cache.Instance.InSpace && DateTime.UtcNow < Time.Instance.LastInStation.AddSeconds(16))) //if we are in Space and are still in session change
            {
                if (Logging.DebugWeShouldBeInSpaceORInStationAndOutOfSessionChange)
                    Logging.Log("InSpace or InStation and still in a session change");
                //wait... we must be in a session change
                return false;
            }

            if (Logging.DebugWeShouldBeInSpaceORInStationAndOutOfSessionChange)
                Logging.Log("We are not in a session change, continue");
            return true;
        }

        private void IdleCMBState()
        {
            //
            // Note: This does not interact with EVE, no need for ANY delays in this State
            //
            _lastCMBIdle = DateTime.UtcNow;
            if (Cache.Instance.StopBot)
            {
                //
                // this is used by the 'local is safe' routines - standings checks - at the moment is stops questor for the rest of the session.
                //
                if (Logging.DebugAutoStart || Logging.DebugIdle)
                    Logging.Log("DebugIdle: StopBot [" + Cache.Instance.StopBot + "]");
                return;
            }

            if (Cache.Instance.InSpace)
            {
                if (Logging.DebugAutoStart || Logging.DebugIdle)
                    Logging.Log("DebugIdle: InSpace [" + Cache.Instance.InSpace + "]");

                // Questor does not handle in space starts very well, head back to base to try again
                Logging.Log("Started questor while in space, heading back to base in [" + _randomDelay + "] seconds");
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                return;
            }

            _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            _States.CurrentDroneState = DroneState.Idle;
            _States.CurrentSalvageState = SalvageState.Idle;
            _States.CurrentStorylineState = StorylineState.Idle;
            _States.CurrentTravelerState = TravelerState.AtDestination;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;

            if (Settings.Instance.AutoStart)
            {
                if (Logging.DebugAutoStart || Logging.DebugIdle)
                    Logging.Log("DebugAutoStart: Autostart [" + Settings.Instance.AutoStart + "]");

                //                // Don't start a new action an hour before downtime
                //                if (DateTime.UtcNow.Hour == 10)
                //                {
                //                    if (Logging.DebugAutoStart || Logging.DebugIdle) Logging.Log("CombatMissionsBehavior", "DebugIdle: Don't start a new action an hour before downtime, DateTime.UtcNow.Hour [" + DateTime.UtcNow.Hour + "]", Logging.White);
                //                    //QuestorUI.lblCurrentMissionInfo.Text = "less than 1 hour before downtime, waiting";
                //                    return;
                //                }
//
                //                // Don't start a new action near downtime
                //                if (DateTime.UtcNow.Hour == 11 && DateTime.UtcNow.Minute < 15)
                //                {
                //                    if (Logging.DebugAutoStart || Logging.DebugIdle) Logging.Log("CombatMissionsBehavior", "DebugIdle: Don't start a new action near downtime, DateTime.UtcNow.Hour [" + DateTime.UtcNow.Hour + "] DateTime.UtcNow.Minute [" + DateTime.UtcNow.Minute + "]", Logging.White);
                //                    //QuestorUI.lblCurrentMissionInfo.Text = "less than 15min after downtime, waiting";
                //                    return;
                //                }

                if (Settings.Instance.RandomDelay > 0 || Settings.Instance.MinimumDelay > 0)
                {
                    _randomDelay = (Settings.Instance.RandomDelay > 0 ? _random.Next(Settings.Instance.RandomDelay) : 0) + Settings.Instance.MinimumDelay;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedStart;
                    //QuestorUI.lblCurrentMissionInfo.Text = "Random start delay of [" + _randomDelay + "] seconds";
                    Logging.Log("Random start delay of [" + _randomDelay + "] seconds");
                    return;
                }

                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Cleanup;
                return;
            }

            if (Logging.DebugAutoStart)
                Logging.Log("DebugIdle: Autostart is currently [" + Settings.Instance.AutoStart + "]");
            Time.Instance.LastScheduleCheck = DateTime.UtcNow;
//            Questor.TimeCheck(); //Should we close questor due to stoptime or runtime?

            //Questor.WalletCheck(); //Should we close questor due to no wallet balance change? (stuck?)
        }

        private void DelayedStartCMBState()
        {
            if (DateTime.UtcNow < _lastCMBIdle.AddSeconds(_randomDelay))
            {
                return;
            }

            _storyline.Reset();
            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Cleanup;
            return;
        }

        private void DelayedGotoBaseCMBState()
        {
            if (DateTime.UtcNow < _lastCMBIdle.AddSeconds(Time.Instance.DelayedGotoBase_seconds))
            {
                return;
            }

            Logging.Log("Heading back to base");
            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
            return;
        }

        private void CleanupCMBState()
        {
            // this States.CurrentCombatMissionBehaviorState is needed because forced disconnects
            // and crashes can leave "extra" cargo in the
            // cargo hold that is undesirable and causes
            // problems loading the correct ammo on occasion
            //

            Cleanup.CheckEVEStatus();
            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Start;
            return;
        }

        private void StartCMBState()
        {
            if (!WeShouldBeInSpaceORInStationAndOutOfSessionChange()) return;


            if (Cache.LootAlreadyUnloaded == false)
            {
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Switch;
                return;
            }


            if (Cache.Instance.InSpace)
            {
                // Questor does not handle in space starts very well, head back to base to try again
                Logging.Log("Started questor while in space, heading back to base in [" + _randomDelay + "] seconds");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.DelayedGotoBase);
                return;
            }

            if (_firstStart)
            {
                if (string.IsNullOrEmpty(MissionSettings.MissionsPath) || string.IsNullOrWhiteSpace(MissionSettings.MissionsPath))
                {
                    Logging.Log("You *must* set a MissionsPath to use Questor. Without it we do not know what directory to pull mission XMLs from. Disabling Autostart.");
                    Settings.Instance.AutoStart = false;
                    Cache.Instance.Paused = true;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                    return;
                }

                //if you are in wrong station and is not first agent
                _firstStart = false;
//				ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Switch);
                return;
            }

            Salvage.OpenWrecks = false;
            if (_States.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                Cache.Instance.Wealth = Cache.Instance.DirectEve.Me.Wealth;

                Statistics.WrecksThisMission = 0;
                if (Settings.Instance.EnableStorylines && _storyline.HasStoryline())
                {
                    Logging.Log("Storyline detected, doing storyline.");
                    _storyline.Reset();
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineSwitchAgents);
                    return;
                }

                Logging.Log("Start conversation [Start Mission]");
                _States.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                AgentInteraction.Purpose = AgentInteractionPurpose.StartMission;
            }

            AgentInteraction.ProcessState();

            if (AgentInteraction.Purpose == AgentInteractionPurpose.CompleteMission)
                //AgentInteractionPurpose was likely changed by AgentInteraction.ProcessState()
            {
                if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
                {
                    _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                    if (Cache.Instance.CourierMission)
                    {
                        Cache.Instance.CourierMission = false;
                        _States.CurrentQuestorState = QuestorState.Idle;
                        ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle);
                        return;
                    }

                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                    return;
                }

                return;
            }

            if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                MissionSettings.UpdateMissionName(Cache.Instance.Agent.AgentId);
                _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm);
                return;
            }

            if (_States.CurrentAgentInteractionState == AgentInteractionState.ChangeAgent)
            {
                _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ValidateCombatMissionSettings();
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Switch);
                return;
            }

            return;
        }

        private void SwitchCMBState()
        {
            if (!Cache.Instance.InStation)
            {
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                return;
            }

            if (Cache.Instance.DirectEve.Session.StationId != null && Cache.Instance.Agent != null &&
                Cache.Instance.DirectEve.Session.StationId != Cache.Instance.Agent.StationId)
            {
                Logging.Log("We're not in the right station, going home.");
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.DelayedGotoBase;
                return;
            }

            if (Cache.Instance.CurrentShipsCargo == null || Cache.Instance.CurrentShipsCargo.Items == null || Cache.Instance.ItemHangar == null ||
                Cache.Instance.ItemHangar.Items == null)
            {
                return;
            }

            if (Cache.Instance.DirectEve.ActiveShip != null && Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any() &&
                Cache.Instance.DirectEve.ActiveShip.GivenName.ToLower() != Combat.CombatShipName.ToLower())
            {
                Logging.Log("if(Cache.Instance.CurrentShipsCargo.Items.Any())");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                return;
            }

            if (_States.CurrentArmState == ArmState.Idle)
            {
                Logging.Log("Begin");
                _States.CurrentArmState = ArmState.ActivateCombatShip;
                Arm.SwitchShipsOnly = true;
            }

            if (Logging.DebugArm) Logging.Log("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            Arm.ProcessState();

            if (_States.CurrentArmState == ArmState.Done)
            {
                Logging.Log("Done");
                _States.CurrentArmState = ArmState.Idle;
                Arm.SwitchShipsOnly = false;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            return;
        }

        private void ArmCMBState()
        {
            if (Settings.Instance.BuyAmmo)
            {
                BuyAmmo.ProcessState();

                if (BuyAmmo.state != BuyAmmoState.Done && BuyAmmo.state != BuyAmmoState.DisabledForThisSession)
                {
                    return;
                }
            }

            if (_States.CurrentArmState == ArmState.Idle)
            {
                if (Cache.Instance.CourierMission)
                {
                    Logging.Log("Begin: CourierMission");
                    _States.CurrentArmState = ArmState.ActivateTransportShip;
                }
                else
                {
                    Logging.Log("Begin");
                    Arm.ChangeArmState(ArmState.Begin);
                }
            }

            if (!Cache.Instance.InStation) return;

            Arm.ProcessState();

            if (_States.CurrentArmState == ArmState.NotEnoughAmmo)
            {
                // we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                // we may be out of drones/ammo but disconnecting/reconnecting will not fix that so update the timestamp
                if (!Cache.Instance.UpdateMyWalletBalance()) return;
                Logging.Log("Armstate.NotEnoughAmmo");
                Arm.ChangeArmState(ArmState.Idle);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                return;
            }

            if (_States.CurrentArmState == ArmState.NotEnoughDrones)
            {
                // we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                // we may be out of drones/ammo but disconnecting/reconnecting will not fix that so update the timestamp
                if (!Cache.Instance.UpdateMyWalletBalance()) return;
                Logging.Log("Armstate.NotEnoughDrones");
                Arm.ChangeArmState(ArmState.Idle);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                return;
            }

            if (_States.CurrentArmState == ArmState.Done)
            {
                //we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime
                if (!Cache.Instance.UpdateMyWalletBalance()) return;
                Arm.ChangeArmState(ArmState.Idle);

                if (Settings.Instance.BuyAmmo && BuyAmmo.state != BuyAmmoState.DisabledForThisSession)
                {
                    BuyAmmo.state = BuyAmmoState.Idle;
                }

                _States.CurrentDroneState = DroneState.WaitingForTargets;
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.LocalWatch;
                return;
            }

            return;
        }

        private void LocalWatchCMBState()
        {
            if (Settings.Instance.UseLocalWatch)
            {
                if (!Cache.Instance.UpdateMyWalletBalance()) return;
                Time.Instance.LastLocalWatchAction = DateTime.UtcNow;

                if (Logging.DebugArm) Logging.Log("Starting: Is LocalSafe check...");
                if (Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    Logging.Log("local is clear");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                    return;
                }

                Logging.Log("Bad standings pilots in local: We will stay 5 minutes in the station and then we will check if it is clear again");
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.WaitingforBadGuytoGoAway;
                return;
            }

            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
            return;
        }

        private void WaitingFoBadGuyToGoAway()
        {
            if (!Cache.Instance.UpdateMyWalletBalance()) return;
            if (DateTime.UtcNow.Subtract(Time.Instance.LastLocalWatchAction).TotalMinutes <
                Time.Instance.WaitforBadGuytoGoAway_minutes + Cache.Instance.RandomNumber(1, 3))
            {
                return;
            }

            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.LocalWatch;
            return;
        }

        private void WarpOutBookmarkCMBState()
        {
            if (!string.IsNullOrEmpty(Settings.Instance.UndockBookmarkPrefix))
            {
                IEnumerable<DirectBookmark> warpOutBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.UndockBookmarkPrefix ?? "");
                if (warpOutBookmarks != null && warpOutBookmarks.Any())
                {
                    var warpOutBookmark =
                        warpOutBookmarks.OrderByDescending(b => b.CreatedOn).FirstOrDefault(b => b.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId);

                    long solarid = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookmark == null)
                    {
                        Logging.Log("No Bookmark");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                    }
                    else if (warpOutBookmark.LocationId == solarid)
                    {
                        if (Traveler.Destination == null)
                        {
                            Logging.Log("Warp at " + warpOutBookmark.Title);
                            Traveler.Destination = new BookmarkDestination(warpOutBookmark);
                            Defense.DoNotBreakInvul = true;
                        }

                        Traveler.ProcessState();
                        if (_States.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Logging.Log("Safe!");
                            Defense.DoNotBreakInvul = false;
                            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                            Traveler.Destination = null;
                        }
                    }
                    else
                    {
                        Logging.Log("No Bookmark in System");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                    }

                    return;
                }
            }

            Logging.Log("No Bookmark in System");
            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
            return;
        }

        private void GotoBaseCMBState()
        {
            //
            // we can start this in station OR in space, but we STILL do not want to do anything while in session change!
            //
            if (!WeShouldBeInSpaceORInStationAndOutOfSessionChange()) return;

            //
            // This will take a variable number of passes based on what traveler has to do.
            //
            Drones.IsMissionPocketDone = true; //pulls drones if we are not scrambled
            if (Logging.DebugGotobase) Logging.Log("GotoBase: AvoidBumpingThings()");
            Salvage.CurrentlyShouldBeSalvaging = false;

            if (NavigateOnGrid.AvoidBumpingThingsBool)
            {
                if (Logging.DebugGotobase) Logging.Log("GotoBase: if (Settings.Instance.AvoidBumpingThings)");
                if (NavigateOnGrid.AvoidBumpingThings(Cache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.GotoBase"))
                    return; //if true then we sent a command to eve, wait a sec before doing something else w the game.
            }

            if (Logging.DebugGotobase) Logging.Log("GotoBase: Traveler.TravelHome()");

            Traveler.TravelHome("CombatMissionsBehavior.TravelHome");

            if (_States.CurrentTravelerState == TravelerState.AtDestination && Cache.Instance.InStation &&
                DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(Cache.Instance.RandomNumber(10, 15)))
                // || DateTime.UtcNow.Subtract(Time.EnteredCloseQuestor_DateTime).TotalMinutes > 10)
            {
                if (Logging.DebugGotobase) Logging.Log("GotoBase: We are at destination");
                Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                if (Cache.Instance.Agent.AgentId != 0)
                {
                    try
                    {
                        MissionSettings.Mission = Cache.Instance.GetAgentMission(Cache.Instance.Agent.AgentId, true);
                    }
                    catch (Exception exception)
                    {
                        Logging.Log("Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID); [" + exception + "]");
                    }

                    //if (Cache.Instance.Mission == null)
                    //{
                    //    Logging.Log("CombatMissionsBehavior", "Cache.Instance.Mission == null - retry on next iteration", Logging.Teal);
                    //    return;
                    //}
                }

                if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                {
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                    return;
                }

                if (_States.CurrentCombatState != CombatState.OutOfAmmo && MissionSettings.Mission != null &&
                    MissionSettings.Mission.State == (int) MissionState.Accepted)
                {
                    ValidateCombatMissionSettings();
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CompleteMission);
                    return;
                }

                Traveler.Destination = null;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                return;
            }

            return;
        }

        private void CompleteMissionCMBState()
        {
            if (!Cache.Instance.InStation) return; // Do Not Continue if we are not in station and out of session change

            //
            // 2 Iterations needed to finish Complete Mission (1 sec a piece == 2 seconds)
            //
            if (_States.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                Logging.Log("Start Conversation [Complete Mission]");
                _States.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                AgentInteraction.Purpose = AgentInteractionPurpose.CompleteMission;
            }

            AgentInteraction.ProcessState();

            if (_States.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                _States.CurrentAgentInteractionState = AgentInteractionState.Idle;
                if (Cache.Instance.CourierMission)
                {
                    Cache.Instance.CourierMission = false;
                    _States.CurrentQuestorState = QuestorState.Idle;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Idle);
                    return;
                }

                if (Statistics.LastMissionCompletionError.AddSeconds(10) < DateTime.UtcNow)
                {
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Statistics);
                    return;
                }
                Logging.Log("Skipping statistics: We have not yet completed a mission");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                return;
            }

            return;
        }

        private void StatisticsCMBState()
        {
            if (Drones.UseDrones)
            {
                DirectInvType drone = null;
                Cache.Instance.DirectEve.InvTypes.TryGetValue(Drones.DroneTypeID, out drone);
                if (drone != null)
                {
                    if (Drones.DroneBay == null) return;
                    Statistics.LostDrones = (int) Math.Floor((Drones.DroneBay.Capacity - Drones.DroneBay.UsedCapacity)/drone.Volume);
                    //Logging.Log("CombatMissionsBehavior: Starting: Statistics.WriteDroneStatsLog");
                    if (!Statistics.WriteDroneStatsLog()) return;
                }
                else
                {
                    Logging.Log("Could not find the drone TypeID specified in the character settings xml; this should not happen!");
                }
            }

            //Logging.Log("CombatMissionsBehavior: Starting: Statistics.AmmoConsumptionStatistics");
            if (!Statistics.AmmoConsumptionStatistics()) return;
            Statistics.FinishedMission = DateTime.UtcNow;

            // only attempt to write the mission statistics logs if one of the mission stats logs is enabled in settings
            if (Statistics.MissionStats3Log)
            {
                try
                {
                    //Logging.Log("CombatMissionsBehavior.Idle", "Cache.Instance.ActiveShip.Givenname.ToLower() [" + Cache.Instance.ActiveShip.GivenName.ToLower() + "]", Logging.Teal);
                    //Logging.Log("CombatMissionsBehavior.Idle", "Settings.Instance.CombatShipName.ToLower() [" + Settings.Instance.CombatShipName.ToLower() + "]", Logging.Teal);
                    if (!Statistics.MissionLoggingCompleted)
                    {
                        if (Logging.DebugStatistics) Logging.Log("Statistics.WriteMissionStatistics(AgentID);");
                        Statistics.WriteMissionStatistics(Cache.Instance.Agent.AgentId);
                        if (Logging.DebugStatistics)
                            Logging.Log("Done w Statistics.WriteMissionStatistics(AgentID);");
                        return;
                    }
                }
                catch
                {
                    Logging.Log("if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GivenName.ToLower() == Settings.Instance.CombatShipName.ToLower())");
                }
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
            return;
        }

        private void ExecuteMissionCMBState()
        {
            if (!Cache.Instance.InSpace)
            {
                if (Logging.DebugExecuteMission) Logging.Log("if (!Cache.Instance.InSpace)");
                return; //if we are not in space and out of session change wait.
            }

            //
            // Execution time will vary heavily on the mission!
            //

            if (Logging.DebugExecuteMission) Logging.Log("Start: _combatMissionCtrl.ProcessState();");
            _combatMissionCtrl.ProcessState();
            if (Logging.DebugExecuteMission) Logging.Log("Start: Combat.ProcessState();");
            Combat.ProcessState();
            Drones.ProcessState();
            Salvage.ProcessState();

            // If we are out of ammo, return to base, the mission will fail to complete and the bot will reload the ship
            // and try the mission again
            if (_States.CurrentCombatState == CombatState.OutOfAmmo)
            {
                Logging.Log("Out of Ammo! - Not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" + Combat.MinimumAmmoCharges +
                    "]");
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

                // Clear looted containers
                Cache.Instance.LootedContainers.Clear();
                //Cache.Instance.InvalidateBetweenMissionsCache();
            }

            if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Done)
            {
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                // Clear looted containers
                Cache.Instance.LootedContainers.Clear();
                //Cache.Instance.InvalidateBetweenMissionsCache();
            }

            // If in error state, just go home and stop the bot
            if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
            {
                Logging.Log("Error");
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

                // Clear looted containers
                Cache.Instance.LootedContainers.Clear();
                //Cache.Instance.InvalidateBetweenMissionsCache();
            }

            return;
        }

        private void TravelerCMBState()
        {
            try
            {
                if (!WeShouldBeInSpaceORInStationAndOutOfSessionChange())
                    return; //either in space or in station is fine, but we should not continue until we are not in a session change


                if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
                {
                    if (Logging.DebugTargetWrecks) Logging.Log("Salvage.OpenWrecks = false;");
                    Salvage.OpenWrecks = false;
                }

                var destination = Cache.Instance.DirectEve.Navigation.GetDestinationPath();
                if (destination == null || destination.Count == 0)
                {
                    // happens if autopilot is not set and this QuestorState is chosen manually
                    // this also happens when we get to destination (!?)
                    Logging.Log("No destination?");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

                if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                {
                    destination[0] = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                }

                if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.LastOrDefault())
                {
                    if (Cache.Instance.AllBookmarks != null && Cache.Instance.AllBookmarks.Any()) //we might have no bookmarks...
                    {
                        IEnumerable<DirectBookmark> bookmarks = Cache.Instance.AllBookmarks.Where(b => b.LocationId == destination.LastOrDefault()).ToList();
                        if (bookmarks.FirstOrDefault() != null && bookmarks.Any())
                        {
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                            return;
                        }

                        Logging.Log("Destination: [" + Cache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]");
                        var lastSolarSystemInRoute = destination.LastOrDefault();


                        Logging.Log("Destination: [" + lastSolarSystemInRoute + "]");
                        Traveler.Destination = new SolarSystemDestination(destination.LastOrDefault());
                        return;
                    }

                    return;
                }

                //we also assume you are connected during a manual set of questor into travel mode (safe assumption considering someone is at the kb)
                if (!Cache.Instance.UpdateMyWalletBalance()) return;


                if (_States.CurrentTravelerState == TravelerState.AtDestination)
                {
                    if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                    {
                        Logging.Log("an error has occurred");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                        return;
                    }

                    if (Cache.Instance.InSpace)
                    {
                        Logging.Log("Arrived at destination (in space, Questor stopped)");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                        return;
                    }

                    Logging.Log("Arrived at destination");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                    return;
                }

                Traveler.ProcessState();

                return;
            }
            catch (Exception exception)
            {
                Logging.Log("Exception [" + exception + "]");
                return;
            }
        }

        private void GotoMissionCMBState()
        {
            try
            {
                if (!WeShouldBeInSpaceORInStationAndOutOfSessionChange())
                    return; //either in space or in station is fine, but we should not continue until we are not in a session change
                Statistics.MissionLoggingCompleted = false;
                Drones.IsMissionPocketDone = false;
                Cache.LootAlreadyUnloaded = false;

                var missionDestination = Traveler.Destination as MissionBookmarkDestination;

                if (missionDestination == null || missionDestination.AgentId != Cache.Instance.Agent.AgentId)
                    // We assume that this will always work "correctly" (tm)
                {
                    var nameOfBookmark = "";
                    if (Settings.Instance.EveServerName == "Tranquility") nameOfBookmark = "Encounter";
                    if (Settings.Instance.EveServerName == "Serenity") nameOfBookmark = "遭遇战";
                    if (nameOfBookmark == "") nameOfBookmark = "Encounter";
                    if (MissionSettings.GetMissionBookmark(Cache.Instance.Agent.AgentId, nameOfBookmark) != null)
                    {
                        Logging.Log("Setting Destination to 1st bookmark from AgentID: " + Cache.Instance.Agent.AgentId + " with [" + nameOfBookmark + "] in the title");
                        Traveler.Destination = new MissionBookmarkDestination(MissionSettings.GetMissionBookmark(Cache.Instance.Agent.AgentId, nameOfBookmark));
                        if (Cache.Instance.DirectEve.Navigation.GetLocation(Traveler.Destination.SolarSystemId) != null)
                        {
                            Cache.Instance.MissionSolarSystem = Cache.Instance.DirectEve.Navigation.GetLocation(Traveler.Destination.SolarSystemId);
                            Logging.Log("MissionSolarSystem is [" + Cache.Instance.MissionSolarSystem.Name + "]");
                        }
                    }
                    else
                    {
                        Logging.Log("We have no mission bookmark available for our current/normal agent");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    }
                }

                if (Combat.PotentialCombatTargets.Any())
                {
                    Logging.Log("[" + Combat.PotentialCombatTargets.Count() + "] potentialCombatTargets found , Running combat.ProcessState");
                    Combat.ProcessState();
                }

                Traveler.ProcessState();

                if (_States.CurrentTravelerState == TravelerState.AtDestination)
                {
                    // Seeing as we just warped to the mission, start the mission controller
                    _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Start;
                    Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.ExecuteMission);
                    return;
                }

                return;
            }
            catch (Exception exception)
            {
                Logging.Log("Exception [" + exception + "]");
                return;
            }
        }

        private void UnloadLootCMBState()
        {
            try
            {
                if (!Cache.Instance.InStation)
                    return; //if we are in space GotoBase, if we are in station wait until we are out of any session changes before continuing

                if (_States.CurrentUnloadLootState == UnloadLootState.Idle)
                {
                    Logging.Log("UnloadLoot: Begin");
                    _States.CurrentUnloadLootState = UnloadLootState.Begin;
                }

                UnloadLoot.ProcessState();

                if (_States.CurrentUnloadLootState == UnloadLootState.Done)
                {
                    Cache.LootAlreadyUnloaded = true;
                    _States.CurrentUnloadLootState = UnloadLootState.Idle;
                    MissionSettings.Mission = Cache.Instance.GetAgentMission(Cache.Instance.Agent.AgentId, true);

                    //if (Cache.Instance.Mission == null)
                    //{
                    //    Logging.Log("CombatMissionsBehavior", "Cache.Instance.Mission == null - retry on next iteration", Logging.Teal);
                    //    return;
                    //}

                    if (_States.CurrentCombatState == CombatState.OutOfAmmo) // on mission
                    {
                        Logging.Log("_States.CurrentCombatState == CombatState.OutOfAmmo");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                        return;
                    }

                    if ((MissionSettings.Mission != null) && (MissionSettings.Mission.State != (int) MissionState.Offered)) // on mission
                    {
                        Logging.Log("We are on mission");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                        return;
                    }

                    //This salvaging decision tree does not belong here and should be separated out into a different QuestorState
                    if (Salvage.AfterMissionSalvaging)
                    {
                        if (Cache.Instance.GetSalvagingBookmark == null)
                        {
                            Logging.Log(" No more salvaging bookmarks. Setting FinishedSalvaging Update.");

                            //if (Settings.Instance.CharacterMode == "Salvager")
                            //{
                            //    Logging.Log("Salvager mode set and no bookmarks making delay");
                            //    States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorStateState.Error; //or salvageonly. need to check difference
                            //}

                            //Logging.Log("CombatMissionsBehavior: Character mode is not salvage going to next mission.");
                            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle; //add pause here
//							_States.CurrentQuestorState = QuestorState.Idle;

                            Statistics.FinishedSalvaging = DateTime.UtcNow;
                            return;
                        }
                        else //There is at least 1 salvage bookmark
                        {
                            Logging.Log("There are [ " + Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Count +
                                " ] more salvage bookmarks left to process");

                            // Salvage only after multiple missions have been completed
                            if (Salvage.SalvageMultipleMissionsinOnePass)
                            {
                                //if we can still complete another mission before the Wrecks disappear and still have time to salvage
                                if (DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes >
                                    (Time.Instance.WrecksDisappearAfter_minutes - Time.Instance.AverageTimeToCompleteAMission_minutes -
                                     Time.Instance.AverageTimetoSalvageMultipleMissions_minutes))
                                {
                                    Logging.Log("The last finished after mission salvaging session was [" +
                                        DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes + "] ago ");
                                    Logging.Log("we are after mission salvaging again because it has been at least [" +
                                        (Time.Instance.WrecksDisappearAfter_minutes - Time.Instance.AverageTimeToCompleteAMission_minutes -
                                         Time.Instance.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.CheckBookmarkAge;
                                    Statistics.StartedSalvaging = DateTime.UtcNow;

                                    //FIXME: should we be overwriting this timestamp here? What if this is the 3rd run back and fourth to the station?
                                }
                                else //we are salvaging mission 'in one pass' and it has not been enough time since our last run... do another mission
                                {
                                    Logging.Log("The last finished after mission salvaging session was [" +
                                        DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes + "] ago ");
                                    Logging.Log("we are going to the next mission because it has not been [" +
                                        (Time.Instance.WrecksDisappearAfter_minutes - Time.Instance.AverageTimeToCompleteAMission_minutes -
                                         Time.Instance.AverageTimetoSalvageMultipleMissions_minutes) + "] min since the last session. ");
                                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                                }
                            }
                            else //begin after mission salvaging now, rather than later
                            {
                                Logging.Log("The last after mission salvaging session was [" +
                                    Math.Round(DateTime.UtcNow.Subtract(Statistics.FinishedSalvaging).TotalMinutes, 0) + "min] ago ");
                                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.CheckBookmarkAge;
                                Statistics.StartedSalvaging = DateTime.UtcNow;
                            }
                        }
                    }
                    else
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                        _States.CurrentQuestorState = QuestorState.Idle;
                        Logging.Log("CharacterMode: [" + Settings.Instance.CharacterMode + "], AfterMissionSalvaging: [" + Salvage.AfterMissionSalvaging +
                            "], CombatMissionsBehaviorState: [" + _States.CurrentCombatMissionBehaviorState + "]");
                        return;
                    }

                    return;
                }

                return;
            }
            catch (Exception exception)
            {
                Logging.Log("Exception [" + exception + "]");
                return;
            }
        }

        private void BeginAftermissionSalvagingCMBState()
        {
            Statistics.StartedSalvaging = DateTime.UtcNow;
                //this will be reset for each "run" between the station and the field if using <unloadLootAtStation>true</unloadLootAtStation>
            Drones.IsMissionPocketDone = false;
            Salvage.CurrentlyShouldBeSalvaging = true;

            if (DateTime.UtcNow.Subtract(_lastSalvageTrip).TotalMinutes < Time.Instance.DelayBetweenSalvagingSessions_minutes &&
                Settings.Instance.CharacterMode.ToLower() == "salvage".ToLower())
            {
                Logging.Log("Too early for next salvage trip");
                return;
            }

            if (DateTime.UtcNow > _nextBookmarkRefreshCheck)
            {
                _nextBookmarkRefreshCheck = DateTime.UtcNow.AddMinutes(1);
                if (Cache.Instance.InStation && (DateTime.UtcNow > _nextBookmarksrefresh))
                {
                    _nextBookmarksrefresh = DateTime.UtcNow.AddMinutes(Cache.Instance.RandomNumber(2, 4));
                    Logging.Log("Refreshing Bookmarks Now: Next Bookmark refresh in [" + Math.Round(_nextBookmarksrefresh.Subtract(DateTime.UtcNow).TotalMinutes, 0) +
                        "min]");
                    Cache.Instance.DirectEve.RefreshBookmarks();
                    return;
                }

                Logging.Log("Next Bookmark refresh in [" + Math.Round(_nextBookmarksrefresh.Subtract(DateTime.UtcNow).TotalMinutes, 0) + "min]");
            }


            Salvage.OpenWrecks = true;


            if (_States.CurrentArmState == ArmState.Idle)
            {
                _States.CurrentArmState = ArmState.ActivateSalvageShip;
            }

            Arm.ProcessState();
            if (_States.CurrentArmState == ArmState.Done)
            {
                _States.CurrentArmState = ArmState.Idle;
                var bookmark = Cache.Instance.GetSalvagingBookmark;
                if (bookmark == null && Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").Any())
                {
                    bookmark = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ").OrderBy(b => b.CreatedOn).FirstOrDefault();
                    if (bookmark == null)
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                        return;
                    }
                }

                _lastSalvageTrip = DateTime.UtcNow;
                Traveler.Destination = new BookmarkDestination(bookmark);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoSalvageBookmark);
                return;
            }

            return;
        }

        private void SalvageCMBState()
        {
            Salvage.SalvageAll = true;
            Salvage.OpenWrecks = true;
            Salvage.CurrentlyShouldBeSalvaging = true;

            var deadlyNPC = Combat.PotentialCombatTargets.FirstOrDefault();
            if (deadlyNPC != null)
            {
                // found NPCs that will likely kill out fragile salvage boat!
                var missionSalvageBookmarks = Cache.Instance.BookmarksByLabel(Settings.Instance.BookmarkPrefix + " ");
                Logging.Log("could not be completed because of NPCs left in the mission: deleting on grid salvage bookmark");

                if (Salvage.DeleteBookmarksWithNPC)
                {
                    if (!Cache.Instance.DeleteBookmarksOnGrid("CombatMissionsBehavior.Salvage")) return;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoSalvageBookmark;
                    var bookmark = missionSalvageBookmarks.OrderBy(i => i.CreatedOn).FirstOrDefault();
                    Traveler.Destination = new BookmarkDestination(bookmark);
                    return;
                }

                Logging.Log("could not be completed because of NPCs left in the mission: on grid salvage bookmark not deleted");
                Salvage.SalvageAll = false;
                Statistics.FinishedSalvaging = DateTime.UtcNow;
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                return;
            }

            if (Salvage.UnloadLootAtStation)
            {
                if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.UsedCapacity > 0)
                {
                    if ((Cache.Instance.CurrentShipsCargo.Capacity - Cache.Instance.CurrentShipsCargo.UsedCapacity) <
                        Salvage.ReserveCargoCapacity + 10)
                    {
                        Logging.Log("We are full: My Cargo is at [" +
                            Math.Round(Cache.Instance.CurrentShipsCargo.UsedCapacity, 2) + "m3] of[" +
                            Math.Round(Cache.Instance.CurrentShipsCargo.Capacity, 2) + "] Reserve [" +
                            Math.Round((double)Salvage.ReserveCargoCapacity, 2) + "m3 + 10], go to base to unload");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                        return;
                    }
                }
            }

            //if we have no salvagers check for UnlootedContainers before moving on
            // (old - removed, if re-added we need to do this elsewhere to avoid getting stuck!) if we have salvagers check for any wrecks before moving on
            if ((!Cache.Instance.MyShipEntity.SalvagersAvailable || !Cache.Instance.UnlootedContainers.Any()) ||
                DateTime.UtcNow > Time.Instance.LastInWarp.AddMinutes(Time.Instance.MaxSalvageMinutesPerPocket))
                //|| Cache.Instance.MyShipEntity.SalvagersAvailable && !Cache.Instance.Wrecks.Any())
            {
                if (!Cache.Instance.DeleteBookmarksOnGrid("CombatMissionsBehavior.Salvage")) return;

                if (DateTime.UtcNow > Time.Instance.LastInWarp.AddMinutes(Time.Instance.MaxSalvageMinutesPerPocket))
                {
                    Logging.Log("We have been salvaging this pocket for more than [" + Time.Instance.MaxSalvageMinutesPerPocket +
                        "] min - moving on - something probably went wrong here somewhere. (debugSalvage might help narrow down what)");
                }

                Logging.Log("Finished salvaging the pocket. UnlootedContainers [" + Cache.Instance.UnlootedContainers.Count() + "] Wrecks [" + Cache.Instance.Wrecks +
                    "] Salvagers? [" + Cache.Instance.MyShipEntity.SalvagersAvailable + "]");
                Statistics.FinishedSalvaging = DateTime.UtcNow;

                if (!Cache.Instance.AfterMissionSalvageBookmarks.Any() && !Cache.Instance.GateInGrid())
                {
                    Logging.Log("We have salvaged all bookmarks, go to base");
                    Salvage.SalvageAll = false;
                    Statistics.FinishedSalvaging = DateTime.UtcNow;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    return;
                }

                if (!Cache.Instance.GateInGrid()) //no acceleration gate found
                {
                    Logging.Log("Go to the next salvage bookmark");
                    DirectBookmark bookmark;
                    if (Salvage.FirstSalvageBookmarksInSystem)
                    {
                        bookmark =
                            Cache.Instance.AfterMissionSalvageBookmarks.FirstOrDefault(c => c.LocationId == Cache.Instance.DirectEve.Session.SolarSystemId) ??
                            Cache.Instance.AfterMissionSalvageBookmarks.FirstOrDefault();
                    }
                    else
                    {
                        bookmark = Cache.Instance.AfterMissionSalvageBookmarks.OrderBy(i => i.CreatedOn).FirstOrDefault() ??
                                   Cache.Instance.AfterMissionSalvageBookmarks.FirstOrDefault();
                    }

                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoSalvageBookmark;
                    Traveler.Destination = new BookmarkDestination(bookmark);
                    return;
                }

                if (Salvage.UseGatesInSalvage) // acceleration gate found, are we configured to use it or not?
                {
                    Logging.Log("Acceleration gate found - moving to next pocket");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.SalvageUseGate;
                    return;
                }

                Logging.Log("Acceleration gate found, useGatesInSalvage set to false - Returning to base");
                Statistics.FinishedSalvaging = DateTime.UtcNow;
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                Traveler.Destination = null;
                return;
            }

            if (Logging.DebugSalvage)
                Logging.Log("salvage: we __cannot ever__ approach in salvage.cs so this section _is_ needed");
            Salvage.MoveIntoRangeOfWrecks();
            try
            {
                // Overwrite settings, as the 'normal' settings do not apply
                Salvage.DedicatedSalvagerMaximumWreckTargets = Cache.Instance.MaxLockedTargets;
                Salvage.DedicatedSalvagerReserveCargoCapacity = 80;
                Salvage.DedicatedSalvagerLootEverything = true;
                //
                // Run Salvagers ProcessState (the actual salvaging!)
                //
                Salvage.ProcessState();

                //Logging.Log("number of max cache ship: " + Cache.Instance.ActiveShip.MaxLockedTargets);
                //Logging.Log("number of max cache me: " + Cache.Instance.DirectEve.Me.MaxLockedTargets);
                //Logging.Log("number of max math.min: " + _salvage.MaximumWreckTargets);
            }
            finally
            {
                //
                // revert the settings back to NonSalvage values saved in characters settings XML
                //

                Salvage.DedicatedSalvagerMaximumWreckTargets = null;
                Salvage.DedicatedSalvagerReserveCargoCapacity = null;
                Salvage.DedicatedSalvagerLootEverything = null;
            }

            return;
        }

        private void SalvageGotoBookmarkCMBState()
        {
            if (!Cache.Instance.UpdateMyWalletBalance()) return;
            Traveler.ProcessState();

            if (_States.CurrentTravelerState == TravelerState.AtDestination || Cache.Instance.GateInGrid())
            {
                //we know we are connected if we were able to arm the ship - update the lastknownGoodConnectedTime

                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Salvage;
                Traveler.Destination = null;
                return;
            }

            return;
        }

        private void SalageUseGateCMBState()
        {
            Salvage.OpenWrecks = true;

            if (Cache.Instance.AccelerationGates == null || !Cache.Instance.AccelerationGates.Any())
            {
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoSalvageBookmark;
                return;
            }

            _lastX = Cache.Instance.ActiveShip.Entity.X;
            _lastY = Cache.Instance.ActiveShip.Entity.Y;
            _lastZ = Cache.Instance.ActiveShip.Entity.Z;


            var closest = Cache.Instance.AccelerationGates.OrderBy(t => t.Distance).FirstOrDefault();
            if (closest != null && closest.Distance < (int) Distances.DecloakRange)
            {
                Logging.Log("Gate found: [" + closest.Name + "] groupID[" + closest.GroupId + "]");

                // Activate it and move to the next Pocket
                if (closest.Activate())
                {
                    // Do not change actions, if NextPocket gets a timeout (>2 mins) then it reverts to the last action
                    Logging.Log("Activate [" + closest.Name + "] and change States.CurrentCombatMissionBehaviorState to 'NextPocket'");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.SalvageNextPocket;
                    _lastPulse = DateTime.UtcNow;
                    return;
                }

                return;
            }

            if (closest != null && closest.Distance < (int) Distances.WarptoDistance)
            {
                // Move to the target
                if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != closest.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                {
                    if (closest.Approach())
                    {
                        Logging.Log("Approaching target [" + closest.Name + "][" + closest.MaskedId + "][" + Math.Round(closest.Distance / 1000, 0) + "k away]");
                    }
                }
            }
            else if (closest != null)
            {
                // Probably never happens
                if (closest.WarpTo())
                {
                    Logging.Log("Warping to [" + closest.Name + "] which is [" + Math.Round(closest.Distance / 1000, 0) + "k away]");
                }
            }

            _lastPulse = DateTime.UtcNow.AddSeconds(10);
        }

        private void SalvageNextPocketCMBState()
        {
            Salvage.OpenWrecks = true;
            var distance = Cache.Instance.DistanceFromMe(_lastX, _lastY, _lastZ);
            if (distance > (int) Distances.NextPocketDistance)
            {
                //we know we are connected here...
                if (!Cache.Instance.UpdateMyWalletBalance()) return;

                Logging.Log("We have moved to the next Pocket [" + Math.Round(distance / 1000, 0) + "k away]");

                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Salvage;
                return;
            }

            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMinutes > 2)
            {
                Logging.Log("We have timed out, retry last action");

                // We have reached a timeout, revert to ExecutePocketActions (e.g. most likely Activate)
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.SalvageUseGate;
                return;
            }

            return;
        }

        private void PrepareStorylineGotoBaseCMBState()
        {
            if (Logging.DebugGotobase) Logging.Log("PrepareStorylineGotoBase: AvoidBumpingThings()");

            if (NavigateOnGrid.AvoidBumpingThingsBool)
            {
                if (Logging.DebugGotobase)
                    Logging.Log("PrepareStorylineGotoBase: if (Settings.Instance.AvoidBumpingThings)");
                NavigateOnGrid.AvoidBumpingThings(Cache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.PrepareStorylineGotoBase");
            }

            if (Logging.DebugGotobase) Logging.Log("PrepareStorylineGotoBase: Traveler.TravelHome()");

            if (Settings.Instance.StoryLineBaseBookmark != "")
            {
                if (!Traveler.TravelToBookmarkName(Settings.Instance.StoryLineBaseBookmark, "CombatMissionsBehavior.TravelHome"))
                {
                    Traveler.TravelHome("CombatMissionsBehavior.TravelHome");
                }
            }
            else
            {
                Traveler.TravelHome("CombatMissionsBehavior.TravelHome");
            }

            if (_States.CurrentTravelerState == TravelerState.AtDestination && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(5))
            {
                if (Logging.DebugGotobase) Logging.Log("PrepareStorylineGotoBase: We are at destination");
                Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                if (Cache.Instance.Agent.AgentId != 0)
                {
                    try
                    {
                        MissionSettings.Mission = Cache.Instance.GetAgentMission(Cache.Instance.Agent.AgentId, true);
                    }
                    catch (Exception exception)
                    {
                        Logging.Log("Cache.Instance.Mission = Cache.Instance.GetAgentMission(AgentID); [" + exception + "]");
                        return;
                    }
                }

                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Storyline;
                return;
            }

            return;
        }

        private void GotoNearestStationCMBState()
        {
            if (!Cache.Instance.InSpace || (Cache.Instance.InSpace && Cache.Instance.InWarp)) return;
            EntityCache station = null;
            if (Cache.Instance.Stations != null && Cache.Instance.Stations.Any())
            {
                station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
            }

            if (station != null)
            {
                if (station.Distance > (int) Distances.WarptoDistance)
                {
                    if (station.WarpTo())
                    {
                        Logging.Log("[" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Salvage;
                        return;
                    }

                    return;
                }

                if (station.Distance < (int) Distances.DockingRange)
                {
                    if (station.Dock())
                    {
                        Logging.Log("[" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                        return;
                    }
                    return;
                }
                else
                {
                    if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != station.Id ||
                        Cache.Instance.MyShipEntity.Velocity < 50)
                    {
                        if (station.Approach())
                        {
                            Logging.Log("Approaching [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                        }
                    }
                }

                return;
            }

            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
            return;
        }

        private void StorylineReturnToBaseCMBState()
        {
            if (Logging.DebugGotobase) Logging.Log("StorylineReturnToBase: AvoidBumpingThings()");

            if (NavigateOnGrid.AvoidBumpingThingsBool)
            {
                if (Logging.DebugGotobase)
                    Logging.Log("StorylineReturnToBase: if (Settings.Instance.AvoidBumpingThings)");
                NavigateOnGrid.AvoidBumpingThings(Cache.Instance.BigObjects.FirstOrDefault(), "CombatMissionsBehaviorState.StorylineReturnToBase");
            }

            if (Logging.DebugGotobase) Logging.Log("StorylineReturnToBase: TravelToStorylineBase");

            if (Settings.Instance.StoryLineBaseBookmark != "")
            {
                if (!Traveler.TravelToBookmarkName(Settings.Instance.StoryLineBaseBookmark, "CombatMissionsBehavior.TravelToStorylineBase"))
                {
                    Traveler.TravelHome("CombatMissionsBehavior.TravelToStorylineBase");
                }
            }
            else
            {
                Traveler.TravelHome("CombatMissionsBehavior.TravelToStorylineBase");
            }

            if (_States.CurrentTravelerState == TravelerState.AtDestination && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(5))
            {
                if (Logging.DebugGotobase) Logging.Log("StorylineReturnToBase: We are at destination");
                Cache.Instance.GotoBaseNow = false; //we are there - turn off the 'forced' gotobase
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Switch;
            }

            return;
        }

        private bool CMBEveryPulse()
        {
            if (Logging.DebugDisableCombatMissionsBehavior)
            {
                Logging.Log("DebugDisableCombatMissionsBehavior true");
                return false;
            }

            // Only pulse state changes every 1.5s

            var pulseDelay = 400;
            if (Cache.Instance.InSpace) pulseDelay = 800;
            if (Cache.Instance.InStation) pulseDelay = 400;

            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < pulseDelay)
                return false;

            _lastPulse = DateTime.UtcNow;

            //If local unsafe go to base and do not start mission again (for the whole session?)
            if (Settings.Instance.FinishWhenNotSafe && (_States.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoNearestStation))
            {
                //need to remove spam
                if (Cache.Instance.InSpace &&
                    !Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    EntityCache station = null;
                    if (Cache.Instance.Stations != null && Cache.Instance.Stations.Any())
                    {
                        station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    }

                    if (station != null)
                    {
                        Logging.Log("Station found. Going to nearest station");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoNearestStation;
                    }
                    else
                    {
                        Logging.Log("Station not found. Going back to base");
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    }
                    Cache.Instance.StopBot = true;
                }
            }

            if (Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment)
            {
                if (_States.CurrentQuestorState != QuestorState.CloseQuestor)
                {
                    _States.CurrentQuestorState = QuestorState.CloseQuestor;
                    BeginClosingQuestor();
                }
            }

            if (Cache.Instance.GotoBaseNow)
            {
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
            }

            if ((DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalSeconds > 10) &&
                (DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalSeconds < 60))
            {
                if (Cache.Instance.QuestorJustStarted)
                {
                    Cache.Instance.QuestorJustStarted = false;
                    Cleanup.SessionState = "Starting Up";
                }
            }

            Cache.Instance.InMission = _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission;
            if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline &&
                _States.CurrentStorylineState == StorylineState.ExecuteMission)
            {
                Cache.Instance.InMission |= _storyline.StorylineHandler is GenericCombatStoryline &&
                                            (_storyline.StorylineHandler as GenericCombatStoryline).State == GenericCombatStorylineState.ExecuteMission;
            }

            //
            // Panic always runs, not just in space
            //
            Panic.ProcessState();
            if (_States.CurrentPanicState == PanicState.Panic || _States.CurrentPanicState == PanicState.Panicking)
            {
                if (PanicStateReset && (DateTime.UtcNow > Time.Instance.LastSessionChange.AddSeconds(30 + Cache.Instance.RandomNumber(1, 15))))
                {
                    Logging.Log("PanicStateReset [" + PanicStateReset + "] and It has been 30+ sec since our last session change");
                    _States.CurrentPanicState = PanicState.Normal;
                    PanicStateReset = false;
                }

                return false;
            }

            if (_States.CurrentPanicState == PanicState.Resume)
            {
                if (Cache.Instance.InSpace ||
                    (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastSessionChange.AddSeconds(30 + Cache.Instance.RandomNumber(1, 15))))
                {
                    // Reset panic state
                    _States.CurrentPanicState = PanicState.Normal;

                    // Ugly storyline resume hack
                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline)
                    {
                        Logging.Log("PanicState.Resume: CMB State is Storyline");
                        if (_storyline.StorylineHandler is GenericCombatStoryline)
                        {
                            (_storyline.StorylineHandler as GenericCombatStoryline).State = GenericCombatStorylineState.GotoMission;
                            Logging.Log("PanicState.Resume: Setting GenericCombatStorylineState to GotoMission");
                        }

                        return true;
                    }

                    if (Cache.Instance.CurrentStorylineAgentId >= 500)
                    {
                        Logging.Log("PanicState.Resume: CurrentStorylineAgentId >= 500");
                        if (_storyline.StorylineHandler is GenericCombatStoryline)
                        {
                            (_storyline.StorylineHandler as GenericCombatStoryline).State = GenericCombatStorylineState.GotoMission;
                            Logging.Log("PanicState.Resume: Setting GenericCombatStorylineState to GotoMission");
                        }

                        return true;
                    }

                    // Head back to the mission
                    _States.CurrentTravelerState = TravelerState.Idle;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                    return true;
                }

                return false;
            }

            return true;
        }

        public void ProcessState()
        {
            try
            {
                if (Logging.DebugCombatMissionBehavior) Logging.Log("public void ProcessState()");
                if (!CMBEveryPulse()) return;

                if (string.IsNullOrEmpty(Cache.Instance.CurrentAgent))
                {
                    if (Logging.DebugAgentInteractionReplyToAgent)
                        Logging.Log("if (string.IsNullOrEmpty(Cache.Instance.CurrentAgent)) return;");
                    return;
                }

                switch (_States.CurrentCombatMissionBehaviorState)
                {
                    case CombatMissionsBehaviorState.Idle:
                        IdleCMBState();
                        break;

                    case CombatMissionsBehaviorState.DelayedStart:
                        DelayedStartCMBState();
                        break;

                    case CombatMissionsBehaviorState.DelayedGotoBase:
                        DelayedGotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.Cleanup:
                        CleanupCMBState();
                        break;

                    case CombatMissionsBehaviorState.Start:
                        StartCMBState();
                        break;

                    case CombatMissionsBehaviorState.Switch:
                        SwitchCMBState();
                        break;

                    case CombatMissionsBehaviorState.Arm:
                        ArmCMBState();
                        break;

                    case CombatMissionsBehaviorState.LocalWatch:
                        LocalWatchCMBState();
                        break;

                    case CombatMissionsBehaviorState.WaitingforBadGuytoGoAway:
                        WaitingFoBadGuyToGoAway();
                        break;

                    case CombatMissionsBehaviorState.WarpOutStation:
                        WarpOutBookmarkCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoMission:
                        GotoMissionCMBState();
                        break;

                    case CombatMissionsBehaviorState.ExecuteMission:
                        ExecuteMissionCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoBase:
                        GotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.CompleteMission:
                        CompleteMissionCMBState();
                        break;

                    case CombatMissionsBehaviorState.Statistics:
                        StatisticsCMBState();
                        break;

                    case CombatMissionsBehaviorState.UnloadLoot:
                        UnloadLootCMBState();
                        break;

                    case CombatMissionsBehaviorState.CheckBookmarkAge:
                        if (Logging.DebugDisableCombatMissionsBehavior)
                            Logging.Log("Checking for any old bookmarks that may still need to be removed.");
                        if (!Cache.Instance.DeleteUselessSalvageBookmarks("RemoveOldBookmarks")) return;
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.BeginAfterMissionSalvaging;
                        Statistics.StartedSalvaging = DateTime.UtcNow;
                        break;

                    case CombatMissionsBehaviorState.BeginAfterMissionSalvaging:
                        BeginAftermissionSalvagingCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoSalvageBookmark:
                        SalvageGotoBookmarkCMBState();
                        break;

                    case CombatMissionsBehaviorState.Salvage:
                        SalvageCMBState();
                        break;

                    case CombatMissionsBehaviorState.SalvageUseGate:
                        SalageUseGateCMBState();
                        break;

                    case CombatMissionsBehaviorState.SalvageNextPocket:
                        SalvageNextPocketCMBState();
                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineSwitchAgents:


                        DirectAgent agent = null;
                        if (_storyline.StorylineMission != null)
                        {
                            if (_storyline.StorylineMission.AgentId != 0)
                            {
                                agent = Cache.Instance.DirectEve.GetAgentById(_storyline.StorylineMission.AgentId);
                            }
                        }

                        if (agent != null)
                        {
                            Cache.Instance.CurrentAgent = agent.Name;
                            Cache.Instance.CurrentStorylineAgentId = agent.AgentId;
                            Cache.Instance.AgentStationID = agent.StationId;
                            Cache.Instance.Agent = null;
                            var a = Cache.Instance.Agent; // Update agent relevant attributes. The agent related redundancy needs to be recuded a lot.

                            Logging.Log("new agent is " + Cache.Instance.CurrentAgent);

//							_States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.PrepareStorylineGotoBase; // why going to the storyline if the storyline.cs already travels on it's own?
                            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Storyline;
                        }
                        else
                        {
                            Logging.Log("Storyline agent  error.");
                            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                        }

                        break;

                    case CombatMissionsBehaviorState.PrepareStorylineGotoBase:
                        PrepareStorylineGotoBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.Storyline:
                        _storyline.ProcessState();
                        if (_States.CurrentStorylineState == StorylineState.Done)
                        {
                            Logging.Log("We have completed the storyline, returning to base");

                            Cache.Instance.CurrentAgent = null; // just set it to null. the default agent will be then retrieved again by settings.xml
                            var a = Cache.Instance.Agent;

                            if (a != null)
                            {
                                //AgentInteraction.AgentId = a.AgentId;
                            }
                            else
                            {
                                Logging.Log("Storyline agent  error: agent == null");
                                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                            }


                            _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.StorylineReturnToBase;
                            break;
                        }
                        break;

                    case CombatMissionsBehaviorState.StorylineReturnToBase:
                        StorylineReturnToBaseCMBState();
                        break;

                    case CombatMissionsBehaviorState.Traveler:
                        TravelerCMBState();
                        break;

                    case CombatMissionsBehaviorState.GotoNearestStation:
                        GotoNearestStationCMBState();
                        break;

                    case CombatMissionsBehaviorState.Default:
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                        break;
                }
            }
            catch (Exception ex)
            {
                Logging.Log("Exception [" + ex + "]");
            }
        }
    }
}