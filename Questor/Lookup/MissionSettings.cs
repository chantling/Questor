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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.Caching;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.States;

namespace Questor.Modules.Lookup
{
    public static class MissionSettings
    {
        private static List<FactionFitting> _listofFactionFittings;


        private static List<MissionFitting> _listOfMissionFittings;

        private static string _defaultFittingName;

        private static FactionFitting _defaultFitting;
        public static int NumberOfTriesToDeleteBookmarks = 3;
        public static int StopSessionAfterMissionNumber = int.MaxValue;
        public static int GreyListedMissionsDeclined = 0;
        public static string LastGreylistMissionDeclined = string.Empty;
        public static int BlackListedMissionsDeclined = 0;
        public static string LastBlacklistMissionDeclined = string.Empty;
        public static double? PocketOrbitDistance = null;
        public static double? PocketOptimalRange = null;
        public static bool? MissionDronesKillHighValueTargets = null;
        public static double? MissionOrbitDistance = null;
        public static double? MissionOptimalRange = null;

        public static bool? FactionDronesKillHighValueTargets = null;
        public static double? FactionOrbitDistance = null;
        public static double? FactionOptimalRange = null;

        //public XDocument InvTypes;
        public static XDocument UnloadLootTheseItemsAreLootItems;

        private static string _fittingToLoad; //name of the final fitting we want to use

        private static string _factionFittingNameForThisMissionsFaction;
        //public static List<Ammo> FactionAmmoTypesToLoad { get; set; }

        public static int MissionsThisSession = 0;

        public static bool loadedAmmo = false;

        static MissionSettings()
        {
            ChangeMissionShipFittings = false;
            DefaultFittingName = null;
            FactionBlacklist = new List<string>();
            ListOfAgents = new List<AgentsList>();
            ListofFactionFittings = new List<FactionFitting>();
            ListOfMissionFittings = new List<MissionFitting>();
            _listOfMissionFittings = new List<MissionFitting>();
            AmmoTypesToLoad = new Dictionary<Ammo, DateTime>();
            MissionBlacklist = new List<string>();
            MissionGreylist = new List<string>();
            MissionItems = new List<string>();
            MissionUseDrones = null;
            UseMissionShip = false;
            DamageTypesForThisMission = new Dictionary<DamageType, DateTime>();
            DamageTypesInMissionXML = new List<DamageType>();
        }

        //
        // Fitting Settings - if enabled
        //
        public static List<FactionFitting> ListofFactionFittings
        {
            get
            {
                try
                {
                    if (Settings.Instance.UseFittingManager) //no need to look for or load these settings if FittingManager is disabled
                    {
                        if (_listofFactionFittings != null && _listofFactionFittings.Any())
                        {
                            return _listofFactionFittings;
                        }

                        //
                        // if _listofFactionFittings is empty make sure it is NOT null!
                        //
                        _listofFactionFittings = new List<FactionFitting>();

                        var factionFittings = Settings.Instance.CharacterSettingsXml.Element("factionFittings") ??
                                              Settings.Instance.CharacterSettingsXml.Element("factionfittings") ??
                                              Settings.Instance.CommonSettingsXml.Element("factionFittings") ??
                                              Settings.Instance.CommonSettingsXml.Element("factionfittings");

                        if (factionFittings != null)
                        {
                            var factionFittingXmlElementName = "";
                            if (factionFittings.Elements("factionFitting").Any())
                            {
                                factionFittingXmlElementName = "factionFitting";
                            }
                            else
                            {
                                factionFittingXmlElementName = "factionfitting";
                            }

                            var i = 0;
                            foreach (var factionfitting in factionFittings.Elements(factionFittingXmlElementName))
                            {
                                i++;
                                _listofFactionFittings.Add(new FactionFitting(factionfitting));
                                if (Logging.Logging.DebugFittingMgr)
                                    Logging.Logging.Log("[" + i + "] Faction Fitting [" + factionfitting + "]");
                            }

                            return _listofFactionFittings;
                        }

                        Settings.Instance.UseFittingManager = false;
                        if (Logging.Logging.DebugFittingMgr)
                            Logging.Logging.Log("No faction fittings specified.  Fitting manager will not be used.");
                        return new List<FactionFitting>();
                    }

                    return new List<FactionFitting>();
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Faction Fittings Settings [" + exception + "]");
                    return new List<FactionFitting>();
                }
            }

            private set { _listofFactionFittings = value; }
        }

        public static List<AgentsList> ListOfAgents { get; set; }

