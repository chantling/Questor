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
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Activities
{
    public static class Traveler
    {
        private static TravelerDestination _destination;
        private static DateTime _lastTravelerPulse;
        private static DateTime _nextGetLocation;

        private static List<int> _destinationRoute;
        public static DirectLocation _location;
        private static IEnumerable<DirectBookmark> myHomeBookmarks;
        private static string _locationName;
        private static int _locationErrors;

        static Traveler()
        {
            _lastTravelerPulse = DateTime.MinValue;
        }

        public static TravelerDestination Destination
        {
            get { return _destination; }
            set
            {
                _destination = value;
                _States.CurrentTravelerState = _destination == null ? TravelerState.AtDestination : TravelerState.Idle;
            }
        }

        /// <summary>
        ///     Set destination to a solar system
        /// </summary>
        public static bool SetStationDestination(long stationId)
        {
            _location = Cache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (Logging.Logging.DebugTraveler)
                Logging.Logging.Log("Location = [" + Logging.Logging.Yellow + Cache.Instance.DirectEve.Navigation.GetLocationName(stationId) + Logging.Logging.Green + "]");
            if (_location != null && _location.IsValid)
            {
                _locationErrors = 0;
                if (Logging.Logging.DebugTraveler)
                    Logging.Logging.Log("Setting destination to [" + Logging.Logging.Yellow + _location.Name + Logging.Logging.Green + "]");
                try
                {
                    _location.SetDestination();
                }
                catch (Exception)
                {
                    Logging.Logging.Log("SetStationDestination: set destination to [" + _location.ToString() + "] failed ");
                }
                return true;
            }

            Logging.Logging.Log("Error setting station destination [" + Logging.Logging.Yellow + stationId + Logging.Logging.Green + "]");
            _locationErrors++;
            if (_locationErrors > 20)
            {
                return false;
            }
            return false;
        }

        /// <summary>
        ///     Navigate to a solar system
        /// </summary>
        /// <param name="solarSystemId"></param>
        private static void NavigateToBookmarkSystem(long solarSystemId)
        {
            if (Time.Instance.NextTravelerAction > DateTime.UtcNow)
            {
                if (Logging.Logging.DebugTraveler)
                    Logging.Logging.Log("NavigateToBookmarkSystem: will continue in [ " + Math.Round(Time.Instance.NextTravelerAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                        " ]sec");
                return;
            }

            if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(7))
            {
                if (Logging.Logging.DebugTraveler)
                    Logging.Logging.Log("NavigateToBookmarkSystem: We just session changed less than 7 sec go, wait.");
                return;
            }

            Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(2);
            if (Logging.Logging.DebugTraveler)
                Logging.Logging.Log("NavigateToBookmarkSystem - Iterating- next iteration should be in no less than [1] second ");

            _destinationRoute = null;
            _destinationRoute = Cache.Instance.DirectEve.Navigation.GetDestinationPath();

            if (_destinationRoute == null || _destinationRoute.Count == 0 || _destinationRoute.All(d => d != solarSystemId))
            {
                if (_destinationRoute != null || (_destinationRoute != null && _destinationRoute.Count == 0))
                    Logging.Logging.Log("NavigateToBookmarkSystem: We have no destination");
                if (_destinationRoute != null || (_destinationRoute != null && _destinationRoute.All(d => d != solarSystemId)))
                    Logging.Logging.Log("NavigateToBookmarkSystem: the destination is not currently set to solarsystemId [" + solarSystemId + "]");

                // We do not have the destination set
                if (DateTime.UtcNow > _nextGetLocation || _location == null)
                {
                    Logging.Logging.Log("NavigateToBookmarkSystem: getting Location of solarSystemId [" + solarSystemId + "]");
                    _nextGetLocation = DateTime.UtcNow.AddSeconds(10);
                    _location = Cache.Instance.DirectEve.Navigation.GetLocation(solarSystemId);
                    Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(2);
                    return;
                }

                if (_location != null && _location.IsValid)
                {
                    _locationErrors = 0;
                    Logging.Logging.Log("Setting destination to [" + Logging.Logging.Yellow + _location.Name + Logging.Logging.Green + "]");
                    try
                    {
                        _location.SetDestination();
                    }
                    catch (Exception)
                    {
                        Logging.Logging.Log("NavigateToBookmarkSystem: set destination to [" + _location.ToString() + "] failed ");
                    }

                    Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                Logging.Logging.Log("NavigateToBookmarkSystem: Error setting solar system destination [" + Logging.Logging.Yellow + solarSystemId + Logging.Logging.Green + "]");
                _locationErrors++;
                if (_locationErrors > 20)
                {
                    _States.CurrentTravelerState = TravelerState.Error;
                    return;
                }

                return;
            }

            _locationErrors = 0;
            if (!Cache.Instance.InSpace)
            {
                if (Cache.Instance.InStation)
                {
                    if (DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(25))
                        //do not try to leave the station until you have been docked for at least 45seconds! (this gives some overhead to load the station env + session change timer)
                    {
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        Time.Instance.LastDockAction = DateTime.UtcNow;
                        Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerExitStationAmIInSpaceYet_seconds);
                    }
                }

                Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(2, 3));

                // We are not yet in space, wait for it
                return;
            }

            // We are apparently not really in space yet...
            if (Cache.Instance.ActiveShip == null || Cache.Instance.ActiveShip.Entity == null)
                return;

            //if (Logging.DebugTraveler) Logging.Log("Traveler", "Destination is set: processing...", Logging.Teal);

            // Find the first waypoint
            var waypoint = _destinationRoute.FirstOrDefault();

            //if (Logging.DebugTraveler) Logging.Log("Traveler", "NavigateToBookmarkSystem: getting next way-points locationName", Logging.Teal);
            _locationName = Cache.Instance.DirectEve.Navigation.GetLocationName(waypoint);
            if (Logging.Logging.DebugTraveler)
                Logging.Logging.Log("NavigateToBookmarkSystem: Next Waypoint is: [" + _locationName + "]");

            if (waypoint > 60000000) // this MUST be a station
            {
                //insert code to handle station destinations here
            }

            if (waypoint < 60000000) // this is not a station, probably a system
            {
                //useful?a
            }

            var solarSystemInRoute = Cache.Instance.DirectEve.SolarSystems[waypoint];

            if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
            {
                if (solarSystemInRoute != null && solarSystemInRoute.Security < 0.45 &&
                    (Cache.Instance.ActiveShip.GroupId != (int) Group.Shuttle || Cache.Instance.ActiveShip.GroupId != (int) Group.Frigate ||
                     Cache.Instance.ActiveShip.GroupId != (int) Group.Interceptor || Cache.Instance.ActiveShip.GroupId != (int) Group.TransportShip ||
                     Cache.Instance.ActiveShip.GroupId != (int) Group.ForceReconShip || Cache.Instance.ActiveShip.GroupId != (int) Group.StealthBomber))
                {
                    Logging.Logging.Log("NavigateToBookmarkSystem: Next Waypoint is: [" + _locationName +
                        "] which is LOW SEC! This should never happen. Turning off AutoStart and going home.");
                    Settings.Instance.AutoStart = false;
                    if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    }
                    return;
                }
            }

            // Find the stargate associated with it

            if (!Cache.Instance.Stargates.Any())
            {
                // not found, that cant be true?!?!?!?!
                Logging.Logging.Log("Error [" + Logging.Logging.Yellow + _locationName + Logging.Logging.Green + "] not found, most likely lag waiting [" +
                    Time.Instance.TravelerNoStargatesFoundRetryDelay_seconds + "] seconds.");
                Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerNoStargatesFoundRetryDelay_seconds);
                return;
            }

            // Warp to, approach or jump the stargate
            var MyNextStargate = Cache.Instance.StargateByName(_locationName);
            if (MyNextStargate != null)
            {
                if ((MyNextStargate.Distance < (int) Distances.DecloakRange && !Cache.Instance.ActiveShip.Entity.IsCloaked) ||
                    (MyNextStargate.Distance < (int) Distances.JumpRange && Cache.Instance.Modules.Any(i => i.GroupId != (int) Group.CloakingDevice)))
                {
                    if (MyNextStargate.Jump())
                    {
                        Logging.Logging.Log("Jumping to [" + Logging.Logging.Yellow + _locationName + Logging.Logging.Green + "]");
                        return;
                    }

                    return;
                }

                if (MyNextStargate.Distance < (int) Distances.WarptoDistance && MyNextStargate.Distance != 0)
                {
                    if (DateTime.UtcNow > Time.Instance.NextApproachAction && !Cache.Instance.IsApproaching(MyNextStargate.Id))
                    {
                        if (Logging.Logging.DebugTraveler)
                            Logging.Logging.Log("NavigateToBookmarkSystem: approaching the stargate named [" + MyNextStargate.Name + "]");
                        MyNextStargate.Approach();
                            //you could use a negative approach distance here but ultimately that is a bad idea.. Id like to go toward the entity without approaching it so we would end up inside the docking ring (eventually)
                        return;
                    }

                    if (Logging.Logging.DebugTraveler)
                        Logging.Logging.Log("NavigateToBookmarkSystem: we are already approaching the stargate named [" + MyNextStargate.Name + "]");
                    return;
                }

                if (Cache.Instance.InSpace && !Combat.Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
                {
                    if (MyNextStargate.WarpTo())
                    {
                        Logging.Logging.Log("Warping to [" + Logging.Logging.Yellow + _locationName + Logging.Logging.Green + "][" + Logging.Logging.Yellow +
                            Math.Round((MyNextStargate.Distance / 1000) / 149598000, 2) + Logging.Logging.Green + " AU away]");
                        return;
                    }

                    return;
                }
            }

            Time.Instance.NextTravelerAction = DateTime.UtcNow.AddSeconds(Time.Instance.WarptoDelay_seconds);
                //this should probably use a different Time definition, but this works for now. (5 seconds)
            if (!Combat.Combat.ReloadAll(Cache.Instance.MyShipEntity)) return;
            return;
        }

        public static void TravelHome(string module)
        {
            //only call bookmark stuff if UseHomebookmark is true
            if (Settings.Instance.UseHomebookmark)
            {
                // if we can't travel to bookmark, travel to agent's station
                if (!TravelToBookmarkName(Settings.Instance.HomeBookmarkName, module))
                {
                    TravelToAgentsStation(module);
                }

                return;
            }

            TravelToAgentsStation(module);
        }

