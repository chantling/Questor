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
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using D3DDetour;
using DirectEve;
using Questor.Modules.Actions;
using Questor.Modules.BackgroundTasks;
using Questor.Modules.Combat;
using Questor.Modules.Logging;
using Questor.Modules.Lookup;
using Questor.Storylines;
using Ut::EVE;
using Ut::WCF;

namespace Questor.Modules.Caching
{
    public partial class Cache
    {
        private static Cache _instance = new Cache();
        public static int CacheInstances;
        public static bool LootAlreadyUnloaded;
        public static Random _random = new Random();
        private static readonly Func<DirectAgent, DirectSession, bool> AgentInThisSolarSystemSelector = (a, s) => a.SolarSystemId == s.SolarSystemId;
        private static readonly Func<DirectAgent, DirectSession, bool> AgentInThisStationSelector = (a, s) => a.StationId == s.StationId;
        public static DateTime QuestorProgramLaunched = DateTime.UtcNow;
        public static DateTime QuestorSchedulerReadyToLogin = DateTime.UtcNow;
        public static DateTime EVEAccountLoginStarted = DateTime.UtcNow;
        public static DateTime NextSlotActivate = DateTime.UtcNow;
        public static bool _humanInterventionRequired;
        public static int ServerStatusCheck = 0;
        public static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
        public static DateTime DoneLoggingInToEVETimeStamp = DateTime.MaxValue;

        // save the last used agentname/id to be able to retrieve the current agent by id
        private static String _agentName = String.Empty;
        private static long _agentId = 0;
        private readonly Dictionary<long, EntityCache> _entitiesById;
        private DirectAgent _agent;
        private EntityCache _approaching;
        private List<EntityCache> _bigObjects;
        private List<EntityCache> _bigObjectsAndGates;
        private List<EntityCache> _containers;
        private DirectContainer _currentShipsCargo;
        private EveAccount _EveAccount = null;
        public DirectContainer _fittedModules;
        private List<EntityCache> _gates;
        public List<long> _IDsinInventoryTree;
        public bool _isCorpInWar = false;
        private IEnumerable<EntityCache> _jumpBridges;
        private int? _maxLockedTargets;
        private List<ModuleCache> _modules;
        private DirectItem _myCurrentAmmoInWeapon;
        private List<EntityCache> _objects;
        private List<DirectBookmark> _safeSpotBookmarks;
        private EntityCache _star;
        private EntityCache _stargate;
        private List<EntityCache> _stargates;
        private List<EntityCache> _stations;
        private List<EntityCache> _targeting;
        private List<EntityCache> _targets;
        private List<EntityCache> _unlootedContainers;
        private List<EntityCache> _unlootedWrecksAndSecureCans;
        private IEnumerable<ModuleCache> _weapons;
        private List<DirectWindow> _windows;
        public List<long> AgentBlacklist;
        public float AgentCorpEffectiveStandingtoMe;
        public float AgentEffectiveStandingtoMe;
        public string AgentEffectiveStandingtoMeText;
        public float AgentFactionEffectiveStandingtoMe;
        public long AgentSolarSystemID;
        public string AgentStationName;
        public long AmmoHangarID = -99;
        public bool CloseQuestorCMDExitGame = true;
        public bool CloseQuestorCMDLogoff;
        public bool CloseQuestorEndProcess;
        public bool CourierMission;
        public bool doneUsingRepairWindow;
        public string DungeonId;
        public bool ExitWhenIdle;
        public bool GotoBaseNow;

        bool inMission;
        public volatile bool IsLoadingSettings;

        private DateTime LastEveAccountPoll = DateTime.MinValue;
        public HashSet<long> ListNeutralizingEntities = new HashSet<long>();
        public HashSet<long> ListofContainersToLoot = new HashSet<long>();
        public HashSet<long> ListOfDampenuingEntities = new HashSet<long>();
        public HashSet<long> ListOfJammingEntities = new HashSet<long>();
        public HashSet<string> ListofMissionCompletionItemsToLoot = new HashSet<string>();
        public HashSet<long> ListOfTargetPaintingEntities = new HashSet<long>();
        public HashSet<long> ListOfTrackingDisruptingEntities = new HashSet<long>();

