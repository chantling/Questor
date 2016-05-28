using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.Activities;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Lookup;
using Questor.Modules.States;

namespace Questor.Modules.Logging
{
    public partial class Statistics
    {
        public static DateTime StartedMission = DateTime.UtcNow;
        public static DateTime FinishedMission = DateTime.UtcNow;
        public static DateTime StartedSalvaging = DateTime.UtcNow;
        public static DateTime FinishedSalvaging = DateTime.UtcNow;
        public static DateTime StartedPocket = DateTime.UtcNow;
        public static Dictionary<long, double> BountyValues = new Dictionary<long, double>();
        public static bool MissionLoggingCompleted; //false
        public static bool DroneLoggingCompleted; //false
        public static DateTime DateTimeForLogs;

        //singleton class
        private static readonly Statistics _instance = new Statistics();
        public static DateTime LastMissionCompletionError;
        public static bool PocketStatsUseIndividualFilesPerPocket = true;
        public static int TimeSpentReloading_seconds = 0;
        public static int TimeSpentInMission_seconds = 0;
        public static int TimeSpentInMissionInRange = 0;
        public static int TimeSpentInMissionOutOfRange = 0;
        public static int WrecksThisPocket;
        public static int WrecksThisMission;
        public bool MissionLoggingStarted = true;

        Statistics()
        {
            PanicAttemptsThisPocket = 0;
            LowestShieldPercentageThisPocket = 100;
            LowestArmorPercentageThisPocket = 100;
            LowestCapacitorPercentageThisPocket = 100;
            PanicAttemptsThisMission = 0;
            LowestShieldPercentageThisMission = 100;
            LowestArmorPercentageThisMission = 100;
            LowestCapacitorPercentageThisMission = 100;
        }

        public StatisticsState State { get; set; }

        public DateTime MissionLoggingStartedTimestamp { get; set; }

        public static int LootValue { get; set; }
        public static int LoyaltyPointsTotal { get; set; }
        public static int LoyaltyPointsForCurrentMission { get; set; }

        public static int ISKMissionReward { get; set; }
        public static int LostDrones { get; set; }
        public static int DroneRecalls { get; set; }
        public static int AmmoConsumption { get; set; }
        public static int AmmoValue { get; set; }
        public static int MissionsThisSession { get; set; }
        public static int MissionCompletionErrors { get; set; }
        public static int OutOfDronesCount { get; set; }
        public static int AgentLPRetrievalAttempts { get; set; }
        public static bool DroneStatsLog { get; set; }
        public static string DroneStatsLogPath { get; set; }
        public static string DroneStatslogFile { get; set; }
        public static bool VolleyStatsLog { get; set; }
        public static string VolleyStatsLogPath { get; set; }
        public static string VolleyStatslogFile { get; set; }
        public static bool WindowStatsLog { get; set; }
        public static string WindowStatsLogPath { get; set; }
        public static string WindowStatslogFile { get; set; }
        public static bool WreckLootStatistics { get; set; }
        public static string WreckLootStatisticsPath { get; set; }
        public static string WreckLootStatisticsFile { get; set; }
        public static bool MissionStats3Log { get; set; }
        public static string MissionStats3LogPath { get; set; }
        public static string MissionStats3LogFile { get; set; }
        public static bool MissionDungeonIdLog { get; set; }
        public static string MissionDungeonIdLogPath { get; set; }
        public static string MissionDungeonIdLogFile { get; set; }
        public static bool PocketStatistics { get; set; }
        public static string PocketStatisticsPath { get; set; }
        public static string PocketStatisticsFile { get; set; }
        public static bool PocketObjectStatisticsBool { get; set; }
        public static string PocketObjectStatisticsPath { get; set; }
        public static string PocketObjectStatisticsFile { get; set; }
        public static string MissionDetailsHtmlPath { get; set; }
        public static bool PocketObjectStatisticsLog { get; set; }
        public static int RepairCycleTimeThisPocket { get; set; }
        public static int PanicAttemptsThisPocket { get; set; }
        public static double LowestShieldPercentageThisMission { get; set; }
        public static double LowestArmorPercentageThisMission { get; set; }
        public static double LowestCapacitorPercentageThisMission { get; set; }
        public static double LowestShieldPercentageThisPocket { get; set; }
        public static double LowestArmorPercentageThisPocket { get; set; }
        public static double LowestCapacitorPercentageThisPocket { get; set; }
        public static int PanicAttemptsThisMission { get; set; }
        public static int RepairCycleTimeThisMission { get; set; }
        public static int SessionRunningTime { get; set; }
        public static double SessionIskPerHrGenerated { get; set; }
        public static double SessionLootPerHrGenerated { get; set; }
        public static double SessionLPPerHrGenerated { get; set; }
        public static double SessionTotalPerHrGenerated { get; set; }
        public static double IskPerLP { get; set; }