//		private static int agentRetrievalCnt =  0;
        public static void TravelToAgentsStation(string module)
        {
            // if we can't warp because we are scrambled, prevent next actions
            if (!_defendOnTravel(module))
                return;

//			if(_destination == null) {
//				agentRetrievalCnt++;
//				if(agentRetrievalCnt <= 5 && (Cache.Instance.Agent == null || !Cache.Instance.Agent.IsValid))
//					return;
//			}
//			
//			agentRetrievalCnt = 0;
            var destinationId = (Cache.Instance.Agent != null && Cache.Instance.Agent.IsValid) || Cache.Instance.AgentStationID > 0
                ? Cache.Instance.AgentStationID
                : MissionSettings.ListOfAgents.FirstOrDefault().HomeStationId;


            if (Logging.Logging.DebugGotobase)
                Logging.Logging.Log("TravelToAgentsStation:      Cache.Instance.AgentStationId [" + Cache.Instance.AgentStationID + "]");
            if (Logging.Logging.DebugGotobase)
                Logging.Logging.Log("TravelToAgentsStation:  Cache.Instance.AgentSolarSystemId [" + Cache.Instance.AgentSolarSystemID + "]");

            if (_destination == null || (((StationDestination) _destination) != null && ((StationDestination) _destination).StationId != destinationId))
            {
                Logging.Logging.Log("StationDestination: [" + destinationId + "]");
                _destination = new StationDestination(destinationId);

                _States.CurrentTravelerState = TravelerState.Idle;
                return;
            }

            if (Logging.Logging.DebugGotobase)
                if (Destination != null)
                    Logging.Logging.Log("TravelToAgentsStation: Traveler.Destination.SolarSystemId [" + Destination.SolarSystemId + "]");
            _processAtDestinationActions(module);
            ProcessState();


            return;
        }

        public static bool TravelToBookmarkName(string bookmarkName, string module)
        {
            var travel = false;

            myHomeBookmarks = Cache.Instance.BookmarksByLabel(bookmarkName).ToList();

            if (myHomeBookmarks.Any())
            {
                var oldestHomeBookmark = myHomeBookmarks.OrderBy(b => b.CreatedOn).FirstOrDefault();
                if (oldestHomeBookmark != null && oldestHomeBookmark.LocationId != null)
                {
                    TravelToBookmark(oldestHomeBookmark, module);
                    travel = true;
                }
            }
            else
            {
                Logging.Logging.Log("bookmark not found! We were Looking for bookmark starting with [" + bookmarkName + "] found none.");
            }

            return travel;
        }

        public static void TravelToBookmark(DirectBookmark bookmark, string module)
        {
            // if we can't warp because we are scrambled, prevent next actions
            if (!_defendOnTravel(module))
                return;

            if (Logging.Logging.DebugGotobase) Logging.Logging.Log("TravelToBookmark:      bookmark [" + bookmark.Title + "]");

            if (_destination == null)
            {
                Logging.Logging.Log("Destination: bookmark[" + bookmark.Description + "]");

                _destination = new BookmarkDestination(bookmark);
                _States.CurrentTravelerState = TravelerState.Idle;
                return;
            }

            if (Logging.Logging.DebugGotobase)
                if (Destination != null)
                    Logging.Logging.Log("TravelToAgentsStation: Traveler.Destination.SolarSystemId [" + Destination.SolarSystemId + "]");
            _processAtDestinationActions(module);
            ProcessState();


            return;
        }

        public static void ProcessState()
        {
            // Only pulse state changes every 1.5s
            if (DateTime.UtcNow.Subtract(_lastTravelerPulse).TotalMilliseconds < 1000) //default: 1000ms
                return;

            _lastTravelerPulse = DateTime.UtcNow;

            switch (_States.CurrentTravelerState)
            {
                case TravelerState.Idle:
                    _States.CurrentTravelerState = TravelerState.Traveling;
                    break;

                case TravelerState.Traveling:
                    if ((!Cache.Instance.InSpace && !Cache.Instance.InStation) || Cache.Instance.InWarp)
                        //if we are in warp, do nothing, as nothing can actually be done until we are out of warp anyway.
                        return;

                    if (Destination == null)
                    {
                        _States.CurrentTravelerState = TravelerState.Error;
                        break;
                    }

                    if (Destination.SolarSystemId != Cache.Instance.DirectEve.Session.SolarSystemId)
                    {
                        //Logging.Log("traveler: NavigateToBookmarkSystem(Destination.SolarSystemId);");
                        NavigateToBookmarkSystem(Destination.SolarSystemId);
                    }
                    else if (Destination.PerformFinalDestinationTask())
                    {
                        _destinationRoute = null;
                        _location = null;
                        _locationName = string.Empty;
                        _locationErrors = 0;

                        //Logging.Log("traveler: _States.CurrentTravelerState = TravelerState.AtDestination;");
                        _States.CurrentTravelerState = TravelerState.AtDestination;
                    }
                    break;

                case TravelerState.AtDestination:

                    //do nothing when at destination
                    //Traveler sits in AtDestination when it has nothing to do, NOT in idle.
                    break;

                default:
                    break;
            }
        }

        private static bool _defendOnTravel(string module)
        {
            var canWarp = true;
            //
            // defending yourself is more important that the traveling part... so it comes first.
            //
            if (Cache.Instance.InSpace && Settings.Instance.DefendWhileTraveling)
            {
                if (!Cache.Instance.ActiveShip.Entity.IsCloaked || (Time.Instance.LastSessionChange.AddSeconds(60) > DateTime.UtcNow))
                {
                    if (Logging.Logging.DebugGotobase) Logging.Logging.Log("Travel: _combat.ProcessState()");

                    try
                    {
                        Combat.Combat.ProcessState();
                    }
                    catch (Exception exception)
                    {
                        Logging.Logging.Log("Exception [" + exception + "]");
                    }

                    if (!Combat.Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
                    {
                        if (Logging.Logging.DebugGotobase) Logging.Logging.Log("Travel: we are not scrambled - pulling drones.");
                        Drones.IsMissionPocketDone = true; //tells drones.cs that we can pull drones

                        //Logging.Log("CombatmissionBehavior","TravelToAgentStation: not pointed",Logging.White);
                    }
                    else if (Combat.Combat.TargetedBy.Any(t => t.IsWarpScramblingMe))
                    {
                        Drones.IsMissionPocketDone = false;
                        if (Logging.Logging.DebugGotobase) Logging.Logging.Log("Travel: we are scrambled");
                        Drones.ProcessState();

                        canWarp = false;
                    }
                }
            }


            if (NavigateOnGrid.SpeedTank && !Settings.Instance.LootWhileSpeedTanking)
            {
                if (Logging.Logging.DebugTargetWrecks) Logging.Logging.Log("Salvage.OpenWrecks = false;");
                Salvage.OpenWrecks = false;
            }

            return canWarp;
        }

        private static void _processAtDestinationActions(string module)
        {
            if (!Cache.Instance.UpdateMyWalletBalance()) return;

            if (_States.CurrentTravelerState == TravelerState.AtDestination)
            {
                if (_States.CurrentCombatMissionCtrlState == CombatMissionCtrlState.Error)
                {
                    Logging.Logging.Log("an error has occurred");
                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Traveler)
                    {
                        _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    }

                    return;
                }

                if (Cache.Instance.InSpace)
                {
                    Logging.Logging.Log("Arrived at destination (in space, Questor stopped)");
                    Cache.Instance.Paused = true;
                    return;
                }

                if (Logging.Logging.DebugTraveler) Logging.Logging.Log("Arrived at destination");
                if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Traveler)
                {
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                    _lastTravelerPulse = DateTime.UtcNow;
                    return;
                }

                return;
            }
        }
    }
}