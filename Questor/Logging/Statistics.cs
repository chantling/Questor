

using global::Questor.Modules.Actions;

namespace Questor.Modules.Logging
{
	using System;
	using System.Linq;
	using DirectEve;
	using System.IO;
	using System.Globalization;
	using System.Collections.Generic;
	using global::Questor.Modules.Activities;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Caching;
	using global::Questor.Modules.Combat;
	using global::Questor.Modules.Lookup;
	using global::Questor.Modules.States;

	public partial class Statistics
	{
		public StatisticsState State { get; set; }

		public DateTime MissionLoggingStartedTimestamp { get; set; }

		public static DateTime StartedMission = DateTime.UtcNow;
		public static DateTime FinishedMission = DateTime.UtcNow;
		public static DateTime StartedSalvaging = DateTime.UtcNow;
		public static DateTime FinishedSalvaging = DateTime.UtcNow;
		public static DateTime StartedPocket = DateTime.UtcNow;
		
		public static int LootValue { get; set; }
		public static int LoyaltyPointsTotal { get; set; }
		public static int LoyaltyPointsForCurrentMission { get; set; }
		public static Dictionary<long,double> BountyValues = new Dictionary<long, double>();
		
		public static int ISKMissionReward { get; set; }
		public static int LostDrones { get; set; }
		public static int DroneRecalls { get; set; }
		public static int AmmoConsumption { get; set; }
		public static int AmmoValue { get; set; }
		public static int MissionsThisSession { get; set; }
		public static int MissionCompletionErrors { get; set; }
		public static int OutOfDronesCount { get; set; }
		public static int AgentLPRetrievalAttempts { get; set; }
		public static bool MissionLoggingCompleted; //false
		public static bool DroneLoggingCompleted; //false
		public bool MissionLoggingStarted = true;
		public static DateTime DateTimeForLogs;
		
		//singleton class
		private static readonly Statistics _instance = new Statistics();
		public static DateTime LastMissionCompletionError;
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
		public static bool PocketStatsUseIndividualFilesPerPocket = true;
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
		public static int TimeSpentReloading_seconds = 0;
		public static int TimeSpentInMission_seconds = 0;
		public static int TimeSpentInMissionInRange = 0;
		public static int TimeSpentInMissionOutOfRange = 0;
		public static int WrecksThisPocket;
		public static int WrecksThisMission;
		public static double IskPerLP { get; set; }

		Statistics()
		{
			Statistics.PanicAttemptsThisPocket = 0;
			Statistics.LowestShieldPercentageThisPocket = 100;
			Statistics.LowestArmorPercentageThisPocket = 100;
			Statistics.LowestCapacitorPercentageThisPocket = 100;
			Statistics.PanicAttemptsThisMission = 0;
			Statistics.LowestShieldPercentageThisMission = 100;
			Statistics.LowestArmorPercentageThisMission = 100;
			Statistics.LowestCapacitorPercentageThisMission = 100;
		}

		public double TimeInCurrentMission()
		{
			double missiontimeMinutes = Math.Round(DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes, 0);
			return missiontimeMinutes;
		}

	
		public static bool EntityStatistics(IEnumerable<EntityCache> things)
		{
			string objectline = "Name;Distance;TypeId;GroupId;CategoryId;IsNPC;IsNPCByGroupID;IsPlayer;TargetValue;Velocity;HaveLootRights;IsContainer;ID;\r\n";
			Logging.Log("Statistics", ";EntityStatistics;" + objectline, Logging.White);

			things = things.ToList();

			if (!things.Any()) //if their are no entries, return
			{
				Logging.Log("Statistics", "EntityStatistics: No entries to log", Logging.White);
				return true;
			}

			foreach (EntityCache thing in things.OrderBy(i => i.Distance)) // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
			{
				objectline = thing.Name + ";";
				objectline += Math.Round(thing.Distance / 1000, 0) + ";";
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
				
				Logging.Log("Statistics", ";EntityStatistics;" + objectline, Logging.White);
			}
			return true;
		}

