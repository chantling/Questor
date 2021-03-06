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
using Questor.Modules.Lookup;

namespace Questor.Modules.Caching
{
    public class ModuleCache
    {
        private readonly DirectModule _module;

        private int ActivateCountThisFrame = 0;

        private int ClickCountThisFrame = 0;

        private DateTime ThisModuleCacheCreated = DateTime.UtcNow;

        public ModuleCache(DirectModule module)
        {
            //
            // reminder: this class and all the info within it is created (and destroyed!) each frame for each module!
            //
            _module = module;
            ThisModuleCacheCreated = DateTime.UtcNow;
        }

        public int TypeId
        {
            get { return _module.TypeId; }
        }

        public string TypeName
        {
            get { return _module.TypeName; }
        }

        public int GroupId
        {
            get { return _module.GroupId; }
        }

        public double Damage
        {
            get { return _module.Damage; }
        }

        public bool ActivatePlex //do we need to make sure this is ONLY valid on a PLEX?
        {
            get { return _module.ActivatePLEX(); }
        }

        public bool AssembleShip // do we need to make sure this is ONLY valid on a packaged ship?
        {
            get { return _module.AssembleShip(); }
        }

        public double AveragePrice
        {
            get { return _module.AveragePrice(); }
        }

        public double Duration
        {
            get { return _module.Duration ?? 0; }
        }

        public double FallOff
        {
            get { return _module.FallOff ?? 0; }
        }

        public double MaxRange
        {
            get
            {
                try
                {
                    double? _maxRange = null;
                    //_maxRange = _module.Attributes.TryGet<double>("maxRange");

                    if (_maxRange == null || _maxRange == 0)
                    {
                        //
                        // if we could not find the max range via EVE use the XML setting for RemoteRepairers
                        //
                        if (_module.GroupId == (int) Group.RemoteArmorRepairer || _module.GroupId == (int) Group.RemoteShieldRepairer ||
                            _module.GroupId == (int) Group.RemoteHullRepairer)
                        {
                            return Combat.Combat.RemoteRepairDistance;
                        }
                        //
                        // if we could not find the max range via EVE use the XML setting for Nos/Neuts
                        //
                        if (_module.GroupId == (int) Group.NOS || _module.GroupId == (int) Group.Neutralizer)
                        {
                            return Combat.Combat.NosDistance;
                        }
                        //
                        // Add other types of modules here?
                        //
                        return 0;
                    }

                    return (double) _maxRange;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [ " + ex + " ]");
                }

                return 0;
            }
        }

        public double Hp
        {
            get { return _module.Hp; }
        }

        public bool IsOverloaded
        {
            get { return _module.IsOverloaded; }
        }

        public bool IsPendingOverloading
        {
            get { return _module.IsPendingOverloading; }
        }

        public bool IsPendingStopOverloading
        {
            get { return _module.IsPendingStopOverloading; }
        }

        public bool ToggleOverload
        {
            get { return _module.ToggleOverload(); }
        }

        public bool IsActivatable
        {
            get { return _module.IsActivatable; }
        }

        public long ItemId
        {
            get { return _module.ItemId; }
        }

        public bool IsActive
        {
            get { return _module.IsActive; }
        }

        public bool IsOnline
        {
            get { return _module.IsOnline; }
        }

        public bool IsGoingOnline
        {
            get { return _module.IsGoingOnline; }
        }

        public bool IsReloadingAmmo
        {
            get
            {
                int reloadDelayToUseForThisWeapon;
                if (IsEnergyWeapon)
                {
                    reloadDelayToUseForThisWeapon = 1;
                }
                else
                {
                    reloadDelayToUseForThisWeapon = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                }

                if (Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(reloadDelayToUseForThisWeapon))
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("TypeName: [" + _module.TypeName + "] This module is likely still reloading! Last reload was [" +
                                Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastReloadedTimeStamp[ItemId]).TotalSeconds, 0) + "sec ago]");
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsDeactivating
        {
            get { return _module.IsDeactivating; }
        }

        public bool IsChangingAmmo
        {
            get
            {
                int reloadDelayToUseForThisWeapon;
                if (IsEnergyWeapon)
                {
                    reloadDelayToUseForThisWeapon = 1;
                }
                else
                {
                    reloadDelayToUseForThisWeapon = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                }

                if (Time.Instance.LastChangedAmmoTimeStamp != null && Time.Instance.LastChangedAmmoTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastChangedAmmoTimeStamp[ItemId].AddSeconds(reloadDelayToUseForThisWeapon))
                    {
                        //if (Logging.DebugActivateWeapons) Logging.Log("ModuleCache", "TypeName: [" + _module.TypeName + "] This module is likely still changing ammo! aborting activating this module.", Logging.Debug);
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsTurret
        {
            get
            {
                if (GroupId == (int) Group.EnergyWeapon) return true;
                if (GroupId == (int) Group.ProjectileWeapon) return true;
                if (GroupId == (int) Group.HybridWeapon) return true;
                return false;
            }
        }

        public bool IsMissileLauncher
        {
            get
            {
                if (GroupId == (int) Group.AssaultMissilelaunchers) return true;
                if (GroupId == (int) Group.CruiseMissileLaunchers) return true;
                if (GroupId == (int) Group.TorpedoLaunchers) return true;
                if (GroupId == (int) Group.StandardMissileLaunchers) return true;
                if (GroupId == (int) Group.AssaultMissilelaunchers) return true;
                if (GroupId == (int) Group.HeavyMissilelaunchers) return true;
                if (GroupId == (int) Group.DefenderMissilelaunchers) return true;
                return false;
            }
        }

        public bool IsEnergyWeapon
        {
            get { return GroupId == (int) Group.EnergyWeapon; }
        }

        public long TargetId
        {
            get { return _module.TargetId ?? -1; }
        }

        public long LastTargetId
        {
            get
            {
                if (Cache.Instance.LastModuleTargetIDs.ContainsKey(ItemId))
                {
                    return Cache.Instance.LastModuleTargetIDs[ItemId];
                }

                return -1;
            }
        }

        public DirectItem Charge
        {
            get { return _module.Charge; }
        }

        public int CurrentCharges
        {
            get
            {
                if (_module.Charge != null)
                    return _module.Charge.Quantity;

                return -1;
            }
        }

        public int MaxCharges
        {
            get { return _module.MaxCharges; }
        }

        public double OptimalRange
        {
            get { return _module.OptimalRange ?? 0; }
        }

        public bool AutoReload
        {
            get { return _module.AutoReload; }
        }

        public bool DisableAutoReload
        {
            get
            {
                if (IsActivatable && !InLimboState)
                {
                    if (_module.AutoReload)
                    {
                        _module.SetAutoReload(false);
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        public bool DoesNotRequireAmmo
        {
            get
            {
                if (TypeId == (int) TypeID.CivilianGatlingPulseLaser) return true;
                if (TypeId == (int) TypeID.CivilianGatlingAutocannon) return true;
                if (TypeId == (int) TypeID.CivilianGatlingRailgun) return true;
                if (TypeId == (int) TypeID.CivilianLightElectronBlaster) return true;
                return false;
            }
        }

        public bool InLimboState
        {
            get
            {
                try
                {
                    var result = false;
                    result |= !IsActivatable;
                    result |= !IsOnline;
                    result |= IsDeactivating;
                    result |= IsGoingOnline;
                    result |= IsReloadingAmmo;
                    result |= IsChangingAmmo;
                    result |= !Cache.Instance.InSpace;
                    result |= Cache.Instance.InStation;
                    result |= Time.Instance.LastInStation.AddSeconds(7) > DateTime.UtcNow;
                    return result;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public bool IsEwarModule
        {
            get
            {
                try
                {
                    var result = false;
                    result |= GroupId == (int) Group.WarpDisruptor;
                    result |= GroupId == (int) Group.StasisWeb;
                    result |= GroupId == (int) Group.TargetPainter;
                    result |= GroupId == (int) Group.TrackingDisruptor;
                    result |= GroupId == (int) Group.Neutralizer;
                    //result |= GroupId == (int)Group.ECM;
                    return result;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]");
                    return false;
                }
            }
        }

        public void OnlineModule()
        {
            _module.OnlineModule();
        }

        public void OfflineModule()
        {
            _module.OfflineModule();
        }

        public bool ReloadAmmo(DirectItem charge, int weaponNumber, double Range)
        {
            if (!IsReloadingAmmo)
            {
                if (!IsChangingAmmo)
                {
                    if (!InLimboState)
                    {
                        Logging.Logging.Log("Reloading [" + weaponNumber + "] [" + _module.TypeName + "] with [" + charge.TypeName + "][" + Math.Round(Range / 1000, 0) + "]");
                        _module.ReloadAmmo(charge);
                        Time.Instance.LastReloadedTimeStamp[ItemId] = DateTime.UtcNow;
                        if (Time.Instance.ReloadTimePerModule.ContainsKey(ItemId))
                        {
                            Time.Instance.ReloadTimePerModule[ItemId] = Time.Instance.ReloadTimePerModule[ItemId] +
                                                                        Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                        }
                        else
                        {
                            Time.Instance.ReloadTimePerModule[ItemId] = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                        }

                        return true;
                    }

                    Logging.Logging.Log("[" + weaponNumber + "][" + _module.TypeName + "] is currently in a limbo state, waiting");
                    return false;
                }

                Logging.Logging.Log("[" + weaponNumber + "][" + _module.TypeName + "] is already changing ammo, waiting");
                return false;
            }

            Logging.Logging.Log("[" + weaponNumber + "][" + _module.TypeName + "] is already reloading, waiting");
            return false;
        }

        public bool UnloadToCargo()
        {
            if (!IsReloadingAmmo)
            {
                if (!IsChangingAmmo)
                {
                    if (!InLimboState)
                    {
                        Logging.Logging.Log("[" + _module.TypeName + "]" + "Calling UnloadToCargo()");
                        _module.UnloadToCargo();
                    }
                }
            }
            return false;
        }

        public bool ChangeAmmo(DirectItem charge, int weaponNumber, double Range, String entityName = "n/a", Double entityDistance = 0)
        {
            if (!IsReloadingAmmo)
            {
                if (!IsChangingAmmo)
                {
                    if (!InLimboState)
                    {
                        _module.ChangeAmmo(charge);
                        Logging.Logging.Log("Changing [" + weaponNumber + "][" + _module.TypeName + "] with [" + charge.TypeName + "][" + Math.Round(Range / 1000, 0) +
                            "] so we can hit [" + entityName + "][" + Math.Round(entityDistance / 1000, 0) + "k]");
                        Time.Instance.LastChangedAmmoTimeStamp[ItemId] = DateTime.UtcNow;
                        if (Time.Instance.ReloadTimePerModule.ContainsKey(ItemId))
                        {
                            Time.Instance.ReloadTimePerModule[ItemId] = Time.Instance.ReloadTimePerModule[ItemId] +
                                                                        Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                        }
                        else
                        {
                            Time.Instance.ReloadTimePerModule[ItemId] = Time.Instance.ReloadWeaponDelayBeforeUsable_seconds;
                        }

                        return true;
                    }

                    Logging.Logging.Log("[" + weaponNumber + "][" + _module.TypeName + "] is currently in a limbo state, waiting");
                    return false;
                }

                Logging.Logging.Log("[" + weaponNumber + "][" + _module.TypeName + "] is already changing ammo, waiting");
                return false;
            }

            Logging.Logging.Log("[" + weaponNumber + "][" + _module.TypeName + "] is already reloading, waiting");
            return false;
        }

        public bool Click()
        {
            try
            {
                if (InLimboState || ClickCountThisFrame > 0)
                {
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("if (InLimboState || ClickCountThisFrame > 0)");
                    return false;
                }

                if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(5))
                {
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(5))");
                    return false;
                }

                if (Time.Instance.LastClickedTimeStamp != null && Time.Instance.LastClickedTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(Settings.Instance.EnforcedDelayBetweenModuleClicks))
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(Settings.Instance.EnforcedDelayBetweenModuleClicks))");
                        return false;
                    }

                    if (_module.Duration != null)
                    {
                        var CycleTime = (double) _module.Duration + 500;
                        if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(CycleTime))
                        {
                            if (Logging.Logging.DebugDefense)
                                Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.LastClickedTimeStamp[ItemId].AddMilliseconds(CycleTime))");
                            return false;
                        }
                    }
                }

                ClickCountThisFrame++;

                if (IsActivatable)
                {
                    if (!IsActive) //it is not yet active, this click should activate it.
                    {
                        _module.Click();
                        Time.Instance.LastActivatedTimeStamp[ItemId] = DateTime.UtcNow;
                        return true;
                    }

                    if (IsActive) //it is active, this click should deactivate it.
                    {
                        _module.Click();
                        return true;
                    }

                    if (Time.Instance.LastClickedTimeStamp != null) Time.Instance.LastClickedTimeStamp[ItemId] = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("ModuleCache.Click - Exception: [" + exception + "]");
                return false;
            }
        }


        public bool Activate(EntityCache target)
        {
            try
            {
                if (InLimboState || IsActive || ActivateCountThisFrame > 0)
                    return false;

                if (!DisableAutoReload)
                    return false;

                ActivateCountThisFrame++;

                if (Time.Instance.LastReloadedTimeStamp != null && Time.Instance.LastReloadedTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastReloadedTimeStamp[ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("TypeName: [" + _module.TypeName + "] This module is likely still reloading! aborting activating this module.");
                        return false;
                    }
                }

                if (Time.Instance.LastChangedAmmoTimeStamp != null && Time.Instance.LastChangedAmmoTimeStamp.ContainsKey(ItemId))
                {
                    if (DateTime.UtcNow < Time.Instance.LastChangedAmmoTimeStamp[ItemId].AddSeconds(Time.Instance.ReloadWeaponDelayBeforeUsable_seconds))
                    {
                        if (Logging.Logging.DebugActivateWeapons)
                            Logging.Logging.Log("TypeName: [" + _module.TypeName + "] This module is likely still changing ammo! aborting activating this module.");
                        return false;
                    }
                }

                if (!target.IsTarget)
                {
                    Logging.Logging.Log("Target [" + target.Name + "][" + Math.Round(target.Distance / 1000, 2) + "]IsTargeting[" + target.IsTargeting +
                        "] was not locked, aborting activating module as we cant activate a module on something that is not locked!");
                    return false;
                }

                if (target.IsEwarImmune && IsEwarModule)
                {
                    Logging.Logging.Log("Target [" + target.Name + "][" + Math.Round(target.Distance / 1000, 2) + "]IsEwarImmune[" + target.IsEwarImmune +
                        "] is EWar Immune and Module [" + _module.TypeName + "] isEwarModule [" + IsEwarModule + "]");
                    return false;
                }

                if (!_module.Activate(target.Id))
                {
                    return false;
                }


                Time.Instance.LastActivatedTimeStamp[ItemId] = DateTime.UtcNow;
                Cache.Instance.LastModuleTargetIDs[ItemId] = target.Id;
                return true;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]");
                return false;
            }
        }

        public void Deactivate()
        {
            if (InLimboState || !IsActive)
                return;

            _module.Deactivate();
        }
    }
}