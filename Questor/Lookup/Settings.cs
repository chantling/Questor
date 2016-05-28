// ------------------------------------------------------------------------------
// <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
// Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
// Please look in the accompanying license.htm file for the license that
// applies to this source code. (a copy can also be found at:
// http://www.thehackerwithin.com/license.htm)
// </copyright>
// -------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Questor.Modules.Actions;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.States;

namespace Questor.Modules.Lookup
{
    public class Settings
    {
        /// <summary>
        ///     Singleton implementation
        /// </summary>
        static Settings _instance;

        public static int SettingsInstances = 0;
        private DateTime _lastModifiedDateOfMyCommonSettingsFile;
        private DateTime _lastModifiedDateOfMySettingsFile;

        public string CharacterName;
        public List<string> CharacterNamesForMasterToInviteToFleet = new List<string>();

        public bool CharacterXMLExists = true;
        public bool CommonXMLExists = false;

        public bool DefaultSettingsLoaded;
        public bool EVEMemoryManager = false;
        public bool FactionXMLExists = true;
        public long MemoryManagerTrimThreshold = 524288000;

        public int NumberOfModulesToActivateInCycle = 4;

        //
        // path information - used to load the XML and used in other modules
        //
        public string Path = Logging.Logging.PathToCurrentDirectory;
        public bool QuestorManagerExists = true;
        public bool QuestorSettingsExists = true;
        public bool QuestorStatisticsExists = true;
        public bool SchedulesXMLExists = true;
        private int SettingsLoadedICount = 0;

        public Settings()
        {
            try
            {
                Interlocked.Increment(ref SettingsInstances);
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception: [" + exception + "]");
                return;
            }
        }

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                }