        public double TimeInCurrentMission()
        {
            var missiontimeMinutes = Math.Round(DateTime.UtcNow.Subtract(StartedMission).TotalMinutes, 0);
            return missiontimeMinutes;
        }


        public static bool EntityStatistics(IEnumerable<EntityCache> things)
        {
            var objectline = "Name;Distance;TypeId;GroupId;CategoryId;IsNPC;IsNPCByGroupID;IsPlayer;TargetValue;Velocity;HaveLootRights;IsContainer;ID;\r\n";
            Logging.Log(";EntityStatistics;" + objectline);

            things = things.ToList();

            if (!things.Any()) //if their are no entries, return
            {
                Logging.Log("EntityStatistics: No entries to log");
                return true;
            }

            foreach (var thing in things.OrderBy(i => i.Distance))
                // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
            {
                objectline = thing.Name + ";";
                objectline += Math.Round(thing.Distance/1000, 0) + ";";
                objectline += thing.TypeId + ";";
                objectline += thing.GroupId + ";";
                objectline += thing.CategoryId + ";";
                objectline += thing.IsNpc + ";";
                objectline += thing.IsNpcByGroupID + ";";
                objectline += thing.IsPlayer + ";";
                objectline += thing.TargetValue + ";";
                objectline += Math.Round(thing.Velocity, 0) + ";";
                objectline += thing.HaveLootRights + ";";
                objectline += thing.IsContainer + ";";
                objectline += thing.Id + ";\r\n";

                Logging.Log(";EntityStatistics;" + objectline);
            }
            return true;
        }

