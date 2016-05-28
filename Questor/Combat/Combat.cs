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
using Questor.Modules.Activities;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Combat
{
    /// <summary>
    ///     The combat class will target and kill any NPC that is targeting the questor.
    ///     It will also kill any NPC that is targeted but not aggressive  toward the questor.
    /// </summary>
    public static class Combat
    {
        //~Combat()
        //{
        //    Interlocked.Decrement(ref CombatInstances);
        //}

        private static string _combatShipName;
        private static bool _isJammed;
        private static int _weaponNumber;
        private static DateTime _lastCombatProcessState;
        private static DateTime _lastReloadAll;
        //private static int _reloadAllIteration;
        public static IEnumerable<EntityCache> highValueTargetsTargeted;
        public static IEnumerable<EntityCache> lowValueTargetsTargeted;
        public static int? maxHighValueTargets;
        public static int? maxLowValueTargets;
        //public static int CombatInstances = 0;
        private static int icount = 0;
        private static bool _killSentries;


        /// <summary>
        ///     Targeted by cache //cleared in InvalidateCache
        /// </summary>
        private static List<EntityCache> _targetedBy;

        /// <summary>
        ///     Aggressed cache //cleared in InvalidateCache
        /// </summary>
        private static List<EntityCache> _aggressed;


        private static double? _maxrange;

        private static double? _maxTargetRange;

        public static long? PreferredPrimaryWeaponTargetID;
        private static EntityCache _preferredPrimaryWeaponTarget;

        private static List<PriorityTarget> _primaryWeaponPriorityTargetsPerFrameCaching;

        private static List<PriorityTarget> _primaryWeaponPriorityTargets;

        private static IEnumerable<EntityCache> _primaryWeaponPriorityEntities;

        /// <summary>
        ///     _CombatTarget Entities cache - list of things we have targeted to kill //cleared in InvalidateCache
        /// </summary>
        private static List<EntityCache> _combatTargets;

        /// <summary>
        ///     _PotentialCombatTarget Entities cache - list of things we can kill //cleared in InvalidateCache
        /// </summary>
        private static List<EntityCache> _potentialCombatTargets;

        public static bool? _doWeCurrentlyHaveTurretsMounted;


        public static bool? _doWeCurrentlyHaveProjectilesMounted;


        public static EntityCache LastTargetPrimaryWeaponsWereShooting = null;

        static Combat()
        {
            //Interlocked.Increment(ref CombatInstances);
            Ammo = new List<Ammo>();
        }

        public static string CombatShipName
        {
            get
            {
                if (MissionSettings.MissionSpecificShip != null)
                {
                    return MissionSettings.MissionSpecificShip;
                }

                if (MissionSettings.FactionSpecificShip != null)
                {
                    return MissionSettings.FactionSpecificShip;
                }

                return _combatShipName;
            }
            set { _combatShipName = value; }
        }

        private static int MaxCharges { get; set; }

        private static int maxTotalTargets
        {
            get
            {
                try
                {
                    return maxHighValueTargets + maxLowValueTargets ?? 2;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                    return 2;
                }
            }
        }

        public static int NosDistance { get; set; }
        public static int RemoteRepairDistance { get; set; }
        public static List<EntityCache> TargetingMe { get; set; }
        public static List<EntityCache> NotYetTargetingMeAndNotYetTargeted { get; set; }

        public static bool KillSentries
        {
            get
            {
                if (MissionSettings.MissionKillSentries != null)
                    return (bool) MissionSettings.MissionKillSentries;
                return _killSentries;
            }
            set { _killSentries = value; }
        }

        public static bool DontShootFrigatesWithSiegeorAutoCannons { get; set; }
        public static int WeaponGroupId { get; set; }
        //public static int MaximumHighValueTargets { get; set; }
        //public static int MaximumLowValueTargets { get; set; }
        public static int MinimumAmmoCharges { get; set; }
        public static List<Ammo> Ammo { get; set; }

        public static int MinimumTargetValueToConsiderTargetAHighValueTarget { get; set; }
        public static int MaximumTargetValueToConsiderTargetALowValueTarget { get; set; }
        public static bool SelectAmmoToUseBasedOnShipSize { get; set; }
        public static int DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage { get; set; }
        public static double DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons { get; set; } //also requires SpeedFrigatesShouldBeIgnoredByMainWeapons
        public static double SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons { get; set; } //also requires DistanceFrigatesShouldBeIgnoredByMainWeapons
        public static bool AddWarpScramblersToPrimaryWeaponsPriorityTargetList { get; set; }
        public static bool AddWebifiersToPrimaryWeaponsPriorityTargetList { get; set; }
        public static bool AddDampenersToPrimaryWeaponsPriorityTargetList { get; set; }
        public static bool AddNeutralizersToPrimaryWeaponsPriorityTargetList { get; set; }
        public static bool AddTargetPaintersToPrimaryWeaponsPriorityTargetList { get; set; }
        public static bool AddECMsToPrimaryWeaponsPriorityTargetList { get; set; }
        public static bool AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList { get; set; }
        public static double ListPriorityTargetsEveryXSeconds { get; set; }
        public static double InsideThisRangeIsHardToTrack { get; set; }

        public static IEnumerable<EntityCache> TargetedBy
        {
            get { return _targetedBy ?? (_targetedBy = PotentialCombatTargets.Where(e => e.IsTargetedBy).ToList()); }
        }

        public static IEnumerable<EntityCache> Aggressed
        {
            get { return _aggressed ?? (_aggressed = PotentialCombatTargets.Where(e => e.IsAttacking).ToList()); }
        }

        //
        // entities that have been locked (or are being locked now)
        // entities that are IN range
        // entities that eventually we want to shoot (and now that they are locked that will happen shortly)
        //
        public static IEnumerable<EntityCache> combatTargets
        {
            get
            {
                if (_combatTargets == null)
                {
                    //List<EntityCache>
                    if (Cache.Instance.InSpace)
                    {
                        if (_combatTargets == null)
                        {
                            var targets = new List<EntityCache>();
                            targets.AddRange(Cache.Instance.Targets);
                            targets.AddRange(Cache.Instance.Targeting);

                            _combatTargets = targets.Where(e => e.CategoryId == (int) CategoryID.Entity && e.Distance < (double) Distances.OnGridWithMe
                                                                && !e.IsIgnored
                                                                && (!e.IsSentry || (e.IsSentry && KillSentries) || (e.IsSentry && e.IsEwarTarget))
                                                                && (e.IsNpc || e.IsNpcByGroupID)
                                                                && e.Distance < MaxRange
                                                                && !e.IsContainer
                                                                && !e.IsFactionWarfareNPC
                                                                && !e.IsEntityIShouldLeaveAlone
                                                                && !e.IsBadIdea
                                                                && !e.IsCelestial
                                                                && !e.IsAsteroid)
                                .ToList();

                            return _combatTargets;
                        }

                        return _combatTargets;
                    }

                    return Cache.Instance.Targets.ToList();
                }

                return _combatTargets;
            }
        }

        //
        // entities that have potentially not been locked yet
        // entities that may not be in range yet
        // entities that eventually we want to shoot
        //
        public static IEnumerable<EntityCache> PotentialCombatTargets
        {
            get
            {
                if (_potentialCombatTargets == null)
                {
                    //List<EntityCache>
                    if (Cache.Instance.InSpace)
                    {
                        _potentialCombatTargets = Cache.Instance.EntitiesOnGrid.Where(e => e.CategoryId == (int) CategoryID.Entity
                                                                                           && !e.IsIgnored
                                                                                           &&
                                                                                           (!e.IsSentry || (e.IsSentry && KillSentries) ||
                                                                                            (e.IsSentry && e.IsEwarTarget))
                                                                                           && (e.IsNpcByGroupID || e.IsAttacking)
                            //|| e.isPreferredPrimaryWeaponTarget || e.IsPrimaryWeaponKillPriority || e.IsDronePriorityTarget || e.isPreferredDroneTarget) //|| e.IsNpc)
                            //&& !e.IsTarget
                                                                                           && !e.IsContainer
                                                                                           && !e.IsFactionWarfareNPC
                                                                                           && !e.IsEntityIShouldLeaveAlone
                                                                                           && !e.IsBadIdea // || e.IsBadIdea && e.IsAttacking)
                                                                                           && (!e.IsPlayer || e.IsPlayer && e.IsAttacking)
                                                                                           && !e.IsMiscJunk
                                                                                           && (!e.IsLargeCollidable || e.IsPrimaryWeaponPriorityTarget)
                            )
                            .ToList();


                        if (_potentialCombatTargets == null || !_potentialCombatTargets.Any())
                        {
                            _potentialCombatTargets = new List<EntityCache>();
                        }

                        return _potentialCombatTargets;
                    }

                    return new List<EntityCache>();
                }

                return _potentialCombatTargets;
            }
        }

        public static double MaxRange
        {
            get
            {
                if (_maxrange == null)
                {
                    _maxrange = Math.Min(Cache.Instance.WeaponRange, MaxTargetRange);
                    return _maxrange ?? 0;
                }

                return _maxrange ?? 0;
            }
        }

        public static double MaxTargetRange
        {
            get
            {
                if (_maxTargetRange == null)
                {
                    _maxTargetRange = Cache.Instance.ActiveShip.MaxTargetRange;
                    return _maxTargetRange ?? 0;
                }

                return _maxTargetRange ?? 0;
            }
        }

        public static double LowValueTargetsHaveToBeWithinDistance
        {
            get
            {
                if (Drones.UseDrones && Drones.MaxDroneRange != 0)
                {
                    return Drones.MaxDroneRange;
                }

                //
                // if we are not using drones return min range (Weapons or targeting range whatever is lower)
                //
                return MaxRange;
            }
        }

        public static EntityCache PreferredPrimaryWeaponTarget
        {
            get
            {
                if (_preferredPrimaryWeaponTarget == null)
                {
                    if (PreferredPrimaryWeaponTargetID != null)
                    {
                        _preferredPrimaryWeaponTarget = Cache.Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == PreferredPrimaryWeaponTargetID);

                        return _preferredPrimaryWeaponTarget ?? null;
                    }

                    return null;
                }

                return _preferredPrimaryWeaponTarget;
            }
            set
            {
                if (value == null)
                {
                    if (_preferredPrimaryWeaponTarget != null)
                    {
                        _preferredPrimaryWeaponTarget = null;
                        PreferredPrimaryWeaponTargetID = null;
                        if (Logging.Logging.DebugPreferredPrimaryWeaponTarget)
                            Logging.Logging.Log("[ null ]");
                        return;
                    }
                }
                else if ((_preferredPrimaryWeaponTarget != null && _preferredPrimaryWeaponTarget.Id != value.Id) || _preferredPrimaryWeaponTarget == null)
                {
                    _preferredPrimaryWeaponTarget = value;
                    PreferredPrimaryWeaponTargetID = value.Id;
                    if (Logging.Logging.DebugPreferredPrimaryWeaponTarget)
                        Logging.Logging.Log(value.Name + " [" + value.MaskedId + "][" + Math.Round(value.Distance / 1000, 0) + "k] isTarget [" + value.IsTarget + "]");
                    return;
                }

                //if (Logging.DebugPreferredPrimaryWeaponTarget) Logging.Log("PreferredPrimaryWeaponTarget", "Cache.Instance._preferredPrimaryWeaponTarget [" + Cache.Instance._preferredPrimaryWeaponTarget.Name + "] is already set (no need to change)", Logging.Debug);
                return;
            }
        }

        public static List<PriorityTarget> PrimaryWeaponPriorityTargets
        {
            get
            {
                try
                {
                    if (_primaryWeaponPriorityTargetsPerFrameCaching == null)
                    {
                        //
                        // remove targets that no longer exist
                        //
                        if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any())
                        {
                            foreach (var _primaryWeaponPriorityTarget in _primaryWeaponPriorityTargets)
                            {
                                if (Cache.Instance.EntitiesOnGrid.All(e => e.Id != _primaryWeaponPriorityTarget.EntityID))
                                {
                                    Logging.Logging.Log("Remove Target that is no longer in the Entities list [" + _primaryWeaponPriorityTarget.Name + "]ID[" +
                                        _primaryWeaponPriorityTarget.MaskedID + "] PriorityLevel [" + _primaryWeaponPriorityTarget.PrimaryWeaponPriority + "]");
                                    _primaryWeaponPriorityTargets.Remove(_primaryWeaponPriorityTarget);
                                    break;
                                }
                            }

                            _primaryWeaponPriorityTargetsPerFrameCaching = _primaryWeaponPriorityTargets;
                            return _primaryWeaponPriorityTargets;
                        }

                        //
                        // initialize a fresh list - to be filled in during panic (updated every tick)
                        //
                        _primaryWeaponPriorityTargets = new List<PriorityTarget>();
                        _primaryWeaponPriorityTargetsPerFrameCaching = _primaryWeaponPriorityTargets;
                        return _primaryWeaponPriorityTargets;
                    }

                    return _primaryWeaponPriorityTargetsPerFrameCaching;
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
            set { _primaryWeaponPriorityTargets = value; }
        }

        public static IEnumerable<EntityCache> PrimaryWeaponPriorityEntities
        {
            get
            {
                try
                {
                    //
                    // every frame re-populate the PrimaryWeaponPriorityEntities from the list of IDs we have tucked away in PrimaryWeaponPriorityEntities
                    // this occurs because in Invalidatecache() we are, necessarily,  clearing this every frame!
                    //
                    if (_primaryWeaponPriorityEntities == null)
                    {
                        if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any())
                        {
                            _primaryWeaponPriorityEntities =
                                PrimaryWeaponPriorityTargets.OrderByDescending(pt => pt.PrimaryWeaponPriority)
                                    .ThenBy(pt => pt.Entity.Distance)
                                    .Select(pt => pt.Entity)
                                    .ToList();
                            return _primaryWeaponPriorityEntities;
                        }

                        if (Logging.Logging.DebugAddPrimaryWeaponPriorityTarget)
                            Logging.Logging.Log("if (_primaryWeaponPriorityTargets.Any()) none available yet");
                        _primaryWeaponPriorityEntities = new List<EntityCache>();
                        return _primaryWeaponPriorityEntities;
                    }

                    //
                    // if we have already populated the list this frame return the list we already generated
                    //
                    return _primaryWeaponPriorityEntities;
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

        /// <summary>
        ///     Remove targets from priority list
        /// </summary>
        /// <param name="targets"></param>
        public static bool RemovePrimaryWeaponPriorityTargets(List<EntityCache> targets)
        {
            try
            {
                targets = targets.ToList();

                if (targets.Any() && _primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any() &&
                    _primaryWeaponPriorityTargets.Any(pt => targets.Any(t => t.Id == pt.EntityID)))
                {
                    _primaryWeaponPriorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return false;
        }

        public static void AddPrimaryWeaponPriorityTarget(EntityCache ewarEntity, PrimaryWeaponPriority priority, string module,
            bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if ((ewarEntity.IsIgnored) || PrimaryWeaponPriorityTargets.Any(p => p.EntityID == ewarEntity.Id))
                {
                    if (Logging.Logging.DebugAddPrimaryWeaponPriorityTarget)
                        Logging.Logging.Log("if ((target.IsIgnored) || PrimaryWeaponPriorityTargets.Any(p => p.Id == target.Id)) continue");
                    return;
                }

                if (AddEwarTypeToPriorityTargetList)
                {
                    //
                    // Primary Weapons
                    //
                    if (DoWeCurrentlyHaveTurretsMounted() && (ewarEntity.IsNPCFrigate || ewarEntity.IsFrigate))
                        //we use turrets, and this PrimaryWeaponPriorityTarget is a frigate
                    {
                        if (!ewarEntity.IsTooCloseTooFastTooSmallToHit)
                        {
                            if (PrimaryWeaponPriorityTargets.All(e => e.EntityID != ewarEntity.Id))
                            {
                                Logging.Logging.Log("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + "m/s] Distance [" +
                                    Math.Round(ewarEntity.Distance / 1000, 2) + "k] [ID: " + ewarEntity.MaskedId + "] as a PrimaryWeaponPriorityTarget [" +
                                    priority.ToString() + "]");
                                _primaryWeaponPriorityTargets.Add(new PriorityTarget
                                {
                                    Name = ewarEntity.Name,
                                    EntityID = ewarEntity.Id,
                                    PrimaryWeaponPriority = priority
                                });
                                if (Logging.Logging.DebugKillAction)
                                {
                                    Logging.Logging.Log("Entering StatisticsState.ListPrimaryWeaponPriorityTargets");
                                    _States.CurrentStatisticsState = StatisticsState.ListPrimaryWeaponPriorityTargets;
                                }
                            }
                        }

                        return;
                    }

                    if (PrimaryWeaponPriorityTargets.All(e => e.EntityID != ewarEntity.Id))
                    {
                        Logging.Logging.Log("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + "m/s] Distance [" +
                            Math.Round(ewarEntity.Distance / 1000, 2) + "] [ID: " + ewarEntity.MaskedId + "] as a PrimaryWeaponPriorityTarget [" +
                            priority.ToString() + "]");
                        _primaryWeaponPriorityTargets.Add(new PriorityTarget
                        {
                            Name = ewarEntity.Name,
                            EntityID = ewarEntity.Id,
                            PrimaryWeaponPriority = priority
                        });
                        if (Logging.Logging.DebugKillAction)
                        {
                            Logging.Logging.Log("Entering StatisticsState.ListPrimaryWeaponPriorityTargets");
                            _States.CurrentStatisticsState = StatisticsState.ListPrimaryWeaponPriorityTargets;
                        }
                    }

                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return;
        }

        public static void AddPrimaryWeaponPriorityTargets(IEnumerable<EntityCache> ewarEntities, PrimaryWeaponPriority priority, string module,
            bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                ewarEntities = ewarEntities.ToList();
                if (ewarEntities.Any())
                {
                    foreach (var ewarEntity in ewarEntities)
                    {
                        AddPrimaryWeaponPriorityTarget(ewarEntity, priority, module, AddEwarTypeToPriorityTargetList);
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return;
        }

        public static void AddPrimaryWeaponPriorityTargetsByName(string stringEntitiesToAdd)
        {
            try
            {
                if (Cache.Instance.EntitiesOnGrid.Any(r => r.Name == stringEntitiesToAdd))
                {
                    IEnumerable<EntityCache> entitiesToAdd = Cache.Instance.EntitiesOnGrid.Where(t => t.Name == stringEntitiesToAdd).ToList();
                    if (entitiesToAdd.Any())
                    {
                        foreach (var entityToAdd in entitiesToAdd)
                        {
                            AddPrimaryWeaponPriorityTarget(entityToAdd, PrimaryWeaponPriority.PriorityKillTarget, "AddPWPTByName");
                            continue;
                        }

                        return;
                    }

                    Logging.Logging.Log("[" + stringEntitiesToAdd + "] was not found.");
                    return;
                }

                var EntitiesOnGridCount = 0;
                if (Cache.Instance.EntitiesOnGrid.Any())
                {
                    EntitiesOnGridCount = Cache.Instance.EntitiesOnGrid.Count();
                }

                var EntitiesCount = 0;
                if (Cache.Instance.EntitiesOnGrid.Any())
                {
                    EntitiesCount = Cache.Instance.EntitiesOnGrid.Count();
                }

                Logging.Logging.Log("[" + stringEntitiesToAdd + "] was not found. [" + EntitiesOnGridCount + "] entities on grid [" + EntitiesCount + "] entities");
                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
            }

            return;
        }

        public static void RemovePrimaryWeaponPriorityTargetsByName(string stringEntitiesToRemove)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToRemove = Cache.Instance.EntitiesByName(stringEntitiesToRemove, Cache.Instance.EntitiesOnGrid).ToList();
                if (entitiesToRemove.Any())
                {
                    Logging.Logging.Log("removing [" + stringEntitiesToRemove + "] from the PWPT List");
                    RemovePrimaryWeaponPriorityTargets(entitiesToRemove.ToList());
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

        public static void AddWarpScramblerByName(string stringEntitiesToAdd, int numberToIgnore = 0, bool notTheClosest = false)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToAdd =
                    Cache.Instance.EntitiesByName(stringEntitiesToAdd, Cache.Instance.EntitiesOnGrid).OrderBy(k => k.Distance).ToList();
                if (notTheClosest)
                {
                    entitiesToAdd = entitiesToAdd.OrderByDescending(m => m.Distance);
                }

                if (entitiesToAdd.Any())
                {
                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        if (numberToIgnore > 0)
                        {
                            numberToIgnore--;
                            continue;
                        }

                        Logging.Logging.Log("adding [" + entityToAdd.Name + "][" + Math.Round(entityToAdd.Distance / 1000, 0) + "k][" + entityToAdd.MaskedId +
                            "] to the WarpScrambler List");
                        if (!Cache.Instance.ListOfWarpScramblingEntities.Contains(entityToAdd.Id))
                        {
                            Cache.Instance.ListOfWarpScramblingEntities.Add(entityToAdd.Id);
                        }
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
            }
        }

        public static void AddWebifierByName(string stringEntitiesToAdd, int numberToIgnore = 0, bool notTheClosest = false)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToAdd =
                    Cache.Instance.EntitiesByName(stringEntitiesToAdd, Cache.Instance.EntitiesOnGrid).OrderBy(j => j.Distance).ToList();
                if (notTheClosest)
                {
                    entitiesToAdd = entitiesToAdd.OrderByDescending(e => e.Distance);
                }

                if (entitiesToAdd.Any())
                {
                    foreach (var entityToAdd in entitiesToAdd)
                    {
                        if (numberToIgnore > 0)
                        {
                            numberToIgnore--;
                            continue;
                        }
                        Logging.Logging.Log("adding [" + entityToAdd.Name + "][" + Math.Round(entityToAdd.Distance / 1000, 0) + "k][" + entityToAdd.MaskedId +
                            "] to the Webifier List");
                        if (!Cache.Instance.ListofWebbingEntities.Contains(entityToAdd.Id))
                        {
                            Cache.Instance.ListofWebbingEntities.Add(entityToAdd.Id);
                        }
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
            }
        }

        public static bool DoWeCurrentlyHaveTurretsMounted()
        {
            try
            {
                if (_doWeCurrentlyHaveTurretsMounted == null)
                {
                    //int ModuleNumber = 0;
                    foreach (var m in Cache.Instance.Modules)
                    {
                        if (m.GroupId == (int) Group.ProjectileWeapon
                            || m.GroupId == (int) Group.EnergyWeapon
                            || m.GroupId == (int) Group.HybridWeapon
                            //|| m.GroupId == (int)Group.CruiseMissileLaunchers
                            //|| m.GroupId == (int)Group.RocketLaunchers
                            //|| m.GroupId == (int)Group.StandardMissileLaunchers
                            //|| m.GroupId == (int)Group.TorpedoLaunchers
                            //|| m.GroupId == (int)Group.AssaultMissilelaunchers
                            //|| m.GroupId == (int)Group.HeavyMissilelaunchers
                            //|| m.GroupId == (int)Group.DefenderMissilelaunchers
                            )
                        {
                            _doWeCurrentlyHaveTurretsMounted = true;
                            return _doWeCurrentlyHaveTurretsMounted ?? true;
                        }

                        continue;
                    }

                    _doWeCurrentlyHaveTurretsMounted = false;
                    return _doWeCurrentlyHaveTurretsMounted ?? false;
                }

                return _doWeCurrentlyHaveTurretsMounted ?? false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            return false;
        }

        public static bool DoWeCurrentlyProjectilesMounted()
        {
            try
            {
                if (_doWeCurrentlyHaveProjectilesMounted == null)
                {
                    //int ModuleNumber = 0;
                    foreach (var m in Cache.Instance.Modules)
                    {
                        if (m.GroupId == (int) Group.ProjectileWeapon
                            )
                        {
                            _doWeCurrentlyHaveProjectilesMounted = true;
                            return _doWeCurrentlyHaveProjectilesMounted ?? true;
                        }

                        continue;
                    }

                    _doWeCurrentlyHaveProjectilesMounted = false;
                    return _doWeCurrentlyHaveProjectilesMounted ?? false;
                }

                return _doWeCurrentlyHaveProjectilesMounted ?? false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            return false;
        }

        public static EntityCache CurrentWeaponTarget()
        {
            // Find the first active weapon's target
            EntityCache _currentWeaponTarget = null;
            double OptimalOfWeapon = 0;
            double FallOffOfWeapon = 0;

            try
            {
                // Find the target associated with the weapon
                var weapon = Cache.Instance.Weapons.FirstOrDefault(m => m.IsOnline
                                                                        && !m.IsReloadingAmmo
                                                                        && !m.IsChangingAmmo
                                                                        && m.IsActive);
                if (weapon != null)
                {
                    _currentWeaponTarget = Cache.Instance.EntityById(weapon.TargetId);

                    //
                    // in a perfect world we'd always use the same guns / missiles across the board, for those that do not this will at least come up with sane numbers
                    //
                    if (OptimalOfWeapon <= 1)
                    {
                        OptimalOfWeapon = Math.Min(OptimalOfWeapon, weapon.OptimalRange);
                    }

                    if (FallOffOfWeapon <= 1)
                    {
                        FallOffOfWeapon = Math.Min(FallOffOfWeapon, weapon.FallOff);
                    }

                    if (_currentWeaponTarget != null && _currentWeaponTarget.IsReadyToShoot)
                    {
                        return _currentWeaponTarget;
                    }

                    return null;
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("exception [" + exception + "]");
            }

            return null;
        }

        public static EntityCache FindPrimaryWeaponPriorityTarget(EntityCache currentTarget, PrimaryWeaponPriority priorityType,
            bool AddECMTypeToPrimaryWeaponPriorityTargetList, double Distance, bool FindAUnTargetedEntity = true)
        {
            if (AddECMTypeToPrimaryWeaponPriorityTargetList)
            {
                //if (Logging.DebugGetBestTarget) Logging.Log(callingroutine + " Debug: GetBestTarget", "Checking for Neutralizing priority targets (currentTarget first)", Logging.Teal);
                // Choose any Neutralizing primary weapon priority targets
                try
                {
                    EntityCache target = null;
                    try
                    {
                        if (PrimaryWeaponPriorityEntities.Any(pt => pt.PrimaryWeaponPriorityLevel == priorityType))
                        {
                            target =
                                PrimaryWeaponPriorityEntities.Where(
                                    pt =>
                                        ((FindAUnTargetedEntity || pt.IsReadyToShoot) && currentTarget != null && pt.Id == currentTarget.Id &&
                                         pt.Distance < Distance && pt.IsActivePrimaryWeaponEwarType == priorityType && !pt.IsTooCloseTooFastTooSmallToHit)
                                        ||
                                        ((FindAUnTargetedEntity || pt.IsReadyToShoot) && pt.Distance < Distance && pt.PrimaryWeaponPriorityLevel == priorityType &&
                                         !pt.IsTooCloseTooFastTooSmallToHit))
                                    .OrderByDescending(pt => pt.IsReadyToShoot)
                                    .ThenByDescending(pt => pt.IsCurrentTarget)
                                    .ThenByDescending(pt => !pt.IsNPCFrigate)
                                    .ThenByDescending(pt => pt.IsInOptimalRange)
                                    .ThenBy(pt => (pt.ShieldPct + pt.ArmorPct + pt.StructurePct))
                                    .ThenBy(pt => pt.Distance)
                                    .FirstOrDefault();
                        }
                    }
                    catch (NullReferenceException)
                    {
                    } // Not sure why this happens, but seems to be no problem

                    if (target != null)
                    {
                        if (!FindAUnTargetedEntity)
                        {
                            //if (Logging.DebugGetBestTarget) Logging.Log(callingroutine + " Debug: GetBestTarget", "NeutralizingPrimaryWeaponPriorityTarget [" + NeutralizingPriorityTarget.Name + "][" + Math.Round(NeutralizingPriorityTarget.Distance / 1000, 2) + "k][" + Cache.Instance.MaskedID(NeutralizingPriorityTarget.Id) + "] GroupID [" + NeutralizingPriorityTarget.GroupId + "]", Logging.Debug);
                            Logging.Logging.Log("if (!FindAUnTargetedEntity) Combat.PreferredPrimaryWeaponTargetID = [ " + target.Name + "][" + target.MaskedId + "]");
                            PreferredPrimaryWeaponTarget = target;
                            Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
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

        public static EntityCache FindCurrentTarget()
        {
            try
            {
                EntityCache currentTarget = null;

                if (currentTarget == null)
                {
                    if (CurrentWeaponTarget() != null
                        && CurrentWeaponTarget().IsReadyToShoot
                        && !CurrentWeaponTarget().IsIgnored)
                    {
                        LastTargetPrimaryWeaponsWereShooting = CurrentWeaponTarget();
                        currentTarget = LastTargetPrimaryWeaponsWereShooting;
                    }

                    if (DateTime.UtcNow < Time.Instance.LastPreferredPrimaryWeaponTargetDateTime.AddSeconds(6) &&
                        (PreferredPrimaryWeaponTarget != null && Cache.Instance.EntitiesOnGrid.Any(t => t.Id == PreferredPrimaryWeaponTargetID)))
                    {
                        if (Logging.Logging.DebugGetBestTarget)
                            Logging.Logging.Log("We have a PreferredPrimaryWeaponTarget [" + PreferredPrimaryWeaponTarget.Name + "][" +
                                Math.Round(PreferredPrimaryWeaponTarget.Distance / 1000, 0) + "k] that was chosen less than 6 sec ago, and is still alive.");
                    }
                }

                return currentTarget;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return null;
            }
        }

        public static bool CheckForPrimaryWeaponPriorityTargetsInOrder(EntityCache currentTarget, double distance)
        {
            try
            {
                // Do we have ANY warp scrambling entities targeted starting with currentTarget
                // this needs Settings.Instance.AddWarpScramblersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (
                    FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.WarpScrambler, AddWarpScramblersToPrimaryWeaponsPriorityTargetList,
                        distance) != null)
                    return true;

                // Do we have ANY ECM entities targeted starting with currentTarget
                // this needs Settings.Instance.AddECMsToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Jamming, AddECMsToPrimaryWeaponsPriorityTargetList, distance) != null)
                    return true;

                // Do we have ANY tracking disrupting entities targeted starting with currentTarget
                // this needs Settings.Instance.AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (
                    FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.TrackingDisrupting,
                        AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList, distance) != null)
                    return true;

                // Do we have ANY Neutralizing entities targeted starting with currentTarget
                // this needs Settings.Instance.AddNeutralizersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (
                    FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Neutralizing, AddNeutralizersToPrimaryWeaponsPriorityTargetList,
                        distance) != null)
                    return true;

                // Do we have ANY Target Painting entities targeted starting with currentTarget
                // this needs Settings.Instance.AddTargetPaintersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (
                    FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.TargetPainting, AddTargetPaintersToPrimaryWeaponsPriorityTargetList,
                        distance) != null)
                    return true;

                // Do we have ANY Sensor Dampening entities targeted starting with currentTarget
                // this needs Settings.Instance.AddDampenersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Dampening, AddDampenersToPrimaryWeaponsPriorityTargetList, distance) !=
                    null)
                    return true;

                // Do we have ANY Webbing entities targeted starting with currentTarget
                // this needs Settings.Instance.AddWebifiersToPrimaryWeaponsPriorityTargetList true, otherwise they will just get handled in any order below...
                if (FindPrimaryWeaponPriorityTarget(currentTarget, PrimaryWeaponPriority.Webbing, AddWebifiersToPrimaryWeaponsPriorityTargetList, distance) !=
                    null)
                    return true;

                return false;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return false;
            }
        }

        /// <summary>
        ///     Return the best possible target (based on current target, distance and low value first)
        /// </summary>
        /// <param name="_potentialTargets"></param>
        /// <param name="distance"></param>
        /// <param name="lowValueFirst"></param>
        /// <param name="callingroutine"> </param>
        /// <returns></returns>
        public static bool GetBestPrimaryWeaponTarget(double distance, bool lowValueFirst, string callingroutine, List<EntityCache> _potentialTargets = null)
        {
            if (Logging.Logging.DebugDisableGetBestTarget)
            {
                return true;
            }

            if (Logging.Logging.DebugGetBestTarget)
                Logging.Logging.Log("Attempting to get Best Target");

            if (DateTime.UtcNow < Time.Instance.NextGetBestCombatTarget)
            {
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("No need to run GetBestTarget again so soon. We only want to run once per tick");
                return false;
            }

            Time.Instance.NextGetBestCombatTarget = DateTime.UtcNow.AddMilliseconds(800);

            //if (!Cache.Instance.Targets.Any()) //&& _potentialTargets == null )
            //{
            //    if (Logging.DebugGetBestTarget) Logging.Log(callingroutine + " Debug: GetBestTarget (Weapons):", "We have no locked targets and [" + Cache.Instance.Targeting.Count() + "] targets being locked atm", Logging.Teal);
            //    return false;
            //}

            var currentTarget = FindCurrentTarget();

            //We need to make sure that our current Preferred is still valid, if not we need to clear it out
            //This happens when we have killed the last thing within our range (or the last thing in the pocket)
            //and there is nothing to replace it with.
            //if (Combat.PreferredPrimaryWeaponTarget != null
            //    && Cache.Instance.Entities.All(t => t.Id != Instance.PreferredPrimaryWeaponTargetID))
            //{
            //    if (Logging.DebugGetBestTarget) Logging.Log("GetBestTarget", "PreferredPrimaryWeaponTarget is not valid, clearing it", Logging.White);
            //    Combat.PreferredPrimaryWeaponTarget = null;
            //}

            //
            // process the list of PrimaryWeaponPriorityTargets in this order... Eventually the order itself should be user selectable
            // this allow us to kill the most 'important' things doing e-war first instead of just handling them by range
            //

            //
            // if currentTarget set to something (not null) and it is actually an entity...
            //
            if (currentTarget != null && currentTarget.IsReadyToShoot)
            {
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("We have a target, testing conditions");

                #region Is our current target any other primary weapon priority target?

                //
                // Is our current target any other primary weapon priority target? AND if our target is just a PriorityKillTarget assume ALL E-war is more important.
                //
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("Checking Priority");
                if (PrimaryWeaponPriorityEntities.Any(pt => pt.IsReadyToShoot
                                                            && pt.Distance < MaxRange
                                                            && pt.IsCurrentTarget
                                                            && !currentTarget.IsHigherPriorityPresent))
                {
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("CurrentTarget [" + currentTarget.Name + "][" + Math.Round(currentTarget.Distance / 1000, 2) + "k][" + currentTarget.MaskedId +
                            "] GroupID [" + currentTarget.GroupId + "]");
                    PreferredPrimaryWeaponTarget = currentTarget;
                    Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target any other primary weapon priority target?

                #region Is our current target already in armor? keep shooting the same target if so...

                //
                // Is our current target already in armor? keep shooting the same target if so...
                //
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("Checking Low Health");
                if (currentTarget.IsEntityIShouldKeepShooting)
                {
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("CurrentTarget [" + currentTarget.Name + "][" + Math.Round(currentTarget.Distance / 1000, 2) + "k][" + currentTarget.MaskedId +
                            " GroupID [" + currentTarget.GroupId + "]] has less than 60% armor, keep killing this target");
                    PreferredPrimaryWeaponTarget = currentTarget;
                    Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target already in armor? keep shooting the same target if so...

                #region If none of the above matches, does our current target meet the conditions of being hittable and in range

                if (!currentTarget.IsHigherPriorityPresent)
                {
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("Does the currentTarget exist? can it be hit?");
                    if (currentTarget.IsReadyToShoot
                        && (!currentTarget.IsNPCFrigate || (!Drones.UseDrones && !currentTarget.IsTooCloseTooFastTooSmallToHit))
                        && currentTarget.Distance < MaxRange)
                    {
                        if (Logging.Logging.DebugGetBestTarget)
                            Logging.Logging.Log("if  the currentTarget exists and the target is the right size then continue shooting it;");
                        if (Logging.Logging.DebugGetBestTarget)
                            Logging.Logging.Log("currentTarget is [" + currentTarget.Name + "][" + Math.Round(currentTarget.Distance / 1000, 2) + "k][" + currentTarget.MaskedId +
                                "] GroupID [" + currentTarget.GroupId + "]");

                        PreferredPrimaryWeaponTarget = currentTarget;
                        Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                        return true;
                    }
                }

                #endregion
            }

            if (CheckForPrimaryWeaponPriorityTargetsInOrder(currentTarget, distance)) return true;

            #region Get the closest primary weapon priority target

            //
            // Get the closest primary weapon priority target
            //
            if (Logging.Logging.DebugGetBestTarget)
                Logging.Logging.Log("Checking Closest PrimaryWeaponPriorityTarget");
            EntityCache primaryWeaponPriorityTarget = null;
            try
            {
                if (PrimaryWeaponPriorityEntities != null && PrimaryWeaponPriorityEntities.Any())
                {
                    primaryWeaponPriorityTarget = PrimaryWeaponPriorityEntities.Where(p => p.Distance < MaxRange
                                                                                           && !p.IsIgnored
                                                                                           && p.IsReadyToShoot
                                                                                           &&
                                                                                           ((!p.IsNPCFrigate && !p.IsFrigate) ||
                                                                                            (!Drones.UseDrones && !p.IsTooCloseTooFastTooSmallToHit)))
                        .OrderByDescending(pt => pt.IsTargetedBy)
                        .ThenByDescending(pt => pt.IsCurrentTarget)
                        .ThenByDescending(pt => pt.IsInOptimalRange)
                        .ThenByDescending(pt => pt.IsEwarTarget)
                        .ThenBy(pt => pt.PrimaryWeaponPriorityLevel)
                        .ThenByDescending(pt => pt.TargetValue)
                        .ThenBy(pt => pt.Nearest5kDistance)
                        .FirstOrDefault();
                }
            }
            catch (NullReferenceException)
            {
            } // Not sure why this happens, but seems to be no problem

            if (primaryWeaponPriorityTarget != null)
            {
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("primaryWeaponPriorityTarget is [" + primaryWeaponPriorityTarget.Name + "][" + Math.Round(primaryWeaponPriorityTarget.Distance / 1000, 2) +
                        "k][" + primaryWeaponPriorityTarget.MaskedId + "] GroupID [" + primaryWeaponPriorityTarget.GroupId + "]");
                PreferredPrimaryWeaponTarget = primaryWeaponPriorityTarget;
                Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion Get the closest primary weapon priority target

            #region did our calling routine (CombatMissionCtrl?) pass us targets to shoot?

            //
            // This is where CombatMissionCtrl would pass targets to GetBestTarget
            //
            if (Logging.Logging.DebugGetBestTarget)
                Logging.Logging.Log("Checking Calling Target");
            if (_potentialTargets != null && _potentialTargets.Any())
            {
                EntityCache callingTarget = null;
                try
                {
                    callingTarget = _potentialTargets.OrderBy(t => t.Distance).FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }

                if (callingTarget != null && (callingTarget.IsReadyToShoot || callingTarget.IsLargeCollidable)
                    //((!callingTarget.IsNPCFrigate && !callingTarget.IsFrigate)
                    //|| (!Cache.Instance.UseDrones && !callingTarget.IsTooCloseTooFastTooSmallToHit))
                    )
                {
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("if (callingTarget != null && !callingTarget.IsIgnored)");
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("callingTarget is [" + callingTarget.Name + "][" + Math.Round(callingTarget.Distance / 1000, 2) + "k][" + callingTarget.MaskedId +
                            "] GroupID [" + callingTarget.GroupId + "]");
                    AddPrimaryWeaponPriorityTarget(callingTarget, PrimaryWeaponPriority.PriorityKillTarget, "GetBestTarget: callingTarget");
                    PreferredPrimaryWeaponTarget = callingTarget;
                    Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                //return false; //do not return here, continue to process targets, we did not find one yet
            }

            #endregion

            #region Get the closest High Value Target

            if (Logging.Logging.DebugGetBestTarget)
                Logging.Logging.Log("Checking Closest High Value");
            EntityCache highValueTarget = null;

            if (PotentialCombatTargets.Any())
            {
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("get closest: if (potentialCombatTargets.Any())");

                highValueTarget = PotentialCombatTargets.Where(t => t.IsHighValueTarget && t.IsReadyToShoot)
                    .OrderByDescending(t => !t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenByDescending(t => !t.IsTooCloseTooFastTooSmallToHit)
                    .ThenByDescending(t => t.IsInOptimalRange)
                    .ThenByDescending(pt => pt.TargetValue) //highest value first
                    .ThenByDescending(t => !t.IsCruiser)
                    .ThenBy(Cache.Instance.OrderByLowestHealth())
                    .ThenBy(t => t.Nearest5kDistance)
                    .FirstOrDefault();
            }

            #endregion

            #region Get the closest low value target that is not moving too fast for us to hit

            //
            // Get the closest low value target //excluding things going too fast for guns to hit (if you have guns fitted)
            //
            if (Logging.Logging.DebugGetBestTarget)
                Logging.Logging.Log("Checking closest Low Value");
            EntityCache lowValueTarget = null;
            if (PotentialCombatTargets.Any())
            {
                lowValueTarget = PotentialCombatTargets.Where(t => t.IsLowValueTarget && t.IsReadyToShoot)
                    .OrderByDescending(t => t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenByDescending(t => t.IsTooCloseTooFastTooSmallToHit)
                    //this will return false (not to close to fast to small), then true due to .net sort order of bools
                    .ThenBy(pt => pt.TargetValue) //lowest value first
                    .ThenBy(Cache.Instance.OrderByLowestHealth())
                    .ThenBy(t => t.Nearest5kDistance)
                    .FirstOrDefault();
            }

            #endregion

            #region If lowValueFirst && lowValue aggrod or no high value aggrod

            if ((lowValueFirst && lowValueTarget != null)
                && (lowValueTarget.IsTargetedBy
                    || (highValueTarget == null
                        || (highValueTarget != null
                            && !highValueTarget.IsTargetedBy))))
            {
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("Checking Low Value First");
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("lowValueTarget is [" + lowValueTarget.Name + "][" + Math.Round(lowValueTarget.Distance / 1000, 2) + "k][" + lowValueTarget.MaskedId +
                        "] GroupID [" + lowValueTarget.GroupId + "]");
                PreferredPrimaryWeaponTarget = lowValueTarget;
                Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion

            #region High Value - aggrod, or no low value aggrod

            // high value if aggrod
            // if no high value aggrod, low value thats aggrod
            // if no high aggro, and no low aggro, shoot high value thats present
            if (highValueTarget != null)
            {
                if (highValueTarget.IsTargetedBy
                    || Drones.UseDrones
                    || (lowValueTarget == null
                        || (lowValueTarget != null
                            && !lowValueTarget.IsTargetedBy)))
                {
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("Checking Use High Value");
                    if (Logging.Logging.DebugGetBestTarget)
                        Logging.Logging.Log("highValueTarget is [" + highValueTarget.Name + "][" + Math.Round(highValueTarget.Distance / 1000, 2) + "k][" +
                            highValueTarget.MaskedId + "] GroupID [" + highValueTarget.GroupId + "]");
                    PreferredPrimaryWeaponTarget = highValueTarget;
                    Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }
            }

            #endregion

            #region If we do not have a high value target but we do have a low value target

            if (lowValueTarget != null)
            {
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("Checking use Low Value");
                if (Logging.Logging.DebugGetBestTarget)
                    Logging.Logging.Log("lowValueTarget is [" + lowValueTarget.Name + "][" + Math.Round(lowValueTarget.Distance / 1000, 2) + "k][" + lowValueTarget.MaskedId +
                        "] GroupID [" + lowValueTarget.GroupId + "]");
                PreferredPrimaryWeaponTarget = lowValueTarget;
                Time.Instance.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion

            if (Logging.Logging.DebugGetBestTarget) Logging.Logging.Log("Could not determine a suitable target");

            #region If we did not find anything at all (wtf!?!?)

            if (Logging.Logging.DebugGetBestTarget)
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

                if (PotentialCombatTargets.Any(t => !t.IsTarget && !t.IsTargeting))
                {
                    if (CombatMissionCtrl.IgnoreTargets.Any())
                    {
                        var IgnoreCount = CombatMissionCtrl.IgnoreTargets.Count;
                        Logging.Logging.Log("Ignore List has [" + IgnoreCount + "] Entities in it.");
                    }

                    Logging.Logging.Log("***** ALL [" + PotentialCombatTargets.Count() + "] potentialCombatTargets LISTED BELOW (not yet targeted or targeting)");
                    var potentialCombatTargetNumber = 0;
                    foreach (var potentialCombatTarget in PotentialCombatTargets)
                    {
                        potentialCombatTargetNumber++;
                        Logging.Logging.Log("***** Unlocked [" + potentialCombatTargetNumber + "]: [" + potentialCombatTarget.Name + "][" +
                            Math.Round(potentialCombatTarget.Distance / 1000, 2) + "k][" + potentialCombatTarget.MaskedId + "][isTarget: " +
                            potentialCombatTarget.IsTarget + "] GroupID [" + potentialCombatTarget.GroupId + "]");
                    }
                    Logging.Logging.Log("***** ALL [" + PotentialCombatTargets.Count() + "] potentialCombatTargets LISTED ABOVE (not yet targeted or targeting)");
                    Logging.Logging.Log(".");
                }
            }

            #endregion

            Time.Instance.NextGetBestCombatTarget = DateTime.UtcNow;
            return false;
        }


        // Reload correct (tm) ammo for the NPC
        // (enough/correct) ammo is loaded, false if wrong/not enough ammo is loaded
        private static bool ReloadNormalAmmo(ModuleCache weapon, EntityCache entity, int weaponNumber, bool force = false)
        {
            //Logging.Log("ReloadAll", "ReloadAll", Logging.White);

            if (Cache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
            {
                //Logging.Log("ReloadAll", "Civilian guns do not use ammo.", Logging.Debug);
                return true;
            }

            if (entity == null)
            {
                entity = Cache.Instance.MyShipEntity;
            }

            List<Ammo> correctAmmoToUse = null;
            List<Ammo> correctAmmoInCargo = null;

            if (Ammo.Any(a => a.DamageType == MissionSettings.CurrentDamageType))
            {
                // Get ammo based on damage type
                correctAmmoToUse = Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList();

                // Check if we still have that ammo in our cargo
                correctAmmoInCargo =
                    correctAmmoToUse.Where(
                        a =>
                            Cache.Instance.CurrentShipsCargo != null &&
                            Cache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= MinimumAmmoCharges)).ToList();

                // We are out of ammo! :(
                if (!correctAmmoInCargo.Any())
                {
                    Logging.Logging.Log("ReloadNormalAmmo:: not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" + MinimumAmmoCharges +
                        "] Note: CurrentDamageType [" + MissionSettings.CurrentDamageType + "]");
                    if (MissionSettings.FactionDamageType != null)
                        Logging.Logging.Log("FactionDamageType [" + MissionSettings.FactionDamageType +
                            "] PocketDamageType overrides MissionDamageType overrides FactionDamageType");
                    if (MissionSettings.MissionDamageType != null)
                        Logging.Logging.Log("MissionDamageType [" + MissionSettings.MissionDamageType +
                            "] PocketDamageType overrides MissionDamageType overrides FactionDamageType");
                    if (MissionSettings.PocketDamageType != null)
                        Logging.Logging.Log("PocketDamageType [" + MissionSettings.PocketDamageType +
                            "] PocketDamageType overrides MissionDamageType overrides FactionDamageType");
                    _States.CurrentCombatState = CombatState.OutOfAmmo;
                    return false;
                }
            }
            else
            {
                // here we check what type we have in our hangar and use that instead ( IF IT EXISTS IN SETTINGS! ), if we login in space, this will screw up if we don't.
                if (Cache.Instance.CurrentShipsCargo != null)
                {
                    var result =
                        Ammo.ToList()
                            .Where(a => Cache.Instance.CurrentShipsCargo.Items.ToList().Any(c => c.TypeId == a.TypeId && c.Quantity >= MinimumAmmoCharges));

                    if (result.Any())
                    {
                        correctAmmoInCargo = result.ToList();
                    }
                    else
                    {
                        //Logging.Log("Combat", "ReloadNormalAmmo: if(!result.Any())", Logging.Orange);
                        return false;
                    }
                }
                else
                {
                    Logging.Logging.Log("ReloadNormalAmmo: if Cache.Instance.CurrentShipsCargo == null");
                    _States.CurrentCombatState = CombatState.OutOfAmmo;
                    return false;
                }
            }

            // Get the best possible ammo
            var ammo = correctAmmoInCargo.FirstOrDefault();
            try
            {
                if (ammo != null && entity != null)
                {
                    ammo = correctAmmoInCargo.Where(a => a.Range > entity.Distance).OrderBy(a => a.Range).FirstOrDefault();
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("ReloadNormalAmmo: [" + weaponNumber + "] Unable to find the correct ammo: waiting [" + exception + "]");
                return false;
            }

            // We do not have any ammo left that can hit targets at that range!
            if (ammo == null)
            {
                if (Logging.Logging.DebugReloadAll)
                    Logging.Logging.Log("[" + weaponNumber + "] We do not have any ammo left that can hit targets at that range!");
                return false;
            }

            if (weapon == null)
            {
                // means there is nothing loaded in that weapon, this is a problem
                if (Logging.Logging.DebugReloadAll) Logging.Logging.Log("weapon == null");
                return false;
            }

            // Do we have ANY ammo loaded? CurrentCharges would be 0 if we have no ammo at all.
            if (weapon.Charge != null && (long) weapon.CurrentCharges >= MinimumAmmoCharges && weapon.Charge.TypeId == ammo.TypeId)
            {
                //Force a reload even through we have some ammo loaded already?
                if (!force)
                {
                    if (Logging.Logging.DebugReloadAll)
                        Logging.Logging.Log("[" + weaponNumber + "] MaxRange [ " + weapon.MaxRange + " ] if we have 0 charges MaxRange will be 0");
                    Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId] = DateTime.UtcNow;
                    return true;
                }

                //we must have ammo, no need to reload at the moment\
                if (Logging.Logging.DebugReloadAll)
                    Logging.Logging.Log("[" + weaponNumber + "] MaxRange [ " + weapon.MaxRange + " ] if we have 0 charges MaxRange will be 0");
                Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId] = DateTime.UtcNow;
                return true;
            }

            DirectItem charge = null;
            if (Cache.Instance.CurrentShipsCargo != null)
            {
                if (Cache.Instance.CurrentShipsCargo.Items.Any())
                {
                    charge = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(e => e.TypeId == ammo.TypeId && e.Quantity >= MinimumAmmoCharges);
                    // This should have shown up as "out of ammo"
                    if (charge == null)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("We have no ammo in cargo?! This should have shown up as out of ammo");
                        return false;
                    }
                }
                else
                {
                    if (Logging.Logging.DebugReloadAll)
                        Logging.Logging.Log("We have no items in cargo at all?! This should have shown up as out of ammo");
                    return false;
                }
            }
            else
            {
                if (Logging.Logging.DebugReloadAll) Logging.Logging.Log("CurrentShipsCargo is null?!");
                return false;
            }

            // If we are reloading, wait Time.ReloadWeaponDelayBeforeUsable_seconds (see time.cs)
            if (weapon.IsReloadingAmmo)
            {
                if (Logging.Logging.DebugReloadAll)
                    Logging.Logging.Log("We are already reloading, wait - weapon.IsReloadingAmmo [" + weapon.IsReloadingAmmo + "]");
                return true;
            }

            // If we are changing ammo, wait Time.ReloadWeaponDelayBeforeUsable_seconds (see time.cs)
            if (weapon.IsChangingAmmo)
            {
                if (Logging.Logging.DebugReloadAll)
                    Logging.Logging.Log("We are already changing ammo, wait - weapon.IsReloadingAmmo [" + weapon.IsReloadingAmmo + "]");
                return true;
            }

            try
            {
                // Reload or change ammo
                if (weapon.Charge != null && weapon.Charge.TypeId == charge.TypeId && !weapon.IsChangingAmmo)
                {
                    if (weapon.ReloadAmmo(charge, weaponNumber, (double) ammo.Range))
                    {
                        return true;
                    }

                    Logging.Logging.Log("ReloadAmmo failed.");
                    return false;
                }

                if (entity != null && weapon.ChangeAmmo(charge, weaponNumber, (double) ammo.Range, entity.Name, entity.Distance))
                {
                    return true;
                }

                Logging.Logging.Log("ChangeAmmo failed.");
                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            // Return true as we are reloading ammo, assume it is the correct ammo...
            return true;
        }

        private static bool ReloadEnergyWeaponAmmo(ModuleCache weapon, EntityCache entity, int weaponNumber)
        {
            if (Cache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
            {
                //Logging.Log("ReloadAll", "Civilian guns do not use ammo.", Logging.Debug);
                return true;
            }

            // Get ammo based on damage type
            IEnumerable<Ammo> correctAmmo = Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList();

            // Check if we still have that ammo in our cargo
            IEnumerable<Ammo> correctAmmoInCargo =
                correctAmmo.Where(a => Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == a.TypeId))
                    .ToList();

            //check if mission specific ammo is defined
            if (MissionSettings.AmmoTypesToLoad.Count() != 0)
            {
                //correctAmmoInCargo = MissionSettings.AmmoTypesToLoad.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList();
            }

            // Check if we still have that ammo in our cargo
            correctAmmoInCargo =
                correctAmmoInCargo.Where(
                    a =>
                        Cache.Instance.CurrentShipsCargo != null &&
                        Cache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == a.TypeId && e.Quantity >= MinimumAmmoCharges)).ToList();
            if (MissionSettings.AmmoTypesToLoad.Count() != 0)
            {
                //correctAmmoInCargo = MissionSettings.AmmoTypesToLoad;
            }

            // We are out of ammo! :(
            if (!correctAmmoInCargo.Any())
            {
                Logging.Logging.Log("ReloadEnergyWeapon: not enough [" + MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" + MinimumAmmoCharges + "]");
                _States.CurrentCombatState = CombatState.OutOfAmmo;
                return false;
            }

            if (weapon.Charge != null)
            {
                var areWeMissingAmmo = correctAmmoInCargo.Where(a => a.TypeId == weapon.Charge.TypeId);
                if (!areWeMissingAmmo.Any())
                {
                    Logging.Logging.Log("ReloadEnergyWeaponAmmo: We have ammo loaded that does not have a full reload available in the cargo.");
                }
            }

            // Get the best possible ammo - energy weapons change ammo near instantly
            var ammo = correctAmmoInCargo.Where(a => a.Range > (entity.Distance)).OrderBy(a => a.Range).FirstOrDefault(); //default

            // We do not have any ammo left that can hit targets at that range!
            if (ammo == null)
            {
                if (Logging.Logging.DebugReloadorChangeAmmo)
                    Logging.Logging.Log("ReloadEnergyWeaponAmmo: best possible ammo: [ ammo == null]");
                return false;
            }

            if (Logging.Logging.DebugReloadorChangeAmmo)
                Logging.Logging.Log("ReloadEnergyWeaponAmmo: best possible ammo: [" + ammo.TypeId + "][" + ammo.DamageType + "]");
            if (Logging.Logging.DebugReloadorChangeAmmo)
                Logging.Logging.Log("ReloadEnergyWeaponAmmo: best possible ammo: [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 0) + "]");

            var charge = Cache.Instance.CurrentShipsCargo.Items.OrderBy(e => e.Quantity).FirstOrDefault(e => e.TypeId == ammo.TypeId);

            // We do not have any ammo left that can hit targets at that range!
            if (charge == null)
            {
                if (Logging.Logging.DebugReloadorChangeAmmo)
                    Logging.Logging.Log("ReloadEnergyWeaponAmmo: We do not have any ammo left that can hit targets at that range!");
                return false;
            }

            if (Logging.Logging.DebugReloadorChangeAmmo)
                Logging.Logging.Log("ReloadEnergyWeaponAmmo: charge: [" + charge.TypeName + "][" + charge.TypeId + "]");

            // We have enough ammo loaded
            if (weapon.Charge != null && weapon.Charge.TypeId == ammo.TypeId)
            {
                if (Logging.Logging.DebugReloadorChangeAmmo)
                    Logging.Logging.Log("ReloadEnergyWeaponAmmo: We have Enough Ammo of that type Loaded Already");
                return true;
            }

            // We are reloading, wait
            if (weapon.IsReloadingAmmo)
                return true;

            // We are reloading, wait
            if (weapon.IsChangingAmmo)
                return true;

            // Reload or change ammo
            if (weapon.Charge != null && weapon.Charge.TypeId == charge.TypeId)
            {
                //
                // reload
                //
                if (weapon.ReloadAmmo(charge, weaponNumber, (double) ammo.Range))
                {
                    return true;
                }

                return false;
            }

            //
            // change ammo
            //
            if (weapon.ChangeAmmo(charge, weaponNumber, (double) ammo.Range, entity.Name, entity.Distance))
            {
                return true;
            }

            return false;
        }

        // Reload correct (tm) ammo for the NPC

        private static bool ReloadAmmo(ModuleCache weapon, EntityCache entity, int weaponNumber, bool force = false)
        {
            // We need the cargo bay open for both reload actions
            //if (!Cache.Instance.OpenCargoHold("Combat: ReloadAmmo")) return false;
            if (Cache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
            {
                Logging.Logging.Log("Civilian guns do not use ammo.");
                return true;
            }

            return weapon.IsEnergyWeapon ? ReloadEnergyWeaponAmmo(weapon, entity, weaponNumber) : ReloadNormalAmmo(weapon, entity, weaponNumber, force);
        }

        public static bool ReloadAll(EntityCache entity, bool force = false)
        {
            const int reloadAllDelay = 400;
            if (DateTime.UtcNow.Subtract(_lastReloadAll).TotalMilliseconds < reloadAllDelay)
            {
                return false;
            }

            _lastReloadAll = DateTime.UtcNow;

            if (Cache.Instance.MyShipEntity.Name == Settings.Instance.TransportShipName)
            {
                if (Logging.Logging.DebugReloadAll)
                    Logging.Logging.Log("You are in your TransportShip named [" + Settings.Instance.TransportShipName + "], no need to reload ammo!");
                return true;
            }

            if (Cache.Instance.MyShipEntity.Name == Settings.Instance.TravelShipName)
            {
                if (Logging.Logging.DebugReloadAll)
                    Logging.Logging.Log("You are in your TravelShipName named [" + Settings.Instance.TravelShipName + "], no need to reload ammo!");
                return true;
            }

            if (Cache.Instance.MyShipEntity.GroupId == (int) Group.Shuttle)
            {
                if (Logging.Logging.DebugReloadAll) Logging.Logging.Log("You are in a Shuttle, no need to reload ammo!");
                return true;
            }

            if (Cache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
            {
                if (Logging.Logging.DebugReloadAll) Logging.Logging.Log("Civilian guns do not use ammo.");
                return true;
            }

            _weaponNumber = 0;
            if (Logging.Logging.DebugReloadAll)
                Logging.Logging.Log("Weapons (or stacks of weapons?): [" + Cache.Instance.Weapons.Count() + "]");

            if (Cache.Instance.Weapons.Any())
            {
                foreach (var weapon in Cache.Instance.Weapons)
                {
                    _weaponNumber++;
                    if (Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(weapon.ItemId))
                    {
                        if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[weapon.ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                        {
                            if (Logging.Logging.DebugReloadAll)
                                Logging.Logging.Log("Weapon [" + _weaponNumber + "] was just reloaded [" +
                                    Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadedTimeStamp[weapon.ItemId]).TotalSeconds, 0) +
                                    "] seconds ago , moving on to next weapon");
                            continue;
                        }
                    }

                    if (Time.Instance.LastReloadAttemptTimeStamp != null && Time.Instance.LastReloadAttemptTimeStamp.ContainsKey(weapon.ItemId))
                    {
                        if (DateTime.UtcNow < Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId].AddSeconds(Cache.Instance.RandomNumber(5, 10)))
                        {
                            if (Logging.Logging.DebugReloadAll)
                                Logging.Logging.Log("Weapon [" + _weaponNumber + "] was just attempted to be reloaded [" +
                                    Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadAttemptTimeStamp[weapon.ItemId]).TotalSeconds, 0) +
                                    "] seconds ago , moving on to next weapon");
                            continue;
                        }
                    }

                    if (weapon.CurrentCharges == weapon.MaxCharges)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("Weapon [" + _weaponNumber + "] has [" + weapon.CurrentCharges + "] charges. MaxCharges is [" + weapon.MaxCharges +
                                "]: checking next weapon");
                        continue;
                    }

                    // Reloading energy weapons prematurely just results in unnecessary error messages, so let's not do that
                    if (weapon.IsEnergyWeapon)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("if (weapon.IsEnergyWeapon) continue (energy weapons do not really need to reload)");
                        continue;
                    }

                    if (weapon.IsReloadingAmmo)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("[" + weapon.TypeName + "][" + _weaponNumber + "] is still reloading, moving on to next weapon");
                        continue;
                    }

                    if (weapon.IsDeactivating)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("[" + weapon.TypeName + "][" + _weaponNumber + "] is still Deactivating, moving on to next weapon");
                        continue;
                    }

                    if (weapon.IsChangingAmmo)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("[" + weapon.TypeName + "][" + _weaponNumber + "] is still Changing Ammo, moving on to next weapon");
                        continue;
                    }

                    if (weapon.IsActive)
                    {
                        if (Logging.Logging.DebugReloadAll)
                            Logging.Logging.Log("[" + weapon.TypeName + "][" + _weaponNumber + "] is Active, moving on to next weapon");
                        continue;
                    }

                    if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        if (!ReloadAmmo(weapon, entity, _weaponNumber, force))
                            continue; //by returning false here we make sure we only reload one gun (or stack) per iteration (basically per second)
                    }

                    return false;
                }

                if (Logging.Logging.DebugReloadAll) Logging.Logging.Log("completely reloaded all weapons");
                //_reloadAllIteration = 0;
                return true;
            }

            //_reloadAllIteration = 0;
            return true;
        }

        /// <summary>
        ///     Returns true if it can activate the weapon on the target
        /// </summary>
        /// <remarks>
        ///     The idea behind this function is that a target that explodes is not being fired on within 5 seconds
        /// </remarks>
        /// <param name="module"></param>
        /// <param name="entity"></param>
        /// <param name="isWeapon"></param>
        /// <returns></returns>
        private static bool CanActivate(ModuleCache module, EntityCache entity, bool isWeapon)
        {
            if (!module.IsOnline)
            {
                return false;
            }

            if (module.IsActive || !module.IsActivatable)
            {
                return false;
            }

            if (isWeapon && !entity.IsTarget)
            {
                Logging.Logging.Log("We attempted to shoot [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) + "] which is currently not locked!");
                return false;
            }

            if (isWeapon && entity.Distance > MaxRange)
            {
                Logging.Logging.Log("We attempted to shoot [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) + "] which is out of weapons range!");
                return false;
            }

            if (module.IsReloadingAmmo)
                return false;

            if (module.IsChangingAmmo)
                return false;

            if (module.IsDeactivating)
                return false;

            // We have changed target, allow activation
            if (entity.Id != module.LastTargetId)
                return true;

            // We have reloaded, allow activation
            if (isWeapon && module.CurrentCharges == MaxCharges)
                return true;

            // if the module is not already active, we have a target, it is in range, we are not reloading then ffs shoot it...
            return true;
        }

        /// <summary>
        ///     Activate weapons
        /// </summary>
        private static void ActivateWeapons(EntityCache target)
        {
            // When in warp there's nothing we can do, so ignore everything
            if (Cache.Instance.InSpace && Cache.Instance.InWarp)
            {
                if (PrimaryWeaponPriorityEntities != null && PrimaryWeaponPriorityEntities.Any())
                {
                    RemovePrimaryWeaponPriorityTargets(PrimaryWeaponPriorityEntities.ToList());
                }

                if (Drones.UseDrones && Drones.DronePriorityEntities != null && Drones.DronePriorityEntities.Any())
                {
                    Drones.RemoveDronePriorityTargets(Drones.DronePriorityEntities.ToList());
                }

                if (Logging.Logging.DebugActivateWeapons)
                    Logging.Logging.Log("ActivateWeapons: deactivate: we are in warp! doing nothing");
                return;
            }

            if (!Cache.Instance.Weapons.Any())
            {
                if (Logging.Logging.DebugActivateWeapons) Logging.Logging.Log("ActivateWeapons: you have no weapons?");
                return;
            }

            //
            // Do we really want a non-mission action moving the ship around at all!! (other than speed tanking)?
            // If you are not in a mission by all means let combat actions move you around as needed
            /*
            if (!Cache.Instance.InMission)
            {
                if (Logging.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: we are NOT in a mission: NavigateInToRange", Logging.Teal);
                NavigateOnGrid.NavigateIntoRange(target, "Combat");
            }
            if (Settings.Instance.SpeedTank)
            {
                if (Logging.DebugActivateWeapons) Logging.Log("Combat", "ActivateWeapons: deactivate: We are Speed Tanking: NavigateInToRange", Logging.Teal);
                NavigateOnGrid.NavigateIntoRange(target, "Combat");
            }
			 */
            if (Logging.Logging.DebugActivateWeapons)
                Logging.Logging.Log("ActivateWeapons: deactivate: after navigate into range...");

            // Get the weapons

            // TODO: Add check to see if there is better ammo to use! :)
            // Get distance of the target and compare that with the ammo currently loaded

            //Deactivate weapons that needs to be deactivated for this list of reasons...
            _weaponNumber = 0;
            if (Logging.Logging.DebugActivateWeapons)
                Logging.Logging.Log("ActivateWeapons: deactivate: Do we need to deactivate any weapons?");

            if (Cache.Instance.Weapons.Any())
            {
                foreach (var weapon in Cache.Instance.Weapons)
                {
                    _weaponNumber++;
                    if (Logging.Logging.DebugActivateWeapons)
                        Logging.Logging.Log("ActivateWeapons: deactivate: for each weapon [" + _weaponNumber + "] in weapons");

                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(weapon.ItemId))
                    {
                        if (Time.Instance.LastActivatedTimeStamp[weapon.ItemId].AddMilliseconds(Time.Instance.WeaponDelay_milliseconds) > DateTime.UtcNow)
                        {
                            continue;
                        }
                    }

                    if (!weapon.IsActive)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is not active: no need to do anything");
                        continue;
                    }

                    if (weapon.IsReloadingAmmo)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is reloading ammo: waiting");
                        continue;
                    }

                    if (weapon.IsDeactivating)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is deactivating: waiting");
                        continue;
                    }

                    if (weapon.IsChangingAmmo)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + _weaponNumber + "] is changing ammo: waiting");
                        continue;
                    }

                    // No ammo loaded
                    if (weapon.Charge == null)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: no ammo loaded? [" + weapon.TypeName + "][" + _weaponNumber + "] reload will happen elsewhere");
                        continue;
                    }

                    var ammo = Ammo.FirstOrDefault(a => a.TypeId == weapon.Charge.TypeId);

                    //use mission specific ammo
                    if (MissionSettings.AmmoTypesToLoad.Count() != 0)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: MissionAmmocount is not 0");
                        //var x = 0;
                        //ammo = MissionSettings.AmmoTypesToLoad.TryGetValue((Ammo)weapon.Charge.TypeName, DateTime.Now);
                    }

                    // How can this happen? Someone manually loaded ammo
                    if (ammo == null)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: deactivate: ammo == null [" + weapon.TypeName + "][" + _weaponNumber + "] someone manually loaded ammo?");
                        continue;
                    }

                    if (weapon.CurrentCharges >= 2)
                    {
                        // If we have already activated warp, deactivate the weapons
                        if (!Cache.Instance.ActiveShip.Entity.IsWarping)
                        {
                            // Target is in range
                            if (target.Distance <= ammo.Range)
                            {
                                if (Logging.Logging.DebugActivateWeapons)
                                    Logging.Logging.Log("ActivateWeapons: deactivate: target is in range: do nothing, wait until it is dead");
                                continue;
                            }
                        }
                    }

                    // Target is out of range, stop firing
                    if (Logging.Logging.DebugActivateWeapons)
                        Logging.Logging.Log("ActivateWeapons: deactivate: target is out of range, stop firing");
                    if (weapon.Click()) return;
                    return;
                }

                // Hack for max charges returning incorrect value
                if (!Cache.Instance.Weapons.Any(w => w.IsEnergyWeapon))
                {
                    MaxCharges = Math.Max(MaxCharges, Cache.Instance.Weapons.Max(l => l.MaxCharges));
                    MaxCharges = Math.Max(MaxCharges, Cache.Instance.Weapons.Max(l => l.CurrentCharges));
                }

                var weaponsActivatedThisTick = 0;
                var weaponsToActivateThisTick = Cache.Instance.RandomNumber(3, 5);

                // Activate the weapons (it not yet activated)))
                _weaponNumber = 0;
                if (Logging.Logging.DebugActivateWeapons)
                    Logging.Logging.Log("ActivateWeapons: activate: Do we need to activate any weapons?");
                foreach (var weapon in Cache.Instance.Weapons)
                {
                    _weaponNumber++;

                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(weapon.ItemId))
                    {
                        if (Time.Instance.LastActivatedTimeStamp[weapon.ItemId].AddMilliseconds(Time.Instance.WeaponDelay_milliseconds) > DateTime.UtcNow)
                        {
                            continue;
                        }
                    }

                    // Are we reloading, deactivating or changing ammo?
                    if (weapon.IsReloadingAmmo)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is reloading, waiting.");
                        continue;
                    }

                    if (weapon.IsDeactivating)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is deactivating, waiting.");
                        continue;
                    }

                    if (weapon.IsChangingAmmo)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is changing ammo, waiting.");
                        continue;
                    }

                    if (!target.IsTarget)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is [" + target.Name + "] is not locked, waiting.");
                        continue;
                    }

                    // Are we on the right target?
                    if (weapon.IsActive)
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is active already");
                        if (weapon.TargetId != target.Id && target.IsTarget)
                        {
                            if (Logging.Logging.DebugActivateWeapons)
                                Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] is shooting at the wrong target: deactivating");
                            if (weapon.Click()) return;

                            return;
                        }
                        continue;
                    }

                    // No, check ammo type and if that is correct, activate weapon
                    if (ReloadAmmo(weapon, target, _weaponNumber) && CanActivate(weapon, target, true))
                    {
                        if (weaponsActivatedThisTick > weaponsToActivateThisTick)
                        {
                            if (Logging.Logging.DebugActivateWeapons)
                                Logging.Logging.Log("ActivateWeapons: if we have already activated x number of weapons return, which will wait until the next ProcessState");
                            return;
                        }

                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + _weaponNumber + "] has the correct ammo: activate");
                        if (weapon.Activate(target))
                        {
                            weaponsActivatedThisTick++;
                                //increment the number of weapons we have activated this ProcessState so that we might optionally activate more than one module per tick
                            Logging.Logging.Log("Activating weapon  [" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                                Math.Round(target.Distance / 1000, 0) + "k away]");
                            continue;
                        }

                        continue;
                    }

                    if (Logging.Logging.DebugActivateWeapons)
                        Logging.Logging.Log("ActivateWeapons: ReloadReady [" + ReloadAmmo(weapon, target, _weaponNumber) + "] CanActivateReady [" +
                            CanActivate(weapon, target, true) + "]");
                }
            }
            else
            {
                Logging.Logging.Log("ActivateWeapons: you have no weapons with groupID: [ " + WeaponGroupId + " ]");
                icount = 0;
                foreach (var __module in Cache.Instance.Modules.Where(e => e.IsOnline && e.IsActivatable))
                {
                    icount++;
                    Logging.Logging.Log("[" + icount + "] Module TypeID [ " + __module.TypeId + " ] ModuleGroupID [ " + __module.GroupId +
                        " ] EveCentral Link [ http://eve-central.com/home/quicklook.html?typeid=" + __module.TypeId + " ]");
                }
            }
        }

        /// <summary>
        ///     Activate target painters
        /// </summary>
        private static void ActivateTargetPainters(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (Logging.Logging.DebugKillTargets)
                    Logging.Logging.Log("Ignoring TargetPainter Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var targetPainters = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.TargetPainter).ToList();

            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            foreach (var painter in targetPainters)
            {
                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(painter.ItemId))
                {
                    if (Time.Instance.LastActivatedTimeStamp[painter.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                    {
                        continue;
                    }
                }

                _weaponNumber++;

                // Are we on the right target?
                if (painter.IsActive)
                {
                    if (painter.TargetId != target.Id)
                    {
                        if (painter.Click()) return;

                        return;
                    }

                    continue;
                }

                // Are we deactivating?
                if (painter.IsDeactivating)
                    continue;

                if (CanActivate(painter, target, false))
                {
                    if (painter.Activate(target))
                    {
                        Logging.Logging.Log("Activating [" + painter.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                            Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }

                    continue;
                }
            }
        }

        /// <summary>
        ///     Activate target painters
        /// </summary>
        private static void ActivateSensorDampeners(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (Logging.Logging.DebugKillTargets)
                    Logging.Logging.Log("Ignoring SensorDamps Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var sensorDampeners = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.SensorDampener).ToList();

            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            foreach (var sensorDampener in sensorDampeners)
            {
                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(sensorDampener.ItemId))
                {
                    if (Time.Instance.LastActivatedTimeStamp[sensorDampener.ItemId].AddMilliseconds(Time.Instance.PainterDelay_milliseconds) > DateTime.UtcNow)
                    {
                        continue;
                    }
                }

                _weaponNumber++;

                // Are we on the right target?
                if (sensorDampener.IsActive)
                {
                    if (sensorDampener.TargetId != target.Id)
                    {
                        if (sensorDampener.Click()) return;
                        return;
                    }

                    continue;
                }

                // Are we deactivating?
                if (sensorDampener.IsDeactivating)
                    continue;

                if (CanActivate(sensorDampener, target, false))
                {
                    if (sensorDampener.Activate(target))
                    {
                        Logging.Logging.Log("Activating [" + sensorDampener.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                            Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }

                    continue;
                }
            }
        }

        /// <summary>
        ///     Activate Nos
        /// </summary>
        private static void ActivateNos(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (Logging.Logging.DebugKillTargets)
                    Logging.Logging.Log("Ignoring NOS/NEUT Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var noses = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.NOS || m.GroupId == (int) Group.Neutralizer).ToList();

            //Logging.Log("Combat: we have " + noses.Count.ToString() + " Nos modules");
            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            foreach (var nos in noses)
            {
                _weaponNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(nos.ItemId))
                {
                    if (Time.Instance.LastActivatedTimeStamp[nos.ItemId].AddMilliseconds(Time.Instance.NosDelay_milliseconds) > DateTime.UtcNow)
                    {
                        continue;
                    }
                }

                // Are we on the right target?
                if (nos.IsActive)
                {
                    if (nos.TargetId != target.Id)
                    {
                        if (nos.Click()) return;

                        return;
                    }

                    continue;
                }

                // Are we deactivating?
                if (nos.IsDeactivating)
                    continue;

                //Logging.Log("Combat: Distances Target[ " + Math.Round(target.Distance,0) + " Optimal[" + nos.OptimalRange.ToString()+"]");
                // Target is out of Nos range
                if (target.Distance >= nos.MaxRange)
                    continue;

                if (CanActivate(nos, target, false))
                {
                    if (nos.Activate(target))
                    {
                        Logging.Logging.Log("Activating [" + nos.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                            Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }

                    continue;
                }

                Logging.Logging.Log("Cannot Activate [" + nos.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "][" +
                    Math.Round(target.Distance / 1000, 0) + "k away]");
            }
        }

        /// <summary>
        ///     Activate StasisWeb
        /// </summary>
        private static void ActivateStasisWeb(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (Logging.Logging.DebugKillTargets)
                    Logging.Logging.Log("Ignoring StasisWeb Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var webs = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.StasisWeb).ToList();

            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            foreach (var web in webs)
            {
                _weaponNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(web.ItemId))
                {
                    if (Time.Instance.LastActivatedTimeStamp[web.ItemId].AddMilliseconds(Time.Instance.WebDelay_milliseconds) > DateTime.UtcNow)
                    {
                        continue;
                    }
                }

                // Are we on the right target?
                if (web.IsActive)
                {
                    if (web.TargetId != target.Id)
                    {
                        if (web.Click()) return;

                        return;
                    }

                    continue;
                }

                // Are we deactivating?
                if (web.IsDeactivating)
                    continue;

                // Target is out of web range
                if (target.Distance >= web.OptimalRange)
                    continue;

                if (CanActivate(web, target, false))
                {
                    if (web.Activate(target))
                    {
                        Logging.Logging.Log("Activating [" + web.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "]");
                        return;
                    }

                    continue;
                }
            }
        }

        public static bool ActivateBastion(bool activate = false)
        {
            List<ModuleCache> bastionModules = null;
            bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
            if (!bastionModules.Any()) return true;
            if (bastionModules.Any(i => i.IsActive && i.IsDeactivating)) return true;

            if (!PotentialCombatTargets.Where(e => e.Distance < Cache.Instance.WeaponRange).Any(e => e.IsTarget || e.IsTargeting) &&
                CombatMissionCtrl.DeactivateIfNothingTargetedWithinRange)
            {
                if (Logging.Logging.DebugActivateBastion)
                    Logging.Logging.Log("NextBastionModeDeactivate set to 2 sec ago: We have no targets in range and DeactivateIfNothingTargetedWithinRange [ " +
                        CombatMissionCtrl.DeactivateIfNothingTargetedWithinRange + " ]");
                Time.Instance.NextBastionModeDeactivate = DateTime.UtcNow.AddSeconds(-2);
            }

            if (PotentialCombatTargets.Any(e => e.Distance < Cache.Instance.WeaponRange && e.IsPlayer && e.IsTargetedBy && e.IsAttacking) &&
                _States.CurrentCombatState != CombatState.OutOfAmmo)
            {
                if (Logging.Logging.DebugActivateBastion)
                    Logging.Logging.Log("We are being attacked by a player we should activate bastion");
                activate = true;
            }

            if (_States.CurrentPanicState == PanicState.Panicking || _States.CurrentPanicState == PanicState.StartPanicking)
            {
                if (Logging.Logging.DebugActivateBastion)
                    Logging.Logging.Log("NextBastionModeDeactivate set to 2 sec ago: We are in panic!");
                Time.Instance.NextBastionModeDeactivate = DateTime.UtcNow.AddSeconds(-2);
            }

            if (DateTime.UtcNow < Time.Instance.NextBastionAction)
            {
                if (Logging.Logging.DebugActivateBastion)
                    Logging.Logging.Log("NextBastionAction [" + Time.Instance.NextBastionAction.Subtract(DateTime.UtcNow).TotalSeconds + "] seconds, waiting...");
                return false;
            }

            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            foreach (var bastionMod in bastionModules)
            {
                _weaponNumber++;

                if (Logging.Logging.DebugActivateBastion)
                    Logging.Logging.Log("[" + _weaponNumber + "] BastionModule: IsActive [" + bastionMod.IsActive + "] IsDeactivating [" + bastionMod.IsDeactivating +
                        "] InLimboState [" + bastionMod.InLimboState + "] Duration [" + bastionMod.Duration + "] TypeId [" + bastionMod.TypeId + "]");

                //
                // Deactivate (if needed)
                //
                // Are we on the right target?
                if (bastionMod.IsActive && !bastionMod.IsDeactivating)
                {
                    if (Logging.Logging.DebugActivateBastion)
                        Logging.Logging.Log("IsActive and Is not yet deactivating (we only want one cycle), attempting to Click...");
                    if (bastionMod.Click()) return true;
                    return false;
                }

                if (bastionMod.IsActive)
                {
                    if (Logging.Logging.DebugActivateBastion)
                        Logging.Logging.Log("IsActive: assuming it is deactivating on the next cycle.");
                    return true;
                }

                //
                // Activate (if needed)
                //

                // Are we deactivating?
                if (bastionMod.IsDeactivating)
                    continue;

                if (!bastionMod.IsActive && activate)
                {
                    Logging.Logging.Log("Activating bastion [" + _weaponNumber + "]");
                    if (bastionMod.Click())
                    {
                        Time.Instance.NextBastionAction = DateTime.UtcNow.AddSeconds(Cache.Instance.RandomNumber(3, 20));
                        return true;
                    }

                    return false;
                }
            }

            return true; //if we got  this far we have done all we can do.
        }

        private static void ActivateWarpDisruptor(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (Logging.Logging.DebugKillTargets)
                    Logging.Logging.Log("Ignoring WarpDisruptor Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var WarpDisruptors = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.WarpDisruptor).ToList();

            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            foreach (var WarpDisruptor in WarpDisruptors)
            {
                _weaponNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(WarpDisruptor.ItemId))
                {
                    if (Time.Instance.LastActivatedTimeStamp[WarpDisruptor.ItemId].AddMilliseconds(Time.Instance.WebDelay_milliseconds) > DateTime.UtcNow)
                    {
                        continue;
                    }
                }

                // Are we on the right target?
                if (WarpDisruptor.IsActive)
                {
                    if (WarpDisruptor.TargetId != target.Id)
                    {
                        if (WarpDisruptor.Click()) return;

                        return;
                    }

                    continue;
                }

                // Are we deactivating?
                if (WarpDisruptor.IsDeactivating)
                    continue;

                // Target is out of web range
                if (target.Distance >= WarpDisruptor.OptimalRange)
                    continue;

                if (CanActivate(WarpDisruptor, target, false))
                {
                    if (WarpDisruptor.Activate(target))
                    {
                        Logging.Logging.Log("Activating [" + WarpDisruptor.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "]");
                        return;
                    }

                    continue;
                }
            }
        }

        private static void ActivateRemoteRepair(EntityCache target)
        {
            var RemoteRepairers = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.RemoteArmorRepairer
                                                                    || m.GroupId == (int) Group.RemoteShieldRepairer
                                                                    || m.GroupId == (int) Group.RemoteHullRepairer
                ).ToList();

            // Find the first active weapon
            // Assist this weapon
            _weaponNumber = 0;
            if (RemoteRepairers.Any())
            {
                if (Logging.Logging.DebugRemoteRepair)
                    Logging.Logging.Log("RemoteRepairers [" + RemoteRepairers.Count() + "] Target Distance [" + Math.Round(target.Distance / 1000, 0) + "] RemoteRepairDistance [" +
                        Math.Round(((double)RemoteRepairDistance / 1000), digits: 0) + "]");
                foreach (var RemoteRepairer in RemoteRepairers)
                {
                    _weaponNumber++;

                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(RemoteRepairer.ItemId))
                    {
                        if (Time.Instance.LastActivatedTimeStamp[RemoteRepairer.ItemId].AddMilliseconds(Time.Instance.RemoteRepairerDelay_milliseconds) >
                            DateTime.UtcNow)
                        {
                            continue;
                        }
                    }

                    // Are we on the right target?
                    if (RemoteRepairer.IsActive)
                    {
                        if (RemoteRepairer.TargetId != target.Id)
                        {
                            if (RemoteRepairer.Click()) return;

                            return;
                        }

                        continue;
                    }

                    // Are we deactivating?
                    if (RemoteRepairer.IsDeactivating)
                        continue;

                    // Target is out of RemoteRepair range
                    if (target.Distance >= RemoteRepairer.MaxRange)
                        continue;

                    if (CanActivate(RemoteRepairer, target, false))
                    {
                        if (RemoteRepairer.Activate(target))
                        {
                            Logging.Logging.Log("Activating [" + RemoteRepairer.TypeName + "][" + _weaponNumber + "] on [" + target.Name + "][" + target.MaskedId + "]");
                            return;
                        }

                        continue;
                    }
                }
            }
        }

        private static bool UnlockHighValueTarget(string module, string reason, bool OutOfRangeOnly = false)
        {
            EntityCache unlockThisHighValueTarget = null;
            var preferredId = PreferredPrimaryWeaponTarget != null ? PreferredPrimaryWeaponTarget.Id : -1;

            if (!OutOfRangeOnly)
            {
                if (lowValueTargetsTargeted.Count() > maxLowValueTargets &&
                    maxTotalTargets <= lowValueTargetsTargeted.Count() + highValueTargetsTargeted.Count())
                {
                    return UnlockLowValueTarget(module, reason, OutOfRangeOnly); // We are using HighValueSlots for lowvaluetarget (which is ok)
                    // but we now need 1 slot back to target our PreferredTarget
                }

                try
                {
                    if (highValueTargetsTargeted.Count(t => t.Id != preferredId) >= maxHighValueTargets)
                    {
                        //unlockThisHighValueTarget = Cache.Instance.GetBestWeaponTargets((double)Distances.OnGridWithMe).Where(t => t.IsTarget && highValueTargetsTargeted.Any(e => t.Id == e.Id)).LastOrDefault();

                        unlockThisHighValueTarget = highValueTargetsTargeted.Where(h => (h.IsTarget && h.IsIgnored)
                                                                                        ||
                                                                                        (h.IsTarget &&
                                                                                         (!h.isPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                          !h.isPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                          !h.IsPriorityWarpScrambler && !h.IsInOptimalRange &&
                                                                                          PotentialCombatTargets.Count() >= 3))
                                                                                        ||
                                                                                        (h.IsTarget &&
                                                                                         (!h.isPreferredPrimaryWeaponTarget && !h.IsDronePriorityTarget &&
                                                                                          h.IsHigherPriorityPresent && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                          highValueTargetsTargeted.Count() == maxHighValueTargets) &&
                                                                                         !h.IsPriorityWarpScrambler))
                            .OrderByDescending(t => t.Distance > MaxRange)
                            .ThenByDescending(t => t.Distance)
                            .FirstOrDefault();
                    }
                }
                catch (NullReferenceException)
                {
                }
            }
            else
            {
                try
                {
                    unlockThisHighValueTarget = highValueTargetsTargeted.Where(h => h.IsTarget && h.IsIgnored && !h.IsPriorityWarpScrambler)
                        .OrderByDescending(t => t.Distance > MaxRange)
                        .ThenByDescending(t => t.Distance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }
            }

            if (unlockThisHighValueTarget != null)
            {
                Logging.Logging.Log("Unlocking HighValue " + unlockThisHighValueTarget.Name + "[" + Math.Round(unlockThisHighValueTarget.Distance / 1000, 0) +
                    "k] myTargtingRange:[" + MaxTargetRange + "] myWeaponRange[:" + Cache.Instance.WeaponRange + "] to make room for [" + reason + "]");
                unlockThisHighValueTarget.UnlockTarget("Combat [TargetCombatants]");
                //Cache.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                return false;
            }

            if (!OutOfRangeOnly)
            {
                //Logging.Log("Combat [TargetCombatants]" + module, "We don't have a spot open to target [" + reason + "], this could be a problem", Logging.Orange);
                //Cache.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
            }

            return true;
        }

        private static bool UnlockLowValueTarget(string module, string reason, bool OutOfWeaponsRange = false)
        {
            EntityCache unlockThisLowValueTarget = null;
            if (!OutOfWeaponsRange)
            {
                try
                {
                    unlockThisLowValueTarget = lowValueTargetsTargeted.Where(h => (h.IsTarget && h.IsIgnored)
                                                                                  ||
                                                                                  (h.IsTarget &&
                                                                                   (!h.isPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                    !h.isPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                    !h.IsPriorityWarpScrambler && !h.IsInOptimalRange &&
                                                                                    PotentialCombatTargets.Count() >= 3))
                                                                                  ||
                                                                                  (h.IsTarget &&
                                                                                   (!h.isPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                    !h.isPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                    !h.IsPriorityWarpScrambler &&
                                                                                    lowValueTargetsTargeted.Count() == maxLowValueTargets))
                                                                                  ||
                                                                                  (h.IsTarget &&
                                                                                   (!h.isPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                    !h.isPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                    h.IsHigherPriorityPresent && !h.IsPriorityWarpScrambler &&
                                                                                    lowValueTargetsTargeted.Count() == maxLowValueTargets)))
                        .OrderByDescending(t => t.Distance < (Drones.UseDrones ? Drones.MaxDroneRange : Cache.Instance.WeaponRange))
                        .ThenByDescending(t => t.Nearest5kDistance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }
            }
            else
            {
                try
                {
                    unlockThisLowValueTarget = lowValueTargetsTargeted.Where(h => (h.IsTarget && h.IsIgnored)
                                                                                  ||
                                                                                  (h.IsTarget &&
                                                                                   (!h.isPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                    !h.isPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                    h.IsHigherPriorityPresent && !h.IsPriorityWarpScrambler && !h.IsReadyToShoot &&
                                                                                    lowValueTargetsTargeted.Count() == maxLowValueTargets)))
                        .OrderByDescending(t => t.Distance < (Drones.UseDrones ? Drones.MaxDroneRange : Cache.Instance.WeaponRange))
                        .ThenByDescending(t => t.Nearest5kDistance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }
            }

            if (unlockThisLowValueTarget != null)
            {
                Logging.Logging.Log("Unlocking LowValue " + unlockThisLowValueTarget.Name + "[" + Math.Round(unlockThisLowValueTarget.Distance / 1000, 0) + "k] myTargtingRange:[" +
                    MaxTargetRange + "] myWeaponRange[:" + Cache.Instance.WeaponRange + "] to make room for [" + reason + "]");
                unlockThisLowValueTarget.UnlockTarget("Combat [TargetCombatants]");
                //Cache.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                return false;
            }

            if (!OutOfWeaponsRange)
            {
                //Logging.Log("Combat [TargetCombatants]" + module, "We don't have a spot open to target [" + reason + "], this could be a problem", Logging.Orange);
                //Cache.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
            }

            return true;
        }

        /// <summary>
        ///     Target combatants
        /// </summary>
        private static void TargetCombatants()
        {
            if ((Cache.Instance.InSpace && Cache.Instance.InWarp) // When in warp we should not try to target anything
                || Cache.Instance.InStation //How can we target if we are in a station?
                || DateTime.UtcNow < Time.Instance.NextTargetAction //if we just did something wait a fraction of a second
                //|| !Cache.Instance.OpenCargoHold("Combat.TargetCombatants") //If we can't open our cargohold then something MUST be wrong
                || Logging.Logging.DebugDisableTargetCombatants
                )
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log(
"");
                return;
            }

            #region ECM Jamming checks

            //
            // First, can we even target?
            // We are ECM'd / jammed, forget targeting anything...
            //
            if (Cache.Instance.MaxLockedTargets == 0)
            {
                if (!_isJammed)
                {
                    Logging.Logging.Log("We are jammed and can not target anything");
                }

                _isJammed = true;
                return;
            }

            if (_isJammed)
            {
                // Clear targeting list as it does not apply
                Cache.Instance.TargetingIDs.Clear();
                Drones.LastTargetIDDronesEngaged = null;
                Logging.Logging.Log("We are no longer jammed, reTargeting");
            }

            _isJammed = false;

            #endregion

            #region Current active targets/targeting

            //
            // What do we currently have targeted?
            // Get our current targets/targeting
            //

            // Get lists of the current high and low value targets
            try
            {
                highValueTargetsTargeted = Cache.Instance.EntitiesOnGrid.Where(t => (t.IsTarget || t.IsTargeting) && (t.IsHighValueTarget)).ToList();
            }
            catch (NullReferenceException)
            {
            }

            try
            {
                lowValueTargetsTargeted = Cache.Instance.EntitiesOnGrid.Where(t => (t.IsTarget || t.IsTargeting) && (t.IsLowValueTarget)).ToList();
            }
            catch (NullReferenceException)
            {
            }

            var targetsTargeted = highValueTargetsTargeted.Count() + lowValueTargetsTargeted.Count();


            //Logging.Log("Combat.TargetCombatants", "Rat target amount: [" + targetsTargeted +  "] Total target amount [" + Cache.Instance.TotalTargetsandTargeting.Count() + "]", Logging.White);

            #endregion

            #region Remove any target that is out of range (lower of Weapon Range or targeting range, definitely matters if damped)

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Remove any target that is out of range");
            //
            // If it is currently out of our weapon range unlock it for now, unless it is one of our preferred targets which should technically only happen during kill type actions
            //
            if (Cache.Instance.Targets.Any() && Cache.Instance.Targets.Count() > 1)
            {
                //
                // unlock low value targets that are out of range or ignored
                //
                if (!UnlockLowValueTarget("Combat.TargetCombatants", "[lowValue]OutOfRange or Ignored", true)) return;
                //
                // unlock high value targets that are out of range or ignored
                //
                if (!UnlockHighValueTarget("Combat.TargetCombatants", "[highValue]OutOfRange or Ignored", true)) return;
            }

            #endregion Remove any target that is too far out of range (Weapon Range)

            #region Priority Target Handling

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Priority Target Handling");
            //
            // Now lets deal with the priority targets
            //
            if (PrimaryWeaponPriorityEntities != null && PrimaryWeaponPriorityEntities.Any())
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("We have [" + PrimaryWeaponPriorityEntities.Count() + "] PWPT. We have [" + Cache.Instance.TotalTargetsandTargeting.Count() +
                        "] TargetsAndTargeting. We have [" + PrimaryWeaponPriorityEntities.Count(i => i.IsTarget) + "] PWPT that are already targeted");
                var PrimaryWeaponsPriorityTargetUnTargeted = PrimaryWeaponPriorityEntities.Count() -
                                                             Cache.Instance.TotalTargetsandTargeting.Count(t => PrimaryWeaponPriorityEntities.Contains(t));

                if (PrimaryWeaponsPriorityTargetUnTargeted > 0)
                {
                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("if (PrimaryWeaponsPriorityTargetUnTargeted > 0)");
                    //
                    // unlock a lower priority entity if needed
                    //
                    if (!UnlockHighValueTarget("Combat.TargetCombatants", "PrimaryWeaponPriorityTargets")) return;

                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("if (!UnlockHighValueTarget(Combat.TargetCombatants, PrimaryWeaponPriorityTargets return;");

                    IEnumerable<EntityCache> __primaryWeaponPriorityEntities = PrimaryWeaponPriorityEntities.Where(
                        t => t.IsTargetWeCanShootButHaveNotYetTargeted)
                        .OrderByDescending(c => c.IsLastTargetPrimaryWeaponsWereShooting)
                        .ThenByDescending(c => c.IsInOptimalRange)
                        .ThenBy(c => c.Distance);

                    if (__primaryWeaponPriorityEntities.Any())
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: [" + __primaryWeaponPriorityEntities.Count() + "] primaryWeaponPriority targets");

                        foreach (var primaryWeaponPriorityEntity in __primaryWeaponPriorityEntities)
                        {
                            // Have we reached the limit of high value targets?
                            if (highValueTargetsTargeted.Count() >= maxHighValueTargets)
                            {
                                if (Logging.Logging.DebugTargetCombatants)
                                    Logging.Logging.Log("DebugTargetCombatants: __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] >= maxHighValueTargets [" +
                                        maxHighValueTargets + "]");
                                break;
                            }

                            if (primaryWeaponPriorityEntity.Distance < MaxRange
                                && primaryWeaponPriorityEntity.IsReadyToTarget)
                            {
                                if (Cache.Instance.TotalTargetsandTargetingCount < Cache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                {
                                    if (primaryWeaponPriorityEntity.LockTarget("TargetCombatants.PrimaryWeaponPriorityEntity"))
                                    {
                                        Logging.Logging.Log("Targeting primary weapon priority target [" + primaryWeaponPriorityEntity.Name + "][" +
                                            primaryWeaponPriorityEntity.MaskedId + "][" + Math.Round(primaryWeaponPriorityEntity.Distance / 1000, 0) + "k away]");
                                        Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                                        if (Cache.Instance.TotalTargetsandTargeting.Any() &&
                                            (Cache.Instance.TotalTargetsandTargeting.Count() >= Cache.Instance.MaxLockedTargets))
                                        {
                                            Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                                        }

                                        return;
                                    }
                                }

                                if (Cache.Instance.TotalTargetsandTargetingCount >= Cache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                {
                                    if (lowValueTargetsTargeted.Any())
                                    {
                                        UnlockLowValueTarget("TargetCombatants", "PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    if (highValueTargetsTargeted.Any())
                                    {
                                        UnlockHighValueTarget("TargetCombatants", "PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    //
                                    // if we have nothing to unlock just continue...
                                    //
                                }
                            }

                            continue;
                        }
                    }
                    else
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: 0 primaryWeaponPriority targets");
                    }
                }
            }

            #endregion

            #region Drone Priority Target Handling

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Drone Priority Target Handling");
            //
            // Now lets deal with the priority targets
            //
            if (Drones.DronePriorityTargets.Any())
            {
                var DronesPriorityTargetUnTargeted = Drones.DronePriorityEntities.Count() -
                                                     Cache.Instance.TotalTargetsandTargeting.Count(t => Drones.DronePriorityEntities.Contains(t));

                if (DronesPriorityTargetUnTargeted > 0)
                {
                    if (!UnlockLowValueTarget("Combat.TargetCombatants", "DronePriorityTargets")) return;

                    IEnumerable<EntityCache> _dronePriorityTargets = Drones.DronePriorityEntities.Where(t => t.IsTargetWeCanShootButHaveNotYetTargeted)
                        .OrderByDescending(c => c.IsInDroneRange)
                        .ThenByDescending(c => c.IsLastTargetPrimaryWeaponsWereShooting)
                        .ThenBy(c => c.Nearest5kDistance);

                    if (_dronePriorityTargets.Any())
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: [" + _dronePriorityTargets.Count() + "] dronePriority targets");

                        foreach (var dronePriorityEntity in _dronePriorityTargets)
                        {
                            // Have we reached the limit of low value targets?
                            if (lowValueTargetsTargeted.Count() >= maxLowValueTargets)
                            {
                                if (Logging.Logging.DebugTargetCombatants)
                                    Logging.Logging.Log("DebugTargetCombatants: __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() + "] >= maxLowValueTargets [" +
                                        maxLowValueTargets + "]");
                                break;
                            }

                            if (dronePriorityEntity.Nearest5kDistance < Drones.MaxDroneRange
                                && dronePriorityEntity.IsReadyToTarget
                                && dronePriorityEntity.Nearest5kDistance < LowValueTargetsHaveToBeWithinDistance
                                && !dronePriorityEntity.IsIgnored)
                            {
                                if (Cache.Instance.TotalTargetsandTargetingCount < Cache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                {
                                    if (dronePriorityEntity.LockTarget("TargetCombatants.PrimaryWeaponPriorityEntity"))
                                    {
                                        Logging.Logging.Log("Targeting primary weapon priority target [" + dronePriorityEntity.Name + "][" + dronePriorityEntity.MaskedId + "][" +
                                            Math.Round(dronePriorityEntity.Distance / 1000, 0) + "k away]");
                                        return;
                                    }
                                }

                                if (Cache.Instance.TotalTargetsandTargetingCount >= Cache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                {
                                    if (lowValueTargetsTargeted.Any())
                                    {
                                        UnlockLowValueTarget("TargetCombatants", "PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    if (highValueTargetsTargeted.Any())
                                    {
                                        UnlockHighValueTarget("TargetCombatants", "PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                                    //
                                    // if we have nothing to unlock just continue...
                                    //
                                }
                            }

                            continue;
                        }
                    }
                    else
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: 0 primaryWeaponPriority targets");
                    }
                }
            }

            #endregion

            #region Preferred Primary Weapon target handling

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Preferred Primary Weapon target handling");
            //
            // Lets deal with our preferred targets next (in other words what Q is actively trying to shoot or engage drones on)
            //

            if (PreferredPrimaryWeaponTarget != null)
            {
                if (PreferredPrimaryWeaponTarget.IsIgnored)
                {
                    Logging.Logging.Log("if (Combat.PreferredPrimaryWeaponTarget.IsIgnored) Combat.PreferredPrimaryWeaponTarget = null;");
                    //Combat.PreferredPrimaryWeaponTarget = null;
                }

                if (PreferredPrimaryWeaponTarget != null)
                {
                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("if (Combat.PreferredPrimaryWeaponTarget != null)");
                    if (Cache.Instance.EntitiesOnGrid.Any(e => e.Id == PreferredPrimaryWeaponTarget.Id))
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("if (Cache.Instance.Entities.Any(i => i.Id == Combat.PreferredPrimaryWeaponTarget.Id))");

                        if (Logging.Logging.DebugTargetCombatants)
                        {
                            Logging.Logging.Log(
"");
                        }

                        if (PreferredPrimaryWeaponTarget.IsReadyToTarget)
                        {
                            if (Logging.Logging.DebugTargetCombatants)
                                Logging.Logging.Log("if (Combat.PreferredPrimaryWeaponTarget.IsReadyToTarget)");
                            if (PreferredPrimaryWeaponTarget.Distance <= MaxRange)
                            {
                                if (Logging.Logging.DebugTargetCombatants)
                                    Logging.Logging.Log("if (Combat.PreferredPrimaryWeaponTarget.Distance <= Combat.MaxRange)");
                                //
                                // unlock a lower priority entity if needed
                                //
                                if (highValueTargetsTargeted.Count() >= maxHighValueTargets && maxHighValueTargets > 1)
                                {
                                    if (Logging.Logging.DebugTargetCombatants)
                                        Logging.Logging.Log("DebugTargetCombatants: we have enough targets targeted [" + Cache.Instance.TotalTargetsandTargeting.Count() + "]");
                                    if (!UnlockLowValueTarget("Combat.TargetCombatants", "PreferredPrimaryWeaponTarget")
                                        || !UnlockHighValueTarget("Combat.TargetCombatants", "PreferredPrimaryWeaponTarget"))
                                    {
                                        return;
                                    }

                                    return;
                                }

                                if (PreferredPrimaryWeaponTarget.LockTarget("TargetCombatants.PreferredPrimaryWeaponTarget"))
                                {
                                    Logging.Logging.Log("Targeting preferred primary weapon target [" + PreferredPrimaryWeaponTarget.Name + "][" +
                                        PreferredPrimaryWeaponTarget.MaskedId + "][" + Math.Round(PreferredPrimaryWeaponTarget.Distance / 1000, 0) + "k away]");
                                    Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                                    if (Cache.Instance.TotalTargetsandTargeting.Any() &&
                                        (Cache.Instance.TotalTargetsandTargeting.Count() >= Cache.Instance.MaxLockedTargets))
                                    {
                                        Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                                    }

                                    return;
                                }
                            }

                            return;
                        }
                    }
                }
            }

            #endregion

            //if (Logging.DebugTargetCombatants)
            //{
            //    Logging.Log("Combat.TargetCombatants", "LCOs [" + Cache.Instance.Entities.Count(i => i.IsLargeCollidable) + "]", Logging.Debug);
            //    if (Cache.Instance.Entities.Any(i => i.IsLargeCollidable))
            //    {
            //        foreach (EntityCache LCO in Cache.Instance.Entities.Where(i => i.IsLargeCollidable))
            //        {
            //            Logging.Log("Combat.TargetCombatants", "LCO name [" + LCO.Name + "] Distance [" + Math.Round(LCO.Distance /1000,2) + "] TypeID [" + LCO.TypeId + "] GroupID [" + LCO.GroupId + "]", Logging.Debug);
            //        }
            //    }
            //}

            #region Preferred Drone target handling

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Preferred Drone target handling");
            //
            // Lets deal with our preferred targets next (in other words what Q is actively trying to shoot or engage drones on)
            //

            if (Drones.PreferredDroneTarget != null)
            {
                if (Drones.PreferredDroneTarget.IsIgnored)
                {
                    Drones.PreferredDroneTarget = null;
                }

                if (Drones.PreferredDroneTarget != null
                    && Cache.Instance.EntitiesOnGrid.Any(I => I.Id == Drones.PreferredDroneTarget.Id)
                    && Drones.UseDrones
                    && Drones.PreferredDroneTarget.IsReadyToTarget
                    && Drones.PreferredDroneTarget.Distance < Cache.Instance.WeaponRange
                    && Drones.PreferredDroneTarget.Nearest5kDistance <= Drones.MaxDroneRange)
                {
                    //
                    // unlock a lower priority entity if needed
                    //
                    if (lowValueTargetsTargeted.Count() >= maxLowValueTargets)
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: we have enough targets targeted [" + Cache.Instance.TotalTargetsandTargeting.Count() + "]");
                        if (!UnlockLowValueTarget("Combat.TargetCombatants", "PreferredPrimaryWeaponTarget")
                            || !UnlockHighValueTarget("Combat.TargetCombatants", "PreferredPrimaryWeaponTarget"))
                        {
                            return;
                        }

                        return;
                    }

                    if (Drones.PreferredDroneTarget.LockTarget("TargetCombatants.PreferredDroneTarget"))
                    {
                        Logging.Logging.Log("Targeting preferred drone target [" + Drones.PreferredDroneTarget.Name + "][" + Drones.PreferredDroneTarget.MaskedId + "][" +
                            Math.Round(Drones.PreferredDroneTarget.Distance / 1000, 0) + "k away]");
                        //highValueTargets.Add(primaryWeaponPriorityEntity);
                        Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                        if (Cache.Instance.TotalTargetsandTargeting.Any() &&
                            (Cache.Instance.TotalTargetsandTargeting.Count() >= Cache.Instance.MaxLockedTargets))
                        {
                            Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                        }

                        return;
                    }
                }
            }

            #endregion

            #region Do we have enough targets?

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Do we have enough targets? Locked [" + Cache.Instance.Targets.Count() + "] Locking [" +
                    Cache.Instance.Targeting.Count() + "] Total [" + Cache.Instance.TotalTargetsandTargeting.Count() + "] Slots Total [" +
                    Cache.Instance.MaxLockedTargets + "]");
            //
            // OK so now that we are done dealing with preferred and priorities for now, lets see if we can target anything else
            // First lets see if we have enough targets already
            //

            var highValueSlotsreservedForPriorityTargets = 1;
            var lowValueSlotsreservedForPriorityTargets = 1;

            if (Cache.Instance.MaxLockedTargets <= 4)
            {
                //
                // With a ship/toon combination that has 4 or less slots you really do not have room to reserve 2 slots for priority targets
                //
                highValueSlotsreservedForPriorityTargets = 0;
                lowValueSlotsreservedForPriorityTargets = 0;
            }

            if (maxHighValueTargets <= 2)
            {
                //
                // do not reserve targeting slots if we have none to spare
                //
                highValueSlotsreservedForPriorityTargets = 0;
            }

            if (maxLowValueTargets <= 2)
            {
                //
                // do not reserve targeting slots if we have none to spare
                //
                lowValueSlotsreservedForPriorityTargets = 0;
            }


            if ((highValueTargetsTargeted.Count() >= maxHighValueTargets - highValueSlotsreservedForPriorityTargets)
                && lowValueTargetsTargeted.Count() >= maxLowValueTargets - lowValueSlotsreservedForPriorityTargets)
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: we have enough targets targeted [" + Cache.Instance.TotalTargetsandTargeting.Count() +
                        "] __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() +
                        "] maxHighValueTargets [" + maxHighValueTargets + "] maxLowValueTargets [" + maxLowValueTargets + "]");
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] maxHighValueTargets [" +
                        maxHighValueTargets + "] highValueSlotsreservedForPriorityTargets [" + highValueSlotsreservedForPriorityTargets + "]");
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() + "] maxLowValueTargets [" + maxLowValueTargets +
                        "] lowValueSlotsreservedForPriorityTargets [" + lowValueSlotsreservedForPriorityTargets + "]");

                return;
            }

            if (Cache.Instance.TotalTargetsandTargetingCount >= Cache.Instance.MaxLockedTargets)
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: we have enough targets targeted... [" + Cache.Instance.TotalTargetsandTargeting.Count() +
                        "] __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() +
                        "] maxHighValueTargets [" + maxHighValueTargets + "] maxLowValueTargets [" + maxLowValueTargets + "]");
                return;
            }

            #endregion

            #region Aggro Handling

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: Aggro Handling");
            //
            // OHHHH We are still here? OK Cool lets deal with things that are already targeting me
            //
            TargetingMe = TargetedBy.Where(t => t.Distance < (double) Distances.OnGridWithMe
                                                && t.CategoryId != (int) CategoryID.Asteroid
                                                && t.IsTargetingMeAndNotYetTargeted
                                                && (!t.IsSentry || (t.IsSentry && KillSentries) || (t.IsSentry && t.IsEwarTarget))
                                                && t.Nearest5kDistance < MaxRange).ToList();

            var highValueTargetingMe = TargetingMe.Where(t => (t.IsHighValueTarget))
                .OrderByDescending(t => !t.IsNPCCruiser) //prefer battleships
                .ThenByDescending(t => t.IsBattlecruiser && t.IsLastTargetPrimaryWeaponsWereShooting)
                .ThenByDescending(t => t.IsBattleship && t.IsLastTargetPrimaryWeaponsWereShooting)
                .ThenByDescending(t => t.IsBattlecruiser)
                .ThenByDescending(t => t.IsBattleship)
                .ThenBy(t => t.Nearest5kDistance).ToList();

            var LockedTargetsThatHaveHighValue = Cache.Instance.Targets.Count(t => (t.IsHighValueTarget));

            var lowValueTargetingMe = TargetingMe.Where(t => t.IsLowValueTarget)
                .OrderByDescending(t => !t.IsNPCCruiser) //prefer frigates
                .ThenBy(t => t.Nearest5kDistance).ToList();

            var LockedTargetsThatHaveLowValue = Cache.Instance.Targets.Count(t => (t.IsLowValueTarget));

            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("TargetingMe [" + TargetingMe.Count() + "] lowValueTargetingMe [" + lowValueTargetingMe.Count() + "] targeted [" +
                    LockedTargetsThatHaveLowValue + "] :::  highValueTargetingMe [" + highValueTargetingMe.Count() + "] targeted [" +
                    LockedTargetsThatHaveHighValue + "] LCOs [" + Cache.Instance.EntitiesOnGrid.Count(e => e.IsLargeCollidable) + "]");

            // High Value
            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: foreach (EntityCache entity in highValueTargetingMe)");

            if (highValueTargetingMe.Any())
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: [" + highValueTargetingMe.Count() + "] highValueTargetingMe targets");

                var HighValueTargetsTargetedThisCycle = 1;
                foreach (var highValueTargetingMeEntity in highValueTargetingMe.Where(t => t.IsReadyToTarget && t.Nearest5kDistance < MaxRange))
                {
                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("DebugTargetCombatants: [" + HighValueTargetsTargetedThisCycle + "][" + highValueTargetingMeEntity.Name + "][" +
                            Math.Round(highValueTargetingMeEntity.Distance / 1000, 2) + "k][groupID" + highValueTargetingMeEntity.GroupId + "]");
                    // Have we reached the limit of high value targets?
                    if (highValueTargetsTargeted.Count() >= maxHighValueTargets)
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: __highValueTargetsTargeted.Count() [" + highValueTargetsTargeted.Count() + "] maxHighValueTargets [" +
                                maxHighValueTargets + "], done for this iteration");
                        break;
                    }

                    if (HighValueTargetsTargetedThisCycle >= 4)
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: HighValueTargetsTargetedThisCycle [" + HighValueTargetsTargetedThisCycle + "], done for this iteration");
                        break;
                    }

                    //We need to make sure we do not have too many low value targets filling our slots
                    if (highValueTargetsTargeted.Count() < maxHighValueTargets && lowValueTargetsTargeted.Count() > maxLowValueTargets)
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] < maxHighValueTargets [" +
                                maxHighValueTargets + "] && __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() + "] > maxLowValueTargets [" +
                                maxLowValueTargets + "], try to unlock a low value target, and return.");
                        UnlockLowValueTarget("Combat.TargetCombatants", "HighValueTarget");
                        return;
                    }

                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log((highValueTargetingMeEntity.Distance < MaxRange).ToString()
                                                                       + (highValueTargetingMeEntity.IsReadyToTarget).ToString()
                                                                       + (highValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail).ToString()
                                                                       + (!highValueTargetingMeEntity.IsIgnored).ToString());

                    if (highValueTargetingMeEntity != null
                        && highValueTargetingMeEntity.Distance < MaxRange
                        && highValueTargetingMeEntity.IsReadyToTarget
                        && highValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail
                        && !highValueTargetingMeEntity.IsIgnored
                        && highValueTargetingMeEntity.LockTarget("TargetCombatants.HighValueTargetingMeEntity"))
                    {
                        HighValueTargetsTargetedThisCycle++;
                        Logging.Logging.Log("Targeting high value target [" + highValueTargetingMeEntity.Name + "][" + highValueTargetingMeEntity.MaskedId + "][" +
                            Math.Round(highValueTargetingMeEntity.Distance / 1000, 0) + "k away] highValueTargets.Count [" + highValueTargetsTargeted.Count() +
                            "]");
                        Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                        if (Cache.Instance.TotalTargetsandTargeting.Any() &&
                            (Cache.Instance.TotalTargetsandTargeting.Count() >= Cache.Instance.MaxLockedTargets))
                        {
                            Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                        }

                        if (HighValueTargetsTargetedThisCycle > 2)
                        {
                            if (Logging.Logging.DebugTargetCombatants)
                                Logging.Logging.Log("DebugTargetCombatants: HighValueTargetsTargetedThisCycle [" + HighValueTargetsTargetedThisCycle + "] > 3, return");
                            return;
                        }
                    }

                    continue;
                }

                if (HighValueTargetsTargetedThisCycle > 1)
                {
                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("DebugTargetCombatants: HighValueTargetsTargetedThisCycle [" + HighValueTargetsTargetedThisCycle + "] > 1, return");
                    return;
                }
            }
            else
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: 0 highValueTargetingMe targets");
            }

            // Low Value
            if (Logging.Logging.DebugTargetCombatants)
                Logging.Logging.Log("DebugTargetCombatants: foreach (EntityCache entity in lowValueTargetingMe)");

            if (lowValueTargetingMe.Any())
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: [" + lowValueTargetingMe.Count() + "] lowValueTargetingMe targets");

                var LowValueTargetsTargetedThisCycle = 1;
                foreach (
                    var lowValueTargetingMeEntity in
                        lowValueTargetingMe.Where(t => !t.IsTarget && !t.IsTargeting && t.Nearest5kDistance < LowValueTargetsHaveToBeWithinDistance)
                            .OrderByDescending(i => i.IsLastTargetDronesWereShooting)
                            .ThenBy(i => i.IsLastTargetPrimaryWeaponsWereShooting))
                {
                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("DebugTargetCombatants: lowValueTargetingMe [" + LowValueTargetsTargetedThisCycle + "][" + lowValueTargetingMeEntity.Name + "][" +
                            Math.Round(lowValueTargetingMeEntity.Distance / 1000, 2) + "k] groupID [ " + lowValueTargetingMeEntity.GroupId + "]");

                    // Have we reached the limit of low value targets?
                    if (lowValueTargetsTargeted.Count() >= maxLowValueTargets)
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: __lowValueTargetsTargeted.Count() [" + lowValueTargetsTargeted.Count() + "] maxLowValueTargets [" +
                                maxLowValueTargets + "], done for this iteration");
                        break;
                    }

                    if (LowValueTargetsTargetedThisCycle >= 3)
                    {
                        if (Logging.Logging.DebugTargetCombatants)
                            Logging.Logging.Log("DebugTargetCombatants: LowValueTargetsTargetedThisCycle [" + LowValueTargetsTargetedThisCycle + "], done for this iteration");
                        break;
                    }

                    //We need to make sure we do not have too many high value targets filling our slots
                    if (lowValueTargetsTargeted.Count() < maxLowValueTargets && highValueTargetsTargeted.Count() > maxHighValueTargets)
                    {
                        UnlockLowValueTarget("Combat.TargetCombatants", "HighValueTarget");
                        return;
                    }

                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log((lowValueTargetingMeEntity.Distance < MaxRange).ToString()
                                                                       + (lowValueTargetingMeEntity.IsReadyToTarget).ToString()
                                                                       + (lowValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail).ToString()
                                                                       + (!lowValueTargetingMeEntity.IsIgnored).ToString());

                    if (lowValueTargetingMeEntity != null
                        && lowValueTargetingMeEntity.Distance < Cache.Instance.WeaponRange
                        && lowValueTargetingMeEntity.IsReadyToTarget
                        && lowValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail
                        && lowValueTargetingMeEntity.Nearest5kDistance < LowValueTargetsHaveToBeWithinDistance
                        && !lowValueTargetingMeEntity.IsIgnored
                        && lowValueTargetingMeEntity.LockTarget("TargetCombatants.LowValueTargetingMeEntity"))
                    {
                        LowValueTargetsTargetedThisCycle++;
                        Logging.Logging.Log("Targeting low  value target [" + lowValueTargetingMeEntity.Name + "][" + lowValueTargetingMeEntity.MaskedId + "][" +
                            Math.Round(lowValueTargetingMeEntity.Distance / 1000, 0) + "k away] lowValueTargets.Count [" + lowValueTargetsTargeted.Count() + "]");
                        //lowValueTargets.Add(lowValueTargetingMeEntity);
                        Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                        if (Cache.Instance.TotalTargetsandTargeting.Any() &&
                            (Cache.Instance.TotalTargetsandTargeting.Count() >= Cache.Instance.MaxLockedTargets))
                        {
                            Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                        }
                        if (LowValueTargetsTargetedThisCycle > 2)
                        {
                            if (Logging.Logging.DebugTargetCombatants)
                                Logging.Logging.Log("DebugTargetCombatants: LowValueTargetsTargetedThisCycle [" + LowValueTargetsTargetedThisCycle + "] > 2, return");
                            return;
                        }
                    }

                    continue;
                }

                if (LowValueTargetsTargetedThisCycle > 1)
                {
                    if (Logging.Logging.DebugTargetCombatants)
                        Logging.Logging.Log("DebugTargetCombatants: if (LowValueTargetsTargetedThisCycle > 1)");
                    return;
                }
            }
            else
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: 0 lowValueTargetingMe targets");
            }

            //
            // If we have 2 PotentialCombatTargets targeted at this point return... we do not want to target anything that is not yet aggressed if we have something aggressed.
            // or are in the middle of attempting to aggro something
            // 
//			if (Combat.PotentialCombatTargets.Count(e => e.IsTarget) > 1 || (Cache.Instance.MaxLockedTargets < 2 && Cache.Instance.Targets.Any()))
//			{
//				if (Logging.DebugTargetCombatants) Logging.Log("Combat.TargetCombatants", "DebugTargetCombatants: We already have [" + Combat.PotentialCombatTargets.Count(e => e.IsTarget) + "] PotentialCombatTargets Locked. Do not aggress non aggressed NPCs until we have no targets", Logging.Debug);
//				return;
//			}

            #endregion

            #region All else fails grab an unlocked target that is not yet targeting me

            //
            // Ok, now that that is all handled lets grab the closest non aggressed mob and pew
            // Build a list of things not yet targeting me and not yet targeted
            //

            NotYetTargetingMeAndNotYetTargeted = PotentialCombatTargets.Where(e => e.IsNotYetTargetingMeAndNotYetTargeted)
                .OrderBy(t => t.Nearest5kDistance)
                .ToList();

            if (NotYetTargetingMeAndNotYetTargeted.Any())
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: [" + NotYetTargetingMeAndNotYetTargeted.Count() + "] IsNotYetTargetingMeAndNotYetTargeted targets");

                foreach (var TargetThisNotYetAggressiveNPC in NotYetTargetingMeAndNotYetTargeted)
                {
                    if (TargetThisNotYetAggressiveNPC != null
                        && TargetThisNotYetAggressiveNPC.IsReadyToTarget
                        && TargetThisNotYetAggressiveNPC.IsInOptimalRangeOrNothingElseAvail
                        && TargetThisNotYetAggressiveNPC.Nearest5kDistance < MaxRange
                        && !TargetThisNotYetAggressiveNPC.IsIgnored
                        && TargetThisNotYetAggressiveNPC.LockTarget("TargetCombatants.TargetThisNotYetAggressiveNPC"))
                    {
                        Logging.Logging.Log("Targeting non-aggressed NPC target [" + TargetThisNotYetAggressiveNPC.Name + "][GroupID: " + TargetThisNotYetAggressiveNPC.GroupId +
                            "][TypeID: " + TargetThisNotYetAggressiveNPC.TypeId + "][" + TargetThisNotYetAggressiveNPC.MaskedId + "][" +
                            Math.Round(TargetThisNotYetAggressiveNPC.Distance / 1000, 0) + "k away]");
                        Time.Instance.NextTargetAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.TargetDelay_milliseconds);
                        if (Cache.Instance.TotalTargetsandTargeting.Any() &&
                            (Cache.Instance.TotalTargetsandTargeting.Count() >= Cache.Instance.MaxLockedTargets))
                        {
                            Time.Instance.NextTargetAction = DateTime.UtcNow.AddSeconds(Time.Instance.TargetsAreFullDelay_seconds);
                        }

                        return;
                    }
                }
            }
            else
            {
                if (Logging.Logging.DebugTargetCombatants)
                    Logging.Logging.Log("DebugTargetCombatants: 0 IsNotYetTargetingMeAndNotYetTargeted targets");
            }

            return;

            #endregion
        }

        public static void ProcessState()
        {
            try
            {
                if (DateTime.UtcNow < _lastCombatProcessState.AddMilliseconds(350) || Logging.Logging.DebugDisableCombat)
                    //if it has not been 500ms since the last time we ran this ProcessState return. We can't do anything that close together anyway
                {
                    if (Logging.Logging.DebugCombat)
                        Logging.Logging.Log("if (DateTime.UtcNow < _lastCombatProcessState.AddMilliseconds(350) || Logging.DebugDisableCombat)");
                    return;
                }

                _lastCombatProcessState = DateTime.UtcNow;

                if (Cache.Instance.InSpace && Cache.Instance.InWarp)
                {
                    icount = 0;
                }

                //Cache.Instance.InvalidateCache(); // temporarily


                if ((_States.CurrentCombatState != CombatState.Idle ||
                     _States.CurrentCombatState != CombatState.OutOfAmmo) &&
                    (Cache.Instance.InStation || // There is really no combat in stations (yet)
                     !Cache.Instance.InSpace || // if we are not in space yet, wait...
                     Cache.Instance.ActiveShip.Entity == null || // What? No ship entity?
                     Cache.Instance.ActiveShip.Entity.IsCloaked)) // There is no combat when cloaked
                {
                    _States.CurrentCombatState = CombatState.Idle;
                    if (Logging.Logging.DebugCombat)
                        Logging.Logging.Log("NotIdle, NotOutOfAmmo and InStation or NotInspace or ActiveShip is null or cloaked");
                    return;
                }

                if (Cache.Instance.InStation)
                {
                    _States.CurrentCombatState = CombatState.Idle;
                    if (Logging.Logging.DebugCombat) Logging.Logging.Log("We are in station, do nothing");
                    return;
                }

                try
                {
                    if (!Cache.Instance.InWarp &&
                        !Cache.Instance.MyShipEntity.IsFrigate &&
                        !Cache.Instance.MyShipEntity.IsCruiser &&
                        Cache.Instance.ActiveShip.GivenName != Settings.Instance.SalvageShipName &&
                        Cache.Instance.ActiveShip.GivenName != Settings.Instance.TransportShipName &&
                        Cache.Instance.ActiveShip.GivenName != Settings.Instance.TravelShipName &&
                        Cache.Instance.ActiveShip.GivenName != Settings.Instance.MiningShipName)
                    {
                        //
                        // we are not in something light and fast so assume we need weapons and assume we should be in the defined combatship
                        //
                        if (!Cache.Instance.Weapons.Any() && Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(30))
                        {
                            Logging.Logging.Log("Your Current ship [" + Cache.Instance.ActiveShip.GivenName + "] has no weapons!");
                            _States.CurrentCombatState = CombatState.OutOfAmmo;
                        }

                        if (Cache.Instance.ActiveShip.GivenName.ToLower() != CombatShipName.ToLower())
                        {
                            Logging.Logging.Log("Your Current ship [" + Cache.Instance.ActiveShip.GivenName + "] GroupID [" + Cache.Instance.MyShipEntity.GroupId + "] TypeID [" +
                                Cache.Instance.MyShipEntity.TypeId + "] is not the CombatShipName [" + CombatShipName + "]");
                            _States.CurrentCombatState = CombatState.OutOfAmmo;
                        }
                    }

                    //
                    // we are in something light and fast so assume we do not need weapons and assume we do not need to be in the defined combatship
                    //
                }
                catch (Exception exception)
                {
                    if (Logging.Logging.DebugExceptions)
                        Logging.Logging.Log("if (!Cache.Instance.Weapons.Any() && Cache.Instance.ActiveShip.GivenName == Settings.Instance.CombatShipName ) - exception [" +
                            exception + "]");
                }

                switch (_States.CurrentCombatState)
                {
                    case CombatState.CheckTargets:
                        _States.CurrentCombatState = CombatState.KillTargets;

                        TargetCombatants();

                        break;

                    case CombatState.KillTargets:

                        _States.CurrentCombatState = CombatState.CheckTargets;

                        if (Logging.Logging.DebugPreferredPrimaryWeaponTarget || Logging.Logging.DebugKillTargets)
                        {
                            if (Cache.Instance.Targets.Any())
                            {
                                if (PreferredPrimaryWeaponTarget != null)
                                {
                                    Logging.Logging.Log("PreferredPrimaryWeaponTarget [" + PreferredPrimaryWeaponTarget.Name + "][" +
                                        Math.Round(PreferredPrimaryWeaponTarget.Distance / 1000, 0) + "k][" + PreferredPrimaryWeaponTarget.MaskedId + "]");
                                }
                                else
                                {
                                    Logging.Logging.Log("PreferredPrimaryWeaponTarget [ null ]");
                                }
                            }
                        }

                        EntityCache killTarget = null;
                        if (PreferredPrimaryWeaponTarget != null)
                        {
                            if (Cache.Instance.Targets.Any(t => t.Id == PreferredPrimaryWeaponTarget.Id))
                            {
                                killTarget = Cache.Instance.Targets.FirstOrDefault(t => t.Id == PreferredPrimaryWeaponTarget.Id && t.Distance < MaxRange);
                            }
                        }

                        if (killTarget == null)
                        {
                            if (Cache.Instance.Targets.Any(i => !i.IsContainer && !i.IsBadIdea))
                            {
                                killTarget =
                                    Cache.Instance.Targets.Where(i => !i.IsContainer && !i.IsBadIdea && i.Distance < MaxRange)
                                        .OrderByDescending(i => i.IsInOptimalRange)
                                        .ThenByDescending(i => i.IsCorrectSizeForMyWeapons)
                                        .ThenBy(i => i.Distance)
                                        .FirstOrDefault();
                            }
                        }

                        if (killTarget != null)
                        {
                            if (!Cache.Instance.InMission || NavigateOnGrid.SpeedTank)
                            {
                                if (Logging.Logging.DebugNavigateOnGrid)
                                    Logging.Logging.Log("Navigate Toward the Closest Preferred PWPT");
                                NavigateOnGrid.NavigateIntoRange(killTarget, "Combat", Cache.Instance.normalNav);
                            }

                            if (killTarget.IsReadyToShoot)
                            {
                                icount++;
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating Bastion");
                                ActivateBastion(false);
                                    //by default this will deactivate bastion when needed, but NOT activate it, activation needs activate = true
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating Painters");
                                ActivateTargetPainters(killTarget);
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating Webs");
                                ActivateStasisWeb(killTarget);
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating WarpDisruptors");
                                ActivateWarpDisruptor(killTarget);
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating RemoteRepairers");
                                ActivateRemoteRepair(killTarget);
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating NOS/Neuts");
                                ActivateNos(killTarget);
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating SensorDampeners");
                                ActivateSensorDampeners(killTarget);
                                if (Logging.Logging.DebugKillTargets)
                                    Logging.Logging.Log("[" + icount + "] Activating Weapons");
                                ActivateWeapons(killTarget);
                                return;
                            }

                            if (Logging.Logging.DebugKillTargets)
                                Logging.Logging.Log("killTarget [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" + killTarget.MaskedId +
                                    "] is not yet ReadyToShoot, LockedTarget [" + killTarget.IsTarget + "] My MaxRange [" + Math.Round(MaxRange / 1000, 0) + "]");
                            return;
                        }

                        if (Logging.Logging.DebugKillTargets)
                            Logging.Logging.Log("We do not have a killTarget targeted, waiting");

                        //ok so we do need this, but only use it if we actually have some potential targets
                        if (PrimaryWeaponPriorityTargets.Any() ||
                            (PotentialCombatTargets.Any() && Cache.Instance.Targets.Any() && (!Cache.Instance.InMission || NavigateOnGrid.SpeedTank)))
                        {
                            GetBestPrimaryWeaponTarget(MaxRange, false, "Combat");
                            icount = 0;
                        }

                        break;

                    case CombatState.OutOfAmmo:


                        if (Cache.Instance.InStation)
                        {
                            Logging.Logging.Log("Out of ammo. Pausing questor if in station.");
                            Cache.Instance.Paused = true;
                        }

                        break;

                    case CombatState.Idle:

                        if (Cache.Instance.InSpace && //we are in space (as opposed to being in station or in limbo between systems when jumping)
                            (Cache.Instance.ActiveShip.Entity != null && // we are in a ship!
                             !Cache.Instance.ActiveShip.Entity.IsCloaked && //we are not cloaked anymore
                             Cache.Instance.ActiveShip.GivenName.ToLower() == CombatShipName.ToLower() && //we are in our combat ship
                             !Cache.Instance.InWarp)) // no longer in warp
                        {
                            _States.CurrentCombatState = CombatState.CheckTargets;
                            if (Logging.Logging.DebugCombat)
                                Logging.Logging.Log("We are in space and ActiveShip is null or Cloaked or we arent in the combatship or we are in warp");
                            return;
                        }
                        break;

                    default:

                        // Next state
                        Logging.Logging.Log("CurrentCombatState was not set thus ended up at default");
                        _States.CurrentCombatState = CombatState.CheckTargets;
                        break;
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        /// Invalidate the cached items every pulse (called from cache.invalidatecache, which itself is called every frame in questor.cs)
        public static void InvalidateCache()
        {
            try
            {
                //
                // this list of variables is cleared every pulse.
                //
                _aggressed = null;
                _combatTargets = null;
                _maxrange = null;
                _maxTargetRange = null;
                _potentialCombatTargets = null;
                _primaryWeaponPriorityTargetsPerFrameCaching = null;
                _targetedBy = null;

                _primaryWeaponPriorityEntities = null;
                _preferredPrimaryWeaponTarget = null;

                if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any())
                {
                    _primaryWeaponPriorityTargets.ForEach(pt => pt.ClearCache());
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }
    }
}