                return _instance;
            }
        }

        public string TargetSelectionMethod { get; set; }
        public bool DetailedCurrentTargetHealthLogging { get; set; }
        public bool DefendWhileTraveling { get; set; }

        //public bool setEveClientDestinationWhenTraveling { get; set; }
        public string EveServerName { get; set; }
        public int EnforcedDelayBetweenModuleClicks { get; set; }
        public bool AvoidShootingTargetsWithMissilesIfweKNowTheyAreAboutToBeHitWithAPreviousVolley { get; set; }
        public string CharacterToAcceptInvitesFrom { get; set; }

        //
        // Misc Settings
        //
        public string CharacterMode { get; set; }
        public bool AutoStart { get; set; }
        public bool Disable3D { get; set; }
        public int MinimumDelay { get; set; }
        public int RandomDelay { get; set; }

        //
        // Console Log Settings
        //
        public int MaxLineConsole { get; set; }

        //
        // Enable / Disable Major Features that do not have categories of their own below
        //
        public bool EnableStorylines { get; set; }
        public string StoryLineBaseBookmark { get; set; }
        public bool DeclineStorylinesInsteadofBlacklistingfortheSession { get; set; }
        public bool UseLocalWatch { get; set; }
        public bool UseFittingManager { get; set; }

        public bool WatchForActiveWars { get; set; }

        public bool FleetSupportSlave { get; set; }

        public bool FleetSupportMaster { get; set; }

        public string FleetName { get; set; }

        //
        // Local Watch settings - if enabled
        //
        public int LocalBadStandingPilotsToTolerate { get; set; }
        public double LocalBadStandingLevelToConsiderBad { get; set; }
        public bool FinishWhenNotSafe { get; set; }

        //
        // Invasion Settings
        //
        public int BattleshipInvasionLimit { get; set; }
        public int BattlecruiserInvasionLimit { get; set; }
        public int CruiserInvasionLimit { get; set; }
        public int FrigateInvasionLimit { get; set; }
        public int InvasionMinimumDelay { get; set; }
        public int InvasionRandomDelay { get; set; }

        //
        // Ship Names
        //
        public string SalvageShipName { get; set; }
        public string TransportShipName { get; set; }
        public string TravelShipName { get; set; }
        public string MiningShipName { get; set; }

        //
        //Use HomeBookmark
        //
        public bool UseHomebookmark { get; set; }

        //
        // Storage location for loot, ammo, and bookmarks
        //
        public string HomeBookmarkName { get; set; }
        public string LootHangarTabName { get; set; }
        public string AmmoHangarTabName { get; set; }
        public string BookmarkHangar { get; set; }
        public string LootContainerName { get; set; }
        //        public bool DisableAutoBackgroundMoveToGate { get; set; }

        public string HighTierLootContainer { get; set; }

        //
        // Travel and Undock Settings
        //
        public string BookmarkPrefix { get; set; }
        public string SafeSpotBookmarkPrefix { get; set; }
        public string BookmarkFolder { get; set; }

        public string TravelToBookmarkPrefix { get; set; }

        public string UndockBookmarkPrefix { get; set; }

        //
        // EVE Process Memory Ceiling and EVE wallet balance Change settings
        //
        public int WalletBalanceChangeLogOffDelay { get; set; }

        public string WalletBalanceChangeLogOffDelayLogoffOrExit { get; set; }

        public Int64 EVEProcessMemoryCeiling { get; set; }

        //
        // Script Settings - TypeIDs for the scripts you would like to use in these modules
        //
        public int TrackingDisruptorScript { get; private set; }
        public int TrackingComputerScript { get; private set; }
        public int TrackingLinkScript { get; private set; }
        public int SensorBoosterScript { get; private set; }
        public int SensorDampenerScript { get; private set; }
        public int AncillaryShieldBoosterScript { get; private set; }
        public int CapacitorInjectorScript { get; private set; }
        //they are not scripts, but they work the same, but are consumable for our purposes that does not matter
        public int NumberOfCapBoostersToLoad { get; private set; }
        //
        // OverLoad Settings (this WILL burn out modules, likely very quickly!
        // If you enable the overloading of a slot it is HIGHLY recommended you actually have something overloadable in that slot =/
        //
        public bool OverloadWeapons { get; set; }

        public string CommonSettingsPath { get; private set; }
        public string CommonSettingsFileName { get; private set; }

        public bool BuyAmmo { get; private set; }
        public bool LootWhileSpeedTanking { get; private set; }
        public int BuyAmmoStationID { get; private set; }
        public XElement CommonSettingsXml { get; set; }
        public XElement CharacterSettingsXml { get; set; }

        ~Settings()
        {
            Interlocked.Decrement(ref SettingsInstances);
        }

        public event EventHandler<EventArgs> SettingsLoaded;

        public void ReadSettingsFromXML()
        {
            try
            {
                Logging.Logging.Log("Start reading settings from xml async");
                Cache.Instance.IsLoadingSettings = true;
                Instance.CommonSettingsFileName = (string) CharacterSettingsXml.Element("commonSettingsFileName") ?? "common.xml";
                Instance.CommonSettingsPath = System.IO.Path.Combine(Instance.Path, Instance.CommonSettingsFileName);

                if (File.Exists(Instance.CommonSettingsPath))
                {
                    Instance.CommonXMLExists = true;
                    CommonSettingsXml = XDocument.Load(Instance.CommonSettingsPath).Root;
                    if (CommonSettingsXml == null)
                    {
                        Logging.Logging.Log("found [" + Instance.CommonSettingsPath +
                            "] but was unable to load it: FATAL ERROR - use the provided settings.xml to create that file.");
                    }
                }
                else
                {
                    Instance.CommonXMLExists = false;
                    //
                    // if the common XML does not exist, load the characters XML into the CommonSettingsXml just so we can simplify the XML element loading stuff.
                    //
                    CommonSettingsXml = XDocument.Load(Logging.Logging.CharacterSettingsPath).Root;
                }

                if (CommonSettingsXml == null)
                    return;
                        // this should never happen as we load the characters xml here if the common xml is missing. adding this does quiet some warnings though

                if (Instance.CommonXMLExists)
                    Logging.Logging.Log("Loading Settings from [" + Instance.CommonSettingsPath + "] and");
                Logging.Logging.Log("Loading Settings from [" + Logging.Logging.CharacterSettingsPath + "]");
                //
                // these are listed by feature and should likely be re-ordered to reflect that
                //

                //
                // Debug Settings
                //
                Logging.Logging.DebugActivateGate = (bool?) CharacterSettingsXml.Element("debugActivateGate") ??
                                                    (bool?) CommonSettingsXml.Element("debugActivateGate") ?? false;
                Logging.Logging.DebugActivateBastion = (bool?) CharacterSettingsXml.Element("debugActivateBastion") ??
                                                       (bool?) CommonSettingsXml.Element("debugActivateBastion") ?? false;
                Logging.Logging.DebugActivateWeapons = (bool?) CharacterSettingsXml.Element("debugActivateWeapons") ??
                                                       (bool?) CommonSettingsXml.Element("debugActivateWeapons") ?? false;
                Logging.Logging.DebugAddDronePriorityTarget = (bool?) CharacterSettingsXml.Element("debugAddDronePriorityTarget") ??
                                                              (bool?) CommonSettingsXml.Element("debugAddDronePriorityTarget") ?? false;
                Logging.Logging.DebugAddPrimaryWeaponPriorityTarget = (bool?) CharacterSettingsXml.Element("debugAddPrimaryWeaponPriorityTarget") ??
                                                                      (bool?) CommonSettingsXml.Element("debugAddPrimaryWeaponPriorityTarget") ?? false;
                Logging.Logging.DebugAgentInteractionReplyToAgent = (bool?) CharacterSettingsXml.Element("debugAgentInteractionReplyToAgent") ??
                                                                    (bool?) CommonSettingsXml.Element("debugAgentInteractionReplyToAgent") ?? false;
                Logging.Logging.DebugAllMissionsOnBlackList = (bool?) CharacterSettingsXml.Element("debugAllMissionsOnBlackList") ??
                                                              (bool?) CommonSettingsXml.Element("debugAllMissionsOnBlackList") ?? false;
                Logging.Logging.DebugAllMissionsOnGreyList = (bool?) CharacterSettingsXml.Element("debugAllMissionsOnGreyList") ??
                                                             (bool?) CommonSettingsXml.Element("debugAllMissionsOnGreyList") ?? false;
                Logging.Logging.DebugAmmo = (bool?) CharacterSettingsXml.Element("debugAmmo") ?? (bool?) CommonSettingsXml.Element("debugAmmo") ?? false;
                Logging.Logging.DebugArm = (bool?) CharacterSettingsXml.Element("debugArm") ?? (bool?) CommonSettingsXml.Element("debugArm") ?? false;
                Logging.Logging.DebugAttachVSDebugger = (bool?) CharacterSettingsXml.Element("debugAttachVSDebugger") ??
                                                        (bool?) CommonSettingsXml.Element("debugAttachVSDebugger") ?? false;
                Logging.Logging.DebugAutoStart = (bool?) CharacterSettingsXml.Element("debugAutoStart") ??
                                                 (bool?) CommonSettingsXml.Element("debugAutoStart") ?? false;
                Logging.Logging.DebugBlackList = (bool?) CharacterSettingsXml.Element("debugBlackList") ??
                                                 (bool?) CommonSettingsXml.Element("debugBlackList") ?? false;
                Logging.Logging.DebugCargoHold = (bool?) CharacterSettingsXml.Element("debugCargoHold") ??
                                                 (bool?) CommonSettingsXml.Element("debugCargoHold") ?? false;
                Logging.Logging.DebugChat = (bool?) CharacterSettingsXml.Element("debugChat") ?? (bool?) CommonSettingsXml.Element("debugChat") ?? false;
                Logging.Logging.DebugCleanup = (bool?) CharacterSettingsXml.Element("debugCleanup") ??
                                               (bool?) CommonSettingsXml.Element("debugCleanup") ?? false;
                Logging.Logging.DebugClearPocket = (bool?) CharacterSettingsXml.Element("debugClearPocket") ??
                                                   (bool?) CommonSettingsXml.Element("debugClearPocket") ?? false;
                Logging.Logging.DebugCombat = (bool?) CharacterSettingsXml.Element("debugCombat") ?? (bool?) CommonSettingsXml.Element("debugCombat") ?? false;
                Logging.Logging.DebugCombatMissionBehavior = (bool?) CharacterSettingsXml.Element("debugCombatMissionBehavior") ??
                                                             (bool?) CommonSettingsXml.Element("debugCombatMissionBehavior") ?? false;
                Logging.Logging.DebugCourierMissions = (bool?) CharacterSettingsXml.Element("debugCourierMissions") ??
                                                       (bool?) CommonSettingsXml.Element("debugCourierMissions") ?? false;
                Logging.Logging.DebugDecline = (bool?) CharacterSettingsXml.Element("debugDecline") ??
                                               (bool?) CommonSettingsXml.Element("debugDecline") ?? false;
                Logging.Logging.DebugDefense = (bool?) CharacterSettingsXml.Element("debugDefense") ??
                                               (bool?) CommonSettingsXml.Element("debugDefense") ?? false;
                Logging.Logging.DebugDisableCleanup = (bool?) CharacterSettingsXml.Element("debugDisableCleanup") ??
                                                      (bool?) CommonSettingsXml.Element("debugDisableCleanup") ?? false;
                Logging.Logging.DebugDisableCombatMissionsBehavior = (bool?) CharacterSettingsXml.Element("debugDisableCombatMissionsBehavior") ??
                                                                     (bool?) CommonSettingsXml.Element("debugDisableCombatMissionsBehavior") ?? false;
                Logging.Logging.DebugDisableCombatMissionCtrl = (bool?) CharacterSettingsXml.Element("debugDisableCombatMissionCtrl") ??
                                                                (bool?) CommonSettingsXml.Element("debugDisableCombatMissionCtrl") ?? false;
                Logging.Logging.DebugDisableCombat = (bool?) CharacterSettingsXml.Element("debugDisableCombat") ??
                                                     (bool?) CommonSettingsXml.Element("debugDisableCombat") ?? false;
                Logging.Logging.DebugDisableDrones = (bool?) CharacterSettingsXml.Element("debugDisableDrones") ??
                                                     (bool?) CommonSettingsXml.Element("debugDisableDrones") ?? false;
                Logging.Logging.DebugDisablePanic = (bool?) CharacterSettingsXml.Element("debugDisablePanic") ??
                                                    (bool?) CommonSettingsXml.Element("debugDisablePanic") ?? false;
                Logging.Logging.DebugDisableGetBestTarget = (bool?) CharacterSettingsXml.Element("debugDisableGetBestTarget") ??
                                                            (bool?) CommonSettingsXml.Element("debugDisableGetBestTarget") ?? false;
                Logging.Logging.DebugDisableGetBestDroneTarget = (bool?) CharacterSettingsXml.Element("debugDisableGetBestDroneTarget") ??
                                                                 (bool?) CommonSettingsXml.Element("debugDisableGetBestTarget") ?? false;
                Logging.Logging.DebugDisableSalvage = (bool?) CharacterSettingsXml.Element("debugDisableSalvage") ??
                                                      (bool?) CommonSettingsXml.Element("debugDisableSalvage") ?? false;
                Logging.Logging.DebugDisableGetBestTarget = (bool?) CharacterSettingsXml.Element("debugDisableGetBestTarget") ??
                                                            (bool?) CommonSettingsXml.Element("debugDisableGetBestTarget") ?? false;
                Logging.Logging.DebugDisableTargetCombatants = (bool?) CharacterSettingsXml.Element("debugDisableTargetCombatants") ??
                                                               (bool?) CommonSettingsXml.Element("debugDisableTargetCombatants") ?? false;
                Logging.Logging.DebugDisableNavigateIntoRange = (bool?) CharacterSettingsXml.Element("debugDisableNavigateIntoRange") ??
                                                                (bool?) CommonSettingsXml.Element("debugDisableNavigateIntoRange") ?? false;
                Logging.Logging.DebugDoneAction = (bool?) CharacterSettingsXml.Element("debugDoneAction") ??
                                                  (bool?) CommonSettingsXml.Element("debugDoneAction") ?? false;
                Logging.Logging.DebugDoNotCloseTelcomWindows = (bool?) CharacterSettingsXml.Element("debugDoNotCloseTelcomWindows") ??
                                                               (bool?) CommonSettingsXml.Element("debugDoNotCloseTelcomWindows") ?? false;
                Logging.Logging.DebugDrones = (bool?) CharacterSettingsXml.Element("debugDrones") ?? (bool?) CommonSettingsXml.Element("debugDrones") ?? false;
                Logging.Logging.DebugDroneHealth = (bool?) CharacterSettingsXml.Element("debugDroneHealth") ??
                                                   (bool?) CommonSettingsXml.Element("debugDroneHealth") ?? false;
                Logging.Logging.DebugEachWeaponsVolleyCache = (bool?) CharacterSettingsXml.Element("debugEachWeaponsVolleyCache") ??
                                                              (bool?) CommonSettingsXml.Element("debugEachWeaponsVolleyCache") ?? false;
                Logging.Logging.DebugEntityCache = (bool?) CharacterSettingsXml.Element("debugEntityCache") ??
                                                   (bool?) CommonSettingsXml.Element("debugEntityCache") ?? false;
                Logging.Logging.DebugExecuteMission = (bool?) CharacterSettingsXml.Element("debugExecutMission") ??
                                                      (bool?) CommonSettingsXml.Element("debugExecutMission") ?? false;
                Logging.Logging.DebugExceptions = (bool?) CharacterSettingsXml.Element("debugExceptions") ??
                                                  (bool?) CommonSettingsXml.Element("debugExceptions") ?? false;
                Logging.Logging.DebugFittingMgr = (bool?) CharacterSettingsXml.Element("debugFittingMgr") ??
                                                  (bool?) CommonSettingsXml.Element("debugFittingMgr") ?? false;
                Logging.Logging.DebugFleetSupportSlave = (bool?) CharacterSettingsXml.Element("debugFleetSupportSlave") ??
                                                         (bool?) CommonSettingsXml.Element("debugFleetSupportSlave") ?? false;
                Logging.Logging.DebugFleetSupportMaster = (bool?) CharacterSettingsXml.Element("debugFleetSupportMaster") ??
                                                          (bool?) CommonSettingsXml.Element("debugFleetSupportMaster") ?? false;
                Logging.Logging.DebugGetBestTarget = (bool?) CharacterSettingsXml.Element("debugGetBestTarget") ??
                                                     (bool?) CommonSettingsXml.Element("debugGetBestTarget") ?? false;
                Logging.Logging.DebugGetBestDroneTarget = (bool?) CharacterSettingsXml.Element("debugGetBestDroneTarget") ??
                                                          (bool?) CommonSettingsXml.Element("debugGetBestDroneTarget") ?? false;
                Logging.Logging.DebugGotobase = (bool?) CharacterSettingsXml.Element("debugGotobase") ??
                                                (bool?) CommonSettingsXml.Element("debugGotobase") ?? false;
                Logging.Logging.DebugGreyList = (bool?) CharacterSettingsXml.Element("debugGreyList") ??
                                                (bool?) CommonSettingsXml.Element("debugGreyList") ?? false;
                Logging.Logging.DebugHangars = (bool?) CharacterSettingsXml.Element("debugHangars") ??
                                               (bool?) CommonSettingsXml.Element("debugHangars") ?? false;
                Logging.Logging.DebugIdle = (bool?) CharacterSettingsXml.Element("debugIdle") ?? (bool?) CommonSettingsXml.Element("debugIdle") ?? false;
                Logging.Logging.DebugInSpace = (bool?) CharacterSettingsXml.Element("debugInSpace") ??
                                               (bool?) CommonSettingsXml.Element("debugInSpace") ?? false;
                Logging.Logging.DebugInStation = (bool?) CharacterSettingsXml.Element("debugInStation") ??
                                                 (bool?) CommonSettingsXml.Element("debugInStation") ?? false;
                Logging.Logging.DebugInWarp = (bool?) CharacterSettingsXml.Element("debugInWarp") ?? (bool?) CommonSettingsXml.Element("debugInWarp") ?? false;
                Logging.Logging.DebugIsReadyToShoot = (bool?) CharacterSettingsXml.Element("debugIsReadyToShoot") ??
                                                      (bool?) CommonSettingsXml.Element("debugIsReadyToShoot") ?? false;
                Logging.Logging.DebugItemHangar = (bool?) CharacterSettingsXml.Element("debugItemHangar") ??
                                                  (bool?) CommonSettingsXml.Element("debugItemHangar") ?? false;
                Logging.Logging.DebugKillTargets = (bool?) CharacterSettingsXml.Element("debugKillTargets") ??
                                                   (bool?) CommonSettingsXml.Element("debugKillTargets") ?? false;
                Logging.Logging.DebugKillAction = (bool?) CharacterSettingsXml.Element("debugKillAction") ??
                                                  (bool?) CommonSettingsXml.Element("debugKillAction") ?? false;
                Logging.Logging.DebugLoadScripts = (bool?) CharacterSettingsXml.Element("debugLoadScripts") ??
                                                   (bool?) CommonSettingsXml.Element("debugLoadScripts") ?? false;
                Logging.Logging.DebugLogging = (bool?) CharacterSettingsXml.Element("debugLogging") ??
                                               (bool?) CommonSettingsXml.Element("debugLogging") ?? false;
                Logging.Logging.DebugLootWrecks = (bool?) CharacterSettingsXml.Element("debugLootWrecks") ??
                                                  (bool?) CommonSettingsXml.Element("debugLootWrecks") ?? false;
                Logging.Logging.DebugLootValue = (bool?) CharacterSettingsXml.Element("debugLootValue") ??
                                                 (bool?) CommonSettingsXml.Element("debugLootValue") ?? false;
                Logging.Logging.DebugMaintainConsoleLogs = (bool?) CharacterSettingsXml.Element("debugMaintainConsoleLogs") ??
                                                           (bool?) CommonSettingsXml.Element("debugMaintainConsoleLogs") ?? false;
                Logging.Logging.DebugMiningBehavior = (bool?) CharacterSettingsXml.Element("debugMiningBehavior") ??
                                                      (bool?) CommonSettingsXml.Element("debugMiningBehavior") ?? false;
                Logging.Logging.DebugMissionFittings = (bool?) CharacterSettingsXml.Element("debugMissionFittings") ??
                                                       (bool?) CommonSettingsXml.Element("debugMissionFittings") ?? false;
                Logging.Logging.DebugMoveTo = (bool?) CharacterSettingsXml.Element("debugMoveTo") ?? (bool?) CommonSettingsXml.Element("debugMoveTo") ?? false;
                Logging.Logging.DebugNavigateOnGrid = (bool?) CharacterSettingsXml.Element("debugNavigateOnGrid") ??
                                                      (bool?) CommonSettingsXml.Element("debugNavigateOnGrid") ?? false;
                Logging.Logging.DebugOnframe = (bool?) CharacterSettingsXml.Element("debugOnframe") ??
                                               (bool?) CommonSettingsXml.Element("debugOnframe") ?? false;
                Logging.Logging.DebugOverLoadWeapons = (bool?) CharacterSettingsXml.Element("debugOverLoadWeapons") ??
                                                       (bool?) CommonSettingsXml.Element("debugOverLoadWeapons") ?? false;
                Logging.Logging.DebugPanic = (bool?) CharacterSettingsXml.Element("debugPanic") ?? (bool?) CommonSettingsXml.Element("debugPanic") ?? false;
                Logging.Logging.DebugPerformance = (bool?) CharacterSettingsXml.Element("debugPerformance") ??
                                                   (bool?) CommonSettingsXml.Element("debugPerformance") ?? false;
                    //enables more console logging having to do with the sub-states within each state
                Logging.Logging.DebugPotentialCombatTargets = (bool?) CharacterSettingsXml.Element("debugPotentialCombatTargets") ??
                                                              (bool?) CommonSettingsXml.Element("debugPotentialCombatTargets") ?? false;
                Logging.Logging.DebugPreferredPrimaryWeaponTarget = (bool?) CharacterSettingsXml.Element("debugPreferredPrimaryWeaponTarget") ??
                                                                    (bool?) CommonSettingsXml.Element("debugPreferredPrimaryWeaponTarget") ?? false;
                Logging.Logging.DebugPreLogin = (bool?) CharacterSettingsXml.Element("debugPreferredPrimaryWeaponTarget") ??
                                                (bool?) CommonSettingsXml.Element("debugPreferredPrimaryWeaponTarget") ?? false;
                Logging.Logging.DebugQuestorManager = (bool?) CharacterSettingsXml.Element("debugQuestorManager") ??
                                                      (bool?) CommonSettingsXml.Element("debugQuestorManager") ?? false;
                Logging.Logging.DebugQuestorEVEOnFrame = (bool?) CharacterSettingsXml.Element("debugQuestorEVEOnFrame") ??
                                                         (bool?) CommonSettingsXml.Element("debugQuestorEVEOnFrame") ?? false;
                Logging.Logging.DebugReloadAll = (bool?) CharacterSettingsXml.Element("debugReloadAll") ??
                                                 (bool?) CommonSettingsXml.Element("debugReloadAll") ?? false;
                Logging.Logging.DebugReloadorChangeAmmo = (bool?) CharacterSettingsXml.Element("debugReloadOrChangeAmmo") ??
                                                          (bool?) CommonSettingsXml.Element("debugReloadOrChangeAmmo") ?? false;
                Logging.Logging.DebugRemoteRepair = (bool?) CharacterSettingsXml.Element("debugRemoteRepair") ??
                                                    (bool?) CommonSettingsXml.Element("debugRemoteRepair") ?? false;
                Logging.Logging.DebugSalvage = (bool?) CharacterSettingsXml.Element("debugSalvage") ??
                                               (bool?) CommonSettingsXml.Element("debugSalvage") ?? false;
                Logging.Logging.DebugScheduler = (bool?) CharacterSettingsXml.Element("debugScheduler") ??
                                                 (bool?) CommonSettingsXml.Element("debugScheduler") ?? false;
                Logging.Logging.DebugSettings = (bool?) CharacterSettingsXml.Element("debugSettings") ??
                                                (bool?) CommonSettingsXml.Element("debugSettings") ?? false;
                Logging.Logging.DebugShipTargetValues = (bool?) CharacterSettingsXml.Element("debugShipTargetValues") ??
                                                        (bool?) CommonSettingsXml.Element("debugShipTargetValues") ?? false;
                Logging.Logging.DebugSkillTraining = (bool?) CharacterSettingsXml.Element("debugSkillTraining") ??
                                                     (bool?) CommonSettingsXml.Element("debugSkillTraining") ?? false;
                Logging.Logging.DebugSpeedMod = (bool?) CharacterSettingsXml.Element("debugSpeedMod") ??
                                                (bool?) CommonSettingsXml.Element("debugSpeedMod") ?? false;
                Logging.Logging.DebugStatistics = (bool?) CharacterSettingsXml.Element("debugStatistics") ??
                                                  (bool?) CommonSettingsXml.Element("debugStatistics") ?? false;
                Logging.Logging.DebugStorylineMissions = (bool?) CharacterSettingsXml.Element("debugStorylineMissions") ??
                                                         (bool?) CommonSettingsXml.Element("debugStorylineMissions") ?? false;
                Logging.Logging.DebugTargetCombatants = (bool?) CharacterSettingsXml.Element("debugTargetCombatants") ??
                                                        (bool?) CommonSettingsXml.Element("debugTargetCombatants") ?? false;
                Logging.Logging.DebugTargetWrecks = (bool?) CharacterSettingsXml.Element("debugTargetWrecks") ??
                                                    (bool?) CommonSettingsXml.Element("debugTargetWrecks") ?? false;
                Logging.Logging.DebugTraveler = (bool?) CharacterSettingsXml.Element("debugTraveler") ??
                                                (bool?) CommonSettingsXml.Element("debugTraveler") ?? false;
                Logging.Logging.DebugTractorBeams = (bool?) CharacterSettingsXml.Element("debugTractorBeams") ??
                                                    (bool?) CommonSettingsXml.Element("debugTractorBeams") ?? false;
                Logging.Logging.DebugUI = (bool?) CharacterSettingsXml.Element("debugUI") ?? (bool?) CommonSettingsXml.Element("debugUI") ?? false;
                Logging.Logging.DebugUndockBookmarks = (bool?) CharacterSettingsXml.Element("debugUndockBookmarks") ??
                                                       (bool?) CommonSettingsXml.Element("debugUndockBookmarks") ?? false;
                Logging.Logging.DebugUnloadLoot = (bool?) CharacterSettingsXml.Element("debugUnloadLoot") ??
                                                  (bool?) CommonSettingsXml.Element("debugUnloadLoot") ?? false;
                Logging.Logging.DebugValuedump = (bool?) CharacterSettingsXml.Element("debugValuedump") ??
                                                 (bool?) CommonSettingsXml.Element("debugValuedump") ?? false;
                Logging.Logging.DebugWalletBalance = (bool?) CharacterSettingsXml.Element("debugWalletBalance") ??
                                                     (bool?) CommonSettingsXml.Element("debugWalletBalance") ?? false;
                Logging.Logging.DebugWeShouldBeInSpaceORInStationAndOutOfSessionChange =
                    (bool?) CharacterSettingsXml.Element("debugWeShouldBeInSpaceORInStationAndOutOfSessionChange") ??
                    (bool?) CommonSettingsXml.Element("debugWeShouldBeInSpaceORInStationAndOutOfSessionChange") ?? false;
                Logging.Logging.DebugWatchForActiveWars = (bool?) CharacterSettingsXml.Element("debugWatchForActiveWars") ??
                                                          (bool?) CommonSettingsXml.Element("debugWatchForActiveWars") ?? false;
                DetailedCurrentTargetHealthLogging = (bool?) CharacterSettingsXml.Element("detailedCurrentTargetHealthLogging") ??
                                                     (bool?) CommonSettingsXml.Element("detailedCurrentTargetHealthLogging") ?? true;

                BuyAmmo = (bool?) CharacterSettingsXml.Element("buyAmmo") ?? (bool?) CommonSettingsXml.Element("buyAmmo") ?? false;
                BuyAmmoStationID = (int?) CharacterSettingsXml.Element("buyAmmoStationID") ?? (int?) CommonSettingsXml.Element("buyAmmoStationID") ?? 60003760;

                DefendWhileTraveling = (bool?) CharacterSettingsXml.Element("defendWhileTraveling") ??
                                       (bool?) CommonSettingsXml.Element("defendWhileTraveling") ?? true;
                TargetSelectionMethod = (string) CharacterSettingsXml.Element("targetSelectionMethod") ??
                                        (string) CommonSettingsXml.Element("targetSelectionMethod") ?? "isdp"; //other choice is "old"
                CharacterToAcceptInvitesFrom = (string) CharacterSettingsXml.Element("characterToAcceptInvitesFrom") ??
                                               (string) CommonSettingsXml.Element("characterToAcceptInvitesFrom") ?? Instance.CharacterName;
                MemoryManagerTrimThreshold = (long?) CharacterSettingsXml.Element("memoryManagerTrimThreshold") ??
                                             (long?) CommonSettingsXml.Element("memoryManagerTrimThreshold") ?? 524288000;
                EveServerName = (string) CharacterSettingsXml.Element("eveServerName") ?? (string) CommonSettingsXml.Element("eveServerName") ?? "Tranquility";
                EnforcedDelayBetweenModuleClicks = (int?) CharacterSettingsXml.Element("enforcedDelayBetweenModuleClicks") ??
                                                   (int?) CommonSettingsXml.Element("enforcedDelayBetweenModuleClicks") ?? 3000;
                AvoidShootingTargetsWithMissilesIfweKNowTheyAreAboutToBeHitWithAPreviousVolley =
                    (bool?) CharacterSettingsXml.Element("avoidShootingTargetsWithMissilesIfweKNowTheyAreAboutToBeHitWithAPreviousVolley") ??
                    (bool?) CommonSettingsXml.Element("AvoidShootingTargetsWithMissilesIfweKNowTheyAreAboutToBeHitWithAPreviousVolley") ?? false;
                //
                // Misc Settings
                //
                CharacterMode = (string) CharacterSettingsXml.Element("characterMode") ??
                                (string) CommonSettingsXml.Element("characterMode") ?? "Combat Missions".ToLower();

                //other option is "salvage"

                //if (!Cache.Instance.DirectEve.Login.AtLogin || DateTime.UtcNow > Time.Instance.QuestorStarted_DateTime.AddMinutes(1))
                //{
                Combat.Combat.Ammo = new List<Ammo>();
                if (Instance.CharacterMode.ToLower() == "dps".ToLower())
                {
                    Instance.CharacterMode = "Combat Missions".ToLower();
                }

                AutoStart = (bool?) CharacterSettingsXml.Element("autoStart") ?? (bool?) CommonSettingsXml.Element("autoStart") ?? false;
                    // auto Start enabled or disabled by default?
                //}

                MaxLineConsole = (int?) CharacterSettingsXml.Element("maxLineConsole") ?? (int?) CommonSettingsXml.Element("maxLineConsole") ?? 1000;
                // maximum console log lines to show in the GUI
                Disable3D = (bool?) CharacterSettingsXml.Element("disable3D") ?? (bool?) CommonSettingsXml.Element("disable3D") ?? false;
                    // Disable3d graphics while in space
                RandomDelay = (int?) CharacterSettingsXml.Element("randomDelay") ?? (int?) CommonSettingsXml.Element("randomDelay") ?? 0;
                MinimumDelay = (int?) CharacterSettingsXml.Element("minimumDelay") ?? (int?) CommonSettingsXml.Element("minimumDelay") ?? 0;

                try
                {
                    UseFittingManager = (bool?) CharacterSettingsXml.Element("UseFittingManager") ??
                                        (bool?) CommonSettingsXml.Element("UseFittingManager") ?? true;
                    EnableStorylines = (bool?) CharacterSettingsXml.Element("enableStorylines") ??
                                       (bool?) CommonSettingsXml.Element("enableStorylines") ?? false;
                    StoryLineBaseBookmark = (string) CharacterSettingsXml.Element("storyLineBaseBookmark") ??
                                            (string) CommonSettingsXml.Element("storyLineBaseBookmark") ?? string.Empty;
                    DeclineStorylinesInsteadofBlacklistingfortheSession =
                        (bool?) CharacterSettingsXml.Element("declineStorylinesInsteadofBlacklistingfortheSession") ??
                        (bool?) CommonSettingsXml.Element("declineStorylinesInsteadofBlacklistingfortheSession") ?? false;
                    UseLocalWatch = (bool?) CharacterSettingsXml.Element("UseLocalWatch") ?? (bool?) CommonSettingsXml.Element("UseLocalWatch") ?? true;
                    WatchForActiveWars = (bool?) CharacterSettingsXml.Element("watchForActiveWars") ??
                                         (bool?) CommonSettingsXml.Element("watchForActiveWars") ?? true;

                    FleetSupportSlave = (bool?) CharacterSettingsXml.Element("fleetSupportSlave") ??
                                        (bool?) CommonSettingsXml.Element("fleetSupportSlave") ?? true;
                    FleetSupportMaster = (bool?) CharacterSettingsXml.Element("fleetSupportMaster") ??
                                         (bool?) CommonSettingsXml.Element("fleetSupportMaster") ?? true;
                    FleetName = (string) CharacterSettingsXml.Element("fleetName") ?? (string) CommonSettingsXml.Element("fleetName") ?? "Fleet1";
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Major Feature Settings: Exception [" + exception + "]");
                }


                //
                //CharacterNamesForMasterToInviteToFleet
                //
                Instance.CharacterNamesForMasterToInviteToFleet.Clear();
                var xmlCharacterNamesForMasterToInviteToFleet = CharacterSettingsXml.Element("characterNamesForMasterToInviteToFleet") ??
                                                                CharacterSettingsXml.Element("characterNamesForMasterToInviteToFleet");
                if (xmlCharacterNamesForMasterToInviteToFleet != null)
                {
                    Logging.Logging.Log("Loading CharacterNames For Master To Invite To Fleet");
                    var i = 1;
                    foreach (var CharacterToInvite in xmlCharacterNamesForMasterToInviteToFleet.Elements("character"))
                    {
                        Instance.CharacterNamesForMasterToInviteToFleet.Add((string) CharacterToInvite);
                        if (Logging.Logging.DebugFleetSupportMaster)
                            Logging.Logging.Log("[" + i + "] CharacterName [" + (string)CharacterToInvite + "]");
                        i++;
                    }
                    if (Instance.FleetSupportMaster)
                        Logging.Logging.Log("        CharacterNamesForMasterToInviteToFleet now has [" + CharacterNamesForMasterToInviteToFleet.Count + "] entries");
                }

                //
                // Agent Standings and Mission Settings
                //
                try
                {
                    //if (Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower())
                    //{
                    MissionSettings.MinAgentBlackListStandings = (float?) CharacterSettingsXml.Element("minAgentBlackListStandings") ??
                                                                 (float?) CommonSettingsXml.Element("minAgentBlackListStandings") ?? (float) 6.0;
                    MissionSettings.MinAgentGreyListStandings = (float?) CharacterSettingsXml.Element("minAgentGreyListStandings") ??
                                                                (float?) CommonSettingsXml.Element("minAgentGreyListStandings") ?? (float) 5.0;
                    MissionSettings.WaitDecline = (bool?) CharacterSettingsXml.Element("waitDecline") ??
                                                  (bool?) CommonSettingsXml.Element("waitDecline") ?? false;

                    var relativeMissionsPath = (string) CharacterSettingsXml.Element("missionsPath") ?? (string) CommonSettingsXml.Element("missionsPath");
                    MissionSettings.MissionsPath = System.IO.Path.Combine(Instance.Path, relativeMissionsPath);
                    Logging.Logging.Log("MissionsPath is: [" + MissionSettings.MissionsPath + "]");

                    MissionSettings.RequireMissionXML = (bool?) CharacterSettingsXml.Element("requireMissionXML") ??
                                                        (bool?) CommonSettingsXml.Element("requireMissionXML") ?? false;
                    MissionSettings.AllowNonStorylineCourierMissionsInLowSec = (bool?) CharacterSettingsXml.Element("LowSecMissions") ??
                                                                               (bool?) CommonSettingsXml.Element("LowSecMissions") ?? false;
                    MissionSettings.MaterialsForWarOreID = (int?) CharacterSettingsXml.Element("MaterialsForWarOreID") ??
                                                           (int?) CommonSettingsXml.Element("MaterialsForWarOreID") ?? 20;
                    MissionSettings.MaterialsForWarOreQty = (int?) CharacterSettingsXml.Element("MaterialsForWarOreQty") ??
                                                            (int?) CommonSettingsXml.Element("MaterialsForWarOreQty") ?? 8000;
                    Combat.Combat.KillSentries = (bool?) CharacterSettingsXml.Element("killSentries") ??
                                                 (bool?) CommonSettingsXml.Element("killSentries") ?? false;
                    //}
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Agent Standings and Mission Settings: Exception [" + exception + "]");
                }


                //
                // Local Watch Settings - if enabled
                //
                try
                {
                    LocalBadStandingPilotsToTolerate = (int?) CharacterSettingsXml.Element("LocalBadStandingPilotsToTolerate") ??
                                                       (int?) CommonSettingsXml.Element("LocalBadStandingPilotsToTolerate") ?? 1;
                    LocalBadStandingLevelToConsiderBad = (double?) CharacterSettingsXml.Element("LocalBadStandingLevelToConsiderBad") ??
                                                         (double?) CommonSettingsXml.Element("LocalBadStandingLevelToConsiderBad") ?? -0.1;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Local watch Settings: Exception [" + exception + "]");
                }

                //
                // Invasion Settings
                //
                try
                {
                    BattleshipInvasionLimit = (int?) CharacterSettingsXml.Element("battleshipInvasionLimit") ??
                                              (int?) CommonSettingsXml.Element("battleshipInvasionLimit") ?? 0;

                    // if this number of BattleShips lands on grid while in a mission we will enter panic
                    BattlecruiserInvasionLimit = (int?) CharacterSettingsXml.Element("battlecruiserInvasionLimit") ??
                                                 (int?) CommonSettingsXml.Element("battlecruiserInvasionLimit") ?? 0;

                    // if this number of BattleCruisers lands on grid while in a mission we will enter panic
                    CruiserInvasionLimit = (int?) CharacterSettingsXml.Element("cruiserInvasionLimit") ??
                                           (int?) CommonSettingsXml.Element("cruiserInvasionLimit") ?? 0;

                    // if this number of Cruisers lands on grid while in a mission we will enter panic
                    FrigateInvasionLimit = (int?) CharacterSettingsXml.Element("frigateInvasionLimit") ??
                                           (int?) CommonSettingsXml.Element("frigateInvasionLimit") ?? 0;

                    // if this number of Frigates lands on grid while in a mission we will enter panic
                    InvasionRandomDelay = (int?) CharacterSettingsXml.Element("invasionRandomDelay") ??
                                          (int?) CommonSettingsXml.Element("invasionRandomDelay") ?? 0; // random relay to stay docked
                    InvasionMinimumDelay = (int?) CharacterSettingsXml.Element("invasionMinimumDelay") ??
                                           (int?) CommonSettingsXml.Element("invasionMinimumDelay") ?? 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Invasion Settings: Exception [" + exception + "]");
                }

                // minimum delay to stay docked

                //
                // Value - Used in calculations
                //
                Statistics.IskPerLP = (double?) CharacterSettingsXml.Element("IskPerLP") ?? (double?) CommonSettingsXml.Element("IskPerLP") ?? 500;
                    //used in value calculations

                //
                // Undock settings
                //
                UndockBookmarkPrefix = (string) CharacterSettingsXml.Element("undockprefix") ??
                                       (string) CommonSettingsXml.Element("undockprefix") ??
                                       (string) CharacterSettingsXml.Element("bookmarkWarpOut") ?? (string) CommonSettingsXml.Element("bookmarkWarpOut") ?? "";

                //
                // Ship Names
                //
                try
                {
                    Combat.Combat.CombatShipName = (string) CharacterSettingsXml.Element("combatShipName") ??
                                                   (string) CommonSettingsXml.Element("combatShipName") ?? "My frigate of doom";
                    SalvageShipName = (string) CharacterSettingsXml.Element("salvageShipName") ??
                                      (string) CommonSettingsXml.Element("salvageShipName") ?? "My Destroyer of salvage";
                    TransportShipName = (string) CharacterSettingsXml.Element("transportShipName") ??
                                        (string) CommonSettingsXml.Element("transportShipName") ?? "My Hauler of transportation";
                    TravelShipName = (string) CharacterSettingsXml.Element("travelShipName") ??
                                     (string) CommonSettingsXml.Element("travelShipName") ?? "My Shuttle of traveling";
                    MiningShipName = (string) CharacterSettingsXml.Element("miningShipName") ??
                                     (string) CommonSettingsXml.Element("miningShipName") ?? "My Exhumer of Destruction";
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Ship Name Settings [" + exception + "]");
                }

                try
                {
                    //
                    // Storage Location for Loot, Ammo, Bookmarks
                    //
                    UseHomebookmark = (bool?) CharacterSettingsXml.Element("UseHomebookmark") ?? (bool?) CommonSettingsXml.Element("UseHomebookmark") ?? false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading UseHomebookmark [" + exception + "]");
                }

                //
                // Storage Location for Loot, Ammo, Bookmarks
                //
                try
                {
                    HomeBookmarkName = (string) CharacterSettingsXml.Element("homeBookmarkName") ??
                                       (string) CommonSettingsXml.Element("homeBookmarkName") ?? "myHomeBookmark";
                    LootHangarTabName = (string) CharacterSettingsXml.Element("lootHangar") ?? (string) CommonSettingsXml.Element("lootHangar");
                    if (string.IsNullOrEmpty(Instance.LootHangarTabName))
                    {
                        Logging.Logging.Log("LootHangar [" + "ItemsHangar" + "]");
                    }
                    else
                    {
                        Logging.Logging.Log("LootHangar [" + Instance.LootHangarTabName + "]");
                    }
                    AmmoHangarTabName = (string) CharacterSettingsXml.Element("ammoHangar") ?? (string) CommonSettingsXml.Element("ammoHangar");
                    if (string.IsNullOrEmpty(Instance.AmmoHangarTabName))
                    {
                        Logging.Logging.Log("AmmoHangar [" + "ItemHangar" + "]");
                    }
                    else
                    {
                        Logging.Logging.Log("AmmoHangar [" + Instance.AmmoHangarTabName + "]");
                    }
                    BookmarkHangar = (string) CharacterSettingsXml.Element("bookmarkHangar") ?? (string) CommonSettingsXml.Element("bookmarkHangar");
                    LootContainerName = (string) CharacterSettingsXml.Element("lootContainer") ?? (string) CommonSettingsXml.Element("lootContainer");
                    if (LootContainerName != null)
                    {
                        LootContainerName = LootContainerName.ToLower();
                    }
                    HighTierLootContainer = (string) CharacterSettingsXml.Element("highValueLootContainer") ??
                                            (string) CommonSettingsXml.Element("highValueLootContainer");
                    if (HighTierLootContainer != null)
                    {
                        HighTierLootContainer = HighTierLootContainer.ToLower();
                    }
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Hangar Settings [" + exception + "]");
                }

                //
                // Loot and Salvage Settings
                //
                try
                {
                    Salvage.LootEverything = (bool?) CharacterSettingsXml.Element("lootEverything") ??
                                             (bool?) CommonSettingsXml.Element("lootEverything") ?? true;
                    Salvage.UseGatesInSalvage = (bool?) CharacterSettingsXml.Element("useGatesInSalvage") ??
                                                (bool?) CommonSettingsXml.Element("useGatesInSalvage") ?? false;

                    // if our mission does not DeSpawn (likely someone in the mission looting our stuff?) use the gates when salvaging to get to our bookmarks
                    Salvage.CreateSalvageBookmarks = (bool?) CharacterSettingsXml.Element("createSalvageBookmarks") ??
                                                     (bool?) CommonSettingsXml.Element("createSalvageBookmarks") ?? false;
                    Salvage.CreateSalvageBookmarksIn = (string) CharacterSettingsXml.Element("createSalvageBookmarksIn") ??
                                                       (string) CommonSettingsXml.Element("createSalvageBookmarksIn") ?? "Player";

                    BookmarkPrefix = (string) CharacterSettingsXml.Element("bookmarkPrefix") ??
                                     (string) CommonSettingsXml.Element("bookmarkPrefix") ?? "Salvage:";
                    SafeSpotBookmarkPrefix = (string) CharacterSettingsXml.Element("safeSpotBookmarkPrefix") ??
                                             (string) CommonSettingsXml.Element("safeSpotBookmarkPrefix") ?? "safespot";
                    BookmarkFolder = (string) CharacterSettingsXml.Element("bookmarkFolder") ??
                                     (string) CommonSettingsXml.Element("bookmarkFolder") ?? "Salvage:";
                    TravelToBookmarkPrefix = (string) CharacterSettingsXml.Element("travelToBookmarkPrefix") ??
                                             (string) CommonSettingsXml.Element("travelToBookmarkPrefix") ?? "MeetHere:";
                    Salvage.MinimumWreckCount = (int?) CharacterSettingsXml.Element("minimumWreckCount") ??
                                                (int?) CommonSettingsXml.Element("minimumWreckCount") ?? 1;
                    Salvage.AfterMissionSalvaging = (bool?) CharacterSettingsXml.Element("afterMissionSalvaging") ??
                                                    (bool?) CommonSettingsXml.Element("afterMissionSalvaging") ?? false;
                    Salvage.FirstSalvageBookmarksInSystem = (bool?) CharacterSettingsXml.Element("FirstSalvageBookmarksInSystem") ??
                                                            (bool?) CommonSettingsXml.Element("FirstSalvageBookmarksInSystem") ?? false;
                    Salvage.SalvageMultipleMissionsinOnePass = (bool?) CharacterSettingsXml.Element("salvageMultpleMissionsinOnePass") ??
                                                               (bool?) CommonSettingsXml.Element("salvageMultpleMissionsinOnePass") ?? false;
                    Salvage.UnloadLootAtStation = (bool?) CharacterSettingsXml.Element("unloadLootAtStation") ??
                                                  (bool?) CommonSettingsXml.Element("unloadLootAtStation") ?? false;
                    Salvage.ReserveCargoCapacity = (int?) CharacterSettingsXml.Element("reserveCargoCapacity") ??
                                                   (int?) CommonSettingsXml.Element("reserveCargoCapacity") ?? 0;
                    Salvage.MaximumWreckTargets = (int?) CharacterSettingsXml.Element("maximumWreckTargets") ??
                                                  (int?) CommonSettingsXml.Element("maximumWreckTargets") ?? 0;
                    Salvage.WreckBlackListSmallWrecks = (bool?) CharacterSettingsXml.Element("WreckBlackListSmallWrecks") ??
                                                        (bool?) CommonSettingsXml.Element("WreckBlackListSmallWrecks") ?? false;
                    Salvage.WreckBlackListMediumWrecks = (bool?) CharacterSettingsXml.Element("WreckBlackListMediumWrecks") ??
                                                         (bool?) CommonSettingsXml.Element("WreckBlackListMediumWrecks") ?? false;
                    Salvage.AgeofBookmarksForSalvageBehavior = (int?) CharacterSettingsXml.Element("ageofBookmarksForSalvageBehavior") ??
                                                               (int?) CommonSettingsXml.Element("ageofBookmarksForSalvageBehavior") ?? 45;
                    Salvage.AgeofSalvageBookmarksToExpire = (int?) CharacterSettingsXml.Element("ageofSalvageBookmarksToExpire") ??
                                                            (int?) CommonSettingsXml.Element("ageofSalvageBookmarksToExpire") ?? 120;
                    Salvage.LootOnlyWhatYouCanWithoutSlowingDownMissionCompletion =
                        (bool?) CharacterSettingsXml.Element("lootOnlyWhatYouCanWithoutSlowingDownMissionCompletion") ??
                        (bool?) CommonSettingsXml.Element("lootOnlyWhatYouCanWithoutSlowingDownMissionCompletion") ?? false;
                    Salvage.TractorBeamMinimumCapacitor = (int?) CharacterSettingsXml.Element("tractorBeamMinimumCapacitor") ??
                                                          (int?) CommonSettingsXml.Element("tractorBeamMinimumCapacitor") ?? 0;
                    Salvage.SalvagerMinimumCapacitor = (int?) CharacterSettingsXml.Element("salvagerMinimumCapacitor") ??
                                                       (int?) CommonSettingsXml.Element("salvagerMinimumCapacitor") ?? 0;
                    Salvage.DoNotDoANYSalvagingOutsideMissionActions = (bool?) CharacterSettingsXml.Element("doNotDoANYSalvagingOutsideMissionActions") ??
                                                                       (bool?) CommonSettingsXml.Element("doNotDoANYSalvagingOutsideMissionActions") ?? false;
                    Salvage.LootItemRequiresTarget = (bool?) CharacterSettingsXml.Element("lootItemRequiresTarget") ??
                                                     (bool?) CommonSettingsXml.Element("lootItemRequiresTarget") ?? false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Loot and Salvage Settings [" + exception + "]");
                }

                //
                // Weapon and targeting Settings
                //
                try
                {
                    MissionSettings.DefaultDamageType =
                        (DamageType)
                            Enum.Parse(typeof(DamageType),
                                (string) CharacterSettingsXml.Element("defaultDamageType") ?? (string) CommonSettingsXml.Element("defaultDamageType") ?? "EM",
                                true);
                    Combat.Combat.WeaponGroupId = (int?) CharacterSettingsXml.Element("weaponGroupId") ?? (int?) CommonSettingsXml.Element("weaponGroupId") ?? 0;
                    Combat.Combat.DontShootFrigatesWithSiegeorAutoCannons = (bool?) CharacterSettingsXml.Element("DontShootFrigatesWithSiegeorAutoCannons") ??
                                                                            (bool?) CommonSettingsXml.Element("DontShootFrigatesWithSiegeorAutoCannons") ??
                                                                            false;
                    Combat.Combat.maxHighValueTargets = (int?) CharacterSettingsXml.Element("maximumHighValueTargets") ??
                                                        (int?) CommonSettingsXml.Element("maximumHighValueTargets") ?? 2;
                    Combat.Combat.maxLowValueTargets = (int?) CharacterSettingsXml.Element("maximumLowValueTargets") ??
                                                       (int?) CommonSettingsXml.Element("maximumLowValueTargets") ?? 2;
                    Combat.Combat.DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage =
                        (int?) CharacterSettingsXml.Element("doNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage") ??
                        (int?) CommonSettingsXml.Element("doNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage") ?? 60;
                    Combat.Combat.DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons =
                        (int?) CharacterSettingsXml.Element("distanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons") ??
                        (int?) CommonSettingsXml.Element("distanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons") ?? 7000;
                        //also requires SpeedFrigatesShouldBeIgnoredByMainWeapons
                    Combat.Combat.SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons =
                        (int?) CharacterSettingsXml.Element("speedNPCFrigatesShouldBeIgnoredByPrimaryWeapons") ??
                        (int?) CommonSettingsXml.Element("speedNPCFrigatesShouldBeIgnoredByPrimaryWeapons") ?? 300;
                        //also requires DistanceFrigatesShouldBeIgnoredByMainWeapons
                    Arm.ArmLoadCapBoosters = (bool?) CharacterSettingsXml.Element("armLoadCapBoosters") ??
                                             (bool?) CommonSettingsXml.Element("armLoadCapBoosters") ?? false;
                    Combat.Combat.SelectAmmoToUseBasedOnShipSize = (bool?) CharacterSettingsXml.Element("selectAmmoToUseBasedOnShipSize") ??
                                                                   (bool?) CommonSettingsXml.Element("selectAmmoToUseBasedOnShipSize") ?? false;

                    Combat.Combat.MinimumTargetValueToConsiderTargetAHighValueTarget =
                        (int?) CharacterSettingsXml.Element("minimumTargetValueToConsiderTargetAHighValueTarget") ??
                        (int?) CommonSettingsXml.Element("minimumTargetValueToConsiderTargetAHighValueTarget") ?? 2;
                    Combat.Combat.MaximumTargetValueToConsiderTargetALowValueTarget =
                        (int?) CharacterSettingsXml.Element("maximumTargetValueToConsiderTargetALowValueTarget") ??
                        (int?) CommonSettingsXml.Element("maximumTargetValueToConsiderTargetALowValueTarget") ?? 1;

                    Combat.Combat.AddDampenersToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addDampenersToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addDampenersToPrimaryWeaponsPriorityTargetList") ?? true;
                    Combat.Combat.AddECMsToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addECMsToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addECMsToPrimaryWeaponsPriorityTargetList") ?? true;
                    Combat.Combat.AddNeutralizersToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addNeutralizersToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addNeutralizersToPrimaryWeaponsPriorityTargetList") ?? true;
                    Combat.Combat.AddTargetPaintersToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addTargetPaintersToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addTargetPaintersToPrimaryWeaponsPriorityTargetList") ?? true;
                    Combat.Combat.AddTrackingDisruptorsToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addTrackingDisruptorsToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addTrackingDisruptorsToPrimaryWeaponsPriorityTargetList") ?? true;
                    Combat.Combat.AddWarpScramblersToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addWarpScramblersToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addWarpScramblersToPrimaryWeaponsPriorityTargetList") ?? true;
                    Combat.Combat.AddWebifiersToPrimaryWeaponsPriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addWebifiersToPrimaryWeaponsPriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addWebifiersToPrimaryWeaponsPriorityTargetList") ?? true;

                    Drones.AddDampenersToDronePriorityTargetList = (bool?) CharacterSettingsXml.Element("addDampenersToDronePriorityTargetList") ??
                                                                   (bool?) CommonSettingsXml.Element("addDampenersToDronePriorityTargetList") ?? true;
                    Drones.AddECMsToDroneTargetList = (bool?) CharacterSettingsXml.Element("addECMsToDroneTargetList") ??
                                                      (bool?) CommonSettingsXml.Element("addECMsToDroneTargetList") ?? true;
                    Drones.AddNeutralizersToDronePriorityTargetList = (bool?) CharacterSettingsXml.Element("addNeutralizersToDronePriorityTargetList") ??
                                                                      (bool?) CommonSettingsXml.Element("addNeutralizersToDronePriorityTargetList") ?? true;
                    Drones.AddTargetPaintersToDronePriorityTargetList = (bool?) CharacterSettingsXml.Element("addTargetPaintersToDronePriorityTargetList") ??
                                                                        (bool?) CommonSettingsXml.Element("addTargetPaintersToDronePriorityTargetList") ?? true;
                    Drones.AddTrackingDisruptorsToDronePriorityTargetList =
                        (bool?) CharacterSettingsXml.Element("addTrackingDisruptorsToDronePriorityTargetList") ??
                        (bool?) CommonSettingsXml.Element("addTrackingDisruptorsToDronePriorityTargetList") ?? true;
                    Drones.AddWarpScramblersToDronePriorityTargetList = (bool?) CharacterSettingsXml.Element("addWarpScramblersToDronePriorityTargetList") ??
                                                                        (bool?) CommonSettingsXml.Element("addWarpScramblersToDronePriorityTargetList") ?? true;
                    Drones.AddWebifiersToDronePriorityTargetList = (bool?) CharacterSettingsXml.Element("addWebifiersToDronePriorityTargetList") ??
                                                                   (bool?) CommonSettingsXml.Element("addWebifiersToDronePriorityTargetList") ?? true;

                    Combat.Combat.ListPriorityTargetsEveryXSeconds = (double?) CharacterSettingsXml.Element("listPriorityTargetsEveryXSeconds") ??
                                                                     (double?) CommonSettingsXml.Element("listPriorityTargetsEveryXSeconds") ?? 900;

                    Combat.Combat.InsideThisRangeIsHardToTrack = (double?) CharacterSettingsXml.Element("insideThisRangeIsHardToTrack") ??
                                                                 (double?) CommonSettingsXml.Element("insideThisRangeIsHardToTrack") ?? 15000;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Weapon and targeting Settings [" + exception + "]");
                }
                // ------------------

                //
                // Script and Booster Settings - TypeIDs for the scripts you would like to use in these modules
                //
                try
                {
                    TrackingDisruptorScript = (int?) CharacterSettingsXml.Element("trackingDisruptorScript") ??
                                              (int?) CommonSettingsXml.Element("trackingDisruptorScript") ?? (int) TypeID.TrackingSpeedDisruptionScript;
                    TrackingComputerScript = (int?) CharacterSettingsXml.Element("trackingComputerScript") ??
                                             (int?) CommonSettingsXml.Element("trackingComputerScript") ?? (int) TypeID.TrackingSpeedScript;
                    TrackingLinkScript = (int?) CharacterSettingsXml.Element("trackingLinkScript") ??
                                         (int?) CommonSettingsXml.Element("trackingLinkScript") ?? (int) TypeID.TrackingSpeedScript;
                    SensorBoosterScript = (int?) CharacterSettingsXml.Element("sensorBoosterScript") ??
                                          (int?) CommonSettingsXml.Element("sensorBoosterScript") ?? (int) TypeID.TargetingRangeScript;
                    SensorDampenerScript = (int?) CharacterSettingsXml.Element("sensorDampenerScript") ??
                                           (int?) CommonSettingsXml.Element("sensorDampenerScript") ?? (int) TypeID.TargetingRangeDampeningScript;
                    AncillaryShieldBoosterScript = (int?) CharacterSettingsXml.Element("ancillaryShieldBoosterScript") ??
                                                   (int?) CommonSettingsXml.Element("ancillaryShieldBoosterScript") ?? (int) TypeID.AncillaryShieldBoosterScript;
                    CapacitorInjectorScript = (int?) CharacterSettingsXml.Element("capacitorInjectorScript") ??
                                              (int?) CommonSettingsXml.Element("capacitorInjectorScript") ?? (int) TypeID.CapacitorInjectorScript;
                    NumberOfCapBoostersToLoad = (int?) CharacterSettingsXml.Element("capacitorInjectorToLoad") ??
                                                (int?) CommonSettingsXml.Element("capacitorInjectorToLoad") ??
                                                (int?) CharacterSettingsXml.Element("capBoosterToLoad") ??
                                                (int?) CommonSettingsXml.Element("capBoosterToLoad") ?? 15;

                    OverloadWeapons = (bool?) CharacterSettingsXml.Element("overloadWeapons") ?? (bool?) CommonSettingsXml.Element("overloadWeapons") ?? false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Script and Booster Settings [" + exception + "]");
                }

                //
                // Speed and Movement Settings
                //
                try
                {
                    NavigateOnGrid.AvoidBumpingThingsBool = (bool?) CharacterSettingsXml.Element("avoidBumpingThings") ??
                                                            (bool?) CommonSettingsXml.Element("avoidBumpingThings") ?? true;
                    NavigateOnGrid.SpeedTank = (bool?) CharacterSettingsXml.Element("speedTank") ?? (bool?) CommonSettingsXml.Element("speedTank") ?? false;
                    NavigateOnGrid.OrbitDistance = (int?) CharacterSettingsXml.Element("orbitDistance") ??
                                                   (int?) CommonSettingsXml.Element("orbitDistance") ?? 0;
                    NavigateOnGrid.OrbitStructure = (bool?) CharacterSettingsXml.Element("orbitStructure") ??
                                                    (bool?) CommonSettingsXml.Element("orbitStructure") ?? false;
                    NavigateOnGrid.OptimalRange = (int?) CharacterSettingsXml.Element("optimalRange") ?? (int?) CommonSettingsXml.Element("optimalRange") ?? 0;
                    Combat.Combat.NosDistance = (int?) CharacterSettingsXml.Element("NosDistance") ?? (int?) CommonSettingsXml.Element("NosDistance") ?? 38000;
                    Combat.Combat.RemoteRepairDistance = (int?) CharacterSettingsXml.Element("remoteRepairDistance") ??
                                                         (int?) CommonSettingsXml.Element("remoteRepairDistance") ?? 2000;
                    Defense.MinimumPropulsionModuleDistance = (int?) CharacterSettingsXml.Element("minimumPropulsionModuleDistance") ??
                                                              (int?) CommonSettingsXml.Element("minimumPropulsionModuleDistance") ?? 5000;
                    Defense.MinimumPropulsionModuleCapacitor = (int?) CharacterSettingsXml.Element("minimumPropulsionModuleCapacitor") ??
                                                               (int?) CommonSettingsXml.Element("minimumPropulsionModuleCapacitor") ?? 0;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Speed and Movement Settings [" + exception + "]");
                }

                //
                // Tanking Settings
                //
                try
                {
                    Defense.ActivateRepairModulesAtThisPerc = (int?) CharacterSettingsXml.Element("activateRepairModules") ??
                                                              (int?) CommonSettingsXml.Element("activateRepairModules") ?? 65;
                    Defense.DeactivateRepairModulesAtThisPerc = (int?) CharacterSettingsXml.Element("deactivateRepairModules") ??
                                                                (int?) CommonSettingsXml.Element("deactivateRepairModules") ?? 95;
                    Defense.InjectCapPerc = (int?) CharacterSettingsXml.Element("injectcapperc") ?? (int?) CommonSettingsXml.Element("injectcapperc") ?? 60;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Tanking Settings [" + exception + "]");
                }

                //
                // Panic Settings
                //
                try
                {
                    Panic.MinimumShieldPct = (int?) CharacterSettingsXml.Element("minimumShieldPct") ??
                                             (int?) CommonSettingsXml.Element("minimumShieldPct") ?? 100;
                    Panic.MinimumArmorPct = (int?) CharacterSettingsXml.Element("minimumArmorPct") ?? (int?) CommonSettingsXml.Element("minimumArmorPct") ?? 100;
                    Panic.MinimumCapacitorPct = (int?) CharacterSettingsXml.Element("minimumCapacitorPct") ??
                                                (int?) CommonSettingsXml.Element("minimumCapacitorPct") ?? 50;
                    Panic.SafeShieldPct = (int?) CharacterSettingsXml.Element("safeShieldPct") ?? (int?) CommonSettingsXml.Element("safeShieldPct") ?? 90;
                    Panic.SafeArmorPct = (int?) CharacterSettingsXml.Element("safeArmorPct") ?? (int?) CommonSettingsXml.Element("safeArmorPct") ?? 90;
                    Panic.SafeCapacitorPct = (int?) CharacterSettingsXml.Element("safeCapacitorPct") ??
                                             (int?) CommonSettingsXml.Element("safeCapacitorPct") ?? 80;
                    Panic.UseStationRepair = (bool?) CharacterSettingsXml.Element("useStationRepair") ??
                                             (bool?) CommonSettingsXml.Element("useStationRepair") ?? true;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Panic Settings [" + exception + "]");
                }
                // ------------------


                //
                // Drone Settings
                //
                try
                {
                    Drones.UseDrones = (bool?) CharacterSettingsXml.Element("useDrones") ?? (bool?) CommonSettingsXml.Element("useDrones") ?? true;
                    Drones.DroneTypeID = (int?) CharacterSettingsXml.Element("droneTypeId") ?? (int?) CommonSettingsXml.Element("droneTypeId") ?? 0;
                    Drones.BuyAmmoDroneAmmount = (int?) CharacterSettingsXml.Element("buyAmmoDroneAmount") ??
                                                 (int?) CommonSettingsXml.Element("buyAmmoDroneAmount") ?? 200;
                    LootWhileSpeedTanking = (bool?) CharacterSettingsXml.Element("lootWhileSpeedTanking") ??
                                            (bool?) CommonSettingsXml.Element("lootWhileSpeedTanking") ?? false;
                    Drones.DroneControlRange = (int?) CharacterSettingsXml.Element("droneControlRange") ??
                                               (int?) CommonSettingsXml.Element("droneControlRange") ?? 0;
                    Drones.DronesDontNeedTargetsBecauseWehaveThemSetOnAggressive =
                        (bool?) CharacterSettingsXml.Element("dronesDontNeedTargetsBecauseWehaveThemSetOnAggressive") ??
                        (bool?) CommonSettingsXml.Element("dronesDontNeedTargetsBecauseWehaveThemSetOnAggressive") ?? true;
                    Drones.DroneMinimumShieldPct = (int?) CharacterSettingsXml.Element("droneMinimumShieldPct") ??
                                                   (int?) CommonSettingsXml.Element("droneMinimumShieldPct") ?? 50;
                    Drones.DroneMinimumArmorPct = (int?) CharacterSettingsXml.Element("droneMinimumArmorPct") ??
                                                  (int?) CommonSettingsXml.Element("droneMinimumArmorPct") ?? 50;
                    Drones.DroneMinimumCapacitorPct = (int?) CharacterSettingsXml.Element("droneMinimumCapacitorPct") ??
                                                      (int?) CommonSettingsXml.Element("droneMinimumCapacitorPct") ?? 0;
                    Drones.DroneRecallShieldPct = (int?) CharacterSettingsXml.Element("droneRecallShieldPct") ??
                                                  (int?) CommonSettingsXml.Element("droneRecallShieldPct") ?? 0;
                    Drones.DroneRecallArmorPct = (int?) CharacterSettingsXml.Element("droneRecallArmorPct") ??
                                                 (int?) CommonSettingsXml.Element("droneRecallArmorPct") ?? 0;
                    Drones.DroneRecallCapacitorPct = (int?) CharacterSettingsXml.Element("droneRecallCapacitorPct") ??
                                                     (int?) CommonSettingsXml.Element("droneRecallCapacitorPct") ?? 0;
                    Drones.LongRangeDroneRecallShieldPct = (int?) CharacterSettingsXml.Element("longRangeDroneRecallShieldPct") ??
                                                           (int?) CommonSettingsXml.Element("longRangeDroneRecallShieldPct") ?? 0;
                    Drones.LongRangeDroneRecallArmorPct = (int?) CharacterSettingsXml.Element("longRangeDroneRecallArmorPct") ??
                                                          (int?) CommonSettingsXml.Element("longRangeDroneRecallArmorPct") ?? 0;
                    Drones.LongRangeDroneRecallCapacitorPct = (int?) CharacterSettingsXml.Element("longRangeDroneRecallCapacitorPct") ??
                                                              (int?) CommonSettingsXml.Element("longRangeDroneRecallCapacitorPct") ?? 0;
                    Drones.DronesKillHighValueTargets = (bool?) CharacterSettingsXml.Element("dronesKillHighValueTargets") ??
                                                        (bool?) CommonSettingsXml.Element("dronesKillHighValueTargets") ?? false;
                    Drones.BelowThisHealthLevelRemoveFromDroneBay = (int?) CharacterSettingsXml.Element("belowThisHealthLevelRemoveFromDroneBay") ??
                                                                    (int?) CommonSettingsXml.Element("belowThisHealthLevelRemoveFromDroneBay") ?? 150;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Drone Settings [" + exception + "]");
                }


                // ------------------ fine until here
                //
                // Ammo settings
                //
                try
                {
                    Combat.Combat.Ammo = new List<Ammo>();
                    var ammoTypes = CharacterSettingsXml.Element("ammoTypes") ?? CommonSettingsXml.Element("ammoTypes");

                    if (ammoTypes != null)
                    {
                        foreach (var ammo in ammoTypes.Elements("ammoType"))
                        {
                            Combat.Combat.Ammo.Add(new Ammo(ammo));
                        }
                    }

                    Combat.Combat.MinimumAmmoCharges = (int?) CharacterSettingsXml.Element("minimumAmmoCharges") ??
                                                       (int?) CommonSettingsXml.Element("minimumAmmoCharges") ?? 2;
                    if (Combat.Combat.MinimumAmmoCharges < 2)
                        Combat.Combat.MinimumAmmoCharges = 2;
                            //do not allow MinimumAmmoCharges to be set lower than 1. We always want to reload before the weapon is empty!
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Ammo Settings [" + exception + "]");
                }


                // ------------------ fine until here without the event listener


                //
                // List of Agents we should use
                //
                try
                {
                    //if (Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower())
                    //{
                    MissionSettings.ListOfAgents = new List<AgentsList>();
                    var agentList = CharacterSettingsXml.Element("agentsList") ?? CommonSettingsXml.Element("agentsList");

                    if (agentList != null)
                    {
                        if (agentList.HasElements)
                        {
                            foreach (var agent in agentList.Elements("agentList"))
                            {
                                MissionSettings.ListOfAgents.Add(new AgentsList(agent));
                            }
                        }
                        else
                        {
                            Logging.Logging.Log("agentList exists in your characters config but no agents were listed.");
                        }
                    }
                    else
                    {
                        Logging.Logging.Log("Error! No Agents List specified.");
                    }

                    //}
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Agent Settings [" + exception + "]");
                }

//				return;

                //
                // Loading Mission Blacklists/GreyLists
                //
                try
                {
                    MissionSettings.LoadMissionBlackList(CharacterSettingsXml, CommonSettingsXml);
                    MissionSettings.LoadMissionGreyList(CharacterSettingsXml, CommonSettingsXml);
                    MissionSettings.LoadFactionBlacklist(CharacterSettingsXml, CommonSettingsXml);
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Mission Blacklists/GreyLists [" + exception + "]");
                }

                //
                // agent standing requirements
                //
                try
                {
                    AgentInteraction.StandingsNeededToAccessLevel1Agent = (float?) CharacterSettingsXml.Element("standingsNeededToAccessLevel1Agent") ??
                                                                          (float?) CommonSettingsXml.Element("standingsNeededToAccessLevel1Agent") ?? -11;
                    AgentInteraction.StandingsNeededToAccessLevel2Agent = (float?) CharacterSettingsXml.Element("standingsNeededToAccessLevel2Agent") ??
                                                                          (float?) CommonSettingsXml.Element("standingsNeededToAccessLevel2Agent") ?? 1;
                    AgentInteraction.StandingsNeededToAccessLevel3Agent = (float?) CharacterSettingsXml.Element("standingsNeededToAccessLevel3Agent") ??
                                                                          (float?) CommonSettingsXml.Element("standingsNeededToAccessLevel3Agent") ?? 3;
                    AgentInteraction.StandingsNeededToAccessLevel4Agent = (float?) CharacterSettingsXml.Element("standingsNeededToAccessLevel4Agent") ??
                                                                          (float?) CommonSettingsXml.Element("standingsNeededToAccessLevel4Agent") ?? 5;
                    AgentInteraction.StandingsNeededToAccessLevel5Agent = (float?) CharacterSettingsXml.Element("standingsNeededToAccessLevel5Agent") ??
                                                                          (float?) CommonSettingsXml.Element("standingsNeededToAccessLevel5Agent") ?? 7;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading AgentStandings requirements [" + exception + "]");
                }

                // at what memory usage do we need to restart this session?
                //
                EVEProcessMemoryCeiling = (int?) CharacterSettingsXml.Element("EVEProcessMemoryCeiling") ??
                                          (int?) CommonSettingsXml.Element("EVEProcessMemoryCeiling") ?? 2048;

                //the above setting can be set to any script or commands available on the system. make sure you test it from a command prompt while in your .net programs directory

                WalletBalanceChangeLogOffDelay = (int?) CharacterSettingsXml.Element("walletbalancechangelogoffdelay") ??
                                                 (int?) CommonSettingsXml.Element("walletbalancechangelogoffdelay") ?? 30;
                WalletBalanceChangeLogOffDelayLogoffOrExit = (string) CharacterSettingsXml.Element("walletbalancechangelogoffdelayLogofforExit") ??
                                                             (string) CommonSettingsXml.Element("walletbalancechangelogoffdelayLogofforExit") ?? "exit";

                //
                // Enable / Disable the different types of logging that are available
                //
                Logging.Logging.SaveLogRedacted = (bool?) CharacterSettingsXml.Element("saveLogRedacted") ??
                                                  (bool?) CommonSettingsXml.Element("saveLogRedacted") ?? true; // save the console log redacted to file
                Statistics.DroneStatsLog = (bool?) CharacterSettingsXml.Element("DroneStatsLog") ?? (bool?) CommonSettingsXml.Element("DroneStatsLog") ?? true;
                Statistics.WreckLootStatistics = (bool?) CharacterSettingsXml.Element("WreckLootStatistics") ??
                                                 (bool?) CommonSettingsXml.Element("WreckLootStatistics") ?? true;
                Statistics.MissionStats3Log = (bool?) CharacterSettingsXml.Element("MissionStats3Log") ??
                                              (bool?) CommonSettingsXml.Element("MissionStats3Log") ?? true;
                Statistics.MissionDungeonIdLog = (bool?) CharacterSettingsXml.Element("MissionDungeonIdLog") ??
                                                 (bool?) CommonSettingsXml.Element("MissionDungeonIdLog") ?? true;
                Statistics.PocketStatistics = (bool?) CharacterSettingsXml.Element("PocketStatistics") ??
                                              (bool?) CommonSettingsXml.Element("PocketStatistics") ?? true;
                Statistics.PocketStatsUseIndividualFilesPerPocket = (bool?) CharacterSettingsXml.Element("PocketStatsUseIndividualFilesPerPocket") ??
                                                                    (bool?) CommonSettingsXml.Element("PocketStatsUseIndividualFilesPerPocket") ?? true;
                Statistics.PocketObjectStatisticsLog = (bool?) CharacterSettingsXml.Element("PocketObjectStatisticsLog") ??
                                                       (bool?) CommonSettingsXml.Element("PocketObjectStatisticsLog") ?? true;
                Statistics.WindowStatsLog = (bool?) CharacterSettingsXml.Element("WindowStatsLog") ??
                                            (bool?) CommonSettingsXml.Element("WindowStatsLog") ?? true;

                //
                // number of days of console logs to keep (anything older will be deleted on startup)
                //
                Logging.Logging.ConsoleLogDaysOfLogsToKeep = (int?) CharacterSettingsXml.Element("consoleLogDaysOfLogsToKeep") ??
                                                             (int?) CommonSettingsXml.Element("consoleLogDaysOfLogsToKeep") ?? 14;
                Cache.Instance.IsLoadingSettings = false;
                Logging.Logging.Log("Done reading settings from xml async");
                //Logging.tryToLogToFile = (bool?)CharacterSettingsXml.Element("tryToLogToFile") ?? (bool?)CommonSettingsXml.Element("tryToLogToFile") ?? true;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("ReadSettingsFromXML: Exception [" + exception + "]");
                Cache.Instance.IsLoadingSettings = false;
            }
        }

        public void LoadSettings(bool forcereload = false)
        {
            try
            {
                if (DateTime.UtcNow < Time.Instance.NextLoadSettings)
                {
                    return;
                }

                Time.Instance.NextLoadSettings = DateTime.UtcNow.AddSeconds(1);

                try
                {
                    if (Logging.Logging.MyCharacterName != null)
                    {
                        Instance.CharacterName = Logging.Logging.MyCharacterName;
                        //Logging.Log("Settings", "CharacterName was pulled from the Scheduler: [" + Settings.Instance.CharacterName + "]", Logging.White);
                    }
                    else
                    {
                        Instance.CharacterName = Cache.Instance.DirectEve.Me.Name;
                        //Logging.Log("Settings", "CharacterName was pulled from your live EVE session: [" + Settings.Instance.CharacterName + "]", Logging.White);
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception trying to find CharacterName [" + ex + "]");
                    Instance.CharacterName = "AtLoginScreenNoCharactersLoggedInYet";
                }

                Logging.Logging.CharacterSettingsPath = System.IO.Path.Combine(Instance.Path, Logging.Logging.FilterPath(Instance.CharacterName) + ".xml");
                //Settings.Instance.CommonSettingsPath = System.IO.Path.Combine(Settings.Instance.Path, Settings.Instance.CommonSettingsFileName);

                if (Logging.Logging.CharacterSettingsPath == System.IO.Path.Combine(Instance.Path, ".xml"))
                {
                    if (DateTime.UtcNow > Time.Instance.LastSessionChange.AddSeconds(30))
                    {
                        Cleanup.ReasonToStopQuestor =
                            "CharacterName not defined! - Are we still logged in? Did we lose connection to eve? Questor should be restarting here.";
                        Logging.Logging.Log("CharacterName not defined! - Are we still logged in? Did we lose connection to eve? Questor should be restarting here.");
                        Instance.CharacterName = "NoCharactersLoggedInAnymore";
                        Time.EnteredCloseQuestor_DateTime = DateTime.UtcNow;
                        Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                        _States.CurrentQuestorState = QuestorState.CloseQuestor;
                        Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor);
                        return;
                    }

                    Logging.Logging.Log("CharacterName not defined! - Are we logged in yet? Did we lose connection to eve?");
                    Instance.CharacterName = "AtLoginScreenNoCharactersLoggedInYet";
                    //Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                }

                try
                {
                    var reloadSettings = false;
                    if (File.Exists(Logging.Logging.CharacterSettingsPath))
                    {
                        reloadSettings = _lastModifiedDateOfMySettingsFile != File.GetLastWriteTime(Logging.Logging.CharacterSettingsPath);
                        if (!reloadSettings)
                        {
                            if (File.Exists(Instance.CommonSettingsPath))
                                reloadSettings = _lastModifiedDateOfMyCommonSettingsFile != File.GetLastWriteTime(CommonSettingsPath);
                        }
                        if (!reloadSettings && forcereload) reloadSettings = true;

                        if (!reloadSettings)
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                }

                if (!File.Exists(Logging.Logging.CharacterSettingsPath) && !Instance.DefaultSettingsLoaded)
                    //if the settings file does not exist initialize these values. Should we not halt when missing the settings XML?
                {
                    _States.CurrentQuestorState = QuestorState.Error;
                    _States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Error;
                    Logging.Logging.Log("ERROR: if (!File.Exists(Logging.CharacterSettingsPath) && !Settings.Instance.DefaultSettingsLoaded)");
                }
                else //if the settings file exists - load the characters settings XML
                {
                    Instance.CharacterXMLExists = true;

                    using (var reader = new XmlTextReader(Logging.Logging.CharacterSettingsPath))
                    {
                        reader.EntityHandling = EntityHandling.ExpandEntities;
                        CharacterSettingsXml = XDocument.Load(reader).Root;
                    }

                    if (CharacterSettingsXml == null)
                    {
                        Logging.Logging.Log("unable to find [" + Logging.Logging.CharacterSettingsPath + "] FATAL ERROR - use the provided settings.xml to create that file.");
                    }
                    else
                    {
                        if (File.Exists(Logging.Logging.CharacterSettingsPath))
                            _lastModifiedDateOfMySettingsFile = File.GetLastWriteTime(Logging.Logging.CharacterSettingsPath);
                        if (File.Exists(Instance.CommonSettingsPath)) _lastModifiedDateOfMyCommonSettingsFile = File.GetLastWriteTime(CommonSettingsPath);
//						new Thread(() => {ReadSettingsFromXML();}).Start();
                        ReadSettingsFromXML();
                    }
                }

                Statistics.DroneStatsLogPath = Logging.Logging.Logpath;
                Statistics.DroneStatslogFile = System.IO.Path.Combine(Statistics.DroneStatsLogPath, Logging.Logging.characterNameForLogs + ".DroneStats.log");

                Statistics.WindowStatsLogPath = System.IO.Path.Combine(Logging.Logging.Logpath, "WindowStats\\");
                Statistics.WindowStatslogFile = System.IO.Path.Combine(Statistics.WindowStatsLogPath,
                    Logging.Logging.characterNameForLogs + ".WindowStats-DayOfYear[" + DateTime.UtcNow.DayOfYear + "].log");
                Statistics.WreckLootStatisticsPath = Logging.Logging.Logpath;
                Statistics.WreckLootStatisticsFile = System.IO.Path.Combine(Statistics.WreckLootStatisticsPath,
                    Logging.Logging.characterNameForLogs + ".WreckLootStatisticsDump.log");

                Statistics.MissionStats3LogPath = System.IO.Path.Combine(Logging.Logging.Logpath, "MissionStats\\");
                Statistics.MissionStats3LogFile = System.IO.Path.Combine(Statistics.MissionStats3LogPath,
                    Logging.Logging.characterNameForLogs + ".CustomDatedStatistics.csv");
                Statistics.MissionDungeonIdLogPath = System.IO.Path.Combine(Logging.Logging.Logpath, "MissionStats\\");
                Statistics.MissionDungeonIdLogFile = System.IO.Path.Combine(Statistics.MissionDungeonIdLogPath,
                    Logging.Logging.characterNameForLogs + "Mission-DungeonId-list.csv");
                Statistics.PocketStatisticsPath = System.IO.Path.Combine(Logging.Logging.Logpath, "PocketStats\\");
                Statistics.PocketStatisticsFile = System.IO.Path.Combine(Statistics.PocketStatisticsPath,
                    Logging.Logging.characterNameForLogs + "pocketstats-combined.csv");
                Statistics.PocketObjectStatisticsPath = System.IO.Path.Combine(Logging.Logging.Logpath, "PocketObjectStats\\");
                Statistics.PocketObjectStatisticsFile = System.IO.Path.Combine(Statistics.PocketObjectStatisticsPath,
                    Logging.Logging.characterNameForLogs + "PocketObjectStats-combined.csv");
                Statistics.MissionDetailsHtmlPath = System.IO.Path.Combine(Logging.Logging.Logpath, "MissionDetailsHTML\\");

                try
                {
                    Directory.CreateDirectory(Logging.Logging.Logpath);
                    Directory.CreateDirectory(Logging.Logging.SessionDataCachePath);
                    Directory.CreateDirectory(Logging.Logging.ConsoleLogPath);
                    Directory.CreateDirectory(Statistics.DroneStatsLogPath);
                    Directory.CreateDirectory(Statistics.WreckLootStatisticsPath);
                    Directory.CreateDirectory(Statistics.MissionStats3LogPath);
                    Directory.CreateDirectory(Statistics.MissionDungeonIdLogPath);
                    Directory.CreateDirectory(Statistics.PocketStatisticsPath);
                    Directory.CreateDirectory(Statistics.PocketObjectStatisticsPath);
                    Directory.CreateDirectory(Statistics.WindowStatsLogPath);
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Problem creating directories for logs [" + exception + "]");
                }
                //create all the logging directories even if they are not configured to be used - we can adjust this later if it really bugs people to have some potentially empty directories.

                if (!Instance.DefaultSettingsLoaded)
                {
                    if (SettingsLoaded != null)
                    {
                        SettingsLoadedICount++;
                        if (Instance.CommonXMLExists)
                            Logging.Logging.Log("[" + SettingsLoadedICount + "] Done Loading Settings from [" + Instance.CommonSettingsPath + "] and");
                        Logging.Logging.Log("[" + SettingsLoadedICount + "] Done Loading Settings from [" + Logging.Logging.CharacterSettingsPath + "]");

                        //SettingsLoaded(this, new EventArgs());
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Problem creating directories for logs [" + ex + "]");
            }
        }

        public int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }
    }
}