        public HashSet<long> ListOfWarpScramblingEntities = new HashSet<long>();
        public HashSet<long> ListofWebbingEntities = new HashSet<long>();
        public long LootHangarID = -99;
        public bool MissionBookmarkTimerSet;
        public DirectLocation MissionSolarSystem;
        public bool NormalApproach = true;

        public bool normalNav = true;
        public string OrbitEntityNamed;
        public string Path;
        public bool QuestorJustStarted = true;
        public bool RouteIsAllHighSecBool;
        public float StandingUsedToAccessAgent;
        public bool StopBot;
        public long TotalMegaBytesOfMemoryUsed = 0;
        public long VolleyCount;

        public Cache()
        {
            LastModuleTargetIDs = new Dictionary<long, long>();
            TargetingIDs = new Dictionary<long, DateTime>();
            _entitiesById = new Dictionary<long, EntityCache>();

            LootedContainers = new HashSet<long>();
            Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;

            Interlocked.Increment(ref CacheInstances);
        }

        public static Storyline storyline { get; set; }
        public string CharName { get; set; }
        public string PipeName { get; set; }
        public long CurrentStorylineAgentId { get; set; }
        public HashSet<long> LootedContainers { get; private set; }
        public bool CanSafelyCloseQuestorWindow { get; set; }
        public double Wealth { get; set; }
        public double WealthatStartofPocket { get; set; }
        public int StackHangarAttempts { get; set; }
        public static D3DVersion D3DVersion { get; set; }

        public bool InMission
        {
            get
            {
                if (!InStation)
                {
                    var station = Instance.Stations.OrderBy(s => s.Distance).FirstOrDefault();
                    var stargate = Instance.Stargates.OrderBy(s => s.Distance).FirstOrDefault();

                    if (station != null && station.Distance < 1000000)
                    {
                        return false;
                    }

                    if (stargate != null && stargate.Distance < 1000000)
                    {
                        return false;
                    }
                }

                return inMission;
            }

            set { inMission = value; }
        }

        public bool onlyKillAggro { get; set; }
        public int StackLoothangarAttempts { get; set; }
        public int StackAmmohangarAttempts { get; set; }
        public int StackItemhangarAttempts { get; set; }
        public Dictionary<int, String> UnloadLootTheseItemsAreLootById { get; private set; }
        public List<ShipTargetValue> ShipTargetValues { get; private set; }
        public DamageType FrigateDamageType { get; set; }
        public DamageType CruiserDamageType { get; set; }
        public DamageType BattleCruiserDamageType { get; set; }
        public DamageType BattleShipDamageType { get; set; }
        public DamageType LargeColidableDamageType { get; set; }
        public bool AfterMissionSalvaging { get; set; }
        public DirectEve.DirectEve DirectEve { get; set; }
        public Dictionary<long, long> LastModuleTargetIDs { get; private set; }
        public Dictionary<long, DateTime> TargetingIDs { get; private set; }
        public bool AllAgentsStillInDeclineCoolDown { get; set; }
        private string _currentAgent { get; set; }
        public bool Paused { get; set; }
        public double MyWalletBalance { get; set; }
        public string CurrentPocketAction { get; set; }
        public long AgentStationID { get; set; }
        public DirectContainer _containerInSpace { get; set; }
        public static bool UseDx9 { get; set; }

        public EveAccount EveAccount
        {
            get
            {
                if (_EveAccount == null || LastEveAccountPoll.AddSeconds(1) < DateTime.UtcNow)
                {
                    LastEveAccountPoll = DateTime.UtcNow;
                    _EveAccount = WCFClient.Instance.GetPipeProxy.GetEveAccount(this.CharName);
                }

                return _EveAccount;
            }
        }

        public WCFClient WCFClient
        {
            get { return WCFClient.Instance; }
        }

