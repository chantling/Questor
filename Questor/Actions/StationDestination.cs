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
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;

namespace Questor.Modules.Actions
{
    public class StationDestination2 : TravelerDestination
    {
        private DateTime _nextStationAction;

        public StationDestination2(long stationId)
        {
            var station = Cache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (station == null || !station.ItemId.HasValue || !station.SolarSystemId.HasValue)
            {
                Logging.Logging.Log("Invalid station id [" + stationId + "]");

                SolarSystemId = Cache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                StationId = -1;
                StationName = "";
                return;
            }

            Logging.Logging.Log("Destination set to [" + station.Name + "]");

            StationId = stationId;
            StationName = station.Name;
            SolarSystemId = station.SolarSystemId.Value;
            //Logging.Log(station.SolarSystemId.Value + " " + stationId + " " + station.Name);
        }

        public StationDestination2(long solarSystemId, long stationId, string stationName)
        {
            Logging.Logging.Log("Destination set to [" + stationName + "]");
            //Logging.Log(solarSystemId + " " + stationId + " " + stationName);

            SolarSystemId = solarSystemId;
            StationId = stationId;
            StationName = stationName;
        }

        public long StationId { get; set; }

        public string StationName { get; set; }

        public override bool PerformFinalDestinationTask()
        {
            return PerformFinalDestinationTask(StationId, StationName, ref _nextStationAction);
        }

        internal static bool PerformFinalDestinationTask(long stationId, string stationName, ref DateTime nextAction)
        {
            if (Cache.Instance.InStation && Cache.Instance.DirectEve.Session.StationId == stationId)
            {
                Logging.Logging.Log("Arrived in station");
                return true;
            }

            if (Cache.Instance.InStation)
            {
                // We are in a station, but not the correct station!
                if (Time.Instance.NextUndockAction < DateTime.UtcNow)
                {
                    if (DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(45))
                        //do not try to leave the station until you have been docked for at least 45seconds! (this gives some overhead to load the station env + session change timer)
                    {
                        Logging.Logging.Log("We're docked in the wrong station, undocking");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        Time.Instance.LastDockAction = DateTime.UtcNow;
                        Time.Instance.NextUndockAction = DateTime.UtcNow.AddSeconds(Time.Instance.TravelerExitStationAmIInSpaceYet_seconds);
                        return false;
                    }
                }

                // We are not there yet
                return false;
            }

            if (!Cache.Instance.InSpace)
            {
                // We are not in station and not in space?  Wait for a bit
                return false;
            }

            if (nextAction > DateTime.UtcNow)
                return false;

            var station = Cache.Instance.EntityByName(stationName);
            if (station == null)
            {
                // We are there but no station? Wait a bit
                return false;
            }

            if (station.Distance < (int) Distances.DockingRange)
            {
                if (station.Dock())
                {
                    Logging.Logging.Log("Dock at [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                    nextAction = DateTime.UtcNow.AddSeconds(30);
                }

                return false;
            }

            if (station.Distance < (int) Distances.WarptoDistance)
            {
                if (station.Approach())
                {
                    Logging.Logging.Log("Approaching [" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                    nextAction = DateTime.UtcNow.AddSeconds(30);
                }

                return false;
            }

            if (station.WarpTo())
            {
                Logging.Logging.Log("Warp to and dock at [" + station.Name + "]");
                nextAction = DateTime.UtcNow.AddSeconds(30);
                return false;
            }


            return false;
        }
    }
}