        public static List<MissionFitting> ListOfMissionFittings
        {
            get
            {
                //
                // Load List of Mission Fittings available for Fitting based on the name of the mission
                //
                try
                {
                    var xmlElementMissionFittingsSection = Settings.Instance.CharacterSettingsXml.Element("missionfittings") ??
                                                           Settings.Instance.CommonSettingsXml.Element("missionfittings");
                    if (Settings.Instance.UseFittingManager) //no need to look for or load these settings if FittingManager is disabled
                    {
                        if (xmlElementMissionFittingsSection != null)
                        {
                            if (Logging.Logging.DebugFittingMgr) Logging.Logging.Log("Loading Mission Fittings");
                            var i = 0;
                            foreach (var missionfitting in xmlElementMissionFittingsSection.Elements("missionfitting"))
                            {
                                i++;
                                _listOfMissionFittings.Add(new MissionFitting(missionfitting));
                                if (Logging.Logging.DebugFittingMgr)
                                    Logging.Logging.Log("[" + i + "] Mission Fitting [" + missionfitting + "]");
                            }

                            if (Logging.Logging.DebugFittingMgr)
                                Logging.Logging.Log("        Mission Fittings now has [" + _listOfMissionFittings.Count + "] entries");
                            return _listOfMissionFittings;
                        }

                        return new List<MissionFitting>();
                    }

                    return new List<MissionFitting>();
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Error Loading Mission Fittings Settings [" + exception + "]");
                    return new List<MissionFitting>();
                }
            }

            private set { _listOfMissionFittings = value; }
        }

        public static string DefaultFittingName
        {
            get
            {
                if (ListofFactionFittings != null && ListofFactionFittings.Any())
                {
                    _defaultFitting = ListofFactionFittings.Find(m => m.FactionName.ToLower() == "default");
                    _defaultFittingName = _defaultFitting.FittingName;
                    return _defaultFittingName;
                }

                Logging.Logging.Log("DefaultFittingName - no fitting found for the faction named [ default ], assuming a fitting name of [ default ] exists");
                return "default";
            }
            set { _defaultFittingName = value; }
        }

        public static DirectAgentMission Mission { get; set; }
        public static DirectAgentMission FirstAgentMission { get; set; }
        public static IEnumerable<DirectAgentMission> myAgentMissionList { get; set; }
        public static bool MissionXMLIsAvailable { get; set; }
        public static string MissionXmlPath { get; set; }
        public static string MissionName { get; set; }
        public static float MinAgentBlackListStandings { get; set; }
        public static float MinAgentGreyListStandings { get; set; }
        public static string MissionsPath { get; set; }
        public static bool RequireMissionXML { get; set; }
        public static bool AllowNonStorylineCourierMissionsInLowSec { get; set; }
        public static bool WaitDecline { get; set; }
        public static int MaterialsForWarOreID { get; set; }
        public static int MaterialsForWarOreQty { get; set; }

        //
        // Pocket Specific Settings (we should make these ALL settable via the mission XML inside of pockets
        //

        public static int? PocketDroneTypeID { get; set; }
        public static bool? PocketKillSentries { get; set; }
        public static bool? PocketUseDrones { get; set; }
        public static int? PocketActivateRepairModulesAtThisPerc { get; set; }

        //
        // Mission Specific Settings (we should make these ALL settable via the mission XML outside of pockets (just inside the mission tag)
        //
        public static int? MissionDroneTypeID { get; set; }
        public static bool? MissionKillSentries { get; set; }
        public static bool? MissionUseDrones { get; set; }
        public static int? MissionActivateRepairModulesAtThisPerc { get; set; }
        public static int MissionWeaponGroupId { get; set; }
        public static string MoveMissionItems { get; set; }
        public static int MoveMissionItemsQuantity { get; set; }
        public static string MoveOptionalMissionItems { get; set; }
        public static int MoveOptionalMissionItemQuantity { get; set; }
        public static double MissionWarpAtDistanceRange { get; set; } //in km

        //
        // Faction Specific Settings (we should make these ALL settable via some mechanic that I have not come up with yet
        //
        public static int? FactionDroneTypeID { get; set; }
        public static int? FactionActivateRepairModulesAtThisPerc { get; set; }


        //
        // Mission Blacklist / Greylist Settings
        //
        public static List<string> MissionBlacklist { get; private set; }
        public static List<string> MissionGreylist { get; private set; }
        public static List<string> FactionBlacklist { get; private set; }
        //public static XDocument InvIgnore;

        /// <summary>
        ///     Returns the mission objectives from
        /// </summary>
        public static List<string> MissionItems { get; private set; }

        public static string FittingToLoad
        {
            get
            {
                if (MissionFittingNameForThisMissionName == null)
                {
                    Logging.Logging.Log("if (MissionFittingNameForThisMissionName == null) Loading faction fitting.");
                    if (FactionFittingNameForThisMissionsFaction == null)
                    {
                        Logging.Logging.Log("if (FactionFittingNameForThisMissionsFaction == null) Loading default fitting.");
                        //
                        // if both mission and faction fittings are null we need to try to locate and use the default fitting
                        //
                        _fittingToLoad = DefaultFittingName.ToLower();
                    }

                    _fittingToLoad = FactionFittingNameForThisMissionsFaction;
                    return _fittingToLoad;
                }

                _fittingToLoad = MissionFittingNameForThisMissionName;
                return _fittingToLoad;
            }

            set { _fittingToLoad = value; }
        }


        public static string MissionSpecificShip { get; set; } //stores name of mission specific ship
        public static string FactionSpecificShip { get; set; } //stores name of mission specific ship
        public static string CurrentFit { get; set; }
        public static bool OfflineModulesFound { get; set; }

        private static FactionFitting FactionFittingForThisMissionsFaction { get; set; }