		public static bool AmmoConsumptionStatistics()
		{
			if (Cache.Instance.CurrentShipsCargo == null)
			{
				Logging.Log("AmmoConsumptionStatistics", "if (Cache.Instance.CurrentShipsCargo == null)", Logging.Teal);
				return false;
			}

			IEnumerable<Ammo> correctAmmo1 = Combat.Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType);
			IEnumerable<DirectItem> ammoCargo = Cache.Instance.CurrentShipsCargo.Items.Where(i => correctAmmo1.Any(a => a.TypeId == i.TypeId));
			try
			{
				foreach (DirectItem item in ammoCargo)
				{
					Ammo ammo1 = Combat.Ammo.FirstOrDefault(a => a.TypeId == item.TypeId);
					DirectInvType ammoType;
					Cache.Instance.DirectEve.InvTypes.TryGetValue(item.TypeId, out ammoType);
					if (ammo1 != null) Statistics.AmmoConsumption = (ammo1.Quantity - item.Quantity);
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Statistics.AmmoConsumptionStatistics","Exception: " + exception,Logging.Debug);
			}

			return true;
		}

		public static bool WriteDroneStatsLog()
		{
			DateTimeForLogs = DateTime.Now;

			if (DroneStatsLog && !Statistics.DroneLoggingCompleted)
			{
				if (Drones.UseDrones &&
				    Cache.Instance.ActiveShip.GroupId != (int)Group.Capsule &&
				    Cache.Instance.ActiveShip.GroupId != (int)Group.Shuttle &&
				    Cache.Instance.ActiveShip.GroupId != (int)Group.Frigate &&
				    Cache.Instance.ActiveShip.GroupId != (int)Group.Industrial &&
				    Cache.Instance.ActiveShip.GroupId != (int)Group.TransportShip &&
				    Cache.Instance.ActiveShip.GroupId != (int)Group.Freighter)
				{
					if (!File.Exists(DroneStatslogFile))
					{
						File.AppendAllText(DroneStatslogFile, "Date;Mission;Number of lost drones;# of Recalls\r\n");
					}

					string droneline = DateTimeForLogs.ToShortDateString() + ";";
					droneline += DateTimeForLogs.ToShortTimeString() + ";";
					droneline += MissionSettings.MissionName + ";";
					droneline += Statistics.LostDrones + ";";
					droneline += +Statistics.DroneRecalls + ";\r\n";
					File.AppendAllText(DroneStatslogFile, droneline);
					Statistics.DroneLoggingCompleted = true;
				}
				else
				{
					Logging.Log("DroneStats", "We do not use drones in this type of ship, skipping drone stats", Logging.White);
					Statistics.DroneLoggingCompleted = true;
				}
			}

			return true;
		}

