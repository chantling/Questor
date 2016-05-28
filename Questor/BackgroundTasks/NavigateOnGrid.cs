using System;
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;

namespace Questor.Modules.BackgroundTasks
{
    public static class NavigateOnGrid
    {
        public static DateTime AvoidBumpingThingsTimeStamp = Time.Instance.StartTime;
        public static int SafeDistanceFromStructureMultiplier = 1;
        public static bool AvoidBumpingThingsWarningSent = false;
        public static DateTime NextNavigateIntoRange = DateTime.UtcNow;
        private static int? _orbitDistance;
        public static bool AvoidBumpingThingsBool { get; set; }
        public static bool SpeedTank { get; set; }

        public static int OrbitDistance
        {
            get
            {
                if (MissionSettings.MissionOrbitDistance != null)
                {
                    return (int) MissionSettings.MissionOrbitDistance;
                }

                return _orbitDistance ?? 2000;
            }
            set { _orbitDistance = value; }
        }

        public static bool OrbitStructure { get; set; }
        private static int? _optimalRange { get; set; }

        public static int OptimalRange
        {
            get
            {
                if (MissionSettings.MissionOptimalRange != null)
                {
                    return (int) MissionSettings.MissionOptimalRange;
                }

                return _optimalRange ?? 10000;
            }
            set { _optimalRange = value; }
        }

        public static bool AvoidBumpingThings(EntityCache thisBigObject, string module)
        {
            if (AvoidBumpingThingsBool)
            {
                //if It has not been at least 60 seconds since we last session changed do not do anything
                if (Cache.Instance.InStation || !Cache.Instance.InSpace || Cache.Instance.ActiveShip.Entity.IsCloaked ||
                    (Cache.Instance.InSpace && Time.Instance.LastSessionChange.AddSeconds(60) < DateTime.UtcNow))
                    return false;

                //we cant move in bastion mode, do not try
                List<ModuleCache> bastionModules = null;
                bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                if (bastionModules.Any(i => i.IsActive)) return false;


                if (Cache.Instance.ClosestStargate != null && Cache.Instance.ClosestStargate.Distance < 9000)
                {
                    //
                    // if we are 'close' to a stargate or a station do not attempt to do any collision avoidance, as its unnecessary that close to a station or gate!
                    //
                    return false;
                }

                if (Cache.Instance.ClosestStation != null && Cache.Instance.ClosestStation.Distance < 11000)
                {
                    //
                    // if we are 'close' to a stargate or a station do not attempt to do any collision avoidance, as its unnecessary that close to a station or gate!
                    //
                    return false;
                }

                //EntityCache thisBigObject = Cache.Instance.BigObjects.FirstOrDefault();
                if (thisBigObject != null)
                {
                    //
                    // if we are "too close" to the bigObject move away... (is orbit the best thing to do here?)
                    //
                    if (thisBigObject.Distance >= (int) Distances.TooCloseToStructure)
                    {
                        //we are no longer "too close" and can proceed.
                        AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                        SafeDistanceFromStructureMultiplier = 1;
                        AvoidBumpingThingsWarningSent = false;
                    }
                    else
                    {
                        if (DateTime.UtcNow > Time.Instance.NextOrbit)
                        {
                            if (DateTime.UtcNow > AvoidBumpingThingsTimeStamp.AddSeconds(30))
                            {
                                if (SafeDistanceFromStructureMultiplier <= 4)
                                {
                                    //
                                    // for simplicities sake we reset this timestamp every 30 sec until the multiplier hits 5 then it should stay static until we are not "too close" anymore
                                    //
                                    AvoidBumpingThingsTimeStamp = DateTime.UtcNow;
                                    SafeDistanceFromStructureMultiplier++;
                                }

                                if (DateTime.UtcNow > AvoidBumpingThingsTimeStamp.AddMinutes(5) && !AvoidBumpingThingsWarningSent)
                                {
                                    Logging.Logging.Log("NavigateOnGrid", "We are stuck on a object and have been trying to orbit away from it for over 5 min",
                                        Logging.Logging.Orange);
                                    AvoidBumpingThingsWarningSent = true;
                                }

                                if (DateTime.UtcNow > AvoidBumpingThingsTimeStamp.AddMinutes(15))
                                {
                                    Cache.Instance.CloseQuestorCMDLogoff = false;
                                    Cache.Instance.CloseQuestorCMDExitGame = true;
                                    Cleanup.ReasonToStopQuestor = "navigateOnGrid: We have been stuck on an object for over 15 min";
                                    Logging.Logging.Log("ReasonToStopQuestor", Cleanup.ReasonToStopQuestor, Logging.Logging.Yellow);
                                    Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                                }
                            }

                            if (thisBigObject.Orbit((int) Distances.SafeDistancefromStructure*SafeDistanceFromStructureMultiplier))
                            {
                                Logging.Logging.Log(module,
                                    ": initiating Orbit of [" + thisBigObject.Name + "] orbiting at [" +
                                    ((int) Distances.SafeDistancefromStructure*SafeDistanceFromStructureMultiplier) + "]", Logging.Logging.White);
                                return true;
                            }

                            return false;
                        }

                        return false;
                        //we are still too close, do not continue through the rest until we are not "too close" anymore
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        public static void OrbitGateorTarget(EntityCache target, string module)
        {
            var OrbitDistanceToUse = OrbitDistance;
            if (!Combat.Combat.PotentialCombatTargets.Any())
            {
                OrbitDistanceToUse = 500;
            }

            if (DateTime.UtcNow > Time.Instance.NextOrbit)
            {
                //we cant move in bastion mode, do not try
                var bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                if (bastionModules.Any(i => i.IsActive)) return;

                if (Logging.Logging.DebugNavigateOnGrid) Logging.Logging.Log("NavigateOnGrid", "OrbitGateorTarget Started", Logging.Logging.White);
                if (OrbitDistanceToUse == 0)
                {
                    OrbitDistanceToUse = 2000;
                }

                if (target.Distance + OrbitDistanceToUse < Combat.Combat.MaxRange - 5000)
                {
                    if (Logging.Logging.DebugNavigateOnGrid)
                        Logging.Logging.Log("NavigateOnGrid", "if (target.Distance + Cache.Instance.OrbitDistance < Combat.MaxRange - 5000)",
                            Logging.Logging.White);

                    //Logging.Log("CombatMissionCtrl." + _pocketActions[_currentAction] ,"StartOrbiting: Target in range");
                    if (!Cache.Instance.IsApproachingOrOrbiting(target.Id))
                    {
                        if (Logging.Logging.DebugNavigateOnGrid)
                            Logging.Logging.Log("CombatMissionCtrl.NavigateIntoRange", "We are not approaching nor orbiting", Logging.Logging.Teal);

                        //
                        // Prefer to orbit the last structure defined in
                        // Cache.Instance.OrbitEntityNamed
                        //
                        EntityCache structure = null;
                        if (!string.IsNullOrEmpty(Cache.Instance.OrbitEntityNamed))
                        {
                            structure =
                                Cache.Instance.EntitiesOnGrid.Where(i => i.Name.Contains(Cache.Instance.OrbitEntityNamed))
                                    .OrderBy(t => t.Distance)
                                    .FirstOrDefault();
                        }

                        if (structure == null)
                        {
                            structure = Cache.Instance.EntitiesOnGrid.Where(i => i.Name.Contains("Gate")).OrderBy(t => t.Distance).FirstOrDefault();
                        }

                        if (OrbitStructure && structure != null)
                        {
                            if (structure.Orbit(OrbitDistanceToUse))
                            {
                                Logging.Logging.Log(module,
                                    "Initiating Orbit [" + structure.Name + "][at " + Math.Round((double) OrbitDistanceToUse/1000, 0) + "k][" +
                                    structure.MaskedId + "]", Logging.Logging.Teal);
                                return;
                            }

                            return;
                        }

                        //
                        // OrbitStructure is false
                        //
                        if (SpeedTank)
                        {
                            if (Combat.Combat.DoWeCurrentlyHaveTurretsMounted() && target.IsFrigate)
                            {
                                Logging.Logging.Log(module, "Target is a frigate and we have turrets mounted, using three times the usual orbit distance.",
                                    Logging.Logging.Teal);
                                OrbitDistanceToUse = OrbitDistanceToUse*3;
                                return;
                            }

                            if (target.Orbit(OrbitDistanceToUse))
                            {
                                Logging.Logging.Log(module,
                                    "Initiating Orbit [" + target.Name + "][at " + Math.Round((double) OrbitDistanceToUse/1000, 0) + "k][ID: " + target.MaskedId +
                                    "]", Logging.Logging.Teal);
                                return;
                            }

                            return;
                        }

                        //
                        // OrbitStructure is false
                        // SpeedTank is false
                        //
                        if (Cache.Instance.MyShipEntity.Velocity < 300) //this will spam a bit until we know what "mode" our active ship is when aligning
                        {
                            if (Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                            {
                                if (Cache.Instance.Star.AlignTo())
                                {
                                    Logging.Logging.Log(module,
                                        "Aligning to the Star so we might possibly hit [" + target.Name + "][ID: " + target.MaskedId +
                                        "][ActiveShip.Entity.Mode:[" + Cache.Instance.ActiveShip.Entity.Mode + "]", Logging.Logging.Teal);
                                    return;
                                }

                                return;
                            }
                        }
                    }
                }
                else
                {
                    if (target.Orbit(OrbitDistanceToUse))
                    {
                        Logging.Logging.Log(module, "Out of range. ignoring orbit around structure.", Logging.Logging.Teal);
                        return;
                    }

                    return;
                }

                return;
            }
        }

        public static void NavigateIntoRange(EntityCache target, string module, bool moveMyShip)
        {
            if (!Cache.Instance.InSpace || (Cache.Instance.InSpace && Cache.Instance.InWarp) || !moveMyShip)
                return;

            if (DateTime.UtcNow < NextNavigateIntoRange || Logging.Logging.DebugDisableNavigateIntoRange)
                return;

            NextNavigateIntoRange = DateTime.UtcNow.AddSeconds(5);

            //we cant move in bastion mode, do not try
            List<ModuleCache> bastionModules = null;
            bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
            if (bastionModules.Any(i => i.IsActive)) return;

            if (Logging.Logging.DebugNavigateOnGrid) Logging.Logging.Log("NavigateOnGrid", "NavigateIntoRange Started", Logging.Logging.White);

            //if (Cache.Instance.OrbitDistance != 0)
            //    Logging.Log("CombatMissionCtrl", "Orbit Distance is set to: " + (Cache.Instance.OrbitDistance / 1000).ToString(CultureInfo.InvariantCulture) + "k", Logging.teal);

            AvoidBumpingThings(Cache.Instance.BigObjectsandGates.FirstOrDefault(), "NavigateOnGrid: NavigateIntoRange");

            if (SpeedTank)
            {
                if (target.Distance > Combat.Combat.MaxRange && !Cache.Instance.IsApproaching(target.Id))
                {
                    if (target.KeepAtRange((int) (Combat.Combat.MaxRange*0.8d)))
                    {
                        if (Logging.Logging.DebugNavigateOnGrid)
                            Logging.Logging.Log("NavigateOnGrid", "NavigateIntoRange: SpeedTank: Moving into weapons range before initiating orbit",
                                Logging.Logging.Teal);
                    }

                    return;
                }

                if (target.Distance < Combat.Combat.MaxRange && !Cache.Instance.IsOrbiting(target.Id))
                {
                    if (Logging.Logging.DebugNavigateOnGrid)
                        Logging.Logging.Log("NavigateOnGrid", "NavigateIntoRange: SpeedTank: orbitdistance is [" + OrbitDistance + "]", Logging.Logging.White);
                    OrbitGateorTarget(target, module);
                    return;
                }

                return;
            }
            else
            //if we are not speed tanking then check optimalrange setting, if that is not set use the less of targeting range and weapons range to dictate engagement range
            {
                if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                {
                    //if optimalrange is set - use it to determine engagement range
                    if (OptimalRange != 0)
                    {
                        if (Logging.Logging.DebugNavigateOnGrid)
                            Logging.Logging.Log("NavigateOnGrid",
                                "NavigateIntoRange: OptimalRange [ " + OptimalRange + "] Current Distance to [" + target.Name + "] is [" +
                                Math.Round(target.Distance/1000, 0) + "]", Logging.Logging.White);

                        if (target.Distance > OptimalRange + (int) Distances.OptimalRangeCushion)
                        {
                            if ((Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id) || Cache.Instance.MyShipEntity.Velocity < 50)
                            {
                                if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                                {
                                    if (Logging.Logging.DebugNavigateOnGrid)
                                        Logging.Logging.Log("NavigateOnGrid",
                                            "NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + Math.Round(target.Distance/1000, 0) + "]",
                                            Logging.Logging.White);
                                    OrbitGateorTarget(target, module);
                                    return;
                                }

                                if (target.KeepAtRange(OptimalRange))
                                {
                                    Logging.Logging.Log(module,
                                        "Using Optimal Range: Approaching target [" + target.Name + "][ID: " + target.MaskedId + "][" +
                                        Math.Round(target.Distance/1000, 0) + "k away]", Logging.Logging.Teal);
                                }

                                return;
                            }
                        }

                        if (target.Distance <= OptimalRange)
                        {
                            if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                            {
                                if ((Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id) ||
                                    Cache.Instance.MyShipEntity.Velocity < 50)
                                {
                                    if (target.KeepAtRange(OptimalRange))
                                    {
                                        Logging.Logging.Log(module, "Target is NPC Frigate and we got Turrets. Keeping target at Range to hit it.",
                                            Logging.Logging.Teal);
                                        Logging.Logging.Log(module,
                                            "Initiating KeepAtRange [" + target.Name + "][at " + Math.Round((double) OptimalRange/1000, 0) + "k][ID: " +
                                            target.MaskedId + "]", Logging.Logging.Teal);
                                    }
                                    return;
                                }
                            }
                            else if (Cache.Instance.Approaching != null && Cache.Instance.MyShipEntity.Velocity != 0)
                            {
                                if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted()) return;

                                StopMyShip();
                                Logging.Logging.Log(module,
                                    "Using Optimal Range: Stop ship, target at [" + Math.Round(target.Distance/1000, 0) + "k away] is inside optimal",
                                    Logging.Logging.Teal);
                                return;
                            }
                        }
                    }
                    else //if optimalrange is not set use MaxRange (shorter of weapons range and targeting range)
                    {
                        if (Logging.Logging.DebugNavigateOnGrid)
                            Logging.Logging.Log("NavigateOnGrid",
                                "NavigateIntoRange: using MaxRange [" + Combat.Combat.MaxRange + "] target is [" + target.Name + "][" + target.Distance + "]",
                                Logging.Logging.White);

                        if (target.Distance > Combat.Combat.MaxRange)
                        {
                            if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                            {
                                if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                                {
                                    if (Logging.Logging.DebugNavigateOnGrid)
                                        Logging.Logging.Log("NavigateOnGrid",
                                            "NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + target.Distance + "]", Logging.Logging.White);
                                    OrbitGateorTarget(target, module);
                                    return;
                                }

                                if (target.KeepAtRange((int) (Combat.Combat.MaxRange*0.8d)))
                                {
                                    Logging.Logging.Log(module,
                                        "Using Weapons Range * 0.8d [" + Math.Round(Combat.Combat.MaxRange*0.8d/1000, 0) + " k]: Approaching target [" +
                                        target.Name + "][ID: " + target.MaskedId + "][" + Math.Round(target.Distance/1000, 0) + "k away]", Logging.Logging.Teal);
                                }

                                return;
                            }
                        }

                        //I think when approach distance will be reached ship will be stopped so this is not needed
                        if (target.Distance <= Combat.Combat.MaxRange - 5000 && Cache.Instance.Approaching != null)
                        {
                            if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                            {
                                if (Logging.Logging.DebugNavigateOnGrid)
                                    Logging.Logging.Log("NavigateOnGrid",
                                        "NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + target.Distance + "]", Logging.Logging.White);
                                OrbitGateorTarget(target, module);
                                return;
                            }
                            if (Cache.Instance.MyShipEntity.Velocity != 0) StopMyShip();
                            Logging.Logging.Log(module, "Using Weapons Range: Stop ship, target is more than 5k inside weapons range", Logging.Logging.Teal);
                            return;
                        }

                        if (target.Distance <= Combat.Combat.MaxRange && Cache.Instance.Approaching == null)
                        {
                            if (target.IsNPCFrigate && Combat.Combat.DoWeCurrentlyHaveTurretsMounted())
                            {
                                if (Logging.Logging.DebugNavigateOnGrid)
                                    Logging.Logging.Log("NavigateOnGrid",
                                        "NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + target.Distance + "]", Logging.Logging.White);
                                OrbitGateorTarget(target, module);
                                return;
                            }
                        }
                    }
                    return;
                }
            }
        }

        public static void StopMyShip()
        {
            if (DateTime.UtcNow > Time.Instance.NextApproachAction)
            {
                Time.Instance.NextApproachAction = DateTime.UtcNow.AddSeconds(Time.Instance.ApproachDelay_seconds);
                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                Cache.Instance.Approaching = null;
            }
        }

        public static bool NavigateToTarget(EntityCache target, string module, bool orbit, int DistanceFromTarget)
            //this needs to accept a distance parameter....
        {
            // if we are inside warpto range you need to approach (you cant warp from here)
            if (target.Distance < (int) Distances.WarptoDistance)
            {
                if (orbit)
                {
                    if (target.Distance < DistanceFromTarget)
                    {
                        return true;
                    }

                    if (DateTime.UtcNow > Time.Instance.NextOrbit)
                    {
                        //we cant move in bastion mode, do not try
                        List<ModuleCache> bastionModules = null;
                        bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                        if (bastionModules.Any(i => i.IsActive)) return false;

                        Logging.Logging.Log(module, "StartOrbiting: Target in range", Logging.Logging.Teal);
                        if (!Cache.Instance.IsApproachingOrOrbiting(target.Id))
                        {
                            Logging.Logging.Log("CombatMissionCtrl.NavigateToObject", "We are not approaching nor orbiting", Logging.Logging.Teal);
                            if (target.Orbit(DistanceFromTarget - 1500))
                            {
                                Logging.Logging.Log(module, "Initiating Orbit [" + target.Name + "][ID: " + target.MaskedId + "]", Logging.Logging.Teal);
                                return false;
                            }

                            return false;
                        }
                    }
                }
                else
                //if we are not speed tanking then check optimalrange setting, if that is not set use the less of targeting range and weapons range to dictate engagement range
                {
                    if (DateTime.UtcNow > Time.Instance.NextApproachAction)
                    {
                        if (target.Distance < DistanceFromTarget)
                        {
                            return true;
                        }

                        if (Cache.Instance.Approaching == null || Cache.Instance.Approaching.Id != target.Id || Cache.Instance.MyShipEntity.Velocity < 50)
                        {
                            if (target.KeepAtRange(DistanceFromTarget - 1500))
                            {
                                Logging.Logging.Log(module,
                                    "Using SafeDistanceFromStructure: Approaching target [" + target.Name + "][ID: " + target.MaskedId + "][" +
                                    Math.Round(target.Distance/1000, 0) + "k away]", Logging.Logging.Teal);
                            }

                            return false;
                        }

                        return false;
                    }

                    return false;
                }
            }
            // Probably never happens
            if (target.AlignTo())
            {
                return false;
            }

            return false;
        }
    }
}