        public static string FactionFittingNameForThisMissionsFaction
        {
            get
            {
                if (_factionFittingNameForThisMissionsFaction == null)
                {
                    if (ListofFactionFittings.Any(i => i.FactionName.ToLower() == FactionName.ToLower()))
                    {
                        if (ListofFactionFittings.FirstOrDefault(m => m.FactionName.ToLower() == FactionName.ToLower()) != null)
                        {
                            FactionFittingForThisMissionsFaction = ListofFactionFittings.FirstOrDefault(m => m.FactionName.ToLower() == FactionName.ToLower());
                            if (FactionFittingForThisMissionsFaction != null)
                            {
                                _factionFittingNameForThisMissionsFaction = FactionFittingForThisMissionsFaction.FittingName;
                                if (FactionFittingForThisMissionsFaction.DroneTypeID != null && FactionFittingForThisMissionsFaction.DroneTypeID != 0)
                                {
                                    Drones.FactionDroneTypeID = (int) FactionFittingForThisMissionsFaction.DroneTypeID;
                                    FactionDroneTypeID = (int) FactionFittingForThisMissionsFaction.DroneTypeID;
                                }

                                Logging.Logging.Log("Faction fitting [" + FactionFittingForThisMissionsFaction.FactionName + "] DroneTypeID [" + Drones.DroneTypeID + "]");
                                return _factionFittingNameForThisMissionsFaction;
                            }

                            return null;
                        }

                        return null;
                    }

                    //
                    // Assume the faction named Default has a fit assigned (we couldnt find the actual faction assigned to a fit (we tried above))
                    //
                    if (ListofFactionFittings.Any(i => i.FactionName.ToLower() == "Default".ToLower()))
                    {
                        if (ListofFactionFittings.FirstOrDefault(m => m.FactionName.ToLower() == "Default".ToLower()) != null)
                        {
                            FactionFittingForThisMissionsFaction = ListofFactionFittings.FirstOrDefault(m => m.FactionName.ToLower() == "Default".ToLower());
                            if (FactionFittingForThisMissionsFaction != null)
                            {
                                _factionFittingNameForThisMissionsFaction = FactionFittingForThisMissionsFaction.FittingName;
                                if (FactionFittingForThisMissionsFaction.DroneTypeID != null && FactionFittingForThisMissionsFaction.DroneTypeID != 0)
                                {
                                    Drones.FactionDroneTypeID = (int) FactionFittingForThisMissionsFaction.DroneTypeID;
                                    FactionDroneTypeID = (int) FactionFittingForThisMissionsFaction.DroneTypeID;
                                }

                                Logging.Logging.Log("Faction fitting [" + FactionFittingForThisMissionsFaction.FactionName + "] Using DroneTypeID [" + Drones.DroneTypeID + "]");
                                return _factionFittingNameForThisMissionsFaction;
                            }

                            return null;
                        }
                    }

                    return null;
                }

                return _factionFittingNameForThisMissionsFaction;
            }

            set { _factionFittingNameForThisMissionsFaction = value; }
        }

        public static string MissionFittingNameForThisMissionName
        {
            get
            {
                if (ListOfMissionFittings != null)
                {
                    if (ListOfMissionFittings.Any())
                    {
                        if (ListOfMissionFittings.Any(i => i.MissionName != null && Mission != null && i.MissionName.ToLower().Equals(Mission.Name.ToLower())))
                        {
                            var tempListOfMissionFittings = ListOfMissionFittings.Where(i => i.MissionName.ToLower().Equals(Mission.Name.ToLower()));
                            if (tempListOfMissionFittings != null && tempListOfMissionFittings.Any())
                            {
                                var fitting = tempListOfMissionFittings.FirstOrDefault();
                                if (fitting != null)
                                {
                                    if (fitting.DroneTypeID != null)
                                    {
                                        MissionDroneTypeID = fitting.DroneTypeID;
                                    }

                                    //_fitting.Ship - this should allow for mission specific ships... if we want to allow for that
                                    return fitting.FittingName;
                                }

                                return null;
                            }

                            Logging.Logging.Log("MissionFittingNameForThisMissionName: if (tempListOfMissionFittings != null && tempListOfMissionFittings.Any())");
                            return null;
                        }

                        Logging.Logging.Log("MissionFittingNameForThisMissionName: if (!MissionSettings.ListOfMissionFittings.Any(i => i.MissionName != null && Mission != null && i.MissionName.ToLower() == Mission.Name))");
                        return null;
                    }

                    Logging.Logging.Log("MissionFittingNameForThisMissionName: if (!MissionSettings.ListOfMissionFittings.Any())");
                    return null;
                }

                Logging.Logging.Log("MissionFittingNameForThisMissionName: if (MissionSettings.ListOfMissionFittings == null )");
                return null;


//				return _factionFittingNameForThisMissionsFaction;
            }

            set { _factionFittingNameForThisMissionsFaction = value; }
        }

        public static string FactionName { get; set; }
        public static bool UseMissionShip { get; set; } // flags whether we're using a mission specific ship
        public static bool ChangeMissionShipFittings { get; set; }
        // used for situations in which missionShip's specified, but no faction or mission fittings are; prevents default
        public static Dictionary<Ammo, DateTime> AmmoTypesToLoad { get; set; }