		public static void WritePocketStatistics()
		{
			DateTimeForLogs = DateTime.Now;

			string currentPocketName = Logging.FilterPath(MissionSettings.MissionName); // //agentID needs to change if its a storyline mission - so its assigned in storyline.cs to the various modules directly.
			if (PocketStatistics)
			{
				if (PocketStatsUseIndividualFilesPerPocket)
				{
					PocketStatisticsFile = Path.Combine(PocketStatisticsPath, Logging.FilterPath(Cache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " + CombatMissionCtrl.PocketNumber + " - PocketStatistics.csv");
				}
				if (!Directory.Exists(PocketStatisticsPath))
					Directory.CreateDirectory(PocketStatisticsPath);

				if (!File.Exists(PocketStatisticsFile))
					File.AppendAllText(PocketStatisticsFile, "Date and Time;Mission Name ;Pocket;Time to complete;Isk;panics;LowestShields;LowestArmor;LowestCapacitor;RepairCycles;Wrecks\r\n");

				string pocketstatsLine = DateTimeForLogs + ";";                                                            //Date
				pocketstatsLine += currentPocketName + ";";                                                                //Mission Name
				pocketstatsLine += "pocket" + (CombatMissionCtrl.PocketNumber) + ";";                                         //Pocket number
				pocketstatsLine += ((int)DateTime.UtcNow.Subtract(Statistics.StartedMission).TotalMinutes) + ";"; //Time to Complete
				pocketstatsLine += Cache.Instance.MyWalletBalance - Cache.Instance.WealthatStartofPocket + ";";            //Isk
				pocketstatsLine += Statistics.PanicAttemptsThisPocket + ";";                                           //Panics
				pocketstatsLine += ((int)Statistics.LowestShieldPercentageThisPocket) + ";";                           //LowestShields
				pocketstatsLine += ((int)Statistics.LowestArmorPercentageThisPocket) + ";";                            //LowestArmor
				pocketstatsLine += ((int)Statistics.LowestCapacitorPercentageThisPocket) + ";";                        //LowestCapacitor
				pocketstatsLine += Statistics.RepairCycleTimeThisPocket + ";";                                         //repairCycles
				pocketstatsLine += Statistics.WrecksThisPocket + ";";                                                  //wrecksThisPocket
				pocketstatsLine += "\r\n";

				Logging.Log("Statistics: WritePocketStatistics", "Writing pocket statistics to [ " + PocketStatisticsFile + " ] and clearing stats for next pocket", Logging.White);
				File.AppendAllText(PocketStatisticsFile, pocketstatsLine);
			}

			// Update statistic values for next pocket stats
			Cache.Instance.WealthatStartofPocket = Cache.Instance.MyWalletBalance;
			Statistics.StartedPocket = DateTime.UtcNow;
			Statistics.PanicAttemptsThisPocket = 0;
			Statistics.LowestShieldPercentageThisPocket = 101;
			Statistics.LowestArmorPercentageThisPocket = 101;
			Statistics.LowestCapacitorPercentageThisPocket = 101;
			Statistics.RepairCycleTimeThisPocket = 0;
			Statistics.WrecksThisMission += Statistics.WrecksThisPocket;
			Statistics.WrecksThisPocket = 0;
			Cache.Instance.OrbitEntityNamed = null;
		}

		public static void SaveMissionHTMLDetails(string MissionDetailsHtml, string missionName)
		{
			DateTimeForLogs = DateTime.Now;

			string missionDetailsHtmlFile = Path.Combine(MissionDetailsHtmlPath, missionName + " - " + "mission-description-html.txt");
			
			if (!Directory.Exists(MissionDetailsHtmlPath))
			{
				Directory.CreateDirectory(MissionDetailsHtmlPath);
			}

			if (!File.Exists(missionDetailsHtmlFile))
			{
				Logging.Log("Statistics: SaveMissionHTMLDetails", "Writing mission details HTML [ " + missionDetailsHtmlFile + " ]", Logging.White);
				File.AppendAllText(missionDetailsHtmlFile, MissionDetailsHtml);
			}
		}

		public static void WriteMissionStatistics(long statisticsForThisAgent)
		{
			DateTimeForLogs = DateTime.Now;
		
			if (Cache.Instance.InSpace)
			{
				Logging.Log("Statistics", "We have started questor in space, assume we do not need to write any statistics at the moment.", Logging.Teal);
				Statistics.MissionLoggingCompleted = true; //if the mission was completed more than 10 min ago assume the logging has been done already.
				return;
			}

			if (AgentLPRetrievalAttempts > 5)
			{
				Logging.Log("Statistics", "WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" + AgentLPRetrievalAttempts + "] giving up", Logging.White);
				AgentLPRetrievalAttempts = 0;
				Statistics.MissionLoggingCompleted = true; //if it is not true - this means we should not be trying to log mission stats atm
				return;
			}

			// Seeing as we completed a mission, we will have loyalty points for this agent
			if (Cache.Instance.Agent.LoyaltyPoints == -1)
			{
				AgentLPRetrievalAttempts++;
				Logging.Log("Statistics", "WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" + AgentLPRetrievalAttempts + "] retrying...", Logging.White);
				return;
			}

			AgentLPRetrievalAttempts = 0;

			int isk = Convert.ToInt32(BountyValues.Sum(x => x.Value));
			int lootVal = UnloadLoot.CurrentLootValueInShipHangar();
			
			MissionsThisSession++;
			if (Logging.DebugStatistics) Logging.Log("Statistics", "We jumped through all the hoops: now do the mission logging", Logging.White);
			
			Logging.Log("Statistics", "Printing All Statistics Related Variables to the console log:", Logging.White);
			Logging.Log("Statistics", "Mission Name: [" + MissionSettings.MissionName + "]", Logging.White);
			Logging.Log("Statistics", "Faction: [" + MissionSettings.FactionName + "]", Logging.White);
			Logging.Log("Statistics", "System: [" + Cache.Instance.MissionSolarSystem + "]", Logging.White);
			Logging.Log("Statistics", "Total Missions completed this session: [" + MissionsThisSession + "]", Logging.White);
			Logging.Log("Statistics", "StartedMission: [ " + Statistics.StartedMission + "]", Logging.White);
			Logging.Log("Statistics", "FinishedMission: [ " + Statistics.FinishedMission + "]", Logging.White);
			Logging.Log("Statistics", "StartedSalvaging: [ " + Statistics.StartedSalvaging + "]", Logging.White);
			Logging.Log("Statistics", "FinishedSalvaging: [ " + Statistics.FinishedSalvaging + "]", Logging.White);
			Logging.Log("Statistics", "Wealth before mission: [ " + Cache.Instance.Wealth + "]", Logging.White);
			Logging.Log("Statistics", "Wealth after mission: [ " + Cache.Instance.MyWalletBalance + "]", Logging.White);
			Logging.Log("Statistics", "Value of Loot from the mission: [" + lootVal + "]", Logging.White);
			Logging.Log("Statistics", "Total LP after mission:  [" + Cache.Instance.Agent.LoyaltyPoints + "]", Logging.White);
			Logging.Log("Statistics", "Total LP before mission: [" + Statistics.LoyaltyPointsTotal + "]", Logging.White);
			Logging.Log("Statistics", "LP from this mission: [" + Statistics.LoyaltyPointsForCurrentMission + "]", Logging.White);
			Logging.Log("Statistics", "ISKBounty from this mission: [" + isk + "]", Logging.White);
			Logging.Log("Statistics", "ISKMissionreward from this mission: [" + Statistics.ISKMissionReward + "]", Logging.White);
			Logging.Log("Statistics", "LostDrones: [" + Statistics.LostDrones + "]", Logging.White);
			Logging.Log("Statistics", "DroneRecalls: [" + Statistics.DroneRecalls + "]", Logging.White);
			Logging.Log("Statistics", "AmmoConsumption: [" + Statistics.AmmoConsumption + "]", Logging.White);
			Logging.Log("Statistics", "AmmoValue: [" + Statistics.AmmoConsumption + "]", Logging.White);
			Logging.Log("Statistics", "Panic Attempts: [" + Statistics.PanicAttemptsThisMission + "]", Logging.White);
			Logging.Log("Statistics", "Lowest Shield %: [" + Math.Round(Statistics.LowestShieldPercentageThisMission, 0) + "]", Logging.White);
			Logging.Log("Statistics", "Lowest Armor %: [" + Math.Round(Statistics.LowestArmorPercentageThisMission, 0) + "]", Logging.White);
			Logging.Log("Statistics", "Lowest Capacitor %: [" + Math.Round(Statistics.LowestCapacitorPercentageThisMission, 0) + "]", Logging.White);
			Logging.Log("Statistics", "Repair Cycle Time: [" + Statistics.RepairCycleTimeThisMission + "]", Logging.White);
			Logging.Log("Statistics", "MissionXMLIsAvailable: [" + MissionSettings.MissionXMLIsAvailable + "]", Logging.White);
			Logging.Log("Statistics", "MissionCompletionerrors: [" + Statistics.MissionCompletionErrors + "]", Logging.White);
			Logging.Log("Statistics", "the stats below may not yet be correct and need some TLC", Logging.White);
			int weaponNumber = 0;
			foreach (ModuleCache weapon in Cache.Instance.Weapons)
			{
				weaponNumber++;
				if (Time.Instance.ReloadTimePerModule != null && Time.Instance.ReloadTimePerModule.ContainsKey(weapon.ItemId))
				{
					Logging.Log("Statistics", "Time Spent Reloading: [" + weaponNumber + "][" + Time.Instance.ReloadTimePerModule[weapon.ItemId] + "]", Logging.White);
				}
			}
			Logging.Log("Statistics", "Time Spent IN Mission: [" + TimeSpentInMission_seconds + "sec]", Logging.White);
			Logging.Log("Statistics", "Time Spent In Range: [" + TimeSpentInMissionInRange + "]", Logging.White);
			Logging.Log("Statistics", "Time Spent Out of Range: [" + TimeSpentInMissionOutOfRange + "]", Logging.White);

			if (MissionStats3Log)
			{
				if (!Directory.Exists(MissionStats3LogPath))
				{
					Directory.CreateDirectory(MissionStats3LogPath);
				}

				if (!File.Exists(MissionStats3LogFile))
				{
					File.AppendAllText(MissionStats3LogFile, "Date;Mission;Time;Isk;IskReward;Loot;LP;DroneRecalls;LostDrones;AmmoConsumption;AmmoValue;Panics;LowestShield;LowestArmor;LowestCap;RepairCycles;AfterMissionsalvageTime;TotalMissionTime;MissionXMLAvailable;Faction;SolarSystem;DungeonID;OutOfDronesCount;ISKWallet\r\n");
				}

				string line3 = DateTimeForLogs + ";";                                                                                  // Date
				line3 += MissionSettings.MissionName + ";";                                                                           // Mission
				line3 += ((int)Statistics.FinishedMission.Subtract(Statistics.StartedMission).TotalMinutes) + ";";        			 // TimeMission
				line3 += isk + ";";                                           														  // Isk
				line3 += Statistics.ISKMissionReward + ";";                                           								// ISKMissionReward 
				line3 += lootVal + ";";                                                                 						     // Loot
				line3 += LoyaltyPointsForCurrentMission + ";";                           										    // LP
				line3 += Statistics.DroneRecalls + ";";                                                                             // Lost Drones
				line3 += "LostDrones:" + Statistics.LostDrones + ";";                                                               // Lost Drones
				line3 += Statistics.AmmoConsumption + ";";                                                                          // Ammo Consumption
				line3 += Statistics.AmmoValue + ";";                                                                                // Ammo Value
				line3 += "Panics:" + Statistics.PanicAttemptsThisMission + ";";                                                          // Panics
				line3 += ((int)Statistics.LowestShieldPercentageThisMission) + ";";                                                      // Lowest Shield %
				line3 += ((int)Statistics.LowestArmorPercentageThisMission) + ";";                                                       // Lowest Armor %
				line3 += ((int)Statistics.LowestCapacitorPercentageThisMission) + ";";                                                   // Lowest Capacitor %
				line3 += Statistics.RepairCycleTimeThisMission + ";";                                                                    // repair Cycle Time
				line3 += ((int)Statistics.FinishedSalvaging.Subtract(Statistics.StartedSalvaging).TotalMinutes) + ";";     // After Mission Salvaging Time
				line3 += ((int)Statistics.FinishedSalvaging.Subtract(Statistics.StartedSalvaging).TotalMinutes) + ((int)Statistics.FinishedMission.Subtract(Statistics.StartedMission).TotalMinutes) + ";"; // Total Time, Mission + After Mission Salvaging (if any)
				line3 += MissionSettings.MissionXMLIsAvailable.ToString(CultureInfo.InvariantCulture) + ";";
				line3 += MissionSettings.FactionName + ";";                                                                                   // FactionName that the mission is against
				line3 += Cache.Instance.MissionSolarSystem + ";";                                                                            // SolarSystem the mission was located in
				line3 += Cache.Instance.DungeonId + ";";                                                                                     // DungeonID - the unique identifier for this mission
				line3 += Statistics.OutOfDronesCount + ";";                                                                         // OutOfDronesCount - number of times we totally ran out of drones and had to go re-arm
				line3 += Cache.Instance.MyWalletBalance + ";";
				line3 += "\r\n";

				// The mission is finished
				Logging.Log("Statistics", "writing mission log3 to  [ " + MissionStats3LogFile + " ]", Logging.White);
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

				string line4 = DateTimeForLogs + ";";              // Date
				line4 += MissionSettings.MissionName + ";";      // Mission
				line4 += MissionSettings.FactionName + ";";      // FactionName that the mission is against
				line4 += Cache.Instance.DungeonId + ";";        // DungeonID - the unique identifier for this mission (parsed from the mission HTML)
				line4 += "\r\n";

				// The mission is finished
				Logging.Log("Statistics", "writing mission dungeonID log to  [ " + MissionDungeonIdLogFile + " ]", Logging.White);
				File.AppendAllText(MissionDungeonIdLogFile, line4);
			}

			Statistics.MissionLoggingCompleted = true;
			Statistics.LootValue = 0;
			Statistics.LoyaltyPointsTotal = Cache.Instance.Agent.LoyaltyPoints;
			Statistics.StartedMission = DateTime.UtcNow;
			Statistics.FinishedMission = DateTime.UtcNow; //this may need to be reset to DateTime.MinValue, but that was causing other issues...
			MissionSettings.MissionName = string.Empty;
			Statistics.DroneRecalls = 0;
			Statistics.LostDrones = 0;
			Statistics.AmmoConsumption = 0;
			Statistics.AmmoValue = 0;
			Statistics.DroneLoggingCompleted = false;
			Statistics.MissionCompletionErrors = 0;
			Statistics.OutOfDronesCount = 0;
			foreach (ModuleCache weapon in Cache.Instance.Weapons)
			{
				if (Time.Instance.ReloadTimePerModule != null && Time.Instance.ReloadTimePerModule.ContainsKey(weapon.ItemId))
				{
					Time.Instance.ReloadTimePerModule[weapon.ItemId] = 0;
				}
			}

			BountyValues = new Dictionary<long, double>();
			Statistics.PanicAttemptsThisMission = 0;
			Statistics.LowestShieldPercentageThisMission = 101;
			Statistics.LowestArmorPercentageThisMission = 101;
			Statistics.LowestCapacitorPercentageThisMission = 101;
			Statistics.RepairCycleTimeThisMission = 0;
			Statistics.TimeSpentReloading_seconds = 0;             // this will need to be added to whenever we reload or switch ammo
			Statistics.TimeSpentInMission_seconds = 0;             // from landing on grid (loading mission actions) to going to base (changing to gotobase state)
			Statistics.TimeSpentInMissionInRange = 0;              // time spent totally out of range, no targets
			Statistics.TimeSpentInMissionOutOfRange = 0;           // time spent in range - with targets to kill (or no targets?!)
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
						Logging.Log("Statistics", "StatisticsState.LogAllEntities", Logging.Debug);
						Statistics.LogEntities(Cache.Instance.EntitiesOnGrid.ToList());
					}
					_States.CurrentStatisticsState = StatisticsState.Idle;
					break;

				case StatisticsState.ListPotentialCombatTargets:
					if (!Cache.Instance.InWarp)
					{
						_States.CurrentStatisticsState = StatisticsState.Idle;
						Logging.Log("Statistics", "StatisticsState.LogAllEntities", Logging.Debug);
						Statistics.LogEntities(Combat.PotentialCombatTargets.Where(i => i.IsOnGridWithMe).ToList());
					}
					_States.CurrentStatisticsState = StatisticsState.Idle;
					break;

				case StatisticsState.ListHighValueTargets:
					if (!Cache.Instance.InWarp)
					{
						_States.CurrentStatisticsState = StatisticsState.Idle;
						Logging.Log("Statistics", "StatisticsState.LogAllEntities", Logging.Debug);
						Statistics.LogEntities(Combat.PotentialCombatTargets.Where(i => i.IsHighValueTarget).ToList());
					}
					_States.CurrentStatisticsState = StatisticsState.Idle;
					break;

				case StatisticsState.ListLowValueTargets:
					if (!Cache.Instance.InWarp)
					{
						_States.CurrentStatisticsState = StatisticsState.Idle;
						Logging.Log("Statistics", "StatisticsState.LogAllEntities", Logging.Debug);
						Statistics.LogEntities(Combat.PotentialCombatTargets.Where(i => i.IsLowValueTarget).ToList());
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
							Logging.Log("Statistics", "StatisticsState.ModuleInfo", Logging.Debug);
							Statistics.ModuleInfo(Cache.Instance.Modules);
						}
					}
					break;

				case StatisticsState.ListClassInstanceInfo:
					if (!Cache.Instance.InWarp)
					{
						if (Cache.Instance.InSpace)
						{
							_States.CurrentStatisticsState = StatisticsState.Idle;
							Logging.Log("Statistics", "StatisticsState.ListClassInstanceInfo", Logging.Debug);
							Statistics.ListClassInstanceInfo();
						}
					}
					break;

				case StatisticsState.ListIgnoredTargets:
					if (!Cache.Instance.InWarp)
					{
						if (Cache.Instance.InSpace)
						{
							_States.CurrentStatisticsState = StatisticsState.Idle;
							Logging.Log("Statistics", "StatisticsState.ListIgnoredTargets", Logging.Debug);
							Statistics.ListIgnoredTargets();
						}
					}
					break;

				case StatisticsState.ListDronePriorityTargets:
					if (!Cache.Instance.InWarp)
					{
						if (Cache.Instance.InSpace)
						{
							_States.CurrentStatisticsState = StatisticsState.Idle;
							Logging.Log("Statistics", "StatisticsState.ListDronePriorityTargets", Logging.Debug);
							Statistics.ListDronePriorityTargets(Drones.DronePriorityEntities);
						}
					}
					break;

				case StatisticsState.ListTargetedandTargeting:
					if (!Cache.Instance.InWarp)
					{
						if (Cache.Instance.InSpace)
						{
							_States.CurrentStatisticsState = StatisticsState.Idle;
							Logging.Log("Statistics", "StatisticsState.ListTargetedandTargeting", Logging.Debug);
							Statistics.ListTargetedandTargeting(Cache.Instance.TotalTargetsandTargeting);
						}
					}
					break;

				case StatisticsState.PocketObjectStatistics:
					if (!Cache.Instance.InWarp)
					{
						if (Cache.Instance.EntitiesOnGrid.Any())
						{
							_States.CurrentStatisticsState = StatisticsState.Idle;
							Logging.Log("Statistics", "StatisticsState.PocketObjectStatistics", Logging.Debug);
							Statistics.PocketObjectStatistics(Cache.Instance.EntitiesOnGrid.ToList(), true);
						}
					}
					break;

				case StatisticsState.ListItemHangarItems:
					if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20))
					{
						_States.CurrentStatisticsState = StatisticsState.Idle;
						Logging.Log("Statistics", "StatisticsState.ListItemHangarItems", Logging.Debug);
						List<ItemCache> ItemsToList;
						if (Cache.Instance.ItemHangar != null && Cache.Instance.ItemHangar.Items.Any())
						{
							ItemsToList = Cache.Instance.ItemHangar.Items.Select(i => new ItemCache(i)).ToList();
						}
						else
						{
							ItemsToList = new List<ItemCache>();
						}

						Statistics.ListItems(ItemsToList);
					}
					break;

