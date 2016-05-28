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
using System.Linq;
using DirectEve;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;

namespace Questor.Modules.Actions
{
    public abstract class TravelerDestination
    {
        internal static int _undockAttempts;
        internal static DateTime _nextTravelerDestinationAction;
        public long SolarSystemId { get; protected set; }

        /// <summary>
        ///     This function returns true if we are at the final destination and false if the task is not yet complete
        /// </summary>
        /// <returns></returns>
        public abstract bool PerformFinalDestinationTask();

        internal static void Undock()
        {
            if (Cache.Instance.InStation && !Cache.Instance.InSpace)
            {
                if (_undockAttempts + Cache.Instance.RandomNumber(0, 4) > 10)
                    //If we are having to retry at all there is likely something very wrong. Make it non-obvious if we do have to restart by restarting at diff intervals.
                {
                    Logging.Logging.Log("This is not the destination station, we have tried to undock [" + _undockAttempts +
                        "] times - and it is evidentially not working (lag?) - restarting Questor (and EVE)");
                    Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true; //this will perform a graceful restart
                    return;
                }

                if (DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(25))
                    //do not try to leave the station until you have been docked for at least 45seconds! (this gives some overhead to load the station env + session change timer)
                {
                    if (DateTime.UtcNow > Time.Instance.NextUndockAction)
                    {
                        Logging.Logging.Log("Exiting station");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        Time.Instance.LastDockAction = DateTime.UtcNow;
                        Time.Instance.LastSessionChange = DateTime.UtcNow;
                        _undockAttempts++;
                        Time.Instance.NextUndockAction =
                            DateTime.UtcNow.AddSeconds(Time.Instance.TravelerExitStationAmIInSpaceYet_seconds + Cache.Instance.RandomNumber(0, 20));
                        return;
                    }

                    if (Logging.Logging.DebugTraveler)
                        Logging.Logging.Log("LastInSpace is more than 45 sec old (we are docked), but NextUndockAction is still in the future [" +
                            Time.Instance.NextUndockAction.Subtract(DateTime.UtcNow).TotalSeconds + "seconds]");
                    return;
                }

                // We are not UnDocked yet
                return;
            }
        }