        public static bool AmmoConsumptionStatistics()
        {
            if (Cache.Instance.CurrentShipsCargo == null)
            {
                Logging.Log("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            var correctAmmo1 = Combat.Combat.Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType);
            var ammoCargo = Cache.Instance.CurrentShipsCargo.Items.Where(i => correctAmmo1.Any(a => a.TypeId == i.TypeId));
            try
            {
                foreach (var item in ammoCargo)
                {
                    var ammo1 = Combat.Combat.Ammo.FirstOrDefault(a => a.TypeId == item.TypeId);
                    DirectInvType ammoType;
                    Cache.Instance.DirectEve.InvTypes.TryGetValue(item.TypeId, out ammoType);
                    if (ammo1 != null) AmmoConsumption = (ammo1.Quantity - item.Quantity);
                }
            }
            catch (Exception exception)
            {
                Logging.Log("Exception: " + exception);
            }

            return true;
        }

        public static bool WriteDroneStatsLog()
        {
            DateTimeForLogs = DateTime.Now;

            if (DroneStatsLog && !DroneLoggingCompleted)
            {
                if (Drones.UseDrones &&
                    Cache.Instance.ActiveShip.GroupId != (int) Group.Capsule &&
                    Cache.Instance.ActiveShip.GroupId != (int) Group.Shuttle &&
                    Cache.Instance.ActiveShip.GroupId != (int) Group.Frigate &&
                    Cache.Instance.ActiveShip.GroupId != (int) Group.Industrial &&
                    Cache.Instance.ActiveShip.GroupId != (int) Group.TransportShip &&
                    Cache.Instance.ActiveShip.GroupId != (int) Group.Freighter)
                {
                    if (!File.Exists(DroneStatslogFile))
                    {
                        File.AppendAllText(DroneStatslogFile, "Date;Mission;Number of lost drones;# of Recalls\r\n");
                    }

                    var droneline = DateTimeForLogs.ToShortDateString() + ";";
                    droneline += DateTimeForLogs.ToShortTimeString() + ";";
                    droneline += MissionSettings.MissionName + ";";
                    droneline += LostDrones + ";";
                    droneline += +DroneRecalls + ";\r\n";
                    File.AppendAllText(DroneStatslogFile, droneline);
                    DroneLoggingCompleted = true;
                }
                else
                {
                    Logging.Log("We do not use drones in this type of ship, skipping drone stats");
                    DroneLoggingCompleted = true;
                }
            }

            return true;
        }

        public static void WritePocketStatistics()
        {
            DateTimeForLogs = DateTime.Now;

            var currentPocketName = Logging.FilterPath(MissionSettings.MissionName);
                // //agentID needs to change if its a storyline mission - so its assigned in storyline.cs to the various modules directly.
            if (PocketStatistics)
            {
                if (PocketStatsUseIndividualFilesPerPocket)
                {
                    PocketStatisticsFile = Path.Combine(PocketStatisticsPath,
                        Logging.FilterPath(Cache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " + CombatMissionCtrl.PocketNumber +
                        " - PocketStatistics.csv");
                }
                if (!Directory.Exists(PocketStatisticsPath))
                    Directory.CreateDirectory(PocketStatisticsPath);

                if (!File.Exists(PocketStatisticsFile))
                    File.AppendAllText(PocketStatisticsFile,
                        "Date and Time;Mission Name ;Pocket;Time to complete;Isk;panics;LowestShields;LowestArmor;LowestCapacitor;RepairCycles;Wrecks\r\n");

                var pocketstatsLine = DateTimeForLogs + ";"; //Date
                pocketstatsLine += currentPocketName + ";"; //Mission Name
                pocketstatsLine += "pocket" + (CombatMissionCtrl.PocketNumber) + ";"; //Pocket number
                pocketstatsLine += ((int) DateTime.UtcNow.Subtract(StartedMission).TotalMinutes) + ";"; //Time to Complete
                pocketstatsLine += Cache.Instance.MyWalletBalance - Cache.Instance.WealthatStartofPocket + ";"; //Isk
                pocketstatsLine += PanicAttemptsThisPocket + ";"; //Panics
                pocketstatsLine += ((int) LowestShieldPercentageThisPocket) + ";"; //LowestShields
                pocketstatsLine += ((int) LowestArmorPercentageThisPocket) + ";"; //LowestArmor
                pocketstatsLine += ((int) LowestCapacitorPercentageThisPocket) + ";"; //LowestCapacitor
                pocketstatsLine += RepairCycleTimeThisPocket + ";"; //repairCycles
                pocketstatsLine += WrecksThisPocket + ";"; //wrecksThisPocket
                pocketstatsLine += "\r\n";

                Logging.Log("Writing pocket statistics to [ " + PocketStatisticsFile + " ] and clearing stats for next pocket");
                File.AppendAllText(PocketStatisticsFile, pocketstatsLine);
            }

            // Update statistic values for next pocket stats
            Cache.Instance.WealthatStartofPocket = Cache.Instance.MyWalletBalance;
            StartedPocket = DateTime.UtcNow;
            PanicAttemptsThisPocket = 0;
            LowestShieldPercentageThisPocket = 101;
            LowestArmorPercentageThisPocket = 101;
            LowestCapacitorPercentageThisPocket = 101;
            RepairCycleTimeThisPocket = 0;
            WrecksThisMission += WrecksThisPocket;
            WrecksThisPocket = 0;
            Cache.Instance.OrbitEntityNamed = null;
        }

        public static void SaveMissionHTMLDetails(string MissionDetailsHtml, string missionName)
        {
            DateTimeForLogs = DateTime.Now;

            var missionDetailsHtmlFile = Path.Combine(MissionDetailsHtmlPath, missionName + " - " + "mission-description-html.txt");

            if (!Directory.Exists(MissionDetailsHtmlPath))
            {
                Directory.CreateDirectory(MissionDetailsHtmlPath);
            }

            if (!File.Exists(missionDetailsHtmlFile))
            {
                Logging.Log("Writing mission details HTML [ " + missionDetailsHtmlFile + " ]");
                File.AppendAllText(missionDetailsHtmlFile, MissionDetailsHtml);
            }
        }

        public static void WriteMissionStatistics(long statisticsForThisAgent)
        {
            DateTimeForLogs = DateTime.Now;

            if (Cache.Instance.InSpace)
            {
                Logging.Log("We have started questor in space, assume we do not need to write any statistics at the moment.");
                MissionLoggingCompleted = true; //if the mission was completed more than 10 min ago assume the logging has been done already.
                return;
            }

            if (AgentLPRetrievalAttempts > 5)
            {
                Logging.Log("WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" + AgentLPRetrievalAttempts +
                    "] giving up");
                AgentLPRetrievalAttempts = 0;
                MissionLoggingCompleted = true; //if it is not true - this means we should not be trying to log mission stats atm
                return;
            }

            // Seeing as we completed a mission, we will have loyalty points for this agent
            if (Cache.Instance.Agent.LoyaltyPoints == -1)
            {
                AgentLPRetrievalAttempts++;
                Logging.Log("WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" + AgentLPRetrievalAttempts +
                    "] retrying...");
                return;
            }

            AgentLPRetrievalAttempts = 0;

            var isk = Convert.ToInt32(BountyValues.Sum(x => x.Value));
            long lootValCurrentShipInv = 0;
            long lootValItemHangar = 0;

            try
            {
                lootValCurrentShipInv = UnloadLoot.CurrentLootValueInCurrentShipInventory();
            }
            catch (Exception)
            {
            }

            try
            {
                lootValItemHangar = UnloadLoot.CurrentLootValueInItemHangar();
            }
            catch (Exception)
            {
            }


            MissionsThisSession++;
            if (Logging.DebugStatistics) Logging.Log("We jumped through all the hoops: now do the mission logging");

            Logging.Log("Printing All Statistics Related Variables to the console log:");
            Logging.Log("Mission Name: [" + MissionSettings.MissionName + "]");
            Logging.Log("Faction: [" + MissionSettings.FactionName + "]");
            Logging.Log("System: [" + Cache.Instance.MissionSolarSystem + "]");
            Logging.Log("Total Missions completed this session: [" + MissionsThisSession + "]");
            Logging.Log("StartedMission: [ " + StartedMission + "]");
            Logging.Log("FinishedMission: [ " + FinishedMission + "]");
            Logging.Log("StartedSalvaging: [ " + StartedSalvaging + "]");
            Logging.Log("FinishedSalvaging: [ " + FinishedSalvaging + "]");
            Logging.Log("Wealth before mission: [ " + Cache.Instance.Wealth + "]");
            Logging.Log("Wealth after mission: [ " + Cache.Instance.MyWalletBalance + "]");
            Logging.Log("Value of Loot from the mission: [" + lootValCurrentShipInv + "]");
            Logging.Log("Total LP after mission:  [" + Cache.Instance.Agent.LoyaltyPoints + "]");
            Logging.Log("Total LP before mission: [" + LoyaltyPointsTotal + "]");
            Logging.Log("LP from this mission: [" + LoyaltyPointsForCurrentMission + "]");
            Logging.Log("ISKBounty from this mission: [" + isk + "]");
            Logging.Log("ISKMissionreward from this mission: [" + ISKMissionReward + "]");
            Logging.Log("Lootvalue Itemhangar: [" + lootValItemHangar + "]");
            Logging.Log("LostDrones: [" + LostDrones + "]");
            Logging.Log("DroneRecalls: [" + DroneRecalls + "]");
            Logging.Log("AmmoConsumption: [" + AmmoConsumption + "]");
            Logging.Log("AmmoValue: [" + AmmoConsumption + "]");
            Logging.Log("Panic Attempts: [" + PanicAttemptsThisMission + "]");
            Logging.Log("Lowest Shield %: [" + Math.Round(LowestShieldPercentageThisMission, 0) + "]");
            Logging.Log("Lowest Armor %: [" + Math.Round(LowestArmorPercentageThisMission, 0) + "]");
            Logging.Log("Lowest Capacitor %: [" + Math.Round(LowestCapacitorPercentageThisMission, 0) + "]");
            Logging.Log("Repair Cycle Time: [" + RepairCycleTimeThisMission + "]");
            Logging.Log("MissionXMLIsAvailable: [" + MissionSettings.MissionXMLIsAvailable + "]");
            Logging.Log("MissionCompletionerrors: [" + MissionCompletionErrors + "]");
            Logging.Log("the stats below may not yet be correct and need some TLC");
            var weaponNumber = 0;
            foreach (var weapon in Cache.Instance.Weapons)
            {
                weaponNumber++;
                if (Time.Instance.ReloadTimePerModule != null && Time.Instance.ReloadTimePerModule.ContainsKey(weapon.ItemId))
                {
                    Logging.Log("Time Spent Reloading: [" + weaponNumber + "][" + Time.Instance.ReloadTimePerModule[weapon.ItemId] + "]");
                }
            }
            Logging.Log("Time Spent IN Mission: [" + TimeSpentInMission_seconds + "sec]");
            Logging.Log("Time Spent In Range: [" + TimeSpentInMissionInRange + "]");
            Logging.Log("Time Spent Out of Range: [" + TimeSpentInMissionOutOfRange + "]");

            if (MissionStats3Log)
            {
                if (!Directory.Exists(MissionStats3LogPath))
                {
                    Directory.CreateDirectory(MissionStats3LogPath);
                }

                if (!File.Exists(MissionStats3LogFile))
                {
                    File.AppendAllText(MissionStats3LogFile,
                        "Date;Mission;Time;Isk;IskReward;Loot;LP;DroneRecalls;LostDrones;AmmoConsumption;AmmoValue;Panics;LowestShield;LowestArmor;LowestCap;RepairCycles;AfterMissionsalvageTime;TotalMissionTime;MissionXMLAvailable;Faction;SolarSystem;DungeonID;OutOfDronesCount;ISKWallet;ISKLootHangarItems;TotalLP\r\n");
                }

                var line3 = DateTimeForLogs + ";"; // Date
                line3 += MissionSettings.MissionName + ";"; // Mission
                line3 += ((int) FinishedMission.Subtract(StartedMission).TotalMinutes) + ";"; // TimeMission
                line3 += isk + ";"; // Isk
                line3 += ISKMissionReward + ";"; // ISKMissionReward
                line3 += lootValCurrentShipInv + ";"; // Loot
                line3 += LoyaltyPointsForCurrentMission + ";"; // LP
                line3 += DroneRecalls + ";"; // Lost Drones
                line3 += LostDrones + ";"; // Lost Drones
                line3 += AmmoConsumption + ";"; // Ammo Consumption
                line3 += AmmoValue + ";"; // Ammo Value
                line3 += PanicAttemptsThisMission + ";"; // Panics
                line3 += ((int) LowestShieldPercentageThisMission) + ";"; // Lowest Shield %
                line3 += ((int) LowestArmorPercentageThisMission) + ";"; // Lowest Armor %
                line3 += ((int) LowestCapacitorPercentageThisMission) + ";"; // Lowest Capacitor %
                line3 += RepairCycleTimeThisMission + ";"; // repair Cycle Time
                line3 += ((int) FinishedSalvaging.Subtract(StartedSalvaging).TotalMinutes) + ";"; // After Mission Salvaging Time
                line3 += ((int) FinishedSalvaging.Subtract(StartedSalvaging).TotalMinutes) + ((int) FinishedMission.Subtract(StartedMission).TotalMinutes) + ";";
                    // Total Time, Mission + After Mission Salvaging (if any)
                line3 += MissionSettings.MissionXMLIsAvailable.ToString(CultureInfo.InvariantCulture) + ";";
                line3 += MissionSettings.FactionName + ";"; // FactionName that the mission is against
                line3 += Cache.Instance.MissionSolarSystem.Name + ";"; // SolarSystem the mission was located in
                line3 += Cache.Instance.DungeonId + ";"; // DungeonID - the unique identifier for this mission
                line3 += OutOfDronesCount + ";"; // OutOfDronesCount - number of times we totally ran out of drones and had to go re-arm
                line3 += Cache.Instance.MyWalletBalance + ";"; // Current wallet balance
                line3 += lootValItemHangar + ";"; // loot value in itemhangar
                line3 += LoyaltyPointsTotal; // total LP
                line3 += "\r\n";

                // The mission is finished
                Logging.Log("writing mission log3 to  [ " + MissionStats3LogFile + " ]");
                File.AppendAllText(MissionStats3LogFile, line3);
            }
            if (MissionDungeonIdLog)
            {
                if (!Directory.Exists(MissionDungeonIdLogPath))
                {
                    Directory.CreateDirectory(MissionDungeonIdLogPath);
                }


                if (!File.Exists(MissionDungeonIdLogFile))
                {
                    File.AppendAllText(MissionDungeonIdLogFile, "Mission;Faction;DungeonID;\r\n");
                }

                var line4 = DateTimeForLogs + ";"; // Date
                line4 += MissionSettings.MissionName + ";"; // Mission
                line4 += MissionSettings.FactionName + ";"; // FactionName that the mission is against
                line4 += Cache.Instance.DungeonId + ";"; // DungeonID - the unique identifier for this mission (parsed from the mission HTML)
                line4 += "\r\n";

                // The mission is finished
                Logging.Log("writing mission dungeonID log to  [ " + MissionDungeonIdLogFile + " ]");
                File.AppendAllText(MissionDungeonIdLogFile, line4);
            }

            MissionLoggingCompleted = true;
            LootValue = 0;
            LoyaltyPointsTotal = Cache.Instance.Agent.LoyaltyPoints;
            StartedMission = DateTime.UtcNow;
            FinishedMission = DateTime.UtcNow; //this may need to be reset to DateTime.MinValue, but that was causing other issues...
            MissionSettings.MissionName = string.Empty;
            DroneRecalls = 0;
            LostDrones = 0;
            AmmoConsumption = 0;
            AmmoValue = 0;
            DroneLoggingCompleted = false;
            MissionCompletionErrors = 0;
            OutOfDronesCount = 0;
            foreach (var weapon in Cache.Instance.Weapons)
            {
                if (Time.Instance.ReloadTimePerModule != null && Time.Instance.ReloadTimePerModule.ContainsKey(weapon.ItemId))
                {
                    Time.Instance.ReloadTimePerModule[weapon.ItemId] = 0;
                }
            }

            BountyValues = new Dictionary<long, double>();
            PanicAttemptsThisMission = 0;
            LowestShieldPercentageThisMission = 101;
            LowestArmorPercentageThisMission = 101;
            LowestCapacitorPercentageThisMission = 101;
            RepairCycleTimeThisMission = 0;
            TimeSpentReloading_seconds = 0; // this will need to be added to whenever we reload or switch ammo
            TimeSpentInMission_seconds = 0; // from landing on grid (loading mission actions) to going to base (changing to gotobase state)
            TimeSpentInMissionInRange = 0; // time spent totally out of range, no targets
            TimeSpentInMissionOutOfRange = 0; // time spent in range - with targets to kill (or no targets?!)
            Cache.Instance.MissionSolarSystem = null;
            Cache.Instance.DungeonId = "n/a";
            Cache.Instance.OrbitEntityNamed = null;
        }

        public static void ProcessState()
        {
            switch (_States.CurrentStatisticsState)
            {
                case StatisticsState.Idle:
                    break;

                case StatisticsState.LogAllEntities:
                    if (!Cache.Instance.InWarp)
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.LogAllEntities");
                        LogEntities(Cache.Instance.EntitiesOnGrid.ToList());
                    }
                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;

                case StatisticsState.ListPotentialCombatTargets:
                    if (!Cache.Instance.InWarp)
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.LogAllEntities");
                        LogEntities(Combat.Combat.PotentialCombatTargets.Where(i => i.IsOnGridWithMe).ToList());
                    }
                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;

                case StatisticsState.ListHighValueTargets:
                    if (!Cache.Instance.InWarp)
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.LogAllEntities");
                        LogEntities(Combat.Combat.PotentialCombatTargets.Where(i => i.IsHighValueTarget).ToList());
                    }
                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;

                case StatisticsState.ListLowValueTargets:
                    if (!Cache.Instance.InWarp)
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.LogAllEntities");
                        LogEntities(Combat.Combat.PotentialCombatTargets.Where(i => i.IsLowValueTarget).ToList());
                    }
                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;

                case StatisticsState.SessionLog:
                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;

                case StatisticsState.ModuleInfo:
                    if (!Cache.Instance.InWarp)
                    {
                        if (Cache.Instance.InSpace || Cache.Instance.InStation)
                        {
                            _States.CurrentStatisticsState = StatisticsState.Idle;
                            Logging.Log("StatisticsState.ModuleInfo");
                            ModuleInfo(Cache.Instance.Modules);
                        }
                    }
                    break;

                case StatisticsState.ListClassInstanceInfo:
                    if (!Cache.Instance.InWarp)
                    {
                        if (Cache.Instance.InSpace)
                        {
                            _States.CurrentStatisticsState = StatisticsState.Idle;
                            Logging.Log("StatisticsState.ListClassInstanceInfo");
                            ListClassInstanceInfo();
                        }
                    }
                    break;

                case StatisticsState.ListIgnoredTargets:
                    if (!Cache.Instance.InWarp)
                    {
                        if (Cache.Instance.InSpace)
                        {
                            _States.CurrentStatisticsState = StatisticsState.Idle;
                            Logging.Log("StatisticsState.ListIgnoredTargets");
                            ListIgnoredTargets();
                        }
                    }
                    break;

                case StatisticsState.ListDronePriorityTargets:
                    if (!Cache.Instance.InWarp)
                    {
                        if (Cache.Instance.InSpace)
                        {
                            _States.CurrentStatisticsState = StatisticsState.Idle;
                            Logging.Log("StatisticsState.ListDronePriorityTargets");
                            ListDronePriorityTargets(Drones.DronePriorityEntities);
                        }
                    }
                    break;

                case StatisticsState.ListTargetedandTargeting:
                    if (!Cache.Instance.InWarp)
                    {
                        if (Cache.Instance.InSpace)
                        {
                            _States.CurrentStatisticsState = StatisticsState.Idle;
                            Logging.Log("StatisticsState.ListTargetedandTargeting");
                            ListTargetedandTargeting(Cache.Instance.TotalTargetsandTargeting);
                        }
                    }
                    break;

                case StatisticsState.PocketObjectStatistics:
                    if (!Cache.Instance.InWarp)
                    {
                        if (Cache.Instance.EntitiesOnGrid.Any())
                        {
                            _States.CurrentStatisticsState = StatisticsState.Idle;
                            Logging.Log("StatisticsState.PocketObjectStatistics");
                            PocketObjectStatistics(Cache.Instance.EntitiesOnGrid.ToList(), true);
                        }
                    }
                    break;

                case StatisticsState.ListItemHangarItems:
                    if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20))
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.ListItemHangarItems");
                        List<ItemCache> ItemsToList;
                        if (Cache.Instance.ItemHangar != null && Cache.Instance.ItemHangar.Items.Any())
                        {
                            ItemsToList = Cache.Instance.ItemHangar.Items.Select(i => new ItemCache(i)).ToList();
                        }
                        else
                        {
                            ItemsToList = new List<ItemCache>();
                        }