        /// <summary>
        ///     Best damage type for the mission
        /// </summary>
        public static DamageType? CurrentDamageType
        {
            get
            {
                //if (ManualDamageType == null)
                //{
                if (PocketDamageType == null)
                {
                    if (MissionDamageType == null)
                    {
                        if (FactionDamageType == null)
                        {
                            if (Logging.Logging.DebugCombat)
                                Logging.Logging.Log("Note: ManualDamageType, PocketDamageType, MissionDamageType and FactionDamageType were all NULL, defaulting to 1st Ammo listed in AmmoToLoad");
                            if (AmmoTypesToLoad != null && AmmoTypesToLoad.Any())
                            {
                                var currentDamageType = AmmoTypesToLoad.FirstOrDefault().Key.DamageType;
                                return currentDamageType;
                            }

                            return null;
                        }

                        return (DamageType) FactionDamageType;
                    }

                    return (DamageType) MissionDamageType;
                }

                return (DamageType) PocketDamageType;
                //}
                //return (DamageType) ManualDamageType;
            }
        }

        //
        // FactionDamageType, MissionDamageType, PocketDamageType, ManualDamageType
        //

        public static DamageType DefaultDamageType { get; set; }
        public static DamageType? FactionDamageType { get; set; }
        public static DamageType? MissionDamageType { get; set; }
        public static DamageType? PocketDamageType { get; set; }
        //public static DamageType? ManualDamageType { get; set; }
        public static Dictionary<DamageType, DateTime> DamageTypesForThisMission { get; set; }
        public static IEnumerable<DamageType> DamageTypesInMissionXML { get; set; }

        public static void LoadMissionBlackList(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                //if (Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower())
                //{
                //
                // Mission Blacklist
                //
                MissionBlacklist.Clear();
                var xmlElementBlackListSection = CharacterSettingsXml.Element("blacklist") ?? CommonSettingsXml.Element("blacklist");
                if (xmlElementBlackListSection != null)
                {
                    Logging.Logging.Log("Loading Mission Blacklist");
                    var i = 1;
                    foreach (var BlacklistedMission in xmlElementBlackListSection.Elements("mission"))
                    {
                        MissionBlacklist.Add(Logging.Logging.FilterPath((string) BlacklistedMission));
                        if (Logging.Logging.DebugBlackList)
                            Logging.Logging.Log("[" + i + "] Blacklisted mission Name [" + Logging.Logging.FilterPath((string)BlacklistedMission) + "]");
                        i++;
                    }
                    Logging.Logging.Log("        Mission Blacklist now has [" + MissionBlacklist.Count + "] entries");
                }
                //}
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception: [" + ex + "]");
            }
        }

        public static void LoadMissionGreyList(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                //if (Settings.Instance.CharacterMode.ToLower() == "Combat Missions".ToLower())
                //{
                //
                // Mission Greylist
                //
                MissionGreylist.Clear();
                var xmlElementGreyListSection = CharacterSettingsXml.Element("greylist") ?? CommonSettingsXml.Element("greylist");

                if (xmlElementGreyListSection != null)
                {
                    Logging.Logging.Log("Loading Mission GreyList");
                    var i = 1;
                    foreach (var GreylistedMission in xmlElementGreyListSection.Elements("mission"))
                    {
                        MissionGreylist.Add(Logging.Logging.FilterPath((string) GreylistedMission));
                        if (Logging.Logging.DebugGreyList)
                            Logging.Logging.Log("[" + i + "] GreyListed mission Name [" + Logging.Logging.FilterPath((string)GreylistedMission) + "]");
                        i++;
                    }
                    Logging.Logging.Log("        Mission GreyList now has [" + MissionGreylist.Count + "] entries");
                }
                //}
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception: [" + ex + "]");
            }
        }

        public static void LoadFactionBlacklist(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                //
                // Faction Blacklist
                //
                FactionBlacklist.Clear();
                var factionblacklist = CharacterSettingsXml.Element("factionblacklist") ?? CommonSettingsXml.Element("factionblacklist");
                if (factionblacklist != null)
                {
                    Logging.Logging.Log("Loading Faction Blacklist");
                    foreach (var faction in factionblacklist.Elements("faction"))
                    {
                        Logging.Logging.Log("        Missions from the faction [" + (string)faction + "] will be declined");
                        FactionBlacklist.Add((string) faction);
                    }

                    Logging.Logging.Log(" Faction Blacklist now has [" + FactionBlacklist.Count + "] entries");
                }
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception: [" + ex + "]");
            }
        }

        public static bool ThisMissionIsNotWorthSalvaging()
        {
            if (MissionName != null)
            {
                if (MissionName.ToLower().Contains("Attack of the Drones".ToLower()))
                {
                    Logging.Logging.Log("Do not salvage a drones mission as they are crap now");
                    return true;
                }

                if (MissionName.ToLower().Contains("Infiltrated Outposts".ToLower()))
                {
                    Logging.Logging.Log("Do not salvage a drones mission as they are crap now");
                    return true;
                }

                if (MissionName.ToLower().Contains("Rogue Drone Harassment".ToLower()))
                {
                    Logging.Logging.Log("Do not salvage a drones mission as they are crap now");
                    return true;
                }

                return false;
            }

            return false;
        }

        /// <summary>
        ///     Returns the first mission bookmark that starts with a certain string
        /// </summary>
        /// <returns></returns>
        public static DirectAgentMissionBookmark GetMissionBookmark(long agentId, string startsWith)
        {
            try
            {
                // Get the missions
                var missionForBookmarkInfo = Cache.Instance.GetAgentMission(agentId, true);
                if (missionForBookmarkInfo == null)
                {
                    Logging.Logging.Log("missionForBookmarkInfo [null] <---bad  parameters passed to us:  agentid [" + agentId + "] startswith [" + startsWith + "]");
                    return null;
                }

                // Did we accept this mission?
                if (missionForBookmarkInfo.State != (int) MissionState.Accepted)
                {
                    Logging.Logging.Log("missionForBookmarkInfo.State: [" + missionForBookmarkInfo.State.ToString() + "]");
                }

                if (missionForBookmarkInfo.AgentId != agentId)
                {
                    Logging.Logging.Log("missionForBookmarkInfo.AgentId: [" + missionForBookmarkInfo.AgentId.ToString() + "]");
                    Logging.Logging.Log("agentId: [" + agentId + "]");
                    return null;
                }

                if (missionForBookmarkInfo.Bookmarks.Any(b => b.Title.ToLower().StartsWith(startsWith.ToLower())))
                {
                    Logging.Logging.Log("MissionBookmark Found");
                    return missionForBookmarkInfo.Bookmarks.FirstOrDefault(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));
                }

                if (Cache.Instance.AllBookmarks.Any(b => b.Title.ToLower().StartsWith(startsWith.ToLower())))
                {
                    Logging.Logging.Log("MissionBookmark From your Agent Not Found, but we did find a bookmark for a mission");
                    return (DirectAgentMissionBookmark) Cache.Instance.AllBookmarks.FirstOrDefault(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));
                }

                Logging.Logging.Log("MissionBookmark From your Agent Not Found: and as a fall back we could not find any bookmark starting with [" + startsWith + "] either... ");
                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return null;
            }
        }

        public static void ClearPocketSpecificSettings()
        {
            PocketActivateRepairModulesAtThisPerc = null;
            PocketKillSentries = null;
            PocketOptimalRange = null;
            PocketOrbitDistance = null;
            PocketUseDrones = null;
            PocketDamageType = null;
            //MissionSettings.ManualDamageType = null;
        }

        public static void ClearMissionSpecificSettings()
        {
            //
            // Clear Mission Specific Settings
            //
            MissionDronesKillHighValueTargets = null;
            MissionWeaponGroupId = 0;
            MissionWarpAtDistanceRange = 0;
            MissionXMLIsAvailable = true;
            MissionDroneTypeID = null;
            MissionKillSentries = null;
            MissionUseDrones = null;
            MissionOrbitDistance = null;
            MissionOptimalRange = null;
            MissionDamageType = null;
            _factionFittingNameForThisMissionsFaction = null;
            FactionFittingForThisMissionsFaction = null;
            _fittingToLoad = null;
            _listOfMissionFittings.Clear();
        }

        public static void ClearFactionSpecificSettings()
        {
            FactionActivateRepairModulesAtThisPerc = null;
            FactionDroneTypeID = null;
            FactionDronesKillHighValueTargets = null;
            FactionOptimalRange = null;
            FactionOrbitDistance = null;
            FactionDamageType = null;
            //MissionSettings.ManualDamageType = null;
            _listofFactionFittings.Clear();
        }

        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static void LoadMissionXmlData()
        {
            Logging.Logging.Log("Loading mission xml [" + MissionName + "] from [" + MissionXmlPath + "]");


            //FactionDamageType,
            //MissionDamageType,
            //PocketDamageType,
            //ManualDamageType

            //
            // this loads the settings global to the mission, NOT individual pockets
            //
            XDocument missionXml = null;
            try
            {
                missionXml = XDocument.Load(MissionXmlPath);

                //load mission specific ammo and WeaponGroupID if specified in the mission xml
                if (missionXml.Root != null)
                {
                    var ammoTypes = missionXml.Root.Element("ammoTypes");
                    if (ammoTypes != null && ammoTypes.Elements("ammoType").Any())
                    {
                        Logging.Logging.Log("Clearing existing list of Ammo To load: using ammoTypes from [" + MissionXmlPath + "]");
                        AmmoTypesToLoad = new Dictionary<Ammo, DateTime>();
                        foreach (var ammo in ammoTypes.Elements("ammoType"))
                        {
//							Logging.Log("LoadSpecificAmmo", "Adding [" + new Ammo(ammo).Name + "] to the list of ammo to load: from: ammoTypes", Logging.White);
//							AmmoTypesToLoad.AddOrUpdate(new Ammo(ammo), DateTime.UtcNow);
//							MissionSettings.MissionDamageType = (DamageType)Enum.Parse(typeof(DamageType), (string)ammo, true);
//							MissionSettings.loadedAmmo = true;

                            var am = new Ammo(ammo);
                            Logging.Logging.Log("Adding [" + am.Name + "] Quantity: [" + am.Quantity + "] to the list of ammo to load: from: missionammo");
                            AmmoTypesToLoad.AddOrUpdate(am, DateTime.UtcNow);
                            MissionDamageType = (DamageType) Enum.Parse(typeof(DamageType), (string) ammo, true);
                            loadedAmmo = true;
                        }
                    }

                    ammoTypes = missionXml.Root.Element("missionammo");
                    if (ammoTypes != null && ammoTypes.Elements("ammoType").Any())
                    {
                        Logging.Logging.Log("Clearing existing list of Ammo To load: using missionammo from [" + MissionXmlPath + "]");
                        AmmoTypesToLoad = new Dictionary<Ammo, DateTime>();
                        foreach (var ammo in ammoTypes.Elements("ammo"))
                        {
                            var am = new Ammo(ammo);
                            Logging.Logging.Log("Adding [" + am.Name + "] Quantity: [" + am.Quantity + "] to the list of ammo to load: from: missionammo");
                            AmmoTypesToLoad.AddOrUpdate(am, DateTime.UtcNow);
                            MissionDamageType = (DamageType) Enum.Parse(typeof(DamageType), (string) ammo, true);
                            loadedAmmo = true;
                        }
                    }

                    MissionWeaponGroupId = (int?) missionXml.Root.Element("weaponGroupId") ?? 0;
                    MissionUseDrones = (bool?) missionXml.Root.Element("useDrones");
                    MissionKillSentries = (bool?) missionXml.Root.Element("killSentries");
                    MissionWarpAtDistanceRange = (int?) missionXml.Root.Element("missionWarpAtDistanceRange") ?? 0; //distance in km
                    MissionDroneTypeID = (int?) missionXml.Root.Element("DroneTypeId") ?? null;

                    DamageTypesInMissionXML = new List<DamageType>();
                    DamageTypesForThisMission = new Dictionary<DamageType, DateTime>();

                    //missionXml.XPathSelectElements("//damagetype").Select(e => (DamageType)Enum.Parse(typeof(DamageType), (string)e, true)).ToList();
                    //DamageTypesForThisMission = new List<DamageType>();


                    DamageTypesInMissionXML =
                        missionXml.XPathSelectElements("//damagetype").Select(e => (DamageType) Enum.Parse(typeof(DamageType), (string) e, true)).ToList();
                    foreach (var damageTypeElement in DamageTypesInMissionXML)
                    {
                        DamageTypesForThisMission.AddOrUpdate(damageTypeElement, DateTime.UtcNow);
                        var damageTypeElementCopy = damageTypeElement;
                        foreach (var _ammoType in Combat.Combat.Ammo.Where(i => i.DamageType == damageTypeElementCopy).Select(a => a.Clone()))
                        {
                            Logging.Logging.Log("Mission XML for [" + MissionName + "] specified to load [" + damageTypeElementCopy + "] Damagetype. Adding [" + _ammoType.Name +
                                "] Quantity [" + _ammoType.Quantity + "] to the list of ammoToLoad");
                            AmmoTypesToLoad.AddOrUpdate((_ammoType), DateTime.UtcNow);
                            MissionDamageType = _ammoType.DamageType;
                            loadedAmmo = true;
                        }
                    }

                    if (DamageTypesForThisMission.Any() && !AmmoTypesToLoad.Any())
                    {
                        var _MissionDamageTypeCount_ = DamageTypesInMissionXML.Count();

                        Logging.Logging.Log("Mission XML specified there are [" + _MissionDamageTypeCount_ + "] Damagetype(s) for [" + MissionName + "] listed below: ");

                        _MissionDamageTypeCount_ = 0;
                        foreach (var _missionDamageType in DamageTypesForThisMission)
                        {
                            _MissionDamageTypeCount_++;
                            //MissionDamageType = DamageTypesForThisMission.FirstOrDefault();
                            Logging.Logging.Log("[" + _MissionDamageTypeCount_ + "] DamageType [" + _missionDamageType + "]");
                        }

                        LoadCorrectFactionOrMissionAmmo();
                        loadedAmmo = true;
                        return;
                    }

                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Error in mission (not pocket) specific XML tags [" + MissionName + "], " + ex.Message);
            }
            finally
            {
                missionXml = null;
                GC.Collect();
            }

            return;
        }

        public static void LoadCorrectFactionOrMissionAmmo()
        {
            try
            {
                if (Cache.Instance.Weapons.Any(i => i.TypeId == (int) TypeID.CivilianGatlingAutocannon
                                                    || i.TypeId == (int) TypeID.CivilianGatlingPulseLaser
                                                    || i.TypeId == (int) TypeID.CivilianGatlingRailgun
                                                    || i.TypeId == (int) TypeID.CivilianLightElectronBlaster))
                {
                    Logging.Logging.Log("No ammo needed for civilian guns: no ammo added to MissionAmmo to load");
                    return;
                }

                if (!loadedAmmo)
                {
                    Logging.Logging.Log("Clearing existing list of Ammo To load");
                    AmmoTypesToLoad = new Dictionary<Ammo, DateTime>();
                }

                if (MissionDamageType != null)
                {
                    Logging.Logging.Log("if (MissionDamageType != null)");

                    if (Combat.Combat.Ammo.Any(a => a.DamageType == MissionDamageType))
                    {
                        foreach (var missionDamageType in DamageTypesForThisMission)
                        {
                            Logging.Logging.Log("DamageType [" + missionDamageType + "] is one of the damagetypes we should load");
                            var damageTypeToSearchFor = missionDamageType;
                            foreach (var specificAmmoType in Combat.Combat.Ammo.Where(a => a.DamageType == damageTypeToSearchFor.Key).Select(a => a.Clone()))
                            {
                                Logging.Logging.Log("Adding [" + specificAmmoType + "] to the list of AmmoTypes to load. It is defined as [" + missionDamageType +
                                    "] Quantity [" + specificAmmoType.Quantity + "]");
                                AmmoTypesToLoad.Clear(); // this is probaby bad if we want to load more than one ammo
                                AmmoTypesToLoad.AddOrUpdate(specificAmmoType, DateTime.UtcNow);
                                loadedAmmo = true;
                            }
                        }
                    }
                }

                if (FactionDamageType != null && !AmmoTypesToLoad.Any())
                {
                    Logging.Logging.Log("if (FactionDamageType != null && !MissionSettings.AmmoTypesToLoad.Any())");


                    if (Combat.Combat.Ammo.Any(a => a.DamageType == FactionDamageType))
                    {
                        Logging.Logging.Log("DamageType [" + FactionDamageType + "] is one of the damagetypes we should load");
                        foreach (var specificAmmoType in Combat.Combat.Ammo.Where(a => a.DamageType == FactionDamageType).Select(a => a.Clone()))
                        {
                            Logging.Logging.Log("Adding [" + specificAmmoType + "] to the list of AmmoTypes to load. It is defined as [" + FactionDamageType + "]");
                            AmmoTypesToLoad.AddOrUpdate(specificAmmoType, DateTime.UtcNow);
                            loadedAmmo = true;
                        }
                    }
                }

                Logging.Logging.Log("Done building the AmmoToLoad List. AmmoToLoad list follows:");
                var intAmmoToLoad = 0;
                foreach (var ammoTypeToLoad in AmmoTypesToLoad)
                {
                    intAmmoToLoad++;
                    Logging.Logging.Log("AmmoTypesToLoad [" + intAmmoToLoad + "] Name: [" + ammoTypeToLoad.Key.Name + "] DamageType: [" + ammoTypeToLoad.Key.DamageType +
                        "] Range: [" + ammoTypeToLoad.Key.Range + "] Quantity: [" + ammoTypeToLoad.Key.Quantity + "]");
                }

                return;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
                return;
            }
        }

        /*
        public static void GetDungeonId(string html)
        {
            HtmlAgilityPack.HtmlDocument missionHtml = new HtmlAgilityPack.HtmlDocument();
            missionHtml.LoadHtml(html);
            try
            {
                foreach (HtmlAgilityPack.HtmlNode nd in missionHtml.DocumentNode.SelectNodes("//a[@href]"))
                {
                    if (nd.Attributes["href"].Value.Contains("dungeonID="))
                    {
                        Cache.Instance.DungeonId = nd.Attributes["href"].Value;
                        Logging.Log("GetDungeonId", "DungeonID is: " + Cache.Instance.DungeonId, Logging.White);
                    }
                    else
                    {
                        Cache.Instance.DungeonId = "n/a";
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Log("GetDungeonId", "if (nd.Attributes[href].Value.Contains(dungeonID=)) - Exception: [" + exception + "]", Logging.White);
            }
        }
		 */

        public static void GetFactionName(string html)
        {
            Statistics.SaveMissionHTMLDetails(html, MissionName);
            // We are going to check damage types
            var logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");

            var logoMatch = logoRegex.Match(html);
            if (logoMatch.Success)
            {
                var logo = logoMatch.Groups["factionlogo"].Value;

                // Load faction xml
                var factionsXML = Path.Combine(Settings.Instance.Path, "Factions.xml");
                try
                {
                    var xml = XDocument.Load(factionsXML);
                    if (xml.Root != null)
                    {
                        var faction = xml.Root.Elements("faction").FirstOrDefault(f => (string) f.Attribute("logo") == logo);
                        if (faction != null)
                        {
                            FactionName = (string) faction.Attribute("name");
                            return;
                        }
                    }
                    else
                    {
                        Logging.Logging.Log("ERROR! unable to read [" + factionsXML + "]  no root element named <faction> ERROR!");
                    }
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("ERROR! unable to find [" + factionsXML + "] ERROR! [" + ex.Message + "]");
                }
            }

            var roguedrones = false;
            var mercenaries = false;
            var eom = false;
            var seven = false;
            if (!string.IsNullOrEmpty(html))
            {
                roguedrones |= html.Contains("Destroy the Rogue Drones");
                roguedrones |= html.Contains("Rogue Drone Harassment Objectives");
                roguedrones |= html.Contains("Air Show! Objectives");
                roguedrones |= html.Contains("Alluring Emanations Objectives");
                roguedrones |= html.Contains("Anomaly Objectives");
                roguedrones |= html.Contains("Attack of the Drones Objectives");
                roguedrones |= html.Contains("Drone Detritus Objectives");
                roguedrones |= html.Contains("Drone Infestation Objectives");
                roguedrones |= html.Contains("Evolution Objectives");
                roguedrones |= html.Contains("Infected Ruins Objectives");
                roguedrones |= html.Contains("Infiltrated Outposts Objectives");
                roguedrones |= html.Contains("Mannar Mining Colony");
                roguedrones |= html.Contains("Missing Convoy Objectives");
                roguedrones |= html.Contains("Onslaught Objectives");
                roguedrones |= html.Contains("Patient Zero Objectives");
                roguedrones |= html.Contains("Persistent Pests Objectives");
                roguedrones |= html.Contains("Portal to War Objectives");
                roguedrones |= html.Contains("Rogue Eradication Objectives");
                roguedrones |= html.Contains("Rogue Hunt Objectives");
                roguedrones |= html.Contains("Rogue Spy Objectives");
                roguedrones |= html.Contains("Roving Rogue Drones Objectives");
                roguedrones |= html.Contains("Soothe The Salvage Beast");
                roguedrones |= html.Contains("Wildcat Strike Objectives");
                eom |= html.Contains("Gone Berserk Objectives");
                seven |= html.Contains("The Damsel In Distress Objectives");
            }

            if (roguedrones)
            {
                FactionName = "rogue drones";
                return;
            }
            if (eom)
            {
                FactionName = "eom";
                return;
            }
            if (mercenaries)
            {
                FactionName = "mercenaries";
                return;
            }
            if (seven)
            {
                FactionName = "the seven";
                return;
            }

            Logging.Logging.Log("Unable to find the faction for [" + MissionName + "] when searching through the html (listed below)");

            Logging.Logging.Log(html);
            return;
        }

        public static DamageType GetFactionDamageType(string html)
        {
            DamageType damageTypeToUse;
            // We are going to check damage types
            var logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");

            var logoMatch = logoRegex.Match(html);
            if (logoMatch.Success)
            {
                var logo = logoMatch.Groups["factionlogo"].Value;

                // Load faction xml
                var xml = XDocument.Load(Path.Combine(Settings.Instance.Path, "Factions.xml"));
                if (xml.Root != null)
                {
                    var faction = xml.Root.Elements("faction").FirstOrDefault(f => (string) f.Attribute("logo") == logo);
                    if (faction != null)
                    {
                        FactionName = (string) faction.Attribute("name");
                        Logging.Logging.Log("[" + MissionName + "] Faction [" + FactionName + "]");
                        if (faction.Attribute("damagetype") != null)
                        {
                            damageTypeToUse = ((DamageType) Enum.Parse(typeof(DamageType), (string) faction.Attribute("damagetype")));
                            Logging.Logging.Log("Faction DamageType defined as [" + damageTypeToUse + "]");
                            return (DamageType) damageTypeToUse;
                        }

                        Logging.Logging.Log("DamageType not found for Faction [" + FactionName + "], Defaulting to DamageType  [" + DefaultDamageType + "]");
                        return DefaultDamageType;
                    }

                    Logging.Logging.Log("Faction not found in factions.xml, Defaulting to DamageType  [" + DefaultDamageType + "]");
                    return DefaultDamageType;
                }

                Logging.Logging.Log("Factions.xml is missing, Defaulting to DamageType  [" + DefaultDamageType + "]");
                return DefaultDamageType;
            }

            Logging.Logging.Log("Faction logo not matched, Defaulting to DamageType  [" + DefaultDamageType + "]");
            return DefaultDamageType;
        }

        public static void UpdateMissionName(long AgentID = 0)
        {
            if (AgentID != 0)
            {
                Mission = Cache.Instance.GetAgentMission(AgentID, true);
                if (Mission != null && Cache.Instance.Agent != null)
                {
                    // Update loyalty points again (the first time might return -1)
                    Statistics.LoyaltyPointsTotal = Cache.Instance.Agent.LoyaltyPoints;
                    MissionName = Mission.Name;
                }
            }
        }

        public static void SetmissionXmlPath(string missionName)
        {
            try
            {
                if (!string.IsNullOrEmpty(FactionName))
                {
                    MissionXmlPath = Path.Combine(MissionsPath, Logging.Logging.FilterPath(missionName) + "-" + FactionName + ".xml");
                    if (!File.Exists(MissionXmlPath))
                    {
                        //
                        // This will always fail for courier missions, can we detect those and suppress these log messages?
                        //
                        Logging.Logging.Log("[" + MissionXmlPath + "] not found.");
                        MissionXmlPath = Path.Combine(MissionsPath, Logging.Logging.FilterPath(missionName) + ".xml");
                        if (!File.Exists(MissionXmlPath))
                        {
                            Logging.Logging.Log("[" + MissionXmlPath + "] not found");
                        }

                        if (File.Exists(MissionXmlPath))
                        {
                            Logging.Logging.Log("[" + MissionXmlPath + "] found!");
                        }
                    }
                }
                else
                {
                    MissionXmlPath = Path.Combine(MissionsPath, Logging.Logging.FilterPath(missionName) + ".xml");
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }
    }
}