        internal static bool useInstaBookmark()
        {
            try
            {
                if (Cache.Instance.InWarp) return false;

                if (Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10))
                {
                    if ((Cache.Instance.ClosestStargate != null && Cache.Instance.ClosestStargate.IsOnGridWithMe) ||
                        (Cache.Instance.ClosestStation != null && Cache.Instance.ClosestStation.IsOnGridWithMe))
                    {
                        if (Cache.Instance.UndockBookmark != null)
                        {
                            if (Cache.Instance.UndockBookmark.LocationId == Cache.Instance.DirectEve.Session.LocationId)
                            {
                                var distance = Cache.Instance.DistanceFromMe(Cache.Instance.UndockBookmark.X ?? 0, Cache.Instance.UndockBookmark.Y ?? 0,
                                    Cache.Instance.UndockBookmark.Z ?? 0);
                                if (distance < (int) Distances.WarptoDistance)
                                {
                                    Logging.Logging.Log("Arrived at undock bookmark [" + Logging.Logging.Yellow + Cache.Instance.UndockBookmark.Title + Logging.Logging.Green +
                                        "]");
                                    Cache.Instance.UndockBookmark = null;
                                    return true;
                                }

                                if (distance >= (int) Distances.WarptoDistance)
                                {
                                    if (Cache.Instance.UndockBookmark.WarpTo())
                                    {
                                        Logging.Logging.Log("Warping to undock bookmark [" + Logging.Logging.Yellow + Cache.Instance.UndockBookmark.Title +
                                            Logging.Logging.Green + "][" + Math.Round((distance / 1000) / 149598000, 2) + " AU away]");
                                        //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
                                        _nextTravelerDestinationAction = DateTime.UtcNow.AddSeconds(10);
                                        return true;
                                    }

                                    return false;
                                }

                                return false;
                            }

                            if (Logging.Logging.DebugUndockBookmarks)
                                Logging.Logging.Log("Bookmark Named [" + Cache.Instance.UndockBookmark.Title +
                                    "] was somehow picked as an UndockBookmark but it is not in local with us! continuing without it.");
                            return true;
                        }

                        if (Logging.Logging.DebugUndockBookmarks)
                            Logging.Logging.Log("No undock bookmarks in local matching our undockPrefix [" + Settings.Instance.UndockBookmarkPrefix + "] continuing without it.");
                        return true;
                    }

                    if (Logging.Logging.DebugUndockBookmarks)
                        Logging.Logging.Log("Not currently on grid with a station or a stargate: continue traveling");
                    return true;
                }

                if (Logging.Logging.DebugUndockBookmarks)
                    Logging.Logging.Log("InSpace [" + Cache.Instance.InSpace + "]: waiting until we have been undocked or in system a few seconds");
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return false;
            }
        }
    }

    public class SolarSystemDestination : TravelerDestination
    {
        public SolarSystemDestination(long solarSystemId)
        {
            Logging.Logging.Log("Destination set to solar system id [" + solarSystemId + "]");
            SolarSystemId = solarSystemId;
        }

        public override bool PerformFinalDestinationTask()
        {
            // The destination is the solar system, not the station in the solar system.
            if (Cache.Instance.InStation && !Cache.Instance.InSpace)
            {
                if (_nextTravelerDestinationAction < DateTime.UtcNow)
                {
                    Undock();
                    return false;
                }

                // We are not there yet
                return false;
            }

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            _undockAttempts = 0;

            if (!useInstaBookmark()) return false;

            // The task was to get to the solar system, we're there :)
            Logging.Logging.Log("Arrived in system");
            Cache.Instance.MissionBookmarkTimerSet = false;
            return true;
        }
    }

    public class StationDestination : TravelerDestination
    {
        public StationDestination(long stationId)
        {
            var station = Cache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (station == null || !station.ItemId.HasValue || !station.SolarSystemId.HasValue)
            {
                Logging.Logging.Log("Invalid station id [" + Logging.Logging.Yellow + StationId + Logging.Logging.Green + "]");
                SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                StationId = -1;
                StationName = "";
                return;
            }

            Logging.Logging.Log("Destination set to [" + Logging.Logging.Yellow + station.Name + Logging.Logging.Green + "]");
            StationId = stationId;
            StationName = station.Name;
            SolarSystemId = station.SolarSystemId.Value;
        }

        public StationDestination(long solarSystemId, long stationId, string stationName)
        {
            Logging.Logging.Log("Destination set to [" + Logging.Logging.Yellow + stationName + Logging.Logging.Green + "]");
            SolarSystemId = solarSystemId;
            StationId = stationId;
            StationName = stationName;
        }

        public long StationId { get; set; }

        public string StationName { get; set; }

        public override bool PerformFinalDestinationTask()
        {
            var arrived = PerformFinalDestinationTask(StationId, StationName);
            return arrived;
        }

        internal static bool PerformFinalDestinationTask(long stationId, string stationName)
        {
            if (Cache.Instance.InStation && Cache.Instance.DirectEve.Session.StationId == stationId)
            {
                Logging.Logging.Log("Arrived in station");
                return true;
            }

            if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10))
            {
                // We are in a station, but not the correct station!
                if (DateTime.UtcNow > Time.Instance.NextUndockAction)
                {
                    Undock();
                    return false;
                }

                // We are not there yet
                return false;
            }

            if ((DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10)) && !Cache.Instance.InSpace)
            {
                // We are not in station and not in space?  Wait for a bit
                return false;
            }

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            _undockAttempts = 0;

            if (!useInstaBookmark()) return false;

            //else Logging.Log("TravelerDestination.BookmarkDestination","undock bookmark missing: " + Cache.Instance.DirectEve.GetLocationName((long)Cache.Instance.DirectEve.Session.StationId) + " and " + Settings.Instance.UndockPrefix + " did not both exist in a bookmark");

            if (Cache.Instance.Stations == null)
            {
                // We are there but no stations? Wait a bit
                return false;
            }

            var station = Cache.Instance.EntitiesByName(stationName, Cache.Instance.Stations).FirstOrDefault();
            if (station == null)
            {
                // We are there but no station? Wait a bit
                return false;
            }

            if (station.Distance < (int) Distances.DockingRange)
            {
                if (station.Dock())
                {
                    Logging.Logging.Log("Dock at [" + Logging.Logging.Yellow + station.Name + Logging.Logging.Green + "] which is [" + Math.Round(station.Distance / 1000, 0) +
                        "k away]");
                    return false; //we do not return true until we actually appear in the destination (station in this case)
                }

                return false;
            }

            if (station.Distance < (int) Distances.WarptoDistance)
            {
                if (station.Approach())
                {
                    Logging.Logging.Log("Approaching [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                }

                return false;
            }

            if (station.WarpTo())
            {
                Logging.Logging.Log("Warp to and dock at [" + Logging.Logging.Yellow + station.Name + Logging.Logging.Green + "][" +
                    Math.Round((station.Distance / 1000) / 149598000, 2) + " AU away]");
                return false;
            }

            return false;
        }
    }

    public class BookmarkDestination : TravelerDestination
    {
        public BookmarkDestination(DirectBookmark bookmark)
        {
            if (bookmark == null)
            {
                Logging.Logging.Log("Invalid bookmark destination!");

                SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                BookmarkId = -1;
                return;
            }

            Logging.Logging.Log("Destination set to bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "]");
            BookmarkId = bookmark.BookmarkId ?? -1;
            SolarSystemId = bookmark.LocationId ?? -1;
        }

        public BookmarkDestination(long bookmarkId)
            : this(Cache.Instance.BookmarkById(bookmarkId))
        {
        }

        public long BookmarkId { get; set; }

        public override bool PerformFinalDestinationTask()
        {
            var bookmark = Cache.Instance.BookmarkById(BookmarkId);
            var arrived = PerformFinalDestinationTask(bookmark, 150000);

            return arrived;
        }

        internal static bool PerformFinalDestinationTask(DirectBookmark bookmark, int warpDistance)
        {
            // The bookmark no longer exists, assume we are not there
            if (bookmark == null)
                return false;

            // Is this a station bookmark?
            if (bookmark.Entity != null && bookmark.Entity.GroupId == (int) Group.Station)
            {
                var arrived = StationDestination.PerformFinalDestinationTask(bookmark.Entity.Id, bookmark.Entity.Name);
                if (arrived)
                {
                    Logging.Logging.Log("Arrived at bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "]");
                }

                return arrived;
            }

            if (Cache.Instance.InStation)
            {
                // We have arrived
                if (bookmark.ItemId.HasValue && bookmark.ItemId == Cache.Instance.DirectEve.Session.StationId)
                    return true;

                // We are in a station, but not the correct station!
                if (DateTime.UtcNow > Time.Instance.NextUndockAction)
                {
                    Undock();
                    return false;
                }

                return false;
            }

            if (!Cache.Instance.InSpace)
            {
                // We are not in space and not in a station, wait a bit
                return false;
            }

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            _undockAttempts = 0;

            if (Cache.Instance.UndockBookmark != null)
            {
                var distanceToUndockBookmark = Cache.Instance.DistanceFromMe(bookmark.X ?? 0, bookmark.Y ?? 0, bookmark.Z ?? 0);
                if (distanceToUndockBookmark < (int) Distances.WarptoDistance)
                {
                    Logging.Logging.Log("Arrived at undock bookmark [" + Logging.Logging.Yellow + Cache.Instance.UndockBookmark.Title + Logging.Logging.Green + "]");
                    Cache.Instance.UndockBookmark = null;
                }
                else
                {
                    if (Cache.Instance.UndockBookmark.WarpTo())
                    {
                        Logging.Logging.Log("Warping to undock bookmark [" + Logging.Logging.Yellow + Cache.Instance.UndockBookmark.Title + Logging.Logging.Green + "][" +
                            Logging.Logging.Yellow + Math.Round((distanceToUndockBookmark / 1000) / 149598000, 2) + Logging.Logging.Green + " AU away]");
                        _nextTravelerDestinationAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerInWarpedNextCommandDelay_seconds);
                        //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
                        return false;
                    }
                }
            }

            // This bookmark has no x / y / z, assume we are there.
            if (bookmark.X == -1 || bookmark.Y == -1 || bookmark.Z == -1)
            {
                Logging.Logging.Log("Arrived at the bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "][No XYZ]");
                return true;
            }

            var distance = Cache.Instance.DistanceFromMe(bookmark.X ?? 0, bookmark.Y ?? 0, bookmark.Z ?? 0);
            if (distance < warpDistance)
            {
                Logging.Logging.Log("Arrived at the bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "]");
                return true;
            }

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            if (Math.Round((distance/1000)) < (int) Distances.MaxPocketsDistanceKm && Cache.Instance.AccelerationGates.Count() != 0)
            {
                Logging.Logging.Log("Warp to bookmark in same pocket requested but acceleration gate found delaying.");
                return true;
            }

            Defense.DoNotBreakInvul = false;
            var nameOfBookmark = "";
            if (Settings.Instance.EveServerName == "Tranquility") nameOfBookmark = "Encounter";
            if (Settings.Instance.EveServerName == "Serenity") nameOfBookmark = "遭遇战";
            if (nameOfBookmark == "") nameOfBookmark = "Encounter";
            //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
            if (MissionSettings.MissionWarpAtDistanceRange != 0 && bookmark.Title.Contains(nameOfBookmark))
            {
                if (bookmark.WarpTo(MissionSettings.MissionWarpAtDistanceRange*1000))
                {
                    Logging.Logging.Log("Warping to bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "][" + Logging.Logging.Yellow + " At " +
                        MissionSettings.MissionWarpAtDistanceRange + Logging.Logging.Green + " km]");
                    return true;
                }
            }
            else
            {
                if (bookmark.WarpTo())
                {
                    Logging.Logging.Log("Warping to bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "][" + Logging.Logging.Yellow +
                        Math.Round((distance / 1000) / 149598000, 2) + Logging.Logging.Green + " AU away]");
                    return true;
                }
            }

            return false;
        }
    }

    public class MissionBookmarkDestination : TravelerDestination
    {
        public MissionBookmarkDestination(DirectAgentMissionBookmark bookmark)
        {
            if (bookmark == null)
            {
                if (DateTime.UtcNow > Time.Instance.MissionBookmarkTimeout.AddMinutes(2))
                {
                    Logging.Logging.Log("MissionBookmarkTimeout [ " + Time.Instance.MissionBookmarkTimeout.ToShortTimeString() +
                        " ] did not get reset from last usage: resetting it now");
                    Time.Instance.MissionBookmarkTimeout = DateTime.UtcNow.AddYears(1);
                }

                if (!Cache.Instance.MissionBookmarkTimerSet)
                {
                    Cache.Instance.MissionBookmarkTimerSet = true;
                    Time.Instance.MissionBookmarkTimeout = DateTime.UtcNow.AddSeconds(10);
                }

                if (DateTime.UtcNow > Time.Instance.MissionBookmarkTimeout) //if CurrentTime is after the TimeOut value, freak out
                {
                    AgentId = -1;
                    Title = null;
                    SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    Cache.Instance.CloseQuestorCMDLogoff = false;
                    Cache.Instance.CloseQuestorCMDExitGame = true;
                    Cleanup.ReasonToStopQuestor = "TravelerDestination.MissionBookmarkDestination: Invalid mission bookmark! - Lag?! Closing EVE";
                    Logging.Logging.Log(Cleanup.ReasonToStopQuestor);
                    Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                }
                else
                {
                    Logging.Logging.Log("Invalid Mission Bookmark! retrying for another [ " +
                        Math.Round(Time.Instance.MissionBookmarkTimeout.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " ]sec");
                }
            }

            if (bookmark != null)
            {
                Logging.Logging.Log("Destination set to mission bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.Green + "]");
                AgentId = bookmark.AgentId ?? -1;
                Title = bookmark.Title;
                SolarSystemId = bookmark.SolarSystemId ?? -1;
                Cache.Instance.MissionBookmarkTimerSet = false;
            }
        }

        public MissionBookmarkDestination(int agentId, string title)
            : this(GetMissionBookmark(agentId, title))
        {
        }

        public long AgentId { get; set; }

        public string Title { get; set; }

        private static DirectAgentMissionBookmark GetMissionBookmark(long agentId, string title)
        {
            var mission = Cache.Instance.GetAgentMission(agentId, true);
            if (mission == null)
                return null;

            return mission.Bookmarks.FirstOrDefault(b => b.Title.ToLower() == title.ToLower());
        }

        public override bool PerformFinalDestinationTask()
        {
            var arrived = BookmarkDestination.PerformFinalDestinationTask(GetMissionBookmark(AgentId, Title), (int) Distances.MissionWarpLimit);
            return arrived; // Mission bookmarks have a 1.000.000 distance warp-to limit (changed it to 150.000.000 as there are some bugged missions around)
        }
    }
}