                        ListItems(ItemsToList);
                    }
                    break;

                case StatisticsState.ListLootHangarItems:
                    if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20))
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.ListLootHangarItems");
                        List<ItemCache> ItemsToList;
                        if (Cache.Instance.LootHangar != null && Cache.Instance.LootHangar.Items.Any())
                        {
                            ItemsToList = Cache.Instance.LootHangar.Items.Select(i => new ItemCache(i)).ToList();
                        }
                        else
                        {
                            ItemsToList = new List<ItemCache>();
                        }

                        ListItems(ItemsToList);
                    }
                    break;

                case StatisticsState.ListLootContainerItems:
                    if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20))
                    {
                        _States.CurrentStatisticsState = StatisticsState.Idle;
                        Logging.Log("StatisticsState.ListLootContainerItems");
                        List<ItemCache> ItemsToList;
                        if (Cache.Instance.LootContainer != null && Cache.Instance.LootContainer.Items.Any())
                        {
                            ItemsToList = Cache.Instance.LootContainer.Items.Select(i => new ItemCache(i)).ToList();
                        }
                        else
                        {
                            ItemsToList = new List<ItemCache>();
                        }

                        ListItems(ItemsToList);
                    }
                    break;


                case StatisticsState.Done:

                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;

                default:

                    _States.CurrentStatisticsState = StatisticsState.Idle;
                    break;
            }
        }
    }
}