        public bool IsCorpInWar
        {
            get
            {
                if (DateTime.UtcNow > Time.Instance.NextCheckCorpisAtWar)
                {
                    var war = DirectEve.Me.IsAtWar;
                    Instance._isCorpInWar = war;

                    Time.Instance.NextCheckCorpisAtWar = DateTime.UtcNow.AddMinutes(15);
                    if (!_isCorpInWar)
                    {
                        if (Logging.Logging.DebugWatchForActiveWars)
                            Logging.Logging.Log("Your corp is not involved in any wars (yet)");
                    }
                    else
                    {
                        if (Logging.Logging.DebugWatchForActiveWars)
                            Logging.Logging.Log("Your corp is involved in a war, be careful");
                    }

                    return _isCorpInWar;
                }

                return _isCorpInWar;
            }
        }

        public static Cache Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Cache();
                }
                return _instance;
            }
        }

        public DirectContainer CurrentShipsCargo
        {
            get
            {
                try
                {
                    if ((Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10)) ||
                        (Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10)))
                    {
                        if (_currentShipsCargo == null)
                        {
                            _currentShipsCargo = Instance.DirectEve.GetShipsCargo();
                            if (Logging.Logging.DebugCargoHold) Logging.Logging.Log("_currentShipsCargo is null");
                        }

                        if (Instance.Windows.All(i => i.Type != "form.ActiveShipCargo"))
                            // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
                        {
                            if (DateTime.UtcNow > Time.Instance.NextOpenCurrentShipsCargoWindowAction)
                            {
                                Statistics.LogWindowActionToWindowLog("CargoHold", "Opening CargoHold");
                                if (Logging.Logging.DebugCargoHold)
                                    Logging.Logging.Log("Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);");
                                Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
                                Time.Instance.NextOpenCurrentShipsCargoWindowAction = DateTime.UtcNow.AddMilliseconds(1000 + Instance.RandomNumber(0, 2000));
                            }

                            if (Logging.Logging.DebugCargoHold)
                                Logging.Logging.Log("Waiting on NextOpenCurrentShipsCargoWindowAction [" +
                                    DateTime.UtcNow.Subtract(Time.Instance.NextOpenCurrentShipsCargoWindowAction).TotalSeconds + "sec]");
                        }

                        return _currentShipsCargo;
                    }

                    var EntityCount = 0;
                    if (Instance.Entities.Any())
                    {
                        EntityCount = Instance.Entities.Count();
                    }

                    if (Logging.Logging.DebugCargoHold)
                        Logging.Logging.Log("Cache.Instance.MyShipEntity is null: We have a total of [" + EntityCount + "] entities available at the moment.");
                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Unable to complete ReadyCargoHold [" + exception + "]");
                    return null;
                }
            }
        }

        public DirectContainer ContainerInSpace
        {
            get
            {
                if (_containerInSpace == null)
                {
                    return null;
                }

                return _containerInSpace;
            }

            set { _containerInSpace = value; }
        }

        public DirectActiveShip ActiveShip
        {
            get { return Instance.DirectEve.ActiveShip; }
        }

        public int WeaponRange
        {
            get
            {
                // Get ammo based on current damage type
                IEnumerable<Ammo> ammo = Combat.Combat.Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList();

                try
                {
                    // Is our ship's cargo available?
                    if (Instance.CurrentShipsCargo != null)
                    {
                        ammo = ammo.Where(a => Instance.CurrentShipsCargo.Items.Any(i => a.TypeId == i.TypeId && i.Quantity >= Combat.Combat.MinimumAmmoCharges));
                    }
                    else
                    {
                        return Convert.ToInt32(Combat.Combat.MaxTargetRange);
                    }

                    // Return ship range if there's no ammo left
                    if (!ammo.Any())
                    {
                        return Convert.ToInt32(Combat.Combat.MaxTargetRange);
                    }

                    return ammo.Max(a => a.Range);
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugExceptions) Logging.Logging.Log("exception was:" + ex.Message);

                    // Return max range
                    if (Instance.ActiveShip != null)
                    {
                        return Convert.ToInt32(Combat.Combat.MaxTargetRange);
                    }

                    return 0;
                }
            }
        }

        public DirectItem myCurrentAmmoInWeapon
        {
            get
            {
                try
                {
                    if (_myCurrentAmmoInWeapon == null)
                    {
                        if (Instance.Weapons != null && Instance.Weapons.Any())
                        {
                            var WeaponToCheckForAmmo = Instance.Weapons.FirstOrDefault();
                            if (WeaponToCheckForAmmo != null)
                            {
                                _myCurrentAmmoInWeapon = WeaponToCheckForAmmo.Charge;
                                return _myCurrentAmmoInWeapon;
                            }

                            return null;
                        }

                        return null;
                    }

                    return _myCurrentAmmoInWeapon;
                }
                catch (Exception ex)
                {
                    if (Logging.Logging.DebugExceptions)
                        Logging.Logging.Log("exception was:" + ex.Message);
                    return null;
                }
            }
        }

        public string CurrentAgent
        {
            get
            {
                try
                {
                    if (Settings.Instance.CharacterXMLExists)
                    {
                        if (string.IsNullOrEmpty(_currentAgent))
                        {
                            try
                            {
                                if (MissionSettings.ListOfAgents != null && MissionSettings.ListOfAgents.Count() >= 1)
                                {
                                    _currentAgent = MissionSettings.ListOfAgents.FirstOrDefault().Name;
                                    Logging.Logging.Log("Current Agent is [" + _currentAgent + "]");
                                }
                                else
                                {
                                    Logging.Logging.Log("MissionSettings.ListOfAgents == null ");
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.Logging.Log("Exception [" + ex + "]");
                                return string.Empty;
                            }
                        }

                        return _currentAgent;
                    }

                    return string.Empty;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                    return "";
                }
            }
            set
            {
                try
                {
                    _currentAgent = value;
                }
                catch (Exception ex)
                {
                    Logging.Logging.Log("Exception [" + ex + "]");
                }
            }
        }

        public DirectAgent Agent
        {
            get
            {
                try
                {
                    if (Settings.Instance.CharacterXMLExists)
                    {
                        try
                        {
                            if (_agent == null && (!CurrentAgent.Equals(_agentName) || _agentId <= 0))
                            {
                                _agent = Instance.DirectEve.GetAgentByName(CurrentAgent);
                                if (_agent == null)
                                {
                                    Logging.Logging.Log("Agent == null");
                                }
                            }

                            if (_agent == null && CurrentAgent.Equals(_agentName) && _agentId > 0)
                            {
                                _agent = Instance.DirectEve.GetAgentById(_agentId);
                            }

                            if (_agent != null && !CurrentAgent.Equals(_agentName))
                            {
                                Logging.Logging.Log("New AgentId [" + _agent.AgentId + "] AgentName [" + CurrentAgent + "]");
                                Instance.AgentStationName = Instance.DirectEve.GetLocationName(Instance._agent.StationId);
                                Instance.AgentStationID = Instance._agent.StationId;
                                Instance.AgentSolarSystemID = Instance._agent.SolarSystemId;


                                _agentName = CurrentAgent;
                                _agentId = _agent.AgentId;
                            }

                            return _agent;
                        }
                        catch (Exception ex)
                        {
                            Logging.Logging.Log("Unable to process agent section of [" + Logging.Logging.CharacterSettingsPath +
                                "] make sure you have a valid agent listed! Pausing so you can fix it. [" + ex.Message + "]");
                            Instance.Paused = true;
                        }
                    }
                    else
                    {
                        Logging.Logging.Log("if (!Settings.Instance.CharacterXMLExists)");
                    }
                    return null;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
            set { _agent = null; }
        }

        public IEnumerable<ModuleCache> Modules
        {
            get
            {
                try
                {
                    if (_modules == null || !_modules.Any())
                    {
                        _modules = Instance.DirectEve.Modules.Select(m => new ModuleCache(m)).ToList();
                    }

                    return _modules;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public DirectContainer FittedModules
        {
            get
            {
                try
                {
                    if (_fittedModules == null)
                    {
                        _fittedModules = Instance.DirectEve.GetShipsModules();
                    }

                    return _fittedModules;
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public IEnumerable<ModuleCache> Weapons
        {
            get
            {
                if (_weapons == null)
                {
                    _weapons = Modules.Where(m => m.GroupId == Combat.Combat.WeaponGroupId).ToList(); // ||
                    //m.GroupId == (int)Group.ProjectileWeapon ||
                    //m.GroupId == (int)Group.EnergyWeapon ||
                    //m.GroupId == (int)Group.HybridWeapon ||
                    //m.GroupId == (int)Group.CruiseMissileLaunchers ||
                    //m.GroupId == (int)Group.RocketLaunchers ||
                    //m.GroupId == (int)Group.StandardMissileLaunchers ||
                    //m.GroupId == (int)Group.TorpedoLaunchers ||
                    //m.GroupId == (int)Group.AssaultMissilelaunchers ||
                    //m.GroupId == (int)Group.HeavyMissilelaunchers ||
                    //m.GroupId == (int)Group.DefenderMissilelaunchers);
                    if (MissionSettings.MissionWeaponGroupId != 0)
                    {
                        _weapons = Modules.Where(m => m.GroupId == MissionSettings.MissionWeaponGroupId).ToList();
                    }

                    if (Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10))
                    {
                        if (!_weapons.Any())
                        {
                            var moduleNumber = 0;
                            //Logging.Log("Cache.Weapons", "WeaponGroupID is defined as [" + Combat.WeaponGroupId + "] in your characters settings XML", Logging.Debug);
                            foreach (var _module in Instance.Modules)
                            {
                                moduleNumber++;
                                //Logging.Log("Cache.Weapons", "[" + moduleNumber + "][" + _module.TypeName + "] typeID [" + _module.TypeId + "] groupID [" + _module.GroupId + "]", Logging.White);
                            }
                        }
                        else
                        {
                            if (DateTime.UtcNow > Time.Instance.NextModuleDisableAutoReload)
                            {
                                //int weaponNumber = 0;
                                foreach (var _weapon in Instance.Weapons)
                                {
                                    //weaponNumber++;
                                    if (_weapon.AutoReload)
                                    {
                                        var returnValueHereNotUsed = _weapon.DisableAutoReload;
                                        Time.Instance.NextModuleDisableAutoReload = DateTime.UtcNow.AddSeconds(2);
                                    }
                                    //Logging.Log("Cache.Weapons", "[" + weaponNumber + "][" + _module.TypeName + "] typeID [" + _module.TypeId + "] groupID [" + _module.GroupId + "]", Logging.White);
                                }
                            }
                        }
                    }
                }

                return _weapons;
            }
        }

        public List<DirectWindow> Windows
        {
            get
            {
                try
                {
                    if (Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(20) ||
                        (Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20)))
                    {
                        return _windows ?? (_windows = DirectEve.Windows);
                    }

                    return new List<DirectWindow>();
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("Exception [" + exception + "]");
                }

                return null;
            }
        }

        ~Cache()
        {
            Interlocked.Decrement(ref CacheInstances);
        }

        public void InvalidateCache()
        {
            try
            {
                Logging.Logging.InvalidateCache();
                Arm.InvalidateCache();
                Drones.InvalidateCache();
                Combat.Combat.InvalidateCache();
                Salvage.InvalidateCache();

                _ammoHangar = null;
                _lootHangar = null;
                _lootContainer = null;
                _fittedModules = null;


                //
                // this list of variables is cleared every pulse.
                //
                _agent = null;
                _allBookmarks = null;
                _approaching = null;
                _bigObjects = null;
                _bigObjectsAndGates = null;
                _chargeEntities = null;
                _currentShipsCargo = null;
                _containerInSpace = null;
                _containers = null;
                _entities = null;
                _entitiesNotSelf = null;
                _entitiesOnGrid = null;
                _entitiesById.Clear();
                _fittingManagerWindow = null;
                _gates = null;
                _IDsinInventoryTree = null;
                _itemHangar = null;
                _jumpBridges = null;
                _lpStore = null;
                _maxLockedTargets = null;
                _modules = null;
                _myAmmoInSpace = null;
                _myCurrentAmmoInWeapon = null;
                _myShipEntity = null;
                _objects = null;
                _safeSpotBookmarks = null;
                _shipHangar = null;
                _star = null;
                _stations = null;
                _stargate = null;
                _stargates = null;
                _targets = null;
                _targeting = null;
                _TotalTargetsandTargeting = null;
                _unlootedContainers = null;
                _unlootedWrecksAndSecureCans = null;
                _weapons = null;
                _windows = null;
                _wrecks = null;


//				Logging.Log("Cache.InvalidateCache", "Cache invalidated.", Logging.Debug);
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }
        }

        public static bool LoadDirectEVEInstance(D3DVersion version)
        {
            try
            {
                var TryLoadingDirectVE = 0;
                while (Instance.DirectEve == null && TryLoadingDirectVE < 30)
                {
                    try
                    {
                        Logging.Logging.Log("Starting Instance of DirectEVE using StandaloneFramework");
                        Instance.DirectEve = new DirectEve.DirectEve(new StandaloneFramework(version));
                        TryLoadingDirectVE++;
                        Logging.Logging.Log("DirectEVE should now be active: see above for any messages from DirectEVE");
                        return true;
                    }
                    catch (Exception exception)
                    {
                        Logging.Logging.Log("exception [" + exception + "]");
                        continue;
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("exception [" + exception + "]");
                return false;
            }

            if (Instance.DirectEve == null)
            {
                try
                {
                    Logging.Logging.Log("Error on Loading DirectEve, maybe server is down");
                    Instance.CloseQuestorCMDLogoff = false;
                    Instance.CloseQuestorCMDExitGame = true;
                    Instance.CloseQuestorEndProcess = true;
                    Cleanup.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
                    Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
                    Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor, true);
                    return false;
                }
                catch (Exception exception)
                {
                    Logging.Logging.BasicLog("Startup", "Exception while logging exception, oh joy [" + exception + "]");
                    return false;
                }
            }

            return true;
        }

        public bool LocalSafe(int maxBad, double stand)
        {
            var number = 0;
            var local = (DirectChatWindow) GetWindowByName("Local");

            try
            {
                foreach (var localMember in local.Members)
                {
                    float[] alliance =
                    {
                        DirectEve.Standings.GetPersonalRelationship(localMember.AllianceId),
                        DirectEve.Standings.GetCorporationRelationship(localMember.AllianceId),
                        DirectEve.Standings.GetAllianceRelationship(localMember.AllianceId)
                    };
                    float[] corporation =
                    {
                        DirectEve.Standings.GetPersonalRelationship(localMember.CorporationId),
                        DirectEve.Standings.GetCorporationRelationship(localMember.CorporationId),
                        DirectEve.Standings.GetAllianceRelationship(localMember.CorporationId)
                    };
                    float[] personal =
                    {
                        DirectEve.Standings.GetPersonalRelationship(localMember.CharacterId),
                        DirectEve.Standings.GetCorporationRelationship(localMember.CharacterId),
                        DirectEve.Standings.GetAllianceRelationship(localMember.CharacterId)
                    };

                    if (alliance.Min() <= stand || corporation.Min() <= stand || personal.Min() <= stand)
                    {
                        Logging.Logging.Log("Bad Standing Pilot Detected: [ " + localMember.Name + "] " + " [ " + number + " ] so far... of [ " + maxBad + " ] allowed");
                        number++;
                    }

                    if (number > maxBad)
                    {
                        Logging.Logging.Log("[" + number + "] Bad Standing pilots in local, We should stay in station");
                        return false;
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            return true;
        }

        public bool UpdateMyWalletBalance()
        {
            //we know we are connected here
            Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
            Instance.MyWalletBalance = Instance.DirectEve.Me.Wealth;
            return true;
        }

        public DirectItem CheckCargoForItem(int typeIdToFind, int quantityToFind)
        {
            try
            {
                if (Instance.CurrentShipsCargo != null && Instance.CurrentShipsCargo.Items.Any())
                {
                    var item = Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == typeIdToFind && i.Quantity >= quantityToFind);
                    return item;
                }

                return null; // no items found
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }

            return null;
        }

        public bool CheckifRouteIsAllHighSec()
        {
            Instance.RouteIsAllHighSecBool = false;

            try
            {
                // Find the first waypoint
                if (DirectEve.Navigation.GetDestinationPath() != null && DirectEve.Navigation.GetDestinationPath().Count > 0)
                {
                    var currentPath = DirectEve.Navigation.GetDestinationPath();
                    if (currentPath == null || !currentPath.Any()) return false;
                    if (currentPath[0] == 0) return false; //No destination set - prevents exception if somehow we have got an invalid destination

                    foreach (var _system in currentPath)
                    {
                        if (_system < 60000000) // not a station
                        {
                            var solarSystemInRoute = Instance.DirectEve.SolarSystems[_system];
                            if (solarSystemInRoute != null)
                            {
                                if (solarSystemInRoute.Security < 0.45)
                                {
                                    //Bad bad bad
                                    Instance.RouteIsAllHighSecBool = false;
                                    return true;
                                }

                                continue;
                            }

                            Logging.Logging.Log("Jump number [" + _system + "of" + currentPath.Count() +
                                "] in the route came back as null, we could not get the system name or sec level");
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("Exception [" + exception + "]");
            }


            //
            // if DirectEve.Navigation.GetDestinationPath() is null or 0 jumps then it must be safe (can we assume we are not in lowsec or 0.0 already?!)
            //
            Instance.RouteIsAllHighSecBool = true;
            return true;
        }

        public void ClearPerPocketCache(string callingroutine)
        {
            try
            {
                if (DateTime.UtcNow > Time.NextClearPocketCache)
                {
                    MissionSettings.ClearPocketSpecificSettings();
                    Combat.Combat._doWeCurrentlyHaveProjectilesMounted = null;
                    Combat.Combat.LastTargetPrimaryWeaponsWereShooting = null;
                    Drones.LastTargetIDDronesEngaged = null;

                    _ammoHangar = null;
                    _lootHangar = null;
                    _lootContainer = null;

                    ListOfWarpScramblingEntities.Clear();
                    ListOfJammingEntities.Clear();
                    ListOfTrackingDisruptingEntities.Clear();
                    ListNeutralizingEntities.Clear();
                    ListOfTargetPaintingEntities.Clear();
                    ListOfDampenuingEntities.Clear();
                    ListofWebbingEntities.Clear();
                    ListofContainersToLoot.Clear();
                    ListofMissionCompletionItemsToLoot.Clear();

                    ListOfUndockBookmarks = null;

                    //MyMissileProjectionSkillLevel = SkillPlan.MissileProjectionSkillLevel();

                    EntityNames.Clear();
                    EntityTypeID.Clear();
                    EntityGroupID.Clear();
                    EntityBounty.Clear();
                    EntityIsFrigate.Clear();
                    EntityIsNPCFrigate.Clear();
                    EntityIsCruiser.Clear();
                    EntityIsNPCCruiser.Clear();
                    EntityIsBattleCruiser.Clear();
                    EntityIsNPCBattleCruiser.Clear();
                    EntityIsBattleShip.Clear();
                    EntityIsNPCBattleShip.Clear();
                    EntityIsHighValueTarget.Clear();
                    EntityIsLowValueTarget.Clear();
                    EntityIsLargeCollidable.Clear();
                    EntityIsSentry.Clear();
                    EntityIsMiscJunk.Clear();
                    EntityIsBadIdea.Clear();
                    EntityIsFactionWarfareNPC.Clear();
                    EntityIsNPCByGroupID.Clear();
                    EntityIsEntutyIShouldLeaveAlone.Clear();
                    EntityHaveLootRights.Clear();
                    EntityIsStargate.Clear();

                    Instance.LootedContainers.Clear();
                    return;
                }

                //Logging.Log("ClearPerPocketCache", "[ " + callingroutine + " ] Attempted to ClearPocketCache within 5 seconds of a previous ClearPocketCache, aborting attempt", Logging.Debug);
            }
            catch (Exception ex)
            {
                Logging.Logging.Log("Exception [" + ex + "]");
                return;
            }
            finally
            {
                Time.NextClearPocketCache = DateTime.UtcNow.AddSeconds(5);
            }
        }

        public int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }

        public void IterateShipTargetValues(string module)
        {
            var path = Logging.Logging.PathToCurrentDirectory;

            if (path != null)
            {
                var ShipTargetValuesXmlFile = System.IO.Path.Combine(path, "ShipTargetValues.xml");
                ShipTargetValues = new List<ShipTargetValue>();

                if (!File.Exists(ShipTargetValuesXmlFile))
                {
                    Logging.Logging.Log("IterateShipTargetValues - unable to find [" + ShipTargetValuesXmlFile + "]");
                    return;
                }

                try
                {
                    Logging.Logging.Log("IterateShipTargetValues - Loading [" + ShipTargetValuesXmlFile + "]");
                    var values = XDocument.Load(ShipTargetValuesXmlFile);
                    if (values.Root != null)
                    {
                        foreach (var value in values.Root.Elements("ship"))
                        {
                            ShipTargetValues.Add(new ShipTargetValue(value));
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("IterateShipTargetValues - Exception: [" + exception + "]");
                }
            }
        }

        public void IterateUnloadLootTheseItemsAreLootItems(string module)
        {
            var path = Logging.Logging.PathToCurrentDirectory;

            if (path != null)
            {
                var UnloadLootTheseItemsAreLootItemsXmlFile = System.IO.Path.Combine(path, "UnloadLootTheseItemsAreLootItems.xml");
                UnloadLootTheseItemsAreLootById = new Dictionary<int, string>();

                if (!File.Exists(UnloadLootTheseItemsAreLootItemsXmlFile))
                {
                    Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - unable to find [" + UnloadLootTheseItemsAreLootItemsXmlFile + "]");
                    return;
                }

                try
                {
                    Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - Loading [" + UnloadLootTheseItemsAreLootItemsXmlFile + "]");
                    MissionSettings.UnloadLootTheseItemsAreLootItems = XDocument.Load(UnloadLootTheseItemsAreLootItemsXmlFile);

                    if (MissionSettings.UnloadLootTheseItemsAreLootItems.Root != null)
                    {
                        foreach (var element in MissionSettings.UnloadLootTheseItemsAreLootItems.Root.Elements("invtype"))
                        {
                            UnloadLootTheseItemsAreLootById.Add((int) element.Attribute("id"), (string) element.Attribute("name"));
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]");
                }
            }
            else
            {
                Logging.Logging.Log("IterateUnloadLootTheseItemsAreLootItems - unable to find [" + Logging.Logging.PathToCurrentDirectory + "]");
            }
        }

        public static int GetRandom(int minValue, int maxValue)
        {
            return _random.Next(minValue, maxValue);
        }

        public DirectAgentMission GetAgentMission(long agentId, bool ForceUpdate)
        {
            if (DateTime.UtcNow < Time.Instance.NextGetAgentMissionAction)
            {
                if (MissionSettings.FirstAgentMission != null)
                {
                    return MissionSettings.FirstAgentMission;
                }

                return null;
            }

            try
            {
                if (ForceUpdate || MissionSettings.myAgentMissionList == null || !MissionSettings.myAgentMissionList.Any())
                {
                    MissionSettings.myAgentMissionList = DirectEve.AgentMissions.Where(m => m.AgentId == agentId).ToList();
                    Time.Instance.NextGetAgentMissionAction = DateTime.UtcNow.AddSeconds(5);
                }

                if (MissionSettings.myAgentMissionList.Any())
                {
                    MissionSettings.FirstAgentMission = MissionSettings.myAgentMissionList.FirstOrDefault();
                    return MissionSettings.FirstAgentMission;
                }

                return null;
            }
            catch (Exception exception)
            {
                Logging.Logging.Log("DirectEve.AgentMissions failed: [" + exception + "]");
                return null;
            }
        }
    }
}