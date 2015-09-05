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
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.Activities;
using Questor.Modules.States;
using Questor.Modules.Actions;
using Questor.Modules.BackgroundTasks;

namespace Questor.Behaviors
{
    public class DebugBehavior
    {

        public bool PanicStateReset; //false;


        public DebugBehavior()
        {

            //
            // this is combat mission specific and needs to be generalized
            //
            _States.CurrentDebugBehaviorState = DebugBehaviorState.Idle;
            _States.CurrentArmState = ArmState.Idle;
            _States.CurrentDroneState = DroneState.Idle;
            _States.CurrentUnloadLootState = UnloadLootState.Idle;
            _States.CurrentTravelerState = TravelerState.Idle;
        }

       

        private void BeginClosingQuestor()
        {
            Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
            _States.CurrentQuestorState = QuestorState.CloseQuestor;
        }
        
        #region Old Scanner Functions

        private void OpenDirectionalScanner()
        {
            //_state = TestState.Idle;

            DirectScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner == null)
            {
                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenScanner);
            }
        }

        private void IsDirectionalScannerReady()
        {
            //_state = TestState.Idle;
            DirectScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null && scanner.IsReady)
            {
                Logging.Log("IsScannerReady", "scanner: [" + scanner + "]", Logging.Debug);
            }
            else
            {
                Logging.Log("IsScannerReady", "scanner is not yet ready", Logging.Debug);
            }
        }

        private void SelectDirectionalScanTabInUI()
        {
            //_state = TestState.Idle;
            DirectScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null && scanner.IsReady)
            {
                if (scanner.GetSelectedIdx() != 1)
                {
                    scanner.SelectByIdx(1); // select [h]dscan[/h] tab
                }
            }
            else
            {
                Logging.Log("IsScannerReady", "scanner is not yet ready", Logging.Debug);
            }
        }

        private void ScanRangeTest()
        {
            //_state = TestState.Idle;
            DirectScannerWindow scanner = Cache.Instance.DirectEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null && scanner.IsReady)
            {
                scanner.Range = scanner.Range / 2;
            }
        }

        private void DoDirectionalScan()
        {
            //_state = TestState.Idle;
            var scanner = Cache.Instance.DirectEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null && scanner.IsReady)
            {
                scanner.DirectionSearch();
            }
        }

        private void DumpDirectionalScanResults()
        {
            //_state = TestState.Idle;
            var scanner = Cache.Instance.DirectEve.Windows.OfType<DirectScannerWindow>().FirstOrDefault();
            if (scanner != null && scanner.IsReady)
            {
                foreach (DirectDirectionalScanResult result in scanner.DirectionalScanResults)
                {
                    var entity = result.Entity;
                    if (entity != null && entity.IsValid)
                    {
                        Logging.Log("DumpScanResults", "SR: name [" + result.Name + "] TypeName [" + result.TypeName + "] Distance [" + Math.Round(entity.Distance / 1000, 2) + "k]", Logging.Debug);
                    }
                    else
                    {
                        Logging.Log("DumpScanResults", "SR: name [" + result.Name + "] TypeName [" + result.TypeName + "]", Logging.Debug);
                    }
                }
            }
        }

        #endregion Old Scanner Functions

        public void ProcessState()
        {
            // Invalid settings, quit while we're ahead
            
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //this local is safe check is useless as their is no LocalWatch processstate running every tick...
            //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            //If local unsafe go to base and do not start mission again
            if (Settings.Instance.FinishWhenNotSafe && (_States.CurrentDebugBehaviorState != DebugBehaviorState.GotoNearestStation /*|| State!=QuestorState.GotoBase*/))
            {
                //need to remove spam
                if (Cache.Instance.InSpace && !Cache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad))
                {
                    EntityCache station = null;
                    if (Cache.Instance.Stations != null && Cache.Instance.Stations.Any())
                    {
                        station = Cache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();
                    }

                    if (station != null)
                    {
                        Logging.Log("Local not safe", "Station found. Going to nearest station", Logging.White);
                        _States.CurrentDebugBehaviorState = DebugBehaviorState.GotoNearestStation;
                    }
                    else
                    {
                        Logging.Log("Local not safe", "Station not found. Going back to base", Logging.White);
                        _States.CurrentDebugBehaviorState = DebugBehaviorState.GotoBase;
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
                _States.CurrentDebugBehaviorState = DebugBehaviorState.GotoBase;
            }
            if ((DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalSeconds > 10) && (DateTime.UtcNow.Subtract(Time.Instance.QuestorStarted_DateTime).TotalSeconds < 60))
            {
                if (Cache.Instance.QuestorJustStarted)
                {
                    Cache.Instance.QuestorJustStarted = false;
                    Cleanup.SessionState = "Starting Up";

                    // write session log
                    Statistics.WriteSessionLogStarting();
                }
            }

            //
            // Panic always runs, not just in space
            //
            Panic.ProcessState();
            if (_States.CurrentPanicState == PanicState.Panic || _States.CurrentPanicState == PanicState.Panicking)
            {
                // If Panic is in panic state, questor is in panic States.CurrentDebugBehaviorState :)
                _States.CurrentDebugBehaviorState = DebugBehaviorState.Panic;

                if (PanicStateReset)
                {
                    _States.CurrentPanicState = PanicState.Normal;
                    PanicStateReset = false;
                }
            }
            else if (_States.CurrentPanicState == PanicState.Resume)
            {
                // Reset panic state
                _States.CurrentPanicState = PanicState.Normal;

                // Sit Idle and wait for orders.
                _States.CurrentTravelerState = TravelerState.Idle;
                _States.CurrentDebugBehaviorState = DebugBehaviorState.Idle;
            }

            //Logging.Log("test");
            switch (_States.CurrentDebugBehaviorState)
            {
                case DebugBehaviorState.Traveler:
                    Salvage.OpenWrecks = false;
                    List<int> destination = Cache.Instance.DirectEve.Navigation.GetDestinationPath();
                    if (destination == null || destination.Count == 0)
                    {
                        // happens if autopilot is not set and this QuestorState is chosen manually
                        // this also happens when we get to destination (!?)
                        Logging.Log("DebugBehavior.Traveler", "No destination?", Logging.White);
                        _States.CurrentDebugBehaviorState = DebugBehaviorState.Error;
                        return;
                    }

                    if (destination.Count == 1 && destination.FirstOrDefault() == 0)
                    {
                        destination[0] = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                    }
                    if (Traveler.Destination == null || Traveler.Destination.SolarSystemId != destination.Last())
                    {
                        IEnumerable<DirectBookmark> bookmarks = Cache.Instance.AllBookmarks.Where(b => b.LocationId == destination.Last()).ToList();
                        if (bookmarks != null && bookmarks.Any())
                            Traveler.Destination = new BookmarkDestination(bookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault());
                        else
                        {
                            Logging.Log("DebugBehavior.Traveler", "Destination: [" + Cache.Instance.DirectEve.Navigation.GetLocation(destination.Last()).Name + "]", Logging.White);
                            Traveler.Destination = new SolarSystemDestination(destination.Last());
                        }
                    }
                    else
                    {
                    	
                    	                       //we also assume you are connected during a manual set of questor into travel mode (safe assumption considering someone is at the kb)
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                        Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
                        
                        Traveler.ProcessState();

 

                        if (_States.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                            {
                                Logging.Log("DebugBehavior.Traveler", "an error has occurred", Logging.White);
                                _States.CurrentDebugBehaviorState = DebugBehaviorState.Error;
                                return;
                            }
                            
                            if (Cache.Instance.InSpace)
                            {
                                Logging.Log("DebugBehavior.Traveler", "Arrived at destination (in space, Questor stopped)", Logging.White);
                                _States.CurrentDebugBehaviorState = DebugBehaviorState.Error;
                                return;
                            }
                            
                            Logging.Log("DebugBehavior.Traveler", "Arrived at destination", Logging.White);
                            _States.CurrentDebugBehaviorState = DebugBehaviorState.Idle;
                            return;
                        }
                    }

                    break;

                case DebugBehaviorState.LogCombatTargets:
                    //combat targets
                    //List<EntityCache> combatentitiesInList =  Cache.Instance.Entities.Where(t => t.IsNpc && !t.IsBadIdea && t.CategoryId == (int)CategoryID.Entity && !t.IsContainer && t.Distance < Combat.MaxRange && !Cache.Instance.IgnoreTargets.Contains(t.Name.Trim())).ToList();
                    List<EntityCache> combatentitiesInList = Cache.Instance.EntitiesOnGrid.Where(t => t.IsNpc && !t.IsBadIdea && t.CategoryId == (int)CategoryID.Entity && !t.IsContainer).ToList();
                    Statistics.EntityStatistics(combatentitiesInList);
                    Cache.Instance.Paused = true;
                    break;

                case DebugBehaviorState.LogDroneTargets:
                    //drone targets
                    List<EntityCache> droneentitiesInList = Cache.Instance.EntitiesOnGrid.Where(e => e.IsNpc && !e.IsBadIdea && e.CategoryId == (int)CategoryID.Entity && !e.IsContainer && !e.IsSentry && !e.IsLargeCollidable).ToList();
                    Statistics.EntityStatistics(droneentitiesInList);
                    Cache.Instance.Paused = true;
                    break;

                case DebugBehaviorState.LogStationEntities:
                    //stations
                    List<EntityCache> stationsInList = Cache.Instance.Entities.Where(e => !e.IsSentry && e.GroupId == (int)Group.Station).ToList();
                    Statistics.EntityStatistics(stationsInList);
                    Cache.Instance.Paused = true;
                    break;

                case DebugBehaviorState.LogStargateEntities:
                    //stargates
                    List<EntityCache> stargatesInList = Cache.Instance.Entities.Where(e => !e.IsSentry && e.GroupId == (int)Group.Stargate).ToList();
                    Statistics.EntityStatistics(stargatesInList);
                    Cache.Instance.Paused = true;
                    break;

                case DebugBehaviorState.LogAsteroidBelts:
                    //Asteroid Belts
                    List<EntityCache> asteroidbeltsInList = Cache.Instance.Entities.Where(e => !e.IsSentry && e.GroupId == (int)Group.AsteroidBelt).ToList();
                    Statistics.EntityStatistics(asteroidbeltsInList);
                    Cache.Instance.Paused = true;
                    break;

                case DebugBehaviorState.LogCansAndWrecks:
                    //Asteroid Belts
                    List<EntityCache> cansandWrecksInList = Cache.Instance.EntitiesOnGrid.Where(e => !e.IsSentry && e.GroupId == (int)Group.CargoContainer && e.GroupId == (int)Group.Wreck).ToList();
                    Statistics.EntityStatistics(cansandWrecksInList);
                    Cache.Instance.Paused = true;
                    break;

                case DebugBehaviorState.Default:
                    _States.CurrentDebugBehaviorState = DebugBehaviorState.Idle;
                    break;
            }
        }
    }
}