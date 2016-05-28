// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias Ut;
using System;
using System.Collections.Generic;
using System.Linq;
using DirectEve;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Lookup;
using Ut::WCF;

namespace Questor.Modules.Caching
{
    public partial class Cache
    {
        private List<EntityCache> _chargeEntities;
        private List<EntityCache> _entities;
        private IEnumerable<EntityCache> _entitiesActivelyBeingLocked;
        private List<EntityCache> _entitiesNotSelf;
        private List<EntityCache> _entitiesOnGrid;
        private List<EntityCache> _myAmmoInSpace;
        private EntityCache _myShipEntity;
        public IEnumerable<DirectSolarSystem> _solarSystems;
        public IEnumerable<EntityCache> _TotalTargetsandTargeting;
        private IEnumerable<EntityCache> _wrecks;
        public Dictionary<long, long> EntityBounty = new Dictionary<long, long>();
        public Dictionary<long, int> EntityGroupID = new Dictionary<long, int>();
        public Dictionary<long, bool> EntityHaveLootRights = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsBadIdea = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsBattleCruiser = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsBattleShip = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsCruiser = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsEntutyIShouldLeaveAlone = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsFactionWarfareNPC = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsFrigate = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsHighValueTarget = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsLargeCollidable = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsLowValueTarget = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsMiscJunk = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsNPCBattleCruiser = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsNPCBattleShip = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsNPCByGroupID = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsNPCCruiser = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsNPCFrigate = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsSentry = new Dictionary<long, bool>();
        public Dictionary<long, bool> EntityIsStargate = new Dictionary<long, bool>();

        public Dictionary<long, string> EntityNames = new Dictionary<long, string>();
        public Dictionary<long, int> EntityTypeID = new Dictionary<long, int>();

        public int MaxLockedTargets
        {
            get
            {
                try
                {
                    if (_maxLockedTargets == null)
                    {
                        _maxLockedTargets = Math.Min(Instance.DirectEve.Me.MaxLockedTargets, Instance.ActiveShip.MaxLockedTargets);
                        return (int) _maxLockedTargets;
                    }

                    return (int) _maxLockedTargets;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.MaxLockedTargets", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return -1;
                }
            }
        }

        public IEnumerable<EntityCache> myAmmoInSpace
        {
            get
            {
                if (_myAmmoInSpace == null)
                {
                    if (myCurrentAmmoInWeapon != null)
                    {
                        _myAmmoInSpace =
                            Instance.Entities.Where(e => e.Distance > 3000 && e.IsOnGridWithMe && e.TypeId == myCurrentAmmoInWeapon.TypeId && e.Velocity > 50)
                                .ToList();
                        if (_myAmmoInSpace.Any())
                        {
                            return _myAmmoInSpace;
                        }

                        return null;
                    }

                    return null;
                }

                return _myAmmoInSpace;
            }
        }

        public IEnumerable<EntityCache> Containers
        {
            get
            {
                try
                {
                    return _containers ?? (_containers = Instance.EntitiesOnGrid.Where(e =>
                        e.IsContainer &&
                        e.HaveLootRights &&
                        //(e.GroupId == (int)Group.Wreck && !e.IsWreckEmpty) &&
                        (e.Name != "Abandoned Container")).ToList());
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.Containers", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return new List<EntityCache>();
                }
            }
        }

        public IEnumerable<EntityCache> ContainersIgnoringLootRights
        {
            get
            {
                return _containers ?? (_containers = Instance.EntitiesOnGrid.Where(e =>
                    e.IsContainer &&
                    //(e.GroupId == (int)Group.Wreck && !e.IsWreckEmpty) &&
                    (e.Name != "Abandoned Container")).ToList());
            }
        }

        public IEnumerable<EntityCache> Wrecks
        {
            get { return _wrecks ?? (_wrecks = Instance.EntitiesOnGrid.Where(e => (e.GroupId == (int) Group.Wreck)).ToList()); }
        }

        public IEnumerable<EntityCache> UnlootedContainers
        {
            get
            {
                return _unlootedContainers ?? (_unlootedContainers = Instance.EntitiesOnGrid.Where(e =>
                    e.IsContainer &&
                    e.HaveLootRights &&
                    (!LootedContainers.Contains(e.Id))).OrderBy(
                        e => e.Distance).
                    ToList());
            }
        }

        public IEnumerable<EntityCache> UnlootedWrecksAndSecureCans
        {
            get
            {
                return _unlootedWrecksAndSecureCans ?? (_unlootedWrecksAndSecureCans = Instance.EntitiesOnGrid.Where(e =>
                    (e.GroupId == (int) Group.Wreck || e.GroupId == (int) Group.SecureContainer ||
                     e.GroupId == (int) Group.AuditLogSecureContainer ||
                     e.GroupId == (int) Group.FreightContainer)).OrderBy(e => e.Distance).
                    ToList());
            }
        }

        public IEnumerable<EntityCache> TotalTargetsandTargeting
        {
            get
            {
                if (_TotalTargetsandTargeting == null)
                {
                    _TotalTargetsandTargeting = Instance.Targets.Concat(Instance.Targeting.Where(i => !i.IsTarget));
                    return _TotalTargetsandTargeting;
                }

                return _TotalTargetsandTargeting;
            }
        }

        public int TotalTargetsandTargetingCount
        {
            get
            {
                if (!TotalTargetsandTargeting.Any())
                {
                    return 0;
                }

                return TotalTargetsandTargeting.Count();
            }
        }

        public int TargetingSlotsNotBeingUsedBySalvager
        {
            get
            {
                if (Salvage.MaximumWreckTargets > 0 && Instance.MaxLockedTargets >= 5)
                {
                    return Instance.MaxLockedTargets - Salvage.MaximumWreckTargets;
                }

                return Instance.MaxLockedTargets;
            }
        }

        public IEnumerable<EntityCache> Targets
        {
            get
            {
                if (_targets == null)
                {
                    _targets = Instance.EntitiesOnGrid.Where(e => e.IsTarget).ToList();
                }

                // Remove the target info from the TargetingIDs Queue (its been targeted)
                foreach (var target in _targets.Where(t => TargetingIDs.ContainsKey(t.Id)))
                {
                    TargetingIDs.Remove(target.Id);
                }

                return _targets;
            }
        }

        public IEnumerable<EntityCache> Targeting
        {
            get
            {
                if (_targeting == null)
                {
                    _targeting = Instance.EntitiesOnGrid.Where(e => e.IsTargeting || Instance.TargetingIDs.ContainsKey(e.Id)).ToList();
                }

                if (_targeting.Any())
                {
                    return _targeting;
                }

                return new List<EntityCache>();
            }
        }

        public List<long> IDsinInventoryTree
        {
            get
            {
                Logging.Logging.Log("Cache.IDsinInventoryTree", "Refreshing IDs from inventory tree, it has been longer than 30 seconds since the last refresh",
                    Logging.Logging.Teal);
                return _IDsinInventoryTree ?? (_IDsinInventoryTree = Instance.PrimaryInventoryWindow.GetIdsFromTree(false));
            }
        }

        public IEnumerable<EntityCache> EntitiesOnGrid
        {
            get
            {
                try
                {
                    if (_entitiesOnGrid == null)
                    {
                        _entitiesOnGrid = Instance.Entities.Where(e => e.IsOnGridWithMe).ToList();
                        return _entitiesOnGrid;
                    }

                    return _entitiesOnGrid;
                }
                catch (NullReferenceException)
                {
                } // this can happen during session changes

                return new List<EntityCache>();
            }
        }

        public IEnumerable<EntityCache> Entities
        {
            get
            {
                try
                {
                    if (_entities == null)
                    {
                        _entities =
                            Instance.DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId != (int) CategoryID.Charge)
                                .Select(i => new EntityCache(i))
                                .ToList();
                        return _entities;
                    }

                    return _entities;
                }
                catch (NullReferenceException)
                {
                } // this can happen during session changes

                return new List<EntityCache>();
            }
        }

        public IEnumerable<EntityCache> ChargeEntities
        {
            get
            {
                try
                {
                    if (_chargeEntities == null)
                    {
                        _chargeEntities =
                            Instance.DirectEve.Entities.Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId == (int) CategoryID.Charge)
                                .Select(i => new EntityCache(i))
                                .ToList();
                        return _chargeEntities;
                    }

                    return _chargeEntities;
                }
                catch (NullReferenceException)
                {
                } // this can happen during session changes

                return new List<EntityCache>();
            }
        }

        public IEnumerable<EntityCache> EntitiesActivelyBeingLocked
        {
            get
            {
                if (!InSpace)
                {
                    return new List<EntityCache>();
                }

                if (Instance.EntitiesOnGrid.Any())
                {
                    if (_entitiesActivelyBeingLocked == null)
                    {
                        _entitiesActivelyBeingLocked = Instance.EntitiesOnGrid.Where(i => i.IsTargeting).ToList();
                        if (_entitiesActivelyBeingLocked.Any())
                        {
                            return _entitiesActivelyBeingLocked;
                        }

                        return new List<EntityCache>();
                    }

                    return _entitiesActivelyBeingLocked;
                }

                return new List<EntityCache>();
            }
        }

        public IEnumerable<EntityCache> EntitiesNotSelf
        {
            get
            {
                if (_entitiesNotSelf == null)
                {
                    _entitiesNotSelf =
                        Instance.EntitiesOnGrid.Where(i => i.CategoryId != (int) CategoryID.Asteroid && i.Id != Instance.ActiveShip.ItemId).ToList();
                    if (_entitiesNotSelf.Any())
                    {
                        return _entitiesNotSelf;
                    }

                    return new List<EntityCache>();
                }

                return _entitiesNotSelf;
            }
        }

        public EntityCache MyShipEntity
        {
            get
            {
                if (_myShipEntity == null)
                {
                    if (!Instance.InSpace)
                    {
                        return null;
                    }

                    _myShipEntity = Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == Instance.ActiveShip.ItemId);
                    return _myShipEntity;
                }

                return _myShipEntity;
            }
        }

        public bool InSpace
        {
            get
            {
                try
                {
                    if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(7))
                    {
                        return false;
                    }

                    if (DateTime.UtcNow < Time.Instance.LastInSpace.AddMilliseconds(800))
                    {
                        //if We already set the LastInStation timestamp this iteration we do not need to check if we are in station
                        return true;
                    }

                    if (DirectEve.Session.IsInSpace)
                    {
                        if (!Instance.InStation)
                        {
                            if (Instance.DirectEve.ActiveShip.Entity != null)
                            {
                                if (DirectEve.Session.IsReady)
                                {
                                    if (Instance.Entities.Any())
                                    {
                                        Time.Instance.LastInSpace = DateTime.UtcNow;
                                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(this.CharName, "IsDocked", false);
                                        return true;
                                    }
                                }

                                if (Logging.Logging.DebugInSpace) Logging.Logging.Log("InSpace", "Session is Not Ready", Logging.Logging.Debug);
                                return false;
                            }

                            if (Logging.Logging.DebugInSpace)
                                Logging.Logging.Log("InSpace", "Cache.Instance.DirectEve.ActiveShip.Entity is null", Logging.Logging.Debug);
                            return false;
                        }

                        if (Logging.Logging.DebugInSpace) Logging.Logging.Log("InSpace", "NOT InStation is False", Logging.Logging.Debug);
                        return false;
                    }

                    if (Logging.Logging.DebugInSpace) Logging.Logging.Log("InSpace", "InSpace is False", Logging.Logging.Debug);
                    return false;
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugExceptions)
                        Logging.Logging.Log("Cache.InSpace",
                            "if (DirectEve.Session.IsInSpace && !DirectEve.Session.IsInStation && DirectEve.Session.IsReady && Cache.Instance.ActiveShip.Entity != null) <---must have failed exception was [" +
                            ex.Message + "]", Logging.Logging.Teal);
                    return false;
                }
            }
        }

        public bool InStation
        {
            get
            {
                try
                {
                    if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(7))
                    {
                        return false;
                    }

                    if (DateTime.UtcNow < Time.Instance.LastInStation.AddMilliseconds(800))
                    {
                        //if We already set the LastInStation timestamp this iteration we do not need to check if we are in station
                        return true;
                    }

                    if (DirectEve.Session.IsInStation && !DirectEve.Session.IsInSpace && DirectEve.Session.IsReady)
                    {
                        if (!Instance.Entities.Any())
                        {
                            Time.Instance.LastInStation = DateTime.UtcNow;
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(this.CharName, "IsDocked", true);
                            return true;
                        }
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugExceptions)
                        Logging.Logging.Log("Cache.InStation",
                            "if (DirectEve.Session.IsInStation && !DirectEve.Session.IsInSpace && DirectEve.Session.IsReady) <---must have failed exception was [" +
                            ex.Message + "]", Logging.Logging.Teal);
                    return false;
                }
            }
        }

        public bool InWarp
        {
            get
            {
                try
                {
                    if (Instance.InSpace && !Instance.InStation)
                    {
                        if (Instance.ActiveShip != null)
                        {
                            if (Instance.ActiveShip.Entity != null)
                            {
                                if (Instance.ActiveShip.Entity.Mode == 3)
                                {
                                    if (Instance.Modules != null && Instance.Modules.Any())
                                    {
                                        Combat.Combat.ReloadAll(Instance.MyShipEntity, true);
                                    }

                                    Time.Instance.LastInWarp = DateTime.UtcNow;
                                    return true;
                                }

                                if (Logging.Logging.DebugInWarp && !Instance.Paused)
                                    Logging.Logging.Log("Cache.InWarp",
                                        "We are not in warp.Cache.Instance.ActiveShip.Entity.Mode  is [" + (int) Instance.MyShipEntity.Mode + "]",
                                        Logging.Logging.Teal);
                                return false;
                            }

                            if (Logging.Logging.DebugInWarp && !Instance.Paused)
                                Logging.Logging.Log("Cache.InWarp",
                                    "Why are we checking for InWarp if Cache.Instance.ActiveShip.Entity is Null? (session change?)", Logging.Logging.Teal);
                            return false;
                        }

                        if (Logging.Logging.DebugInWarp && !Instance.Paused)
                            Logging.Logging.Log("Cache.InWarp", "Why are we checking for InWarp if Cache.Instance.ActiveShip is Null? (session change?)",
                                Logging.Logging.Teal);
                        return false;
                    }

                    if (Logging.Logging.DebugInWarp && !Instance.Paused)
                        Logging.Logging.Log("Cache.InWarp", "Why are we checking for InWarp while docked or between session changes?", Logging.Logging.Teal);
                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.InWarp", "InWarp check failed, exception [" + exception + "]", Logging.Logging.Teal);
                }

                return false;
            }
        }

        public List<EntityCache> Stations
        {
            get
            {
                try
                {
                    if (_stations == null)
                    {
                        if (Instance.Entities.Any())
                        {
                            _stations = Instance.Entities.Where(e => e.CategoryId == (int) CategoryID.Station).OrderBy(i => i.Distance).ToList();
                            if (_stations.Any())
                            {
                                return _stations;
                            }

                            return new List<EntityCache>();
                        }

                        return null;
                    }

                    return _stations;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.SolarSystems", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return null;
                }
            }
        }

        public EntityCache ClosestStation
        {
            get
            {
                try
                {
                    if (Stations != null && Stations.Any())
                    {
                        return Stations.OrderBy(s => s.Distance).FirstOrDefault() ?? Instance.Entities.OrderByDescending(s => s.Distance).FirstOrDefault();
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.IsApproaching", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return null;
                }
            }
        }

        public IEnumerable<DirectSolarSystem> SolarSystems
        {
            get
            {
                try
                {
                    //High sec: 1090
                    //Low sec: 817
                    //0.0: 3524 (of which 230 are not connected)
                    //W-space: 2499

                    //High sec + Low sec = Empire: 1907
                    //Empire + 0.0 = K-space: 5431
                    //K-space + W-space = Total: 7930
                    if (Time.Instance.LastSessionChange.AddSeconds(30) > DateTime.UtcNow && (Instance.InSpace || Instance.InStation))
                    {
                        if (_solarSystems == null || !_solarSystems.Any() || _solarSystems.Count() < 5400)
                        {
                            if (Instance.DirectEve.SolarSystems.Any())
                            {
                                if (Instance.DirectEve.SolarSystems.Values.Any())
                                {
                                    _solarSystems = Instance.DirectEve.SolarSystems.Values.OrderBy(s => s.Name).ToList();
                                }

                                return null;
                            }

                            return null;
                        }

                        return _solarSystems;
                    }

                    return null;
                }
                catch (NullReferenceException) // Not sure why this happens, but seems to be no problem
                {
                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.SolarSystems", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return null;
                }
            }
        }

        public IEnumerable<EntityCache> JumpBridges
        {
            get { return _jumpBridges ?? (_jumpBridges = Instance.Entities.Where(e => e.GroupId == (int) Group.JumpBridge).ToList()); }
        }

        public List<EntityCache> Stargates
        {
            get
            {
                try
                {
                    if (_stargates == null)
                    {
                        if (Instance.Entities != null && Instance.Entities.Any())
                        {
                            //if (Cache.Instance.EntityIsStargate.Any())
                            //{
                            //    if (_stargates != null && _stargates.Any()) _stargates.Clear();
                            //    if (_stargates == null) _stargates = new List<EntityCache>();
                            //    foreach (KeyValuePair<long, bool> __stargate in Cache.Instance.EntityIsStargate)
                            //    {
                            //        _stargates.Add(Cache.Instance.Entities.FirstOrDefault(i => i.Id == __stargate.Key));
                            //    }
                            //
                            //    if (_stargates.Any()) return _stargates;
                            //}

                            _stargates = Instance.Entities.Where(e => e.GroupId == (int) Group.Stargate).ToList();
                            //foreach (EntityCache __stargate in _stargates)
                            //{
                            //    if (Cache.Instance.EntityIsStargate.Any())
                            //    {
                            //        if (!Cache.Instance.EntityIsStargate.ContainsKey(__stargate.Id))
                            //        {
                            //            Cache.Instance.EntityIsStargate.Add(__stargate.Id, true);
                            //            continue;
                            //        }
                            //
                            //        continue;
                            //    }
                            //
                            //    Cache.Instance.EntityIsStargate.Add(__stargate.Id, true);
                            //    continue;
                            //}

                            return _stargates;
                        }

                        return null;
                    }

                    return _stargates;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.Stargates", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return null;
                }
            }
        }

        public EntityCache ClosestStargate
        {
            get
            {
                try
                {
                    if (Instance.InSpace)
                    {
                        if (Instance.Entities != null && Instance.Entities.Any())
                        {
                            if (Instance.Stargates != null && Instance.Stargates.Any())
                            {
                                return Instance.Stargates.OrderBy(s => s.Distance).FirstOrDefault() ?? null;
                            }

                            return null;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.ClosestStargate", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return null;
                }
            }
        }

        public IEnumerable<EntityCache> BigObjects
        {
            get
            {
                try
                {
                    return _bigObjects ?? (_bigObjects = Instance.EntitiesOnGrid.Where(e =>
                        e.Distance < (double) Distances.OnGridWithMe &&
                        (e.IsLargeCollidable || e.CategoryId == (int) CategoryID.Asteroid || e.GroupId == (int) Group.SpawnContainer)
                        ).OrderBy(t => t.Distance).ToList());
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.BigObjects", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return new List<EntityCache>();
                }
            }
        }

        public IEnumerable<EntityCache> AccelerationGates
        {
            get
            {
                return _gates ?? (_gates = Instance.EntitiesOnGrid.Where(e =>
                    e.Distance < (double) Distances.OnGridWithMe &&
                    e.GroupId == (int) Group.AccelerationGate &&
                    e.Distance < (double) Distances.OnGridWithMe).OrderBy(t => t.Distance).ToList());
            }
        }

        public IEnumerable<EntityCache> BigObjectsandGates
        {
            get
            {
                return _bigObjectsAndGates ?? (_bigObjectsAndGates = Instance.EntitiesOnGrid.Where(e =>
                    (e.IsLargeCollidable || e.CategoryId == (int) CategoryID.Asteroid || e.GroupId == (int) Group.AccelerationGate ||
                     e.GroupId == (int) Group.SpawnContainer)
                    && e.Distance < (double) Distances.DirectionalScannerCloseRange).OrderBy(t => t.Distance).ToList());
            }
        }

        public IEnumerable<EntityCache> Objects
        {
            get
            {
                return _objects ?? (_objects = Instance.EntitiesOnGrid.Where(e =>
                    !e.IsPlayer &&
                    e.GroupId != (int) Group.SpawnContainer &&
                    e.GroupId != (int) Group.Wreck &&
                    e.Distance < 200000).OrderBy(t => t.Distance).ToList());
            }
        }

        public EntityCache Star
        {
            get { return _star ?? (_star = Entities.FirstOrDefault(e => e.CategoryId == (int) CategoryID.Celestial && e.GroupId == (int) Group.Star)); }
        }

        public EntityCache Approaching
        {
            get
            {
                try
                {
                    if (_approaching == null)
                    {
                        var ship = Instance.ActiveShip.Entity;
                        if (ship != null && ship.IsValid && !ship.HasExploded && !ship.HasReleased)
                        {
                            if (ship.FollowId != 0)
                            {
                                _approaching = EntityById(ship.FollowId);
                                if (_approaching != null && _approaching.IsValid)
                                {
                                    return _approaching;
                                }

                                return null;
                            }

                            return null;
                        }

                        return null;
                    }

                    return _approaching;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Cache.Approaching", "Exception [" + exception + "]", Logging.Logging.Debug);
                    return null;
                }
            }
            set { _approaching = value; }
        }

        public IEnumerable<EntityCache> EntitiesByName(string nameToSearchFor, IEnumerable<EntityCache> EntitiesToLookThrough)
        {
            return EntitiesToLookThrough.Where(e => e.Name.ToLower() == nameToSearchFor.ToLower()).ToList();
        }

        public EntityCache EntityByName(string name)
        {
            return Instance.Entities.FirstOrDefault(e => String.Compare(e.Name, name, StringComparison.OrdinalIgnoreCase) == 0);
        }

        public IEnumerable<EntityCache> EntitiesByPartialName(string nameToSearchFor)
        {
            try
            {
                if (Instance.Entities != null && Instance.Entities.Any())
                {
                    IEnumerable<EntityCache> _entitiesByPartialName = Instance.Entities.Where(e => e.Name.Contains(nameToSearchFor)).ToList();
                    if (!_entitiesByPartialName.Any())
                    {
                        _entitiesByPartialName = Instance.Entities.Where(e => e.Name == nameToSearchFor).ToList();
                    }

                    //if we have no entities by that name return null;
                    if (!_entitiesByPartialName.Any())
                    {
                        _entitiesByPartialName = null;
                    }

                    return _entitiesByPartialName;
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Cache.allBookmarks", "Exception [" + exception + "]", Logging.Logging.Debug);
                return null;
            }
        }

        public IEnumerable<EntityCache> EntitiesThatContainTheName(string label)
        {
            try
            {
                return Instance.Entities.Where(e => !string.IsNullOrEmpty(e.Name) && e.Name.ToLower().Contains(label.ToLower())).ToList();
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Cache.EntitiesThatContainTheName", "Exception [" + exception + "]", Logging.Logging.Debug);
                return null;
            }
        }

        public EntityCache EntityById(long id)
        {
            try
            {
                if (_entitiesById.ContainsKey(id))
                {
                    return _entitiesById[id];
                }

                var entity = Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == id);
                _entitiesById[id] = entity;
                return entity;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Cache.EntityById", "Exception [" + exception + "]", Logging.Logging.Debug);
                return null;
            }
        }

        public double DistanceFromMe(double x, double y, double z)
        {
            try
            {
                if (Instance.ActiveShip.Entity == null)
                {
                    return double.MaxValue;
                }

                var curX = Instance.ActiveShip.Entity.X;
                var curY = Instance.ActiveShip.Entity.Y;
                var curZ = Instance.ActiveShip.Entity.Z;

                return Math.Round(Math.Sqrt((curX - x)*(curX - x) + (curY - y)*(curY - y) + (curZ - z)*(curZ - z)), 2);
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("DistanceFromMe", "Exception [" + ex + "]", Logging.Logging.Debug);
                return 0;
            }
        }

        public Func<EntityCache, int> OrderByLowestHealth()
        {
            try
            {
                return t => (int) (t.ShieldPct + t.ArmorPct + t.StructurePct);
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("OrderByLowestHealth", "Exception [" + ex + "]", Logging.Logging.Debug);
                return null;
            }
        }

        public bool IsOrbiting(long EntityWeWantToBeOrbiting = 0)
        {
            try
            {
                if (Instance.Approaching != null)
                {
                    var _followIDIsOnGrid = false;

                    if (EntityWeWantToBeOrbiting != 0)
                    {
                        _followIDIsOnGrid = (EntityWeWantToBeOrbiting == Instance.ActiveShip.Entity.FollowId);
                    }
                    else
                    {
                        _followIDIsOnGrid = Instance.EntitiesOnGrid.Any(i => i.Id == Instance.ActiveShip.Entity.FollowId);
                    }

                    if (Instance.ActiveShip.Entity != null && Instance.ActiveShip.Entity.Mode == 4 && _followIDIsOnGrid)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Cache.IsApproaching", "Exception [" + exception + "]", Logging.Logging.Debug);
                return false;
            }
        }

        public bool IsApproaching(long EntityWeWantToBeApproaching = 0)
        {
            try
            {
                if (Instance.Approaching != null)
                {
                    var _followIDIsOnGrid = false;

                    if (EntityWeWantToBeApproaching != 0)
                    {
                        _followIDIsOnGrid = (EntityWeWantToBeApproaching == Instance.ActiveShip.Entity.FollowId);
                    }
                    else
                    {
                        _followIDIsOnGrid = Instance.EntitiesOnGrid.Any(i => i.Id == Instance.ActiveShip.Entity.FollowId);
                    }

                    if (Instance.ActiveShip.Entity != null && Instance.ActiveShip.Entity.Mode == 1 && _followIDIsOnGrid)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Cache.IsApproaching", "Exception [" + exception + "]", Logging.Logging.Debug);
                return false;
            }
        }

        public bool IsApproachingOrOrbiting(long EntityWeWantToBeApproachingOrOrbiting = 0)
        {
            try
            {
                if (IsApproaching(EntityWeWantToBeApproachingOrOrbiting))
                {
                    return true;
                }

                if (IsOrbiting(EntityWeWantToBeApproachingOrOrbiting))
                {
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Cache.IsApproachingOrOrbiting", "Exception [" + exception + "]", Logging.Logging.Debug);
                return false;
            }
        }

        public EntityCache StationByName(string stationName)
        {
            var station = Stations.First(x => x.Name.ToLower() == stationName.ToLower());
            return station;
        }

        public EntityCache StargateByName(string locationName)
        {
            {
                return _stargate ??
                       (_stargate =
                           Instance.EntitiesByName(locationName, Instance.Entities.Where(i => i.GroupId == (int) Group.Stargate))
                               .FirstOrDefault(e => e.GroupId == (int) Group.Stargate));
            }
        }

        public bool GateInGrid()
        {
            try
            {
                if (Instance.AccelerationGates.FirstOrDefault() == null || !Instance.AccelerationGates.Any())
                {
                    return false;
                }

                Time.Instance.LastAccelerationGateDetected = DateTime.UtcNow;
                return true;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("GateInGrid", "Exception [" + ex + "]", Logging.Logging.Debug);
                return true;
            }
        }
    }
}