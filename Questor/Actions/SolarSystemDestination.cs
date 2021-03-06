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
    public class SolarSystemDestination2 : TravelerDestination
    {
        private DateTime _nextAction;

        public SolarSystemDestination2(long solarSystemId)
        {
            Logging.Logging.Log("Destination set to solar system id [" + solarSystemId + "]");
            SolarSystemId = solarSystemId;
        }

        public override bool PerformFinalDestinationTask()
        {
            // The destination is the solar system, not the station in the solar system.
            if (Cache.Instance.DirectEve.Session.IsInStation)
            {
                if (_nextAction < DateTime.UtcNow)
                {
                    if (DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(45))
                        //do not try to leave the station until you have been docked for at least 45seconds! (this gives some overhead to load the station env + session change timer)
                    {
                        Logging.Logging.Log("Exiting station");
                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                        Time.Instance.LastDockAction = DateTime.UtcNow;
                        _nextAction = DateTime.UtcNow.AddSeconds(30);
                    }
                }

                // We are not there yet
                return false;
            }

            // The task was to get to the solar system, we are there :)
            Logging.Logging.Log("Arrived in system");
            return true;
        }
    }
}