				case StatisticsState.ListLootHangarItems:
					if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20))
					{
						_States.CurrentStatisticsState = StatisticsState.Idle;
						Logging.Log("Statistics", "StatisticsState.ListLootHangarItems", Logging.Debug);
						List<ItemCache> ItemsToList;
						if (Cache.Instance.LootHangar != null && Cache.Instance.LootHangar.Items.Any())
						{
							ItemsToList = Cache.Instance.LootHangar.Items.Select(i => new ItemCache(i)).ToList();
						}
						else
						{
							ItemsToList = new List<ItemCache>();
						}

						Statistics.ListItems(ItemsToList);
					}
					break;

				case StatisticsState.ListLootContainerItems:
					if (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20))
					{
						_States.CurrentStatisticsState = StatisticsState.Idle;
						Logging.Log("Statistics", "StatisticsState.ListLootContainerItems", Logging.Debug);
						List<ItemCache> ItemsToList;
						if (Cache.Instance.LootContainer != null && Cache.Instance.LootContainer.Items.Any())
						{
							ItemsToList = Cache.Instance.LootContainer.Items.Select(i => new ItemCache(i)).ToList();
						}
						else
						{
							ItemsToList = new List<ItemCache>();
						}
						
						Statistics.ListItems(ItemsToList);
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