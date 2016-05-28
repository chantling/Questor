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
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Combat
{
    /// <summary>
    ///     The drones class will manage any and all drone related combat
    /// </summary>
    /// <remarks>
    ///     Drones will always work their way from lowest value target to highest value target and will only attack entities
    ///     (not structures)
    /// </remarks>
    public static class Drones
    {
        //~Drones()
        //{
        //    Interlocked.Decrement(ref DronesInstances);
        //}

        private static int _lastDroneCount;
        //private static DateTime _lastEngageCommand;
        private static DateTime _lastRecallCommand;

        private static int _recallCount;
        private static DateTime _lastLaunch;
        private static DateTime _lastRecall;

        private static DateTime _launchTimeout;
        private static int _launchTries;
        private static double _activeDronesShieldTotalOnLastPulse;
        private static double _activeDronesArmorTotalOnLastPulse;
        private static double _activeDronesStructureTotalOnLastPulse;
        private static double _activeDronesShieldPercentageOnLastPulse;
        private static double _activeDronesArmorPercentageOnLastPulse;
        private static double _activeDronesStructurePercentageOnLastPulse;
        public static bool WarpScrambled; //false
        private static DateTime _nextDroneAction = DateTime.UtcNow;
        private static DateTime _nextWarpScrambledWarning = DateTime.MinValue;
        private static IEnumerable<EntityCache> _activeDrones; //cleared every frame in Cache.InvalidateCache()

        private static int _droneTypeID; //only cleared by reloading settings

        private static bool _useDrones; //only cleared by reloading settings

        private static DateTime LastDroneFightCmd = DateTime.MinValue;
        private static DateTime LastDroneTargetSwapped = DateTime.MinValue;

        private static bool _dronesKillHighValueTargets;

        private static double? _maxDroneRange;

        /// <summary>
        ///     Drone target chosen by GetBest Target
        /// </summary>
        public static long? PreferredDroneTargetID;

        private static EntityCache _preferredDroneTarget; //cleared every frame in Cache.InvalidateCache()

        private static List<PriorityTarget> _dronePriorityTargets;

        private static IEnumerable<EntityCache> _dronePriorityEntities;

        private static DirectContainer _droneBay;
        //public static int DronesInstances;

        static Drones()
        {
            //Interlocked.Increment(ref DronesInstances);
        }

        public static long? LastTargetIDDronesEngaged { get; set; }

        public static int GetShipsDroneBayAttempts { get; set; }
        public static bool AddWarpScramblersToDronePriorityTargetList { get; set; }
        public static bool AddWebifiersToDronePriorityTargetList { get; set; }
        public static bool AddDampenersToDronePriorityTargetList { get; set; }
        public static bool AddNeutralizersToDronePriorityTargetList { get; set; }
        public static bool AddTargetPaintersToDronePriorityTargetList { get; set; }
        public static bool AddECMsToDroneTargetList { get; set; }
        public static bool AddTrackingDisruptorsToDronePriorityTargetList { get; set; }

        public static IEnumerable<EntityCache> ActiveDrones
        {
            get
            {
                if (_activeDrones == null)
                {
                    if (Cache.Instance.DirectEve.ActiveDrones.Any())
                    {
                        _activeDrones = Cache.Instance.DirectEve.ActiveDrones.Select(d => new EntityCache(d)).ToList();
                        return _activeDrones;
                    }

                    return new List<EntityCache>();
                }

                return _activeDrones;
            }
        }

        public static int DroneTypeID
        {
            get
            {
                if (MissionSettings.PocketDroneTypeID != null)
                {
                    return (int) MissionSettings.PocketDroneTypeID;
                }

                if (MissionSettings.MissionDroneTypeID != null)
                {
                    return (int) MissionSettings.MissionDroneTypeID;
                }

                if (MissionSettings.FactionDroneTypeID != null)
                {
                    return (int) MissionSettings.FactionDroneTypeID;
                }

                return _droneTypeID;
            }
            set { _droneTypeID = value; }
        }

        public static int BuyAmmoDroneAmmount { get; set; }

        public static int FactionDroneTypeID { get; set; }

        public static bool UseDrones
        {
            get
            {
                if (MissionSettings.PocketUseDrones != null)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("We are using PocketDrones setting [" + MissionSettings.PocketUseDrones + "]");
                    return (bool) MissionSettings.PocketUseDrones;
                }

                if (MissionSettings.MissionUseDrones != null)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("We are using MissionDrones setting [" + MissionSettings.PocketUseDrones + "]");
                    return (bool) MissionSettings.MissionUseDrones;
                }

                return _useDrones;
            }
            set { _useDrones = value; }
        }

        public static int DroneControlRange { get; set; }
        public static bool DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive { get; set; }
        public static int DroneMinimumShieldPct { get; set; }
        public static int DroneMinimumArmorPct { get; set; }
        public static int DroneMinimumCapacitorPct { get; set; }
        public static int DroneRecallShieldPct { get; set; }
        public static int DroneRecallArmorPct { get; set; }
        public static int DroneRecallCapacitorPct { get; set; }
        public static int BelowThisHealthLevelRemoveFromDroneBay { get; set; }
        public static int LongRangeDroneRecallShieldPct { get; set; }
        public static int LongRangeDroneRecallArmorPct { get; set; }
        public static int LongRangeDroneRecallCapacitorPct { get; set; }

        public static bool DronesKillHighValueTargets
        {
            get
            {
                if (MissionSettings.MissionDronesKillHighValueTargets != null)
                {
                    return (bool) MissionSettings.MissionDronesKillHighValueTargets;
                }

                return _dronesKillHighValueTargets;
            }
            set { _dronesKillHighValueTargets = value; }
        }

        /// <summary>
        ///     Used for Drones to know that it should retract drones
        /// </summary>
        public static bool IsMissionPocketDone { get; set; }

        public static double MaxDroneRange
        {
            get
            {
                if (_maxDroneRange == null)
                {
                    _maxDroneRange = Math.Min(DroneControlRange, Combat.MaxTargetRange);
                    return (double) _maxDroneRange;
                }

                return (double) _maxDroneRange;
            }
        }

        public static EntityCache PreferredDroneTarget
        {
            get
            {
                if (_preferredDroneTarget == null)
                {
                    if (PreferredDroneTargetID != null)
                    {
                        if (Cache.Instance.EntitiesOnGrid.Any(i => i.Id == PreferredDroneTargetID))
                        {
                            _preferredDroneTarget = Cache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.Id == PreferredDroneTargetID);
                            return _preferredDroneTarget;
                        }

                        return null;
                    }

                    return null;
                }

                return _preferredDroneTarget;
            }
            set
            {
                if (value == null)
                {
                    if (_preferredDroneTarget != null)
                    {
                        _preferredDroneTarget = null;
                        PreferredDroneTargetID = null;
                        Logging.Logging.Log("[ null ]");
                        return;
                    }
                }
                else
                {
                    if (_preferredDroneTarget != null && _preferredDroneTarget.Id != value.Id)
                    {
                        _preferredDroneTarget = value;
                        PreferredDroneTargetID = value.Id;
                        if (Logging.Logging.DebugGetBestTarget)
                            Logging.Logging.Log(value + " [" + value.MaskedId + "]");
                        return;
                    }
                }
            }
        }

        public static List<PriorityTarget> DronePriorityTargets
        {
            get
            {
                try
                {
                    //
                    // remove targets that no longer exist
                    //
                    if (_dronePriorityTargets != null && _dronePriorityTargets.Any())
                    {
                        foreach (var dronePriorityTarget in _dronePriorityTargets)
                        {
                            if (Cache.Instance.EntitiesOnGrid.All(i => i.Id != dronePriorityTarget.EntityID))
                            {
                                _dronePriorityTargets.Remove(dronePriorityTarget);
                                break;
                            }
                        }

                        return _dronePriorityTargets;
                    }

                    //
                    // initialize a fresh list - to be filled in during panic (updated every tick)
                    //
                    _dronePriorityTargets = new List<PriorityTarget>();
                    return _dronePriorityTargets;
                }
                catch (NullReferenceException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public static IEnumerable<EntityCache> DronePriorityEntities
        {
            get
            {
                try
                {
                    //
                    // every frame re-populate the DronePriorityEntities from the list of IDs we have tucked away in DronePriorityTargets
                    // this occurs because in Invalidatecache() we are, necessarily,  clearing this every frame!
                    //
                    if (_dronePriorityEntities == null)
                    {
                        if (DronePriorityTargets != null && DronePriorityTargets.Any())
                        {
                            _dronePriorityEntities =
                                DronePriorityTargets.OrderByDescending(pt => pt.DronePriority).ThenBy(pt => pt.Entity.Distance).Select(pt => pt.Entity);
                            return _dronePriorityEntities;
                        }

                        _dronePriorityEntities = new List<EntityCache>();
                        return _dronePriorityEntities;
                    }

                    //
                    // if we have already populated the list this frame return the list we already generated
                    //
                    return _dronePriorityEntities;
                }
                catch (NullReferenceException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public static DirectContainer DroneBay
        {
            get
            {
                try
                {
                    if (!Cache.Instance.InSpace && Cache.Instance.InStation)
                    {
                        if (_droneBay == null)
                        {
                            _droneBay = Cache.Instance.DirectEve.GetShipsDroneBay();
                        }

                        if (Cache.Instance.Windows.All(i => !i.Type.Contains("Drone")))
                            // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
                        {
                            if (DateTime.UtcNow > Time.Instance.LastOpenHangar.AddSeconds(10))
                            {
                                Statistics.LogWindowActionToWindowLog("Dronebay", "Opening Dronebay");
                                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenDroneBayOfActiveShip);
                                Time.Instance.LastOpenHangar = DateTime.UtcNow;
                            }
                        }

                        return _droneBay;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                    return null;
                }
            }

            set { _droneBay = value; }
        }

        /// <summary>
        ///     Remove targets from priority list
        /// </summary>
        /// <param name="targets"></param>
        public static bool RemoveDronePriorityTargets(List<EntityCache> targets)
        {
            try
            {
                targets = targets.ToList();

                if (targets.Any() && _dronePriorityTargets != null && _dronePriorityTargets.Any() &&
                    _dronePriorityTargets.Any(pt => targets.Any(t => t.Id == pt.EntityID)))
                {
                    _dronePriorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }

        public static void AddDronePriorityTargetsByName(string stringEntitiesToAdd)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToAdd = Cache.Instance.EntitiesByPartialName(stringEntitiesToAdd).ToList();
                if (entitiesToAdd.Any())
                {
                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        Logging.Logging.Log("adding [" + entityToAdd.Name + "][" + Math.Round(entityToAdd.Distance / 1000, 0) + "k][" + entityToAdd.MaskedId +
                            "] to the PWPT List");
                        AddDronePriorityTarget(entityToAdd, DronePriority.PriorityKillTarget, "AddDPTByName");
                        continue;
                    }

                    return;
                }

                Logging.Logging.Log("[" + stringEntitiesToAdd + "] was not found on grid");
                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return;
            }
        }

        public static void RemovedDronePriorityTargetsByName(string stringEntitiesToRemove)
        {
            try
            {
                var entitiesToRemove = Cache.Instance.EntitiesByName(stringEntitiesToRemove, Cache.Instance.EntitiesOnGrid).ToList();
                if (entitiesToRemove.Any())
                {
                    Logging.Logging.Log("removing [" + stringEntitiesToRemove + "] from the DPT List");
                    RemoveDronePriorityTargets(entitiesToRemove);
                    return;
                }

                Logging.Logging.Log("[" + stringEntitiesToRemove + "] was not found on grid");
                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }
        }

        public static void AddDronePriorityTargets(IEnumerable<EntityCache> ewarEntities, DronePriority priority, string module,
            bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                ewarEntities = ewarEntities.ToList();
                if (ewarEntities.Any())
                {
                    foreach (var ewarEntity in ewarEntities)
                    {
                        AddDronePriorityTarget(ewarEntity, priority, module, AddEwarTypeToPriorityTargetList);
                        continue;
                    }

                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }
        }

        public static void AddDronePriorityTarget(EntityCache ewarEntity, DronePriority priority, string module, bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if (AddEwarTypeToPriorityTargetList && UseDrones)
                {
                    if ((ewarEntity.IsIgnored) || DronePriorityTargets.Any(p => p.EntityID == ewarEntity.Id))
                    {
                        if (Logging.Logging.DebugAddDronePriorityTarget)
                            Logging.Logging.Log("if ((target.IsIgnored) || DronePriorityTargets.Any(p => p.Id == target.Id))");
                        return;
                    }

                    if (DronePriorityTargets.All(i => i.EntityID != ewarEntity.Id))
                    {
                        var DronePriorityTargetCount = 0;
                        if (DronePriorityTargets.Any())
                        {
                            DronePriorityTargetCount = DronePriorityTargets.Count();
                        }
                        Logging.Logging.Log("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + " m/s] Distance [" +
                            Math.Round(ewarEntity.Distance / 1000, 2) + "] [ID: " + ewarEntity.MaskedId + "] as a drone priority target [" + priority.ToString() +
                            "] we have [" + DronePriorityTargetCount + "] other DronePriorityTargets");
                        _dronePriorityTargets.Add(new PriorityTarget {Name = ewarEntity.Name, EntityID = ewarEntity.Id, DronePriority = priority});
                    }

                    return;
                }

                if (Logging.Logging.DebugAddDronePriorityTarget)
                    Logging.Logging.Log("UseDrones is [" + UseDrones.ToString() + "] AddWarpScramblersToDronePriorityTargetList is [" +
                        AddWarpScramblersToDronePriorityTargetList + "] [" + ewarEntity.Name +
                        "] was not added as a Drone PriorityTarget (why did we even try?)");
                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }
        }

        public static EntityCache FindDronePriorityTarget(EntityCache currentTarget, DronePriority priorityType, bool AddECMTypeToDronePriorityTargetList,
            double Distance, bool FindAUnTargetedEntity = true)
        {
            if (AddECMTypeToDronePriorityTargetList)
            {
                //if (Logging.DebugGetBestTarget) Logging.Log(callingroutine + " Debug: GetBestTarget", "Checking for Neutralizing priority targets (currentTarget first)", Logging.Teal);
                // Choose any Neutralizing primary weapon priority targets
                try
                {
                    EntityCache target = null;
                    try
                    {
                        if (DronePriorityEntities.Any(pt => pt.DronePriorityLevel == priorityType))
                        {
                            target =
                                DronePriorityEntities.Where(
                                    pt =>
                                        ((FindAUnTargetedEntity || pt.IsReadyToShoot) && currentTarget != null && pt.Id == currentTarget.Id &&
                                         (pt.Distance < Distance) && pt.IsActiveDroneEwarType == priorityType)
                                        || ((FindAUnTargetedEntity || pt.IsReadyToShoot) && pt.Distance < Distance && pt.IsActiveDroneEwarType == priorityType))
                                    .OrderByDescending(pt => pt.IsNPCFrigate)
                                    .ThenByDescending(pt => pt.IsLastTargetDronesWereShooting)
                                    .ThenByDescending(pt => pt.IsInDroneRange)
                                    .ThenBy(pt => pt.IsEntityIShouldKeepShootingWithDrones)
                                    .ThenBy(pt => (pt.ShieldPct + pt.ArmorPct + pt.StructurePct))
                                    .ThenBy(pt => pt.Nearest5kDistance)
                                    .FirstOrDefault();
                        }
                    }
                    catch (NullReferenceException)
                    {
                    } // Not sure why this happens, but seems to be no problem

                    if (target != null)
                    {
                        //if (Logging.DebugGetBestTarget) Logging.Log(callingroutine + " Debug: GetBestTarget", "NeutralizingPrimaryWeaponPriorityTarget [" + NeutralizingPriorityTarget.Name + "][" + Math.Round(NeutralizingPriorityTarget.Distance / 1000, 2) + "k][" + Cache.Instance.MaskedID(NeutralizingPriorityTarget.Id) + "] GroupID [" + NeutralizingPriorityTarget.GroupId + "]", Logging.Debug);

                        if (!FindAUnTargetedEntity)
                        {
                            PreferredDroneTarget = target;
                            Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                            return target;
                        }

                        return target;
                    }

                    return null;
                }
                catch (NullReferenceException)
                {
                }

                return null;
            }

            return null;
        }

        public static bool GetBestDroneTarget(double distance, bool highValueFirst, string callingroutine, List<EntityCache> _potentialTargets = null)
        {
            if (Logging.Logging.DebugDisableGetBestDroneTarget || !UseDrones)
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("!Cache.Instance.UseDrones - drones are disabled currently");
                return true;
            }

            if (Logging.Logging.DebugGetBestDroneTarget)
                Logging.Logging.Log("Attempting to get Best Drone Target");

            if (DateTime.UtcNow < Time.Instance.NextGetBestDroneTarget)
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("Cant GetBest yet....Too Soon!");
                return false;
            }

            Time.Instance.NextGetBestDroneTarget = DateTime.UtcNow.AddMilliseconds(2000);

            //if (!Cache.Instance.Targets.Any()) //&& _potentialTargets == null )
            //{
            //    if (Logging.DebugGetBestDroneTarget) Logging.Log(callingroutine + " Debug: DebugGetBestDroneTarget:", "We have no locked targets and [" + Cache.Instance.Targeting.Count() + "] targets being locked atm", Logging.Teal);
            //    return false;
            //}

            EntityCache currentDroneTarget = null;

            if (Cache.Instance.EntitiesOnGrid.Any(i => i.IsLastTargetDronesWereShooting))
            {
                currentDroneTarget = Cache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.IsLastTargetDronesWereShooting);
            }

            if (DateTime.UtcNow < Time.Instance.LastPreferredDroneTargetDateTime.AddSeconds(6) &&
                (PreferredDroneTarget != null && Cache.Instance.EntitiesOnGrid.Any(t => t.Id == PreferredDroneTarget.Id)))
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("We have a PreferredDroneTarget [" + PreferredDroneTarget.Name + "] that was chosen less than 6 sec ago, and is still alive.");
                return true;
            }

            //We need to make sure that our current Preferred is still valid, if not we need to clear it out
            //This happens when we have killed the last thing within our range (or the last thing in the pocket)
            //and there is nothing to replace it with.
            //if (Cache.Instance.PreferredDroneTarget != null
            //    && Cache.Instance.Entities.All(t => t.Id != Instance.PreferredDroneTargetID))
            //{
            //    if (Logging.DebugGetBestDroneTarget) Logging.Log("GetBestDroneTarget", "PreferredDroneTarget is not valid, clearing it", Logging.White);
            //    Cache.Instance.PreferredDroneTarget = null;
            //}

            //
            // if currentTarget set to something (not null) and it is actually an entity...
            //


            if (currentDroneTarget != null)
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("Checking Low Health");
                if (currentDroneTarget.IsEntityIShouldKeepShootingWithDrones)
                {
                    if (Logging.Logging.DebugGetBestDroneTarget)
                        Logging.Logging.Log("currentDroneTarget [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) + "k][" +
                            currentDroneTarget.MaskedId + " GroupID [" + currentDroneTarget.GroupId + "]] has less than 80% shields, keep killing this target");
                    PreferredDroneTarget = currentDroneTarget;
                    Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }
            }

            if (currentDroneTarget != null && currentDroneTarget.IsReadyToShoot && currentDroneTarget.IsLowValueTarget)
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("We have a currentTarget [" + currentDroneTarget.Name + "][" + currentDroneTarget.MaskedId + "][" +
                        Math.Round(currentDroneTarget.Distance / 1000, 2) + "k], testing conditions");

                #region Is our current target any other drone priority target?

                //
                // Is our current target any other drone priority target? AND if our target is just a PriorityKillTarget assume ALL E-war is more important.
                //
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("Checking Priority");
                if (DronePriorityEntities.Any(pt => pt.IsReadyToShoot
                                                    && pt.Nearest5kDistance < MaxDroneRange
                                                    && pt.Id == currentDroneTarget.Id
                                                    && !currentDroneTarget.IsHigherPriorityPresent))
                {
                    if (Logging.Logging.DebugGetBestDroneTarget)
                        Logging.Logging.Log("CurrentTarget [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) + "k][" +
                            currentDroneTarget.MaskedId + "] GroupID [" + currentDroneTarget.GroupId + "]");
                    PreferredDroneTarget = currentDroneTarget;
                    Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target any other drone priority target?

                #region Is our current target already in armor? keep shooting the same target if so...

                //
                // Is our current target already low health? keep shooting the same target if so...
                //
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("Checking Low Health");
                if (currentDroneTarget.IsEntityIShouldKeepShootingWithDrones)
                {
                    if (Logging.Logging.DebugGetBestDroneTarget)
                        Logging.Logging.Log("currentDroneTarget [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) + "k][" +
                            currentDroneTarget.MaskedId + " GroupID [" + currentDroneTarget.GroupId + "]] has less than 80% shields, keep killing this target");
                    PreferredDroneTarget = currentDroneTarget;
                    Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target already in armor? keep shooting the same target if so...

                #region If none of the above matches, does our current target meet the conditions of being hittable and in range

                if (!currentDroneTarget.IsHigherPriorityPresent)
                {
                    if (Logging.Logging.DebugGetBestDroneTarget)
                        Logging.Logging.Log("Does the currentTarget exist? Can it be hit?");
                    if (currentDroneTarget.IsReadyToShoot && currentDroneTarget.Nearest5kDistance < MaxDroneRange)
                    {
                        if (Logging.Logging.DebugGetBestDroneTarget)
                            Logging.Logging.Log("if  the currentDroneTarget exists and the target is the right size then continue shooting it;");
                        if (Logging.Logging.DebugGetBestDroneTarget)
                            Logging.Logging.Log("currentDroneTarget is [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) + "k][" +
                                currentDroneTarget.MaskedId + "] GroupID [" + currentDroneTarget.GroupId + "]");

                        PreferredDroneTarget = currentDroneTarget;
                        Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                        return true;
                    }
                }

                #endregion
            }

            //
            // process the list of PrimaryWeaponPriorityTargets in this order... Eventually the order itself should be user selectable
            // this allow us to kill the most 'important' things doing e-war first instead of just handling them by range
            //

            // Do we have ANY warp scrambling entities targeted starting with currentTarget
            // this needs Settings.Instance.AddWarpScramblersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.WarpScrambler, AddWarpScramblersToDronePriorityTargetList, distance) != null)
                return true;

            // Do we have ANY ECM entities targeted starting with currentTarget
            // this needs Settings.Instance.AddECMsToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.Webbing, AddECMsToDroneTargetList, distance) != null)
                return true;

            // Do we have ANY tracking disrupting entities targeted starting with currentTarget
            // this needs Settings.Instance.AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.PriorityKillTarget, AddTrackingDisruptorsToDronePriorityTargetList, distance) != null)
                return true;

            // Do we have ANY Neutralizing entities targeted starting with currentTarget
            // this needs Settings.Instance.AddNeutralizersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.PriorityKillTarget, AddNeutralizersToDronePriorityTargetList, distance) != null)
                return true;

            // Do we have ANY Target Painting entities targeted starting with currentTarget
            // this needs Settings.Instance.AddTargetPaintersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.PriorityKillTarget, AddTargetPaintersToDronePriorityTargetList, distance) != null)
                return true;

            // Do we have ANY Sensor Dampening entities targeted starting with currentTarget
            // this needs Settings.Instance.AddDampenersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.PriorityKillTarget, AddDampenersToDronePriorityTargetList, distance) != null)
                return true;

            // Do we have ANY Webbing entities targeted starting with currentTarget
            // this needs Settings.Instance.AddWebifiersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.PriorityKillTarget, AddWebifiersToDronePriorityTargetList, distance) != null)
                return true;

            #region Get the closest drone priority target

            //
            // Get the closest primary weapon priority target
            //
            if (Logging.Logging.DebugGetBestDroneTarget)
                Logging.Logging.Log("Checking Closest DronePriorityTarget");
            EntityCache dronePriorityTarget = null;
            try
            {
                dronePriorityTarget = DronePriorityEntities.Where(p => p.Nearest5kDistance < MaxDroneRange
                                                                       && !p.IsIgnored
                                                                       && p.IsReadyToShoot)
                    .OrderBy(pt => pt.DronePriorityLevel)
                    .ThenByDescending(pt => pt.IsEwarTarget)
                    .ThenByDescending(pt => pt.IsTargetedBy)
                    .ThenBy(pt => pt.Nearest5kDistance)
                    .FirstOrDefault();
            }
            catch (NullReferenceException)
            {
            } // Not sure why this happens, but seems to be no problem

            if (dronePriorityTarget != null)
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("dronePriorityTarget is [" + dronePriorityTarget.Name + "][" + Math.Round(dronePriorityTarget.Distance / 1000, 2) + "k][" +
                        dronePriorityTarget.MaskedId + "] GroupID [" + dronePriorityTarget.GroupId + "]");
                PreferredDroneTarget = dronePriorityTarget;
                Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion Get the closest drone priority target

            #region did our calling routine (CombatMissionCtrl?) pass us targets to shoot?

            //
            // This is where CombatMissionCtrl would pass targets to GetBestDroneTarget
            //
            if (Logging.Logging.DebugGetBestDroneTarget)
                Logging.Logging.Log("Checking Calling Target");
            if (_potentialTargets != null && _potentialTargets.Any())
            {
                EntityCache callingDroneTarget = null;
                try
                {
                    callingDroneTarget = _potentialTargets.OrderBy(t => t.Nearest5kDistance).FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }

                if (callingDroneTarget != null && callingDroneTarget.IsReadyToShoot)
                {
                    if (Logging.Logging.DebugGetBestDroneTarget)
                        Logging.Logging.Log("if (callingDroneTarget != null && !callingDroneTarget.IsIgnored)");
                    if (Logging.Logging.DebugGetBestDroneTarget)
                        Logging.Logging.Log("callingDroneTarget is [" + callingDroneTarget.Name + "][" + Math.Round(callingDroneTarget.Distance / 1000, 2) + "k][" +
                            callingDroneTarget.MaskedId + "] GroupID [" + callingDroneTarget.GroupId + "]");
                    AddDronePriorityTarget(callingDroneTarget, DronePriority.PriorityKillTarget, " GetBestDroneTarget: callingDroneTarget");
                    PreferredDroneTarget = callingDroneTarget;
                    Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                //return false; //do not return here, continue to process targets, we did not find one yet
            }

            #endregion

            #region Get the closest Low Value Target

            if (Logging.Logging.DebugGetBestDroneTarget)
                Logging.Logging.Log("Checking Closest Low Value");
            EntityCache lowValueTarget = null;

            if (Combat.PotentialCombatTargets.Any())
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("get closest: if (potentialCombatTargets.Any())");

                lowValueTarget = Combat.PotentialCombatTargets.Where(t => t.IsLowValueTarget && t.IsReadyToShoot)
                    .OrderBy(t => t.IsEwarTarget)
                    .ThenByDescending(t => t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenBy(Cache.Instance.OrderByLowestHealth())
                    .ThenBy(t => t.Nearest5kDistance)
                    .FirstOrDefault();
            }

            #endregion

            #region Get the closest high value target

            //
            // Get the closest low value target //excluding things going too fast for guns to hit (if you have guns fitted)
            //
            if (Logging.Logging.DebugGetBestDroneTarget)
                Logging.Logging.Log("Checking closest Low Value");
            EntityCache highValueTarget = null;
            if (Combat.PotentialCombatTargets.Any())
            {
                highValueTarget = Combat.PotentialCombatTargets.Where(t => t.IsHighValueTarget && t.IsReadyToShoot)
                    .OrderByDescending(t => !t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenBy(Cache.Instance.OrderByLowestHealth())
                    .ThenBy(t => t.Nearest5kDistance)
                    .FirstOrDefault();
            }

            #endregion

            #region prefer to grab a lowvaluetarget, if none avail use a high value target

            if (lowValueTarget != null || highValueTarget != null)
            {
                if (Logging.Logging.DebugGetBestDroneTarget)
                    Logging.Logging.Log("Checking use High Value");
                if (Logging.Logging.DebugGetBestDroneTarget)
                {
                    if (highValueTarget != null)
                    {
                        Logging.Logging.Log("highValueTarget is [" + highValueTarget.Name + "][" + Math.Round(highValueTarget.Distance / 1000, 2) + "k][" +
                            highValueTarget.MaskedId + "] GroupID [" + highValueTarget.GroupId + "]");
                    }
                    else
                    {
                        Logging.Logging.Log("highValueTarget is [ null ]");
                    }
                }
                PreferredDroneTarget = lowValueTarget ?? highValueTarget ?? null;
                Time.Instance.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion

            if (Logging.Logging.DebugGetBestDroneTarget)
                Logging.Logging.Log("Could not determine a suitable Drone target");

            #region If we did not find anything at all (wtf!?!?)

            if (Logging.Logging.DebugGetBestDroneTarget)
            {
                if (Cache.Instance.Targets.Any())
                {
                    Logging.Logging.Log(".");
                    Logging.Logging.Log("*** ALL LOCKED/LOCKING TARGETS LISTED BELOW");
                    var LockedTargetNumber = 0;
                    foreach (var __target in Cache.Instance.Targets)
                    {
                        LockedTargetNumber++;
                        Logging.Logging.Log("*** Target: [" + LockedTargetNumber + "][" + __target.Name + "][" + Math.Round(__target.Distance / 1000, 2) + "k][" +
                            __target.MaskedId + "][isTarget: " + __target.IsTarget + "][isTargeting: " + __target.IsTargeting + "] GroupID [" + __target.GroupId +
                            "]");
                    }
                    Logging.Logging.Log("*** ALL LOCKED/LOCKING TARGETS LISTED ABOVE");
                    Logging.Logging.Log(".");
                }

                if (Combat.PotentialCombatTargets.Any(t => !t.IsTarget && !t.IsTargeting))
                {
                    if (CombatMissionCtrl.IgnoreTargets.Any())
                    {
                        var IgnoreCount = CombatMissionCtrl.IgnoreTargets.Count;
                        Logging.Logging.Log("Ignore List has [" + IgnoreCount + "] Entities in it.");
                    }

                    Logging.Logging.Log("***** ALL [" + Combat.PotentialCombatTargets.Count() + "] potentialCombatTargets LISTED BELOW (not yet targeted or targeting)");
                    var potentialCombatTargetNumber = 0;
                    foreach (var potentialCombatTarget in Combat.PotentialCombatTargets)
                    {
                        potentialCombatTargetNumber++;
                        Logging.Logging.Log("***** Unlocked [" + potentialCombatTargetNumber + "]: [" + potentialCombatTarget.Name + "][" +
                            Math.Round(potentialCombatTarget.Distance / 1000, 2) + "k][" + potentialCombatTarget.MaskedId + "][isTarget: " +
                            potentialCombatTarget.IsTarget + "] GroupID [" + potentialCombatTarget.GroupId + "]");
                    }
                    Logging.Logging.Log("***** ALL [" + Combat.PotentialCombatTargets.Count() + "] potentialCombatTargets LISTED ABOVE (not yet targeted or targeting)");
                    Logging.Logging.Log(".");
                }
            }

            #endregion

            Time.Instance.NextGetBestDroneTarget = DateTime.UtcNow;
            return false;
        }

        private static double GetActiveDroneShieldTotal()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.ShieldHitPoints);
        }

        private static double GetActiveDroneArmorTotal()
        {
            if (!ActiveDrones.Any())
                return 0;

            if (ActiveDrones.Any(i => i.ArmorPct*100 < 100))
            {
                Arm.NeedRepair = true;
            }

            return ActiveDrones.Sum(d => d.ArmorHitPoints);
        }

        private static double GetActiveDroneStructureTotal()
        {
            if (!ActiveDrones.Any())
                return 0;

            if (ActiveDrones.Any(i => i.StructurePct*100 < 100))
            {
                Arm.NeedRepair = true;
            }

            return ActiveDrones.Sum(d => d.StructureHitPoints);
        }

        private static double GetActiveDroneShieldPercentage()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.ShieldPct*100);
        }

        private static double GetActiveDroneArmorPercentage()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.ArmorPct*100);
        }

        private static double GetActiveDroneStructurePercentage()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.StructurePct*100);
        }

        /// <summary>
        ///     Engage the target
        /// </summary>
        private static void EngageTarget()
        {
            try
            {
                if (Logging.Logging.DebugDrones) Logging.Logging.Log("Entering EngageTarget()");

                // Find the first active weapon's target
                //TargetingCache.CurrentDronesTarget = Cache.Instance.EntityById(_lastTarget);

                if (Logging.Logging.DebugDrones)
                    Logging.Logging.Log("MaxDroneRange [" + MaxDroneRange + "] lowValueTargetTargeted [" + Combat.lowValueTargetsTargeted.Count() + "] LVTT InDroneRange [" +
                        Combat.lowValueTargetsTargeted.Count(i => i.Distance < MaxDroneRange) + "] highValueTargetTargeted [" +
                        Combat.highValueTargetsTargeted.Count() + "] HVTT InDroneRange [" +
                        Combat.highValueTargetsTargeted.Count(i => i.Distance < MaxDroneRange) + "]");
                // Return best possible low value target

                if (PreferredDroneTarget == null || !PreferredDroneTarget.IsFrigate)
                {
                    GetBestDroneTarget(MaxDroneRange, !DronesKillHighValueTargets, "Drones");
                }

                var droneTarget = PreferredDroneTarget;

                if (droneTarget == null)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("PreferredDroneTarget is null, picking a target using a simple rule set...");
                    if (Cache.Instance.Targets.Any(i => !i.IsContainer && !i.IsBadIdea && i.Distance < MaxDroneRange))
                    {
                        droneTarget =
                            Cache.Instance.Targets.Where(i => !i.IsContainer && !i.IsBadIdea && i.Distance < MaxDroneRange)
                                .OrderByDescending(i => i.IsWarpScramblingMe)
                                .ThenByDescending(i => i.IsFrigate)
                                .ThenBy(i => i.Distance)
                                .FirstOrDefault();
                        if (droneTarget == null)
                        {
                            Logging.Logging.Log("DroneToShoot is Null, this is bad.");
                        }
                    }
                }

                if (droneTarget != null)
                {
                    if (droneTarget.IsReadyToShoot && droneTarget.Distance < MaxDroneRange)
                    {
                        if (Logging.Logging.DebugDrones)
                            Logging.Logging.Log("if (DroneToShoot != null && DroneToShoot.IsReadyToShoot && DroneToShoot.Distance < Cache.Instance.MaxDroneRange)");

                        // Nothing to engage yet, probably re-targeting
                        if (!droneTarget.IsTarget)
                        {
                            if (Logging.Logging.DebugDrones) Logging.Logging.Log("if (!DroneToShoot.IsTarget)");
                            return;
                        }

                        if (droneTarget.IsBadIdea) //&& !DroneToShoot.IsAttacking)
                        {
                            if (Logging.Logging.DebugDrones)
                                Logging.Logging.Log("if (DroneToShoot.IsBadIdea && !DroneToShoot.IsAttacking) return;");
                            return;
                        }

                        // Is our current target still the same and are all the drones shooting the PreferredDroneTarget?
                        if (LastTargetIDDronesEngaged != null)
                        {
                            if (LastTargetIDDronesEngaged == droneTarget.Id &&
                                ActiveDrones.All(i => i.FollowId == PreferredDroneTargetID && (i.Mode == 1 || i.Mode == 6 || i.Mode == 10)))
                            {
                                if (Logging.Logging.DebugDrones)
                                    Logging.Logging.Log("if (LastDroneTargetID [" + LastTargetIDDronesEngaged + "] == DroneToShoot.Id [" + droneTarget.Id +
                                        "] && Cache.Instance.ActiveDrones.Any(i => i.FollowId != Cache.Instance.PreferredDroneTargetID) [" +
                                        ActiveDrones.Any(i => i.FollowId != PreferredDroneTargetID) + "])");
                                return;
                            }
                        }

                        //
                        // If we got this far we need to tell the drones to do something
                        // Is the last target our current active target?
                        //

                        if (!Cache.Instance.DirectEve.IsTargetBeingRemoved(droneTarget.Id) && Cache.Instance.DirectEve.IsTargetStillValid(droneTarget.Id))
                        {
                            if (droneTarget.IsActiveTarget)
                            {
                                // Engage target

                                if (LastTargetIDDronesEngaged == null || LastTargetIDDronesEngaged != droneTarget.Id ||
                                    LastDroneFightCmd.AddMinutes(5) < DateTime.UtcNow)
                                {
                                    if (LastDroneFightCmd.AddSeconds(10) < DateTime.UtcNow &&
                                        Cache.Instance.DirectEve.GetLastModuleBlocked.AddSeconds(5) < DateTime.UtcNow)
                                    {
                                        Logging.Logging.Log("Engaging [ " + ActiveDrones.Count() + " ] drones on [" + droneTarget.Name + "][ID: " + droneTarget.MaskedId + "]" +
                                            Math.Round(droneTarget.Distance / 1000, 0) + "k away]");
                                        Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesEngage);
                                        // Save target id (so we do not constantly switch)
                                        LastDroneFightCmd = DateTime.UtcNow;
                                        LastTargetIDDronesEngaged = droneTarget.Id;
                                    }
                                }
                            }

                            else // Make the target active
                            {
                                if (DateTime.UtcNow > Time.Instance.NextMakeActiveTargetAction)
                                {
                                    droneTarget.MakeActiveTarget();
                                    Logging.Logging.Log("[" + droneTarget.Name + "][ID: " + droneTarget.MaskedId + "]IsActiveTarget[" + droneTarget.IsActiveTarget + "][" +
                                        Math.Round(droneTarget.Distance / 1000, 0) + "k away] has been made the ActiveTarget (needed for drones)");
                                    Time.Instance.NextMakeActiveTargetAction = DateTime.UtcNow.AddSeconds(5 + Cache.Instance.RandomNumber(0, 3));
                                }
                            }
                        }
                    }

                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("if (DroneToShoot != null && DroneToShoot.IsReadyToShoot && DroneToShoot.Distance < Cache.Instance.MaxDroneRange)");
                    return;
                }

                if (Logging.Logging.DebugDrones)
                    Logging.Logging.Log("if (Cache.Instance.PreferredDroneTargetID != null)");
                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }
        }

        public static bool CloseDroneBayWindow(string module)
        {
            if (DateTime.UtcNow < Time.Instance.NextDroneBayAction)
            {
                //Logging.Log(module + ": Closing Drone Bay: waiting [" + Math.Round(Cache.Instance.NextOpenDroneBayAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]",Logging.White);
                return false;
            }

            try
            {
                if ((!Cache.Instance.InSpace && !Cache.Instance.InStation))
                {
                    Logging.Logging.Log("Closing Drone Bay: We are not in station or space?!");
                    return false;
                }

                if (Cache.Instance.InStation || Cache.Instance.InSpace)
                {
                    DroneBay = Cache.Instance.DirectEve.GetShipsDroneBay();
                }
                else
                {
                    return false;
                }

                // Is the drone bay open? if so, close it
                if (DroneBay.Window != null)
                {
                    Time.Instance.NextDroneBayAction = DateTime.UtcNow.AddSeconds(2 + Cache.Instance.RandomNumber(1, 3));
                    Logging.Logging.Log("Closing Drone Bay: waiting [" + Math.Round(Time.Instance.NextDroneBayAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");
                    DroneBay.Window.Close();
                    Statistics.LogWindowActionToWindowLog("Dronebay", "Closing DroneBay");
                    return true;
                }

                return true;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Unable to complete CloseDroneBay [" + exception + "]");
                return false;
            }
        }

        private static bool ShouldWeLaunchDrones()
        {
            // Always launch if we're scrambled
            if (!Combat.PotentialCombatTargets.Any(pt => pt.IsWarpScramblingMe))
            {
                if (!UseDrones)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("UseDrones is [" + UseDrones + "] Not Launching Drones");
                    return false;
                }

                // Are we done with this mission pocket?
                if (IsMissionPocketDone)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("IsMissionPocketDone [" + IsMissionPocketDone + "] Not Launching Drones");
                    return false;
                }

                // If above my ships shield minimum for launching drones
                if (Cache.Instance.ActiveShip.ShieldPercentage <= DroneMinimumShieldPct)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("My Ships ShieldPercentage [" + Cache.Instance.ActiveShip.ShieldPercentage + "] is below [" + DroneMinimumShieldPct +
                            "] Not Launching Drones");
                    return false;
                }

                // If above my ships armor minimum for launching drones
                if (Cache.Instance.ActiveShip.ArmorPercentage <= DroneMinimumArmorPct)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("My Ships ArmorPercentage [" + Cache.Instance.ActiveShip.ArmorPercentage + "] is below [" + DroneMinimumArmorPct +
                            "] Not Launching Drones");
                    return false;
                }

                // If above my ships capacitor minimum for launching drones
                if (Cache.Instance.ActiveShip.CapacitorPercentage <= DroneMinimumCapacitorPct)
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("My Ships CapacitorPercentage [" + Cache.Instance.ActiveShip.CapacitorPercentage + "] is below [" + DroneMinimumCapacitorPct +
                            "] Not Launching Drones");
                    return false;
                }

                // yes if there are targets to kill
                if (
                    !Combat.Aggressed.Any(
                        e => (!e.IsSentry || (e.IsSentry && Combat.KillSentries) || (e.IsSentry && e.IsEwarTarget)) && e.Distance < MaxDroneRange) &&
                    !Cache.Instance.Targets.Any(e => e.IsLargeCollidable))
                {
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("We have nothing Aggressed; MaxDroneRange [" + MaxDroneRange + "] DroneControlrange [" + DroneControlRange + "] TargetingRange [" +
                            Combat.MaxTargetRange + "]");
                    return false;
                }

                if (_States.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
                {
                    if (
                        !Cache.Instance.EntitiesOnGrid.Any(
                            e =>
                                ((!e.IsSentry && !e.IsBadIdea && e.CategoryId == (int) CategoryID.Entity && e.IsNpc && !e.IsContainer && !e.IsLargeCollidable) ||
                                 e.IsAttacking) && e.Distance < MaxDroneRange))
                    {
                        if (Logging.Logging.DebugDrones)
                            Logging.Logging.Log("QuestorState is [" + _States.CurrentQuestorState.ToString() + "] We have nothing to shoot;");
                        return false;
                    }
                }

                // If drones get aggro'd within 30 seconds, then wait (5 * _recallCount + 5) seconds since the last recall
                if (_lastLaunch < _lastRecall && _lastRecall.Subtract(_lastLaunch).TotalSeconds < 30)
                {
                    if (_lastRecall.AddSeconds(5*_recallCount + 5) < DateTime.UtcNow)
                    {
                        // Increase recall count and allow the launch
                        _recallCount++;

                        // Never let _recallCount go above 5
                        if (_recallCount > 5)
                        {
                            _recallCount = 5;
                        }

                        return true;
                    }

                    // Do not launch the drones until the delay has passed
                    if (Logging.Logging.DebugDrones)
                        Logging.Logging.Log("We are still in _lastRecall delay.");
                    return false;
                }

                // Drones have been out for more then 30s
                _recallCount = 0;
                return true;
            }

            return true;
        }

        private static bool ShouldWeRecallDrones()
        {
            try
            {
                // Default to long range recall
                var lowShieldWarning = LongRangeDroneRecallShieldPct;
                var lowArmorWarning = LongRangeDroneRecallArmorPct;
                var lowCapWarning = LongRangeDroneRecallCapacitorPct;

                if (ActiveDrones.Average(d => d.Distance) < (MaxDroneRange/2d))
                {
                    lowShieldWarning = DroneRecallShieldPct;
                    lowArmorWarning = DroneRecallArmorPct;
                    lowCapWarning = DroneRecallCapacitorPct;
                }

                if (!UseDrones)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: UseDrones is [" + UseDrones + "]");
                    return true;
                }

                // Are we done (for now) ?
                var TargetedByInDroneRangeCount =
                    Combat.TargetedBy.Count(e => (!e.IsSentry || (e.IsSentry && Combat.KillSentries) || (e.IsSentry && e.IsEwarTarget)) && e.IsInDroneRange);
                if (TargetedByInDroneRangeCount == 0 && !WarpScrambled && !Settings.Instance.FleetSupportSlave)
                {
                    var TargtedByCount = 0;
                    if (Combat.TargetedBy != null && Combat.TargetedBy.Any())
                    {
                        TargtedByCount = Combat.TargetedBy.Count();
                        var __closestTargetedBy =
                            Combat.TargetedBy.OrderBy(i => i.Distance)
                                .FirstOrDefault(e => !e.IsSentry || (e.IsSentry && Combat.KillSentries) || (e.IsSentry && e.IsEwarTarget));
                        if (__closestTargetedBy != null)
                        {
                            Logging.Logging.Log("The closest target that is targeting ME is at [" + __closestTargetedBy.Distance + "]k");
                        }
                    }

                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: There are [" +
                        Combat.PotentialCombatTargets.Count(
                            e => e.IsInDroneRange && (!e.IsSentry || (e.IsSentry && Combat.KillSentries) || (e.IsSentry && e.IsEwarTarget))) +
                        "] PotentialCombatTargets not targeting us within My MaxDroneRange: [" + Math.Round(MaxDroneRange / 1000, 0) + "k] Targeting Range Is [" +
                        Math.Round(Combat.MaxTargetRange / 1000, 0) + "k] We have [" + TargtedByCount + "] total things targeting us and [" +
                        Combat.PotentialCombatTargets.Count(e => !e.IsSentry || (e.IsSentry && Combat.KillSentries) || (e.IsSentry && e.IsEwarTarget)) +
                        "] total PotentialCombatTargets");

                    if (Logging.Logging.DebugDrones)
                    {
                        foreach (var PCTInDroneRange in Combat.PotentialCombatTargets.Where(i => i.IsInDroneRange && i.IsTargetedBy))
                        {
                            Logging.Logging.Log("Recalling Drones Details:  PCTInDroneRange [" + PCTInDroneRange.Name + "][" + PCTInDroneRange.MaskedId + "] at [" +
                                Math.Round(PCTInDroneRange.Distance / 1000, 2) + "] not targeting us yet");
                        }
                    }

                    return true;
                }

                if (IsMissionPocketDone && !WarpScrambled)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: We are done with this pocket.");
                    return true;
                }

                if (_activeDronesShieldTotalOnLastPulse > GetActiveDroneShieldTotal() + 5)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: shields! [Old: " + _activeDronesShieldTotalOnLastPulse.ToString("N2") + "][New: " +
                        GetActiveDroneShieldTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesArmorTotalOnLastPulse > GetActiveDroneArmorTotal() + 5)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: armor! [Old:" + _activeDronesArmorTotalOnLastPulse.ToString("N2") + "][New: " +
                        GetActiveDroneArmorTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesStructureTotalOnLastPulse > GetActiveDroneStructureTotal() + 5)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: structure! [Old:" + _activeDronesStructureTotalOnLastPulse.ToString("N2") +
                        "][New: " + GetActiveDroneStructureTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesShieldPercentageOnLastPulse > GetActiveDroneShieldPercentage() + 1)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: shields! [Old: " + _activeDronesShieldPercentageOnLastPulse.ToString("N2") +
                        "][New: " + GetActiveDroneShieldPercentage().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesArmorPercentageOnLastPulse > GetActiveDroneArmorPercentage() + 1)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: armor! [Old:" + _activeDronesArmorPercentageOnLastPulse.ToString("N2") + "][New: " +
                        GetActiveDroneArmorPercentage().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesStructurePercentageOnLastPulse > GetActiveDroneStructurePercentage() + 1)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: structure! [Old:" + _activeDronesStructurePercentageOnLastPulse.ToString("N2") +
                        "][New: " + GetActiveDroneStructurePercentage().ToString("N2") + "]");
                    return true;
                }

                if (ActiveDrones.Count() < _lastDroneCount)
                {
                    // Did we lose a drone? (this should be covered by total's as well though)
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones: We lost a drone! [Old:" + _lastDroneCount + "][New: " + ActiveDrones.Count() + "]");
                    return true;
                }

                if ((Combat.PotentialCombatTargets.Any() && !Combat.PotentialCombatTargets.Any(i => i.IsTargeting || i.IsTarget)) &&
                    !DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to [" + Cache.Instance.Targets.Count() + "] targets being locked. Locking [" +
                        Cache.Instance.Targeting.Count() + "] targets atm");
                    return true;
                }

                if (Cache.Instance.ActiveShip.ShieldPercentage < lowShieldWarning && !WarpScrambled)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to shield [" + Math.Round(Cache.Instance.ActiveShip.ShieldPercentage, 0) +
                        "%] below [" + lowShieldWarning + "%] minimum");
                    return true;
                }

                if (Cache.Instance.ActiveShip.ArmorPercentage < lowArmorWarning && !WarpScrambled)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to armor [" + Math.Round(Cache.Instance.ActiveShip.ArmorPercentage, 0) +
                        "%] below [" + lowArmorWarning + "%] minimum");
                    return true;
                }

                if (Cache.Instance.ActiveShip.CapacitorPercentage < lowCapWarning && !WarpScrambled)
                {
                    Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to capacitor [" + Math.Round(Cache.Instance.ActiveShip.CapacitorPercentage, 0) +
                        "%] below [" + lowCapWarning + "%] minimum");
                    return true;
                }

                if (_States.CurrentQuestorState == QuestorState.CombatMissionsBehavior && !WarpScrambled)
                {
                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase && !WarpScrambled)
                    {
                        Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to gotobase state");
                        return true;
                    }

                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoMission && !WarpScrambled)
                    {
                        Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to gotomission state");
                        return true;
                    }

                    if (_States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Panic && !WarpScrambled)
                    {
                        Logging.Logging.Log("Recalling [ " + ActiveDrones.Count() + " ] drones due to panic state");
                        return true;
                    }
                }


                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }

        private static bool OnEveryDroneProcessState()
        {
            if (_nextDroneAction > DateTime.UtcNow || Logging.Logging.DebugDisableDrones) return false;

            if (Logging.Logging.DebugDrones) Logging.Logging.Log("Entering Drones.ProcessState");
            _nextDroneAction = DateTime.UtcNow.AddMilliseconds(1200);

            if (Cache.Instance.InStation || // There is really no combat in stations (yet)
                !Cache.Instance.InSpace || // if we are not in space yet, wait...
                Cache.Instance.MyShipEntity == null || // What? No ship entity?
                Cache.Instance.ActiveShip.Entity.IsCloaked // There is no combat when cloaked
                )
            {
                if (Logging.Logging.DebugDrones)
                    Logging.Logging.Log("InStation [" + Cache.Instance.InStation + "] InSpace [" + Cache.Instance.InSpace + "] IsCloaked [" +
                        Cache.Instance.ActiveShip.Entity.IsCloaked + "] - doing nothing");
                _States.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (!UseDrones && ActiveDrones.Any())
            {
                if (Logging.Logging.DebugDrones) Logging.Logging.Log("UseDrones [" + UseDrones + "]");
                if (!RecallingDronesState()) return false;
                return false;
            }

            if (ActiveDrones == null)
            {
                if (Logging.Logging.DebugDrones) Logging.Logging.Log("ActiveDrones == null");
                _States.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (Cache.Instance.MyShipEntity.IsShipWithNoDroneBay)
            {
                if (Logging.Logging.DebugDrones)
                    Logging.Logging.Log("IsShipWithNoDronesBay - Setting useDrones to false.");
                UseDrones = false;
                _States.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (!ActiveDrones.Any() && Cache.Instance.InWarp)
            {
                if (Logging.Logging.DebugDrones)
                    Logging.Logging.Log("No Active Drones in space and we are InWarp - doing nothing");
                RemoveDronePriorityTargets(DronePriorityEntities.ToList());
                _States.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (!Cache.Instance.EntitiesOnGrid.Any())
            {
                if (Logging.Logging.DebugDrones) Logging.Logging.Log("Nothing to shoot on grid - doing nothing");
                RemoveDronePriorityTargets(DronePriorityEntities.ToList());
                _States.CurrentDroneState = DroneState.Idle;
                return false;
            }

            return true;
        }


        private static bool IdleDroneState()
        {
            //
            // below is the reasons we will start the combat state(s) - if the below is not met do nothing
            //
            if (Cache.Instance.InSpace &&
                Cache.Instance.ActiveShip.Entity != null &&
                !Cache.Instance.ActiveShip.Entity.IsCloaked &&
                Cache.Instance.ActiveShip.GivenName.ToLower().Equals(Combat.CombatShipName.ToLower()) &&
                UseDrones &&
                !Cache.Instance.InWarp)
            {
                _States.CurrentDroneState = DroneState.WaitingForTargets;
                return true;
            }

            return false;
        }

        private static bool WaitingForTargetsDroneState()
        {
            // Are we in the right state ?
            if (ActiveDrones.Any())
            {
                // Apparently not, we have drones out, go into fight mode
                _States.CurrentDroneState = DroneState.Fighting;
                return true;
            }

            if (Cache.Instance.Targets.Any() || (Combat.PotentialCombatTargets.Any() && DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive))
            {
                // Should we launch drones?
                if (!ShouldWeLaunchDrones()) return false;

                // Reset launch tries
                _launchTries = 0;
                _lastLaunch = DateTime.UtcNow;
                _States.CurrentDroneState = DroneState.Launch;
                return true;
            }

            return true;
        }

        private static bool LaunchDronesState()
        {
            if (Logging.Logging.DebugDrones) Logging.Logging.Log("LaunchAllDrones");
            // Launch all drones
            _launchTimeout = DateTime.UtcNow;
            Cache.Instance.ActiveShip.LaunchAllDrones();
            _States.CurrentDroneState = DroneState.Launching;
            return true;
        }

        private static bool LaunchingDronesState()
        {
            if (Logging.Logging.DebugDrones) Logging.Logging.Log("Entering Launching State...");
            // We haven't launched anything yet, keep waiting
            if (!ActiveDrones.Any())
            {
                if (Logging.Logging.DebugDrones) Logging.Logging.Log("No Drones in space yet. waiting");
                if (DateTime.UtcNow.Subtract(_launchTimeout).TotalSeconds > 10)
                {
                    // Relaunch if tries < 5
                    if (_launchTries < 5)
                    {
                        _launchTries++;
                        _States.CurrentDroneState = DroneState.Launch;
                        return true;
                    }

                    _States.CurrentDroneState = DroneState.OutOfDrones;
                }

                return true;
            }

            // Are we done launching?
            if (_lastDroneCount == ActiveDrones.Count())
            {
                Logging.Logging.Log("[" + ActiveDrones.Count() + "] Drones Launched");
                _States.CurrentDroneState = DroneState.Fighting;
                return true;
            }

            return true;
        }

        private static bool OutOfDronesDronesState()
        {
            if (UseDrones && Settings.Instance.CharacterMode == "CombatMissions" &&
                _States.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
            {
                if (Statistics.OutOfDronesCount >= 3)
                {
                    Logging.Logging.Log("We are Out of Drones! AGAIN - Headed back to base to stay!");
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    Statistics.MissionCompletionErrors = 10;
                        //this effectively will stop questor in station so we do not try to do this mission again, this needs human intervention if we have lots this many drones
                    Statistics.OutOfDronesCount++;
                }

                Logging.Logging.Log("We are Out of Drones! - Headed back to base to Re-Arm");
                _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                Statistics.OutOfDronesCount++;
                return true;
            }

            return true;
        }

        private static bool FightingDronesState()
        {
            if (Logging.Logging.DebugDrones)
                Logging.Logging.Log("Should we recall our drones? This is a possible list of reasons why we should");

            if (!ActiveDrones.Any())
            {
                Logging.Logging.Log("Apparently we have lost all our drones");
                _States.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (Combat.PotentialCombatTargets.Any(pt => pt.IsWarpScramblingMe))
            {
                var WarpScrambledBy = Cache.Instance.Targets.OrderBy(d => d.Distance).ThenByDescending(i => i.IsWarpScramblingMe).FirstOrDefault();
                if (WarpScrambledBy != null && DateTime.UtcNow > _nextWarpScrambledWarning)
                {
                    _nextWarpScrambledWarning = DateTime.UtcNow.AddSeconds(20);
                    Logging.Logging.Log("We are scrambled by: [" + Logging.Logging.White + WarpScrambledBy.Name + Logging.Logging.Orange + "][" + Logging.Logging.White +
                        Math.Round(WarpScrambledBy.Distance, 0) + Logging.Logging.Orange + "][" + Logging.Logging.White + WarpScrambledBy.Id +
                        Logging.Logging.Orange + "]");
                    WarpScrambled = true;
                }
            }
            else
            {
                //Logging.Log("Drones: We are not warp scrambled at the moment...");
                WarpScrambled = false;
            }

            if (ShouldWeRecallDrones())
            {
                Statistics.DroneRecalls++;
                _States.CurrentDroneState = DroneState.Recalling;
                return true;
            }

            if (Logging.Logging.DebugDrones) Logging.Logging.Log("EngageTarget(); - before");

            EngageTarget();

            if (Logging.Logging.DebugDrones) Logging.Logging.Log("EngageTarget(); - after");
            // We lost a drone and did not recall, assume panicking and launch (if any) additional drones
            if (ActiveDrones.Count() < _lastDroneCount)
            {
                _States.CurrentDroneState = DroneState.Launch;
            }

            return true;
        }

        private static bool RecallingDronesState()
        {
            // Give recall command every x seconds (default is 15)
            if (DateTime.UtcNow.Subtract(_lastRecallCommand).TotalSeconds > Time.Instance.RecallDronesDelayBetweenRetries + Cache.Instance.RandomNumber(0, 2))
            {
                Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                LastTargetIDDronesEngaged = null;
                _lastRecallCommand = DateTime.UtcNow;
                return true;
            }

            // Are we done?
            if (!ActiveDrones.Any())
            {
                _lastRecall = DateTime.UtcNow;
                _nextDroneAction = DateTime.UtcNow.AddSeconds(3);
                if (!UseDrones)
                {
                    _States.CurrentDroneState = DroneState.Idle;
                    return false;
                }

                _States.CurrentDroneState = DroneState.WaitingForTargets;
                return true;
            }

            return true;
        }

        public static void ProcessState()
        {
            try
            {
                if (!OnEveryDroneProcessState()) return;

                switch (_States.CurrentDroneState)
                {
                    case DroneState.WaitingForTargets:
                        if (!WaitingForTargetsDroneState()) return;
                        break;

                    case DroneState.Launch:
                        if (!LaunchDronesState()) return;
                        break;

                    case DroneState.Launching:
                        if (!LaunchingDronesState()) return;
                        break;

                    case DroneState.OutOfDrones:
                        if (!OutOfDronesDronesState()) return;
                        break;

                    case DroneState.Fighting:
                        if (!FightingDronesState()) return;
                        break;

                    case DroneState.Recalling:
                        if (!RecallingDronesState()) return;
                        break;

                    case DroneState.Idle:
                        if (!IdleDroneState()) return;
                        break;
                }

                // Update health values
                _activeDronesShieldTotalOnLastPulse = GetActiveDroneShieldTotal();
                _activeDronesArmorTotalOnLastPulse = GetActiveDroneArmorTotal();
                _activeDronesStructureTotalOnLastPulse = GetActiveDroneStructureTotal();
                _activeDronesShieldPercentageOnLastPulse = GetActiveDroneShieldPercentage();
                _activeDronesArmorPercentageOnLastPulse = GetActiveDroneArmorPercentage();
                _activeDronesStructurePercentageOnLastPulse = GetActiveDroneStructurePercentage();
                _lastDroneCount = ActiveDrones.Count();
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return;
            }
        }

        /// <summary>
        ///     Invalidate the cached items every pulse (called from cache.invalidatecache, which itself is called every frame in
        ///     questor.cs)
        /// </summary>
        public static void InvalidateCache()
        {
            try
            {
                //
                // this list of variables is cleared every pulse.
                //
                _activeDrones = null;
                _droneBay = null;
                _dronePriorityEntities = null;
                _maxDroneRange = null;
                _preferredDroneTarget = null;

                if (_dronePriorityTargets != null && _dronePriorityTargets.Any())
                {
                    _dronePriorityTargets.ForEach(pt => pt.ClearCache());
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }
    }
}