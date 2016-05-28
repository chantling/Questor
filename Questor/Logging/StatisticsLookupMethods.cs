/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 08.05.2016
 * Time: 15:15
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Lookup;

namespace Questor.Modules.Logging
{
    /// <summary>
    ///     Description of StatisticsMethods.
    /// </summary>
    public partial class Statistics
    {
        public static bool ListClassInstanceInfo()
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            if (Cache.Instance.EntitiesOnGrid.Any())
            {
                Logging.Log("InvType Class Instances: [" + InvType.InvTypeInstances + "]");
                Logging.Log("Cache Class Instances: [" + Cache.CacheInstances + "]");
                Logging.Log("Settings Class Instances: [" + Settings.SettingsInstances + "]");
            }
            Logging.Log("--------------------------- Done  (listed above) -----------------------------");


            return true;
        }

        public static bool ListIgnoredTargets()
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            Logging.Log("Note: Ignore Targets are based on Text Matching. If you ignore: Angel Warlord you ignore all of them on the field!");
            if (CombatMissionCtrl.IgnoreTargets.Any())
            {
                var icount = 0;
                foreach (var ignoreTarget in CombatMissionCtrl.IgnoreTargets)
                {
                    icount++;
                    Logging.Log("[" + ignoreTarget + "] of a total of [" + CombatMissionCtrl.IgnoreTargets.Count() + "]");
                }
            }
            Logging.Log("--------------------------- Done  (listed above) -----------------------------");
            return true;
        }

        public static bool ListDronePriorityTargets(IEnumerable<EntityCache> primaryDroneTargets)
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            if (Drones.PreferredDroneTarget != null)
            {
                Logging.Log("[" + 0 + "] PreferredDroneTarget [" + Drones.PreferredDroneTarget.Name + "][" + Math.Round(Drones.PreferredDroneTarget.Distance / 1000, 0) +
                    "k] IsInOptimalRange [" + Drones.PreferredDroneTarget.IsInOptimalRange + "] IsTarget [" + Drones.PreferredDroneTarget.IsTarget + "]");
            }

            primaryDroneTargets = primaryDroneTargets.ToList();
            if (primaryDroneTargets.Any())
            {
                var icount = 0;
                foreach (var dronePriorityTarget in primaryDroneTargets.OrderBy(i => i.DronePriorityLevel).ThenBy(i => i.Name))
                {
                    icount++;
                    Logging.Log("[" + dronePriorityTarget.Name + "][" + Math.Round(dronePriorityTarget.Distance / 1000, 0) + "k] IsInOptimalRange [" +
                        dronePriorityTarget.IsInOptimalRange + "] IsTarget [" + dronePriorityTarget.IsTarget + "] DronePriorityLevel [" +
                        dronePriorityTarget.DronePriorityLevel + "]");
                }
            }
            Logging.Log("--------------------------- Done  (listed above) -----------------------------");
            return true;
        }

        public static bool ListTargetedandTargeting(IEnumerable<EntityCache> targetedandTargeting)
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            targetedandTargeting = targetedandTargeting.ToList();
            if (targetedandTargeting.Any())
            {
                var icount = 0;
                foreach (var targetedandTargetingEntity in targetedandTargeting.OrderBy(i => i.Distance).ThenBy(i => i.Name))
                {
                    icount++;
                    Logging.Log("[" + targetedandTargetingEntity.Name + "][" + Math.Round(targetedandTargetingEntity.Distance / 1000, 0) + "k] IsIgnored [" +
                        targetedandTargetingEntity.IsIgnored + "] IsInOptimalRange [" + targetedandTargetingEntity.IsInOptimalRange + "] isTarget [" +
                        targetedandTargetingEntity.IsTarget + "] isTargeting [" + targetedandTargetingEntity.IsTargeting + "] IsPrimaryWeaponPriorityTarget [" +
                        targetedandTargetingEntity.IsPrimaryWeaponPriorityTarget + "] IsDronePriorityTarget [" +
                        targetedandTargetingEntity.IsDronePriorityTarget + "]");
                }
            }
            Logging.Log("--------------------------- Done  (listed above)-----------------------------");
            return true;
        }


        public static bool WreckStatistics(IEnumerable<ItemCache> items, EntityCache containerEntity)
        {
            DateTimeForLogs = DateTime.Now;


            if (WreckLootStatistics)
            {
                if (containerEntity != null)
                {
                    // Log all items found in the wreck
                    File.AppendAllText(WreckLootStatisticsFile, "TIME: " + string.Format("{0:dd/MM/yyyy HH:mm:ss}", DateTimeForLogs) + "\n");
                    File.AppendAllText(WreckLootStatisticsFile, "NAME: " + containerEntity.Name + "\n");
                    File.AppendAllText(WreckLootStatisticsFile, "ITEMS:" + "\n");
                    foreach (var item in items.OrderBy(i => i.TypeId))
                    {
                        File.AppendAllText(WreckLootStatisticsFile, "TypeID: " + item.TypeId.ToString(CultureInfo.InvariantCulture) + "\n");
                        File.AppendAllText(WreckLootStatisticsFile, "Name: " + item.Name + "\n");
                        File.AppendAllText(WreckLootStatisticsFile, "Quantity: " + item.Quantity.ToString(CultureInfo.InvariantCulture) + "\n");
                        File.AppendAllText(WreckLootStatisticsFile, "=\n");
                    }
                    File.AppendAllText(WreckLootStatisticsFile, ";" + "\n");
                }
            }
            return true;
        }

        public static bool LogWindowActionToWindowLog(string Windowname, string Description)
        {
            try
            {
                string textToLogToFile;
                if (!File.Exists(WindowStatslogFile))
                {
                    textToLogToFile = "WindowName;Description;Time;Seconds Since LastInSpace;Seconds Since LastInStation;Seconds Since We Started;\r\n";
                    File.AppendAllText(WindowStatslogFile, textToLogToFile);
                }

                textToLogToFile = Windowname + ";" + Description + ";" + DateTime.UtcNow.ToShortTimeString() + ";" +
                                  Time.Instance.LastInSpace.Subtract(DateTime.UtcNow).TotalSeconds + ";" +
                                  Time.Instance.LastInStation.Subtract(DateTime.UtcNow).TotalSeconds + ";" +
                                  Time.Instance.QuestorStarted_DateTime.Subtract(DateTime.UtcNow).TotalSeconds + ";";
                textToLogToFile += "\r\n";

                File.AppendAllText(WindowStatslogFile, textToLogToFile);
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log("Exception while logging to file [" + ex.Message + "]");
                return false;
            }
        }

        public static bool PocketObjectStatistics(List<EntityCache> things, bool force = false)
        {
            if (PocketObjectStatisticsLog || force)
            {
                var currentPocketName = Logging.FilterPath("Random-Grid");
                try
                {
                    if (!String.IsNullOrEmpty(MissionSettings.MissionName))
                    {
                        currentPocketName = Logging.FilterPath(MissionSettings.MissionName);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Log("PocketObjectStatistics: is cache.Instance.MissionName null?: exception was [" + ex.Message + "]");
                }

                PocketObjectStatisticsFile = Path.Combine(
                    PocketObjectStatisticsPath,
                    Logging.FilterPath(Cache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " +
                    CombatMissionCtrl.PocketNumber + " - ObjectStatistics.csv");

                Logging.Log("Logging info on the [" + things.Count + "] objects in this pocket to [" + PocketObjectStatisticsFile + "]");

                if (File.Exists(PocketObjectStatisticsFile))
                {
                    File.Delete(PocketObjectStatisticsFile);
                }

                var objectline = "Name;Distance;TypeId;GroupId;CategoryId;IsNPC;IsPlayer;TargetValue;Velocity;ID;\r\n";
                File.AppendAllText(PocketObjectStatisticsFile, objectline);

                foreach (var thing in things.OrderBy(i => i.Distance))
                    // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
                {
                    objectline = thing.Name + ";";
                    objectline += Math.Round(thing.Distance/1000, 0) + ";";
                    objectline += thing.TypeId + ";";
                    objectline += thing.GroupId + ";";
                    objectline += thing.CategoryId + ";";
                    objectline += thing.IsNpc + ";";
                    objectline += thing.IsPlayer + ";";
                    objectline += thing.TargetValue + ";";
                    objectline += Math.Round(thing.Velocity, 0) + ";";
                    objectline += thing.Id + ";\r\n";

                    File.AppendAllText(PocketObjectStatisticsFile, objectline);
                }
            }
            return true;
        }

        public static bool LogEntities(List<EntityCache> things, bool force = false)
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            things = things.ToList();
            if (things.Any())
            {
                var icount = 0;
                foreach (var thing in things.OrderBy(i => i.Distance))
                {
                    icount++;
                    Logging.Log(thing.Name + "[" + Math.Round(thing.Distance / 1000, 0) + "k] GroupID[" + thing.GroupId + "] ID[" + thing.MaskedId + "] isSentry[" +
                        thing.IsSentry + "] IsHVT[" + thing.IsHighValueTarget + "] IsLVT[" + thing.IsLowValueTarget + "] IsIgnored[" + thing.IsIgnored + "]");
                }
            }
            Logging.Log("--------------------------- Done  (listed above)-----------------------------");

            return true;
        }

        public static bool ListItems(IEnumerable<ItemCache> ItemsToList)
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            ItemsToList = ItemsToList.ToList();
            if (ItemsToList.Any())
            {
                var icount = 0;
                foreach (var item in ItemsToList.OrderBy(i => i.TypeId).ThenBy(i => i.GroupId))
                {
                    icount++;
                    Logging.Log("[" + item.Name + "] GroupID [" + item.GroupId + "], IsContraband [" + item.IsContraband + "]");
                }
            }
            Logging.Log("--------------------------- Done  (listed above)-----------------------------");

            return true;
        }

        public static bool ModuleInfo(IEnumerable<ModuleCache> _modules)
        {
            Logging.Log("--------------------------- Start (listed below)-----------------------------");
            _modules = _modules.ToList();
            if (_modules != null && _modules.Any())
            {
                var icount = 0;
                foreach (var _module in _modules.OrderBy(i => i.TypeId).ThenBy(i => i.GroupId))
                {
                    icount++;
                    Logging.Log("TypeID [" + _module.TypeId + "] GroupID [" + _module.GroupId + "] isOnline [" + _module.IsOnline + "] isActivatable [" +
                        _module.IsActivatable + "] IsActive [" + _module.IsActive + "] OptimalRange [" + _module.OptimalRange + "] Falloff [" + _module.FallOff +
                        "] Duration [" + _module.Duration + "] IsActive [" + _module.IsActive + "]");
                }
            }
            Logging.Log("--------------------------- Done  (listed above)-----------------------------");


            return true;
        }
    }
}