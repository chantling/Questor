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
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;

namespace Questor.Modules.Actions
{
    public class BookmarkDestination2 : TravelerDestination
    {
        private DateTime _nextAction;

        public BookmarkDestination2(DirectBookmark bookmark)
        {
            if (bookmark == null)
            {
                Logging.Logging.Log("Invalid bookmark destination!");

                SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                BookmarkId = -1;
                return;
            }

            Logging.Logging.Log("Destination set to bookmark [" + bookmark.Title + "]");
            var location = GetBookmarkLocation(bookmark);
            if (location == null)
            {
                Logging.Logging.Log("Invalid bookmark destination!");

                SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                BookmarkId = -1;
                return;
            }

            BookmarkId = bookmark.BookmarkId ?? -1;
            SolarSystemId = location.SolarSystemId ?? Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
        }

        public BookmarkDestination2(long bookmarkId)
            : this(Cache.Instance.BookmarkById(bookmarkId))
        {
        }

        public long BookmarkId { get; set; }

        private static DirectLocation GetBookmarkLocation(DirectBookmark bookmark)
        {
            var location = Cache.Instance.DirectEve.Navigation.GetLocation(bookmark.ItemId ?? -1);
            if (!location.IsValid)
                location = Cache.Instance.DirectEve.Navigation.GetLocation(bookmark.LocationId ?? -1);
            if (!location.IsValid)
                return null;

            return location;
        }

        public override bool PerformFinalDestinationTask()
        {
            var bookmark = Cache.Instance.BookmarkById(BookmarkId);
            return PerformFinalDestinationTask2(bookmark, 150000, ref _nextAction);
        }

        internal static bool PerformFinalDestinationTask2(DirectBookmark bookmark, int warpDistance, ref DateTime nextAction)
        {
            // The bookmark no longer exists, assume we are there
            if (bookmark == null)
                return true;

            var location = GetBookmarkLocation(bookmark);
            if (Cache.Instance.DirectEve.Session.IsInStation)
            {
                // We have arrived
                if (location != null && location.ItemId == Cache.Instance.DirectEve.Session.StationId)
                    return true;

                if (DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(45))
                    //do not try to leave the station until you have been docked for at least 45seconds! (this gives some overhead to load the station env + session change timer)
                {
                    // We are apparently in a station that is incorrect
                    Logging.Logging.Log("We're docked in the wrong station, undocking");

                    Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                    Time.Instance.LastDockAction = DateTime.UtcNow;
                    nextAction = DateTime.UtcNow.AddSeconds(30);
                    return false;
                }

                return false;
            }

            // Is this a station bookmark?
            if (bookmark.Entity != null && bookmark.Entity.GroupId == (int) Group.Station)
            {
                var arrived = StationDestination2.PerformFinalDestinationTask(bookmark.Entity.Id, bookmark.Entity.Name, ref nextAction);
                if (arrived)
                    Logging.Logging.Log("Arrived at bookmark [" + bookmark.Title + "]");
                return arrived;
            }

            // Its not a station bookmark, make sure we are in space
            if (Cache.Instance.DirectEve.Session.IsInStation)
            {
                // We are in a station, but not the correct station!
                if (nextAction < DateTime.UtcNow)
                {
                    if (DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(45))
                        //do not try to leave the station until you have been docked for at least 45seconds! (this gives some overhead to load the station env + session change timer)
                    {
                        Logging.Logging.Log("We're docked but our destination is in space, undocking");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        Time.Instance.LastDockAction = DateTime.UtcNow;
                        nextAction = DateTime.UtcNow.AddSeconds(30);
                    }
                }

                // We are not there yet
                return false;
            }

            if (!Cache.Instance.DirectEve.Session.IsInSpace)
            {
                // We are not in space and not in a station, wait a bit
                return false;
            }

            // This bookmark has no x / y / z, assume we are there.
            if (bookmark.X == -1 || bookmark.Y == -1 || bookmark.Z == -1)
            {
                Logging.Logging.Log("Arrived at the bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.White + "][No XYZ]");
                return true;
            }

            var distance = Cache.Instance.DistanceFromMe(bookmark.X ?? 0, bookmark.Y ?? 0, bookmark.Z ?? 0);
            if (distance < warpDistance)
            {
                Logging.Logging.Log("Arrived at the bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.White + "]");
                return true;
            }

            if (nextAction > DateTime.UtcNow)
                return false;

            if (Cache.Instance.GateInGrid() && (distance/1000) < (int) Distances.MaxPocketsDistanceKm)
            {
                Logging.Logging.Log("Bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.White + "][" + Logging.Logging.Yellow +
                    Math.Round((distance / 1000) / 149598000, 2) + Logging.Logging.White + "] AU away. Which is [" + Logging.Logging.Yellow +
                    Math.Round((distance / 1000), 2) + Logging.Logging.White + "].");
            }

            if (bookmark.WarpTo())
            {
                Logging.Logging.Log("Warping to bookmark [" + Logging.Logging.Yellow + bookmark.Title + Logging.Logging.White + "][" + Math.Round((distance / 1000) / 149598000, 2) +
                    "] AU away. Which is [" + Math.Round((distance / 1000), 2) + "]");
                nextAction = DateTime.UtcNow.AddSeconds(30);
                Time.Instance.NextWarpAction = DateTime.UtcNow.AddSeconds(5);
                return false;
            }

            return false;
        }
    }
}