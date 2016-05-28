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
using System.Threading;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.BackgroundTasks
{
    public static class Defense
    {
        public static int DefenseInstances;

        private static DateTime _lastCloaked = DateTime.UtcNow;

        private static DateTime _lastPulse = DateTime.UtcNow;
        private static int _trackingLinkScriptAttempts;
        private static int _sensorBoosterScriptAttempts;
        private static int _sensorDampenerScriptAttempts;
        private static int _trackingComputerScriptAttempts;
        private static int _trackingDisruptorScriptAttempts;
        //private int _ancillaryShieldBoosterAttempts;
        //private int _capacitorInjectorAttempts;
        private static DateTime _nextOverloadAttempt = DateTime.UtcNow;
        public static bool DoNotBreakInvul;

        private static readonly Dictionary<long, DateTime> NextScriptReload = new Dictionary<long, DateTime>();

        static Defense()
        {
            Interlocked.Increment(ref DefenseInstances);
        }

        public static int MinimumPropulsionModuleDistance { get; set; }
        public static int MinimumPropulsionModuleCapacitor { get; set; }
        public static int ActivateRepairModulesAtThisPerc { get; set; }
        public static int DeactivateRepairModulesAtThisPerc { get; set; }
        public static int InjectCapPerc { get; set; }

        private static int ModuleNumber { get; set; }

        private static bool LoadthisScript(DirectItem scriptToLoad, ModuleCache module)
        {
            if (scriptToLoad != null)
            {
                if (module.IsReloadingAmmo || module.IsActive || module.IsDeactivating || module.IsChangingAmmo || module.InLimboState || module.IsGoingOnline ||
                    !module.IsOnline)
                    return false;

                // We have enough ammo loaded
                if (module.Charge != null && module.Charge.TypeId == scriptToLoad.TypeId && module.CurrentCharges == module.MaxCharges)
                {
                    Logging.Logging.Log("module is already loaded with the script we wanted");
                    NextScriptReload[module.ItemId] = DateTime.UtcNow.AddSeconds(15);
                        //mark this weapon as reloaded... by the time we need to reload this timer will have aged enough...
                    return false;
                }

                // We are reloading, wait 15
                if (NextScriptReload.ContainsKey(module.ItemId) && DateTime.UtcNow < NextScriptReload[module.ItemId].AddSeconds(15))
                {
                    Logging.Logging.Log("module was reloaded recently... skipping");
                    return false;
                }
                NextScriptReload[module.ItemId] = DateTime.UtcNow.AddSeconds(15);

                // Reload or change ammo
                if (module.Charge != null && module.Charge.TypeId == scriptToLoad.TypeId)
                {
                    if (DateTime.UtcNow.Subtract(Time.Instance.LastLoggingAction).TotalSeconds > 10)
                    {
                        Time.Instance.LastLoggingAction = DateTime.UtcNow;
                    }

                    if (module.ReloadAmmo(scriptToLoad, 0, 0))
                    {
                        Logging.Logging.Log("Reloading [" + module.TypeId + "] with [" + scriptToLoad.TypeName + "][TypeID: " + scriptToLoad.TypeId + "]");
                        return true;
                    }

                    return false;
                }

                if (DateTime.UtcNow.Subtract(Time.Instance.LastLoggingAction).TotalSeconds > 10)
                {
                    Time.Instance.LastLoggingAction = DateTime.UtcNow;
                }

                if (module.ChangeAmmo(scriptToLoad, 0, 0))
                {
                    Logging.Logging.Log("Changing [" + module.TypeId + "] with [" + scriptToLoad.TypeName + "][TypeID: " + scriptToLoad.TypeId + "]");
                    return true;
                }

                return false;
            }
            Logging.Logging.Log("script to load was NULL!");
            return false;
        }

        private static void ActivateOnce()
        {
            //if (Logging.DebugLoadScripts) Logging.Log("Defense", "spam", Logging.White);
            if (DateTime.UtcNow < Time.Instance.NextActivateModules) //if we just did something wait a fraction of a second
                return;

            //Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(1);


            ModuleNumber = 0;
            foreach (var ActivateOncePerSessionModulewScript in Cache.Instance.Modules.Where(i => i.GroupId == (int) Group.TrackingDisruptor ||
                                                                                                  i.GroupId == (int) Group.TrackingComputer ||
                                                                                                  i.GroupId == (int) Group.TrackingLink ||
                                                                                                  i.GroupId == (int) Group.SensorBooster ||
                                                                                                  i.GroupId == (int) Group.SensorDampener ||
                                                                                                  i.GroupId == (int) Group.CapacitorInjector ||
                                                                                                  i.GroupId == (int) Group.AncillaryShieldBooster))
            {
                if (!ActivateOncePerSessionModulewScript.IsActivatable)
                    continue;

                if (ActivateOncePerSessionModulewScript.CurrentCharges < ActivateOncePerSessionModulewScript.MaxCharges)
                {
                    if (Logging.Logging.DebugLoadScripts)
                        Logging.Logging.Log("Found Activatable Module with no charge[typeID:" + ActivateOncePerSessionModulewScript.TypeId + "]");
                    DirectItem scriptToLoad;

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.TrackingDisruptor && _trackingDisruptorScriptAttempts < 5)
                    {
                        _trackingDisruptorScriptAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("TrackingDisruptor Found");
                        scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.TrackingDisruptorScript, 1);

                        // this needs a counter and an abort after 10 tries or so... or it will keep checking the cargo for a script that may not exist
                        // every second we are in space!
                        if (scriptToLoad != null)
                        {
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                //
                                // deactivate the module so we can load the script.
                                //
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("Activating TrackingDisruptor");
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }
                            }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                            return;
                        }
                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.TrackingComputer && _trackingComputerScriptAttempts < 5)
                    {
                        _trackingComputerScriptAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("TrackingComputer Found");
                        var TrackingComputerScript = Cache.Instance.CheckCargoForItem(Settings.Instance.TrackingComputerScript, 1);

                        var EntityTrackingDisruptingMe = Combat.Combat.TargetedBy.FirstOrDefault(t => t.IsTrackingDisruptingMe);
                        if (EntityTrackingDisruptingMe != null || TrackingComputerScript == null)
                        {
                            TrackingComputerScript = Cache.Instance.CheckCargoForItem((int) TypeID.OptimalRangeScript, 1);
                        }

                        scriptToLoad = TrackingComputerScript;
                        if (scriptToLoad != null)
                        {
                            if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("Script Found for TrackingComputer");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("Activate TrackingComputer");
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }
                            }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                            return;
                        }
                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.TrackingLink && _trackingLinkScriptAttempts < 5)
                    {
                        _trackingLinkScriptAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("TrackingLink Found");
                        scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.TrackingLinkScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("Script Found for TrackingLink");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }
                            }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                            return;
                        }
                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.SensorBooster && _sensorBoosterScriptAttempts < 5)
                    {
                        _sensorBoosterScriptAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("SensorBooster Found");
                        scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.SensorBoosterScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("Script Found for SensorBooster");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }
                            }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                            return;
                        }
                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.SensorDampener && _sensorDampenerScriptAttempts < 5)
                    {
                        _sensorDampenerScriptAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("SensorDampener Found");
                        scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.SensorDampenerScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("Script Found for SensorDampener");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }
                            }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                            return;
                        }
                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.AncillaryShieldBooster)
                    {
                        //_ancillaryShieldBoosterAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("ancillaryShieldBooster Found");
                        scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.AncillaryShieldBoosterScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (Logging.Logging.DebugLoadScripts)
                                Logging.Logging.Log("CapBoosterCharges Found for ancillaryShieldBooster");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(500);
                                    return;
                                }
                            }

                            var inCombat = Combat.Combat.TargetedBy.Any();
                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline ||
                                (inCombat && ActivateOncePerSessionModulewScript.CurrentCharges > 0))
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                            return;
                        }
                        ModuleNumber++;
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int) Group.CapacitorInjector)
                    {
                        //_capacitorInjectorAttempts++;
                        if (Logging.Logging.DebugLoadScripts) Logging.Logging.Log("capacitorInjector Found");
                        scriptToLoad = Cache.Instance.CheckCargoForItem(Settings.Instance.CapacitorInjectorScript, 1);
                        if (scriptToLoad != null)
                        {
                            if (Logging.Logging.DebugLoadScripts)
                                Logging.Logging.Log("CapBoosterCharges Found for capacitorInjector");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                            {
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(500);
                                    return;
                                }
                            }

                            var inCombat = Combat.Combat.TargetedBy.Any();
                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsChangingAmmo || ActivateOncePerSessionModulewScript.InLimboState ||
                                ActivateOncePerSessionModulewScript.IsGoingOnline || !ActivateOncePerSessionModulewScript.IsOnline ||
                                (inCombat && ActivateOncePerSessionModulewScript.CurrentCharges > 0))
                            {
                                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                ModuleNumber++;
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                ModuleNumber++;
                                continue;
                            }
                        }
                        else if (ActivateOncePerSessionModulewScript.CurrentCharges == 0)
                        {
                            Logging.Logging.Log("ReloadCapBooster: ran out of cap booster with typeid: [ " + Settings.Instance.CapacitorInjectorScript + " ]");
                            _States.CurrentCombatState = CombatState.OutOfAmmo;
                            continue;
                        }
                        ModuleNumber++;
                        continue;
                    }
                }

                ModuleNumber++;
                Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                continue;
            }

            ModuleNumber = 0;
            foreach (var ActivateOncePerSessionModule in Cache.Instance.Modules.Where(i => i.GroupId == (int) Group.CloakingDevice ||
                                                                                           i.GroupId == (int) Group.ShieldHardeners ||
//			                                                                                  i.GroupId == (int)Group.DamageControl ||
                                                                                           i.GroupId == (int) Group.ArmorHardeners ||
                                                                                           i.GroupId == (int) Group.SensorBooster ||
                                                                                           i.GroupId == (int) Group.TrackingComputer ||
                                                                                           i.GroupId == (int) Group.MissuleGuidanceComputer ||
                                                                                           i.GroupId == (int) Group.ECCM))
            {
                if (!ActivateOncePerSessionModule.IsActivatable)
                    continue;

                ModuleNumber++;

                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName + "] TypeID [" + ActivateOncePerSessionModule.TypeId + "] GroupId [" +
                        ActivateOncePerSessionModule.GroupId + "] Activatable [" + ActivateOncePerSessionModule.IsActivatable + "] Found");

                if (ActivateOncePerSessionModule.IsActive)
                {
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName + "] is already active");
                    continue;
                }

                if (ActivateOncePerSessionModule.InLimboState)
                {
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                            "] is in LimboState (likely being activated or decativated already)");
                    continue;
                }

                if (ActivateOncePerSessionModule.GroupId == (int) Group.CloakingDevice)
                {
                    //Logging.Log("Defense: This module has a typeID of: " + module.TypeId + " !!");
                    if (ActivateOncePerSessionModule.TypeId != 11578)
                        //11578 Covert Ops Cloaking Device - if you don't have a covert ops cloak try the next module
                    {
                        continue;
                    }
                    var stuffThatMayDecloakMe =
                        Cache.Instance.EntitiesOnGrid.Where(
                            t => t.Name != Cache.Instance.DirectEve.Me.Name || t.IsBadIdea || t.IsContainer || t.IsNpc || t.IsPlayer)
                            .OrderBy(t => t.Distance)
                            .FirstOrDefault();
                    if (stuffThatMayDecloakMe != null && (stuffThatMayDecloakMe.Distance <= (int) Distances.SafeToCloakDistance))
                        //if their is anything within 2300m do not attempt to cloak
                    {
                        if ((int) stuffThatMayDecloakMe.Distance != 0)
                        {
                            //Logging.Log(Defense: StuffThatMayDecloakMe.Name + " is very close at: " + StuffThatMayDecloakMe.Distance + " meters");
                            continue;
                        }
                    }
                }
                else
                {
                    //
                    // if capacitor is really low, do not make it worse
                    //
                    if (Cache.Instance.ActiveShip.Capacitor < 45)
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                "] You have less then 45 UNITS of cap: do not make it worse by turning on the hardeners");
                        continue;
                    }

                    if (Cache.Instance.ActiveShip.CapacitorPercentage < 3)
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                "] You have less then 3% of cap: do not make it worse by turning on the hardeners");
                        continue;
                    }

                    //
                    // if total capacitor is really low, do not run stuff unless we are targeted by something
                    // this should only kick in when using frigates as the combatship
                    //
                    if (Cache.Instance.ActiveShip.Capacitor < 400 && !Combat.Combat.TargetedBy.Any() &&
                        Cache.Instance.ActiveShip.GivenName.ToLower() == Combat.Combat.CombatShipName.ToLower())
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName +
                                "] You have less then 400 units total cap and nothing is targeting you yet, no need for hardeners yet.");
                        continue;
                    }
                }

                //
                // at this point the module should be active but is not: activate it, set the delay and return. The process will resume on the next tick
                //
                if (ActivateOncePerSessionModule.Click())
                {
                    Time.Instance.NextActivateModules = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("[" + ModuleNumber + "][" + ActivateOncePerSessionModule.TypeName + "] activated");
                    return;
                }
            }

            ModuleNumber = 0;
        }

        private static bool OverLoadWeapons()
        {
            //if (Logging.DebugLoadScripts) Logging.Log("Defense", "spam", Logging.White);
            if (DateTime.UtcNow < _nextOverloadAttempt) //if we just did something wait a bit
                return true;

            if (!Settings.Instance.OverloadWeapons)
            {
                // if we do not have the OverLoadWeapons setting set to true then just return.
                _nextOverloadAttempt = DateTime.UtcNow.AddSeconds(30);
                return true;
            }

            //
            //if we do not have the skill (to at least lvl1) named thermodynamics, return true and do not try to overload
            //

            ModuleNumber = 0;
            foreach (var module in Cache.Instance.Modules)
            {
                if (!module.IsActivatable)
                    continue;

                if (module.IsOverloaded || module.IsPendingOverloading || module.IsPendingStopOverloading)
                    continue;

                //if (Logging.DebugLoadScripts) Logging.Log("Defense", "Found Activatable Module [typeid: " + module.TypeId + "][groupID: " + module.GroupId +  "]", Logging.White);

                if (module.GroupId == (int) Group.EnergyWeapon ||
                    module.GroupId == (int) Group.HybridWeapon ||
                    module.GroupId == (int) Group.ProjectileWeapon ||
                    module.GroupId == (int) Group.CruiseMissileLaunchers ||
                    module.GroupId == (int) Group.RocketLaunchers ||
                    module.GroupId == (int) Group.TorpedoLaunchers ||
                    module.GroupId == (int) Group.StandardMissileLaunchers ||
                    module.GroupId == (int) Group.HeavyMissilelaunchers ||
                    module.GroupId == (int) Group.AssaultMissilelaunchers ||
                    module.GroupId == (int) Group.DefenderMissilelaunchers
                    )
                {
                    //if (Logging.DebugLoadScripts) Logging.Log("Defense", "---Found mod that could take a script [typeid: " + module.TypeId + "][groupID: " + module.GroupId + "][module.CurrentCharges [" + module.CurrentCharges + "]", Logging.White);

                    ModuleNumber++;

                    if (module.IsOverloaded)
                    {
                        if (module.IsPendingOverloading || module.IsPendingStopOverloading)
                        {
                            continue;
                        }

                        //double DamageThresholdToStopOverloading = 1;

                        if (Logging.Logging.DebugOverLoadWeapons)
                            Logging.Logging.Log("IsOverLoaded - HP [" + Math.Round(module.Hp, 2) + "] Damage [" + Math.Round(module.Damage, 2) + "][" + module.TypeId + "]");

                        //if (module.Damage > DamageThresholdToStopOverloading)
                        //{
                        //    Logging.Log("Defense.Overload","Damage [" + Math.Round(module.Damage,2) + "] Disable Overloading of Module wTypeID[" + module.TypeId + "]",Logging.Debug);
                        //    return module.ToggleOverload;
                        //    return false;
                        //}

                        continue;
                    }

                    if (!module.IsOverloaded)
                    {
                        if (module.IsPendingOverloading || module.IsPendingStopOverloading)
                        {
                            continue;
                        }

                        //double DamageThresholdToAllowOverLoading = 1;

                        if (Logging.Logging.DebugOverLoadWeapons)
                            Logging.Logging.Log("Is not OverLoaded - HP [" + Math.Round(module.Hp, 2) + "] Damage [" + Math.Round(module.Damage, 2) + "][" + module.TypeId + "]");
                        _nextOverloadAttempt = DateTime.UtcNow.AddSeconds(30);

                        //if (module.Damage < DamageThresholdToAllowOverLoading)
                        //{
                        //    Logging.Log("Defense.Overload", "Damage [" + Math.Round(module.Damage, 2) + "] Enable Overloading of Module wTypeID[" + module.TypeId + "]", Logging.Debug);
                        return module.ToggleOverload;
                        //}

                        //continue;
                    }

                    _nextOverloadAttempt = DateTime.UtcNow.AddSeconds(60);
                    return true;
                }

                ModuleNumber++;
                continue;
            }
            ModuleNumber = 0;
            return true;
        }

        private static void ActivateRepairModules()
        {
            //var watch = new Stopwatch();
            if (DateTime.UtcNow < Time.Instance.NextRepModuleAction) //if we just did something wait a fraction of a second
            {
                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("if (DateTime.UtcNow < Time.Instance.NextRepModuleAction [" + Time.Instance.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds +
                        " Sec from now])");
                return;
            }

            ModuleNumber = 0;
            foreach (var repairModule in Cache.Instance.Modules.Where(i => i.GroupId == (int) Group.ShieldBoosters ||
                                                                           i.GroupId == (int) Group.AncillaryShieldBooster ||
                                                                           i.GroupId == (int) Group.CapacitorInjector ||
                                                                           i.GroupId == (int) Group.ArmorRepairer).Where(x => x.IsOnline))
            {
                ModuleNumber++;
                //if (repairModule.IsActive)
                //{
                //    if (Logging.DebugDefense) Logging.Log("ActivateRepairModules", "[" + ModuleNumber + "][" + repairModule.TypeName + "] is currently Active, continue", Logging.Debug);
                //    continue;
                //}

                if (repairModule.InLimboState)
                {
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("[" + ModuleNumber + "][" + repairModule.TypeName + "] is InLimboState, continue");
                    continue;
                }

                double perc;
                double cap;
                cap = Cache.Instance.ActiveShip.CapacitorPercentage;

                if (repairModule.GroupId == (int) Group.ShieldBoosters ||
                    repairModule.GroupId == (int) Group.AncillaryShieldBooster ||
                    repairModule.GroupId == (int) Group.CapacitorInjector)
                {
                    perc = Cache.Instance.ActiveShip.ShieldPercentage;
                }
                else if (repairModule.GroupId == (int) Group.ArmorRepairer)
                {
                    perc = Cache.Instance.ActiveShip.ArmorPercentage;
                }
                else
                {
                    //we should never get here. All rep modules will be either shield or armor rep oriented... if we do, move on to the next module.
                    continue;
                }

                // Module is either for Cap or Tank recharging, so we look at these separated (or random things will happen, like cap recharging when we need to repair but cap is near max)
                // Cap recharging
                var inCombat = Cache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy) || Combat.Combat.PotentialCombatTargets.Any();
                if (!repairModule.IsActive && inCombat && cap < InjectCapPerc && repairModule.GroupId == (int) Group.CapacitorInjector &&
                    repairModule.CurrentCharges > 0)
                {
                    //
                    // Activate Cap Injector
                    //
                    if (repairModule.Click())
                    {
                        perc = Cache.Instance.ActiveShip.ShieldPercentage;
                        Logging.Logging.Log("Cap: [" + Math.Round(cap, 0) + "%] Capacitor Booster: [" + ModuleNumber + "] activated");
                        return;
                    }
                }

                //
                // Do we need to Activate Shield/Armor rep?
                //
                if (!repairModule.IsActive &&
                    ((inCombat && perc < ActivateRepairModulesAtThisPerc) ||
                     (!inCombat && perc < DeactivateRepairModulesAtThisPerc && cap > Panic.SafeCapacitorPct)))
                {
                    if (Cache.Instance.ActiveShip.ShieldPercentage < Statistics.LowestShieldPercentageThisPocket)
                    {
                        Statistics.LowestShieldPercentageThisPocket = Cache.Instance.ActiveShip.ShieldPercentage;
                        Statistics.LowestShieldPercentageThisMission = Cache.Instance.ActiveShip.ShieldPercentage;
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                    }

                    if (Cache.Instance.ActiveShip.ArmorPercentage < Statistics.LowestArmorPercentageThisPocket)
                    {
                        Statistics.LowestArmorPercentageThisPocket = Cache.Instance.ActiveShip.ArmorPercentage;
                        Statistics.LowestArmorPercentageThisMission = Cache.Instance.ActiveShip.ArmorPercentage;
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                    }

                    if (Cache.Instance.ActiveShip.CapacitorPercentage < Statistics.LowestCapacitorPercentageThisPocket)
                    {
                        Statistics.LowestCapacitorPercentageThisPocket = Cache.Instance.ActiveShip.CapacitorPercentage;
                        Statistics.LowestCapacitorPercentageThisMission = Cache.Instance.ActiveShip.CapacitorPercentage;
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                    }

                    if ((Cache.Instance.UnlootedContainers != null) && Statistics.WrecksThisPocket != Cache.Instance.UnlootedContainers.Count())
                    {
                        Statistics.WrecksThisPocket = Cache.Instance.UnlootedContainers.Count();
                    }

                    if (repairModule.GroupId == (int) Group.AncillaryShieldBooster) //this needs to have a huge delay and it currently does not.
                    {
                        if (repairModule.CurrentCharges > 0)
                        {
                            //
                            // Activate Ancillary Shield Booster
                            //
                            if (repairModule.Click())
                            {
                                Logging.Logging.Log("Perc: [" + Math.Round(perc, 0) + "%] Ancillary Shield Booster: [" + ModuleNumber + "] activated");
                                Time.Instance.StartedBoosting = DateTime.UtcNow;
                                Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                                return;
                            }
                        }
                    }

                    //
                    // if capacitor is really very low, do not make it worse
                    //
                    if (Cache.Instance.ActiveShip.Capacitor == 0 || Cache.Instance.ActiveShip.Capacitor < 25)
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("if (Cache.Instance.ActiveShip.Capacitor [" + Cache.Instance.ActiveShip.Capacitor + "] < 25)");
                        continue;
                    }

                    if (Cache.Instance.ActiveShip.CapacitorPercentage == 0 || Cache.Instance.ActiveShip.CapacitorPercentage < 3)
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("if (Cache.Instance.ActiveShip.CapacitorPercentage [" + Cache.Instance.ActiveShip.CapacitorPercentage + "] < 3)");
                        continue;
                    }

                    if (repairModule.GroupId == (int) Group.ShieldBoosters || repairModule.GroupId == (int) Group.ArmorRepairer)
                    {
                        if (Logging.Logging.DebugDefense)
                            Logging.Logging.Log("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Deactivate [" + ModuleNumber +
                                "][" + repairModule.TypeName + "]");
                        //
                        // Activate Repair Module (shields or armor)
                        //
                        if (repairModule.Click())
                        {
                            Time.Instance.StartedBoosting = DateTime.UtcNow;
                            Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);

                            if (Cache.Instance.ActiveShip.ArmorPercentage*100 < 100)
                            {
                                Arm.NeedRepair = true; //triggers repairing during panic recovery, and arm
                            }

                            if (repairModule.GroupId == (int) Group.ShieldBoosters || repairModule.GroupId == (int) Group.AncillaryShieldBooster)
                            {
                                perc = Cache.Instance.ActiveShip.ShieldPercentage;
                                Logging.Logging.Log("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Shield Booster: [" + ModuleNumber + "] activated");
                            }
                            else if (repairModule.GroupId == (int) Group.ArmorRepairer)
                            {
                                perc = Cache.Instance.ActiveShip.ArmorPercentage;
                                Logging.Logging.Log("Tank % [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Armor Repairer: [" + ModuleNumber + "] activated");
                                var aggressiveEntities = Cache.Instance.EntitiesOnGrid.Count(e => e.IsAttacking && e.IsPlayer);
                                if (aggressiveEntities == 0 && Cache.Instance.EntitiesOnGrid.Count(e => e.IsStation) == 1)
                                {
                                    Time.Instance.NextDockAction = DateTime.UtcNow.AddSeconds(10);
                                    Logging.Logging.Log("Repairing Armor outside station with no aggro (yet): delaying docking for [15]seconds");
                                }
                            }

                            return;
                        }
                    }

                    //Logging.Log("LowestShieldPercentage(pocket) [ " + Cache.Instance.lowest_shield_percentage_this_pocket + " ] ");
                    //Logging.Log("LowestArmorPercentage(pocket) [ " + Cache.Instance.lowest_armor_percentage_this_pocket + " ] ");
                    //Logging.Log("LowestCapacitorPercentage(pocket) [ " + Cache.Instance.lowest_capacitor_percentage_this_pocket + " ] ");
                    //Logging.Log("LowestShieldPercentage(mission) [ " + Cache.Instance.lowest_shield_percentage_this_mission + " ] ");
                    //Logging.Log("LowestArmorPercentage(mission) [ " + Cache.Instance.lowest_armor_percentage_this_mission + " ] ");
                    //Logging.Log("LowestCapacitorPercentage(mission) [ " + Cache.Instance.lowest_capacitor_percentage_this_mission + " ] ");
                }

                //
                // Do we need to DeActivate Shield/Armor rep?
                //
                if (repairModule.IsActive && (perc >= DeactivateRepairModulesAtThisPerc || repairModule.GroupId == (int) Group.CapacitorInjector))
                {
                    if (Logging.Logging.DebugDefense)
                        Logging.Logging.Log("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Deactivate [" + ModuleNumber +
                            "][" + repairModule.TypeName + "]");
                    //
                    // Deactivate Module
                    //
                    if (repairModule.Click())
                    {
                        Time.Instance.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(Time.Instance.DefenceDelay_milliseconds);
                        Statistics.RepairCycleTimeThisPocket = Statistics.RepairCycleTimeThisPocket +
                                                               ((int) DateTime.UtcNow.Subtract(Time.Instance.StartedBoosting).TotalSeconds);
                        Statistics.RepairCycleTimeThisMission = Statistics.RepairCycleTimeThisMission +
                                                                ((int) DateTime.UtcNow.Subtract(Time.Instance.StartedBoosting).TotalSeconds);
                        Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
                        if (repairModule.GroupId == (int) Group.ShieldBoosters || repairModule.GroupId == (int) Group.CapacitorInjector)
                        {
                            perc = Cache.Instance.ActiveShip.ShieldPercentage;
                            Logging.Logging.Log("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Shield Booster: [" + ModuleNumber + "] deactivated [" +
                                Math.Round(Time.Instance.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "] sec reactivation delay");
                        }
                        else if (repairModule.GroupId == (int) Group.ArmorRepairer)
                        {
                            perc = Cache.Instance.ActiveShip.ArmorPercentage;
                            Logging.Logging.Log("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Armor Repairer: [" + ModuleNumber + "] deactivated [" +
                                Math.Round(Time.Instance.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "] sec reactivation delay");
                        }

                        //Cache.Instance.repair_cycle_time_this_pocket = Cache.Instance.repair_cycle_time_this_pocket + ((int)watch.Elapsed);
                        //Cache.Instance.repair_cycle_time_this_mission = Cache.Instance.repair_cycle_time_this_mission + watch.Elapsed.TotalMinutes;
                        return;
                    }
                }

                continue;
            }
        }

        private static void ActivateSpeedMod()
        {
            ModuleNumber = 0;
            foreach (var SpeedMod in Cache.Instance.Modules.Where(i => i.GroupId == (int) Group.Afterburner))
            {
                ModuleNumber++;

                if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(SpeedMod.ItemId))
                {
                    if (Logging.Logging.DebugSpeedMod)
                        Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName + "] was last activated [" +
                            Math.Round(DateTime.UtcNow.Subtract(Time.Instance.LastActivatedTimeStamp[SpeedMod.ItemId]).TotalSeconds, 0) + "] sec ago");
                    if (Time.Instance.LastActivatedTimeStamp[SpeedMod.ItemId].AddMilliseconds(Time.Instance.AfterburnerDelay_milliseconds) > DateTime.UtcNow)
                    {
                        //if (Logging.DebugSpeedMod) Logging.Log("Defense.ActivateSpeedMod", "[" + ModuleNumber + "] was last activated [" + Time.Instance.LastActivatedTimeStamp[SpeedMod.ItemId] + "[" + Time.Instance.AfterburnerDelay_milliseconds + "] > [" + DateTime.UtcNow + "], skip this speed mod", Logging.Debug);
                        continue;
                    }
                }

                if (SpeedMod.InLimboState)
                {
                    if (Logging.Logging.DebugSpeedMod)
                        Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName + "] isActive [" + SpeedMod.IsActive + "]");
                    continue;
                }

                //
                // Should we deactivate the module?
                //
                if (Logging.Logging.DebugSpeedMod)
                    Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName + "] isActive [" + SpeedMod.IsActive + "]");

                if (SpeedMod.IsActive)
                {
                    var deactivate = false;

                    //we cant move in bastion mode, do not try
                    List<ModuleCache> bastionModules = null;
                    bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                    if (bastionModules.Any(i => i.IsActive))
                    {
                        if (Logging.Logging.DebugSpeedMod)
                            Logging.Logging.Log("BastionMode is active, we cannot move, deactivating speed module");
                        deactivate = true;
                    }

                    if (!Cache.Instance.IsApproachingOrOrbiting(0))
                    {
                        deactivate = true;
                        if (Logging.Logging.DebugSpeedMod)
                            Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName + "] We are not approaching or orbiting anything: Deactivate [" + deactivate + "]");
                    }
                    else if (!Combat.Combat.PotentialCombatTargets.Any() && DateTime.UtcNow > Statistics.StartedPocket.AddMinutes(10) &&
                             Cache.Instance.ActiveShip.GivenName == Combat.Combat.CombatShipName)
                    {
                        deactivate = true;
                        if (Logging.Logging.DebugSpeedMod)
                            Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName +
                                "] Nothing on grid is attacking and it has been more than 60 seconds since we landed in this pocket. Deactivate [" +
                                deactivate + "]");
                    }
                    else if (!NavigateOnGrid.SpeedTank)
                    {
                        // This only applies when not speed tanking
                        if (Cache.Instance.IsApproachingOrOrbiting(0) && Cache.Instance.Approaching != null)
                        {
                            // Deactivate if target is too close
                            if (Cache.Instance.Approaching.Distance < MinimumPropulsionModuleDistance)
                            {
                                deactivate = true;
                                if (Logging.Logging.DebugSpeedMod)
                                    Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName + "] We are approaching... and [" +
                                        Math.Round(Cache.Instance.Approaching.Distance / 1000, 0) + "] is within [" +
                                        Math.Round((double)MinimumPropulsionModuleDistance / 1000, 0) + "] Deactivate [" + deactivate + "]");
                            }
                        }
                    }
                    else if (Cache.Instance.ActiveShip.CapacitorPercentage < MinimumPropulsionModuleCapacitor)
                    {
                        deactivate = true;
                        if (Logging.Logging.DebugSpeedMod)
                            Logging.Logging.Log("[" + ModuleNumber + "][" + SpeedMod.TypeName + "] Capacitor is at [" + Cache.Instance.ActiveShip.CapacitorPercentage +
                                "] which is below MinimumPropulsionModuleCapacitor [" + MinimumPropulsionModuleCapacitor + "] Deactivate [" + deactivate +
                                "]");
                    }

                    if (deactivate)
                    {
                        if (SpeedMod.Click())
                        {
                            if (Logging.Logging.DebugSpeedMod)
                                Logging.Logging.Log("[" + ModuleNumber + "] [" + SpeedMod.TypeName + "] Deactivated");
                            return;
                        }
                    }
                }

                //
                // Should we activate the module
                //

                if (!SpeedMod.IsActive && !SpeedMod.InLimboState)
                {
                    var activate = false;

                    //we cant move in bastion mode, do not try
                    List<ModuleCache> bastionModules = null;
                    bastionModules = Cache.Instance.Modules.Where(m => m.GroupId == (int) Group.Bastion && m.IsOnline).ToList();
                    if (bastionModules.Any(i => i.IsActive))
                    {
                        if (Logging.Logging.DebugSpeedMod)
                            Logging.Logging.Log("BastionMode is active, we cannot move, do not attempt to activate speed module");
                        activate = false;
                        return;
                    }

                    if (Cache.Instance.IsApproachingOrOrbiting(0) && Cache.Instance.Approaching != null)
                    {
                        // Activate if target is far enough
                        if (Cache.Instance.Approaching.Distance > MinimumPropulsionModuleDistance)
                        {
                            activate = true;
                            if (Logging.Logging.DebugSpeedMod)
                                Logging.Logging.Log("[" + ModuleNumber + "] SpeedTank is [" + NavigateOnGrid.SpeedTank + "] We are approaching or orbiting and [" +
                                    Math.Round(Cache.Instance.Approaching.Distance / 1000, 0) + "k] is within MinimumPropulsionModuleDistance [" +
                                    Math.Round((double)MinimumPropulsionModuleDistance / 1000, 2) + "] Activate [" + activate + "]");
                        }

                        if (NavigateOnGrid.SpeedTank)
                        {
                            activate = true;
                            if (Logging.Logging.DebugSpeedMod)
                                Logging.Logging.Log("[" + ModuleNumber + "] We are approaching or orbiting: Activate [" + activate + "]");
                        }
                    }

                    // If we have less then x% cap, do not activate the module
                    //Logging.Log("Defense: Current Cap [" + Cache.Instance.ActiveShip.CapacitorPercentage + "]" + "Settings: minimumPropulsionModuleCapacitor [" + Settings.Instance.MinimumPropulsionModuleCapacitor + "]");
                    if (Cache.Instance.ActiveShip.CapacitorPercentage < MinimumPropulsionModuleCapacitor)
                    {
                        activate = false;
                        if (Logging.Logging.DebugSpeedMod)
                            Logging.Logging.Log("[" + ModuleNumber + "] CapacitorPercentage is [" + Cache.Instance.ActiveShip.CapacitorPercentage +
                                "] which is less than MinimumPropulsionModuleCapacitor [" + MinimumPropulsionModuleCapacitor + "] Activate [" + activate + "]");
                    }

                    if (activate)
                    {
                        if (SpeedMod.Click())
                        {
                            return;
                        }
                    }
                }

                continue;
            }
        }

        public static void ProcessState()
        {
            // Only pulse state changes every x milliseconds
            if (DateTime.UtcNow.Subtract(_lastPulse).TotalMilliseconds < 350) //default: 350ms
                return;
            _lastPulse = DateTime.UtcNow;


            // Thank god stations are safe ! :)
            if (Cache.Instance.InStation)
            {
                _trackingLinkScriptAttempts = 0;
                _sensorBoosterScriptAttempts = 0;
                _sensorDampenerScriptAttempts = 0;
                _trackingComputerScriptAttempts = 0;
                _trackingDisruptorScriptAttempts = 0;
                _nextOverloadAttempt = DateTime.UtcNow;
                return;
            }

            // temporarily fix
            if (!Cache.Instance.Paused && Cache.Instance.InSpace && Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.Entity != null &&
                Cache.Instance.ActiveShip.GroupId != (int) Group.Capsule && Cache.Instance.Modules.Count(m => !m.IsOnline) > 0)
            {
                if (Time.Instance.LastOfflineModuleCheck.AddSeconds(45) < DateTime.UtcNow)
                {
                    Time.Instance.LastOfflineModuleCheck = DateTime.UtcNow;

                    foreach (var mod in Cache.Instance.Modules.Where(m => !m.IsOnline))
                    {
                        Logging.Logging.Log("Offline module: " + mod.TypeName);
                    }

                    Logging.Logging.Log("Offline modules found, going back to base trying to fit again");
                    MissionSettings.CurrentFit = String.Empty;
                    MissionSettings.OfflineModulesFound = true;
                    _States.CurrentQuestorState = QuestorState.Start;
                    _States.CurrentTravelerState = TravelerState.Idle;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    Traveler.Destination = null;
                    Cache.Instance.GotoBaseNow = true;
                    return;
                }
            }


            if (DateTime.UtcNow.AddSeconds(-2) > Time.Instance.LastInSpace)
            {
                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("it was more than 2 seconds ago since we thought we were in space");
                return;
            }

            if (!Cache.Instance.InSpace)
            {
                if (Logging.Logging.DebugDefense) Logging.Logging.Log("we are not in space (yet?)");
                Time.Instance.LastSessionChange = DateTime.UtcNow;
                return;
            }

            // What? No ship entity?
            if (Cache.Instance.ActiveShip.Entity == null)
            {
                Logging.Logging.Log("no ship entity");
                Time.Instance.LastSessionChange = DateTime.UtcNow;
                return;
            }

            if (Cache.Instance.ActiveShip != null && Cache.Instance.ActiveShip.GroupId == (int) Group.Capsule)
            {
                Logging.Logging.Log("We are in a pod, no defense required...");
                return;
            }

            if (DateTime.UtcNow.Subtract(Time.Instance.LastSessionChange).TotalSeconds < 15)
            {
                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("we just completed a session change less than 7 seconds ago... waiting.");
                _nextOverloadAttempt = DateTime.UtcNow;
                return;
            }

            // There is no better defense then being cloaked ;)
            if (Cache.Instance.ActiveShip.Entity.IsCloaked)
            {
                if (Logging.Logging.DebugDefense) Logging.Logging.Log("we are cloaked... no defense needed.");
                _lastCloaked = DateTime.UtcNow;
                return;
            }

            if (DateTime.UtcNow.Subtract(_lastCloaked).TotalSeconds < 2)
            {
                if (Logging.Logging.DebugDefense) Logging.Logging.Log("we are cloaked.... waiting.");
                return;
            }

            if (DoNotBreakInvul && DateTime.UtcNow < Time.Instance.LastSessionChange.AddSeconds(30))
            {
                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("DoNotBreakInvul == true, not running defense yet as that will break invulnerability");
                return;
            }

            if (DateTime.UtcNow.AddHours(-10) > Time.Instance.WehaveMoved &&
                DateTime.UtcNow < Time.Instance.LastInStation.AddSeconds(20))
            {
                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("we have not moved yet after jumping or undocking... waiting.");
                //
                // we reset this datetime stamp to -7 days when we jump, and set it to DateTime.UtcNow when we move (to deactivate jump cloak!)
                // once we have moved (warp, orbit, dock, etc) this should be false and before that it will be true
                //
                return;
            }

            var station = Cache.Instance.Stations.OrderBy(s => s.Distance).FirstOrDefault();

            if (station != null && station.Distance < 1000000 && Cache.Instance.Modules.Any())
            {
                var amountOfModulesToDeactivateThisPulse = Cache.Instance.RandomNumber(2, 4);
                var modulesDeactivatedThisPulse = 0;
                var debugOutputLogged = false;

                foreach (var module in Cache.Instance.Modules.Where(m => m.IsOnline && m.IsActive && !m.IsDeactivating))
                {
                    modulesDeactivatedThisPulse++;

                    if (modulesDeactivatedThisPulse > amountOfModulesToDeactivateThisPulse)
                    {
                        return;
                    }

                    if (!debugOutputLogged)
                    {
                        Logging.Logging.Log("We're next to a station. Deactivating modules.");
                        debugOutputLogged = true;
                    }

                    module.Click();
                }

                return;
            }


            if (Cache.Instance.ActiveShip.CapacitorPercentage < 10 && !Combat.Combat.TargetedBy.Any() &&
                (Cache.Instance.Modules.Where(i => i.GroupId == (int) Group.ShieldBoosters ||
                                                   i.GroupId == (int) Group.AncillaryShieldBooster ||
                                                   i.GroupId == (int) Group.CapacitorInjector ||
                                                   i.GroupId == (int) Group.ArmorRepairer).All(x => !x.IsActive)))
            {
                if (Logging.Logging.DebugDefense)
                    Logging.Logging.Log("Cap is SO low that we should not care about hardeners/boosters as we are not being targeted anyhow)");
                return;
            }

            var targetedByCount = 0;
            if (Combat.Combat.TargetedBy.Any())
            {
                targetedByCount = Combat.Combat.TargetedBy.Count();
            }

            if (Logging.Logging.DebugDefense)
                Logging.Logging.Log("Starting ActivateRepairModules() Current Health Stats: Shields: [" + Math.Round(Cache.Instance.ActiveShip.ShieldPercentage, 0) +
                    "%] Armor: [" + Math.Round(Cache.Instance.ActiveShip.ArmorPercentage, 0) + "%] Cap: [" +
                    Math.Round(Cache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] We are TargetedBy [" + targetedByCount + "] entities");
            ActivateRepairModules();
            if (Logging.Logging.DebugDefense) Logging.Logging.Log("Starting ActivateOnce();");
            ActivateOnce();

            if (Cache.Instance.InWarp)
            {
                _trackingLinkScriptAttempts = 0;
                _sensorBoosterScriptAttempts = 0;
                _sensorDampenerScriptAttempts = 0;
                _trackingComputerScriptAttempts = 0;
                _trackingDisruptorScriptAttempts = 0;
                return;
            }

            // this allows speed mods only when not paused, which is expected behavior
            if (!Cache.Instance.Paused)
            {
                if (Logging.Logging.DebugDefense || Logging.Logging.DebugSpeedMod)
                    Logging.Logging.Log("Starting ActivateSpeedMod();");
                ActivateSpeedMod();
            }

            return;
        }
    }
}