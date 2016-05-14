﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
// 
//     Please look in the accompanying license.htm file for the license that 
//     applies to this source code. (a copy can also be found at: 
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace DirectEve
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Hooking;
    using PySharp;

    public delegate void LoggingDelegate(string msg);

    public class DirectEve : IDisposable
    {
        //private DirectEveSecurity _security;
        //private bool _securityCheckFailed;

        /// <summary>
        ///     ActiveShip cache
        /// </summary>
        private DirectActiveShip _activeShip;

        /// <summary>
        ///     Cache the Agent Missions
        /// </summary>
        private List<DirectAgentMission> _agentMissions;

        /// <summary>
        ///     Cache the Bookmark Folders
        /// </summary>
        private List<DirectBookmarkFolder> _bookmarkFolders;

        /// <summary>
        ///     Cache the Bookmarks
        /// </summary>
        private List<DirectBookmark> _bookmarks;

        /// <summary>
        ///     Const cache
        /// </summary>
        private DirectConst _const;

        /// <summary>
        ///     Cache the GetConstellations call
        /// </summary>
        private Dictionary<long, DirectConstellation> _constellations;

        /// <summary>
        ///     Item container cache
        /// </summary>
        private Dictionary<long, DirectContainer> _containers;

        /// <summary>
        ///     Cache the Entities
        /// </summary>
        private Dictionary<long, DirectEntity> _entitiesById;

        /// <summary>
        ///     Item Hangar container cache
        /// </summary>
        private DirectContainer _itemHangar;

        /// <summary>
        ///     Info on when a certain target was last targeted
        /// </summary>
        private Dictionary<long, DateTime> _lastKnownTargets;

        /// <summary>
        ///     Cache the LocalSvc objects
        /// </summary>
        private Dictionary<string, PyObject> _localSvcCache;

        /// <summary>
        ///     Login cache
        /// </summary>
        private DirectLogin _login;

        /// <summary>
        ///     Me cache
        /// </summary>
        private DirectMe _me;

        /// <summary>
        ///     Cache the Windows
        /// </summary>
        private List<DirectModule> _modules;

        /// <summary>
        ///     Navigation cache
        /// </summary>
        private DirectNavigation _navigation;

        /// <summary>
        ///     Cache the GetRegions call
        /// </summary>
        private Dictionary<long, DirectRegion> _regions;

        /// <summary>
        ///     Session cache
        /// </summary>
        private DirectSession _session;

        /// <summary>
        ///     Ship Hangar container cache
        /// </summary>
        private DirectContainer _shipHangar;

        /// <summary>
        ///     Ship's cargo container cache
        /// </summary>
        private DirectContainer _shipsCargo;

        /// <summary>
        ///     Ship's ore hold container cache
        /// </summary>
        private DirectContainer _shipsOreHold;

        /// <summary>
        ///     Global Assets cache
        /// </summary>
        private List<DirectItem> _listGlobalAssets;

        /// <summary>
        ///     Ship's drone bay cache
        /// </summary>
        private DirectContainer _shipsDroneBay;

        /// <summary>
        ///     Ship's modules container cache
        /// </summary>
        private DirectContainer _shipsModules;

        private DirectSkills _skills;

        /// <summary>
        ///     Cache the GetRegions call
        /// </summary>
        private Dictionary<int, DirectSolarSystem> _solarSystems;

        /// <summary>
        ///     Standings cache
        /// </summary>
        private DirectStandings _standings;

        /// <summary>
        ///     Cache the GetStations call
        /// </summary>
        private Dictionary<int, DirectStation> _stations;

        /// <summary>
        ///     Cache the GetWindows call
        /// </summary>
        private List<DirectWindow> _windows;

        private bool _enableStatisticsModifying;

        public static bool _enableLoggingNotExistingAttributes = false;
        public static bool _enableLoggingNotExistingFunctions = false;
        public static DateTime _lastModuleBlocked = DateTime.MinValue;

        /// <summary>
        ///     The framework object that wraps OnFrame and Log
        /// </summary>
        private IFramework _framework;

        ////Statistic variables
        private long _lastOnframeTook;

        private double _frameTimeAbove100ms;
        private double _prevFrameTimeAbove100ms;
        private double _frameTimeAbove200ms;
        private double _prevFrameTimeAbove200ms;
        private double _frameTimeAbove300ms;
        private double _prevFrameTimeAbove300ms;
        private double _frameTimeAbove400ms;
        private double _prevFrameTimeAbove400ms;
        private double _frameTimeAbove500ms;
        private double _prevFrameTimeAbove500ms;
        private double _timesliceWarnings;
        private double _prevtimesliceWarnings;

        /// <summary>
        ///     Create a DirectEve object
        /// </summary>
        public DirectEve(IFramework framework = null, bool enableStealth = true, bool enableStatisticModifying = true)
        {
            _enableStatisticsModifying = enableStatisticModifying;

            // create an instance of IFramework
            if (framework != null)
            {
                _framework = framework;
            }
            else
            {
                _framework = new InnerSpaceFramework();
            }

            try
            {
                _localSvcCache = new Dictionary<string, PyObject>();
                _containers = new Dictionary<long, DirectContainer>();
                _lastKnownTargets = new Dictionary<long, DateTime>();

#if DEBUG
                Log("Registering OnFrame event");
#endif
                if (enableStealth)
                {
                    try
                    {
                        Hooks.InitializeHooks();
                    }
                    catch (Exception ex)
                    {
                        Log("Warning: Failed to initialize stealth hooks: " + ex);
                    }
                }
                _framework.RegisterFrameHook(FrameworkOnFrame);
            }
            catch (Exception e)
            {
#if DEBUG
                Log("DirectEve: Debug: Exception after license check: " + e.Message + " stacktrace: " + e.StackTrace);
#endif
                throw;
            }
        }

        /// <summary>
        ///     Return a DirectConst object
        /// </summary>
        internal DirectConst Const
        {
            get { return _const ?? (_const = new DirectConst(this)); }
        }

        public DateTime GetLastModuleBlocked
        {
            get
            {
                return _lastModuleBlocked;
            }
        }

        /// <summary>
        ///     Return a DirectNavigation object
        /// </summary>
        public DirectLogin Login
        {
            get { return _login ?? (_login = new DirectLogin(this)); }
        }

        /// <summary>
        ///     Return a DirectNavigation object
        /// </summary>
        public DirectNavigation Navigation
        {
            get { return _navigation ?? (_navigation = new DirectNavigation(this)); }
        }

        /// <summary>
        ///     Return a DirectMe object
        /// </summary>
        public DirectMe Me
        {
            get { return _me ?? (_me = new DirectMe(this)); }
        }

        /// <summary>
        ///     Return a DirectStandings object
        /// </summary>
        public DirectStandings Standings
        {
            get { return _standings ?? (_standings = new DirectStandings(this)); }
        }

        /// <summary>
        ///     Return a DirectActiveShip object
        /// </summary>
        public DirectActiveShip ActiveShip
        {
            get { return _activeShip ?? (_activeShip = new DirectActiveShip(this)); }
        }

        /// <summary>
        ///     Return a DirectSession object
        /// </summary>
        public DirectSession Session
        {
            get { return _session ?? (_session = new DirectSession(this)); }
        }

        /// <summary>
        ///     Return a DirectSkills object
        /// </summary>
        public DirectSkills Skills
        {
            get { return _skills ?? (_skills = new DirectSkills(this)); }
        }

        /// <summary>
        ///     Internal reference to the PySharp object that is used for the frame
        /// </summary>
        /// <remarks>
        ///     This reference is only valid while in an OnFrame event
        /// </remarks>
        public PySharp.PySharp PySharp { get; private set; }

        /// <summary>
        ///     Return a list of entities
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public List<DirectEntity> Entities
        {
            get { return EntitiesById.Values.ToList(); }
        }

        /// <summary>
        ///     Return a dictionary of entities by id
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public Dictionary<long, DirectEntity> EntitiesById
        {
            get
            {
                if (_entitiesById == null)
                {
                    _entitiesById = DirectEntity.GetEntities(this);
                }
                return _entitiesById;
            }
        }

        /// <summary>
        ///     The last bookmark update
        /// </summary>
        public DateTime LastBookmarksUpdate
        {
            get { return DirectBookmark.GetLastBookmarksUpdate(this) ?? new DateTime(0, 0, 0); }
        }

        /// <summary>
        ///     Return a list of bookmarks
        /// </summary>
        /// <value></value>
        public List<DirectBookmark> Bookmarks
        {
            get { return _bookmarks ?? (_bookmarks = DirectBookmark.GetBookmarks(this)); }
        }

        /// <summary>
        ///     Return a list of bookmark folders
        /// </summary>
        /// <value></value>
        public List<DirectBookmarkFolder> BookmarkFolders
        {
            get { return _bookmarkFolders ?? (_bookmarkFolders = DirectBookmark.GetFolders(this)); }
        }

        /// <summary>
        ///     Return a list of agent missions
        /// </summary>
        /// <value></value>
        public List<DirectAgentMission> AgentMissions
        {
            get { return _agentMissions ?? (_agentMissions = DirectAgentMission.GetAgentMissions(this)); }
        }

        /// <summary>
        ///     Return a list of all open windows
        /// </summary>
        /// <value></value>
        public List<DirectWindow> Windows
        {
            get { return _windows ?? (_windows = DirectWindow.GetWindows(this)); }
        }

        /// <summary>
        ///     Return a list of all modules
        /// </summary>
        /// <value></value>
        /// <remarks>
        ///     Only works inspace and does not return hidden modules
        /// </remarks>
        public List<DirectModule> Modules
        {
            get { return _modules ?? (_modules = DirectModule.GetModules(this)); }
        }

        /// <summary>
        ///     Return active drone id's
        /// </summary>
        /// <value></value>
        public List<DirectEntity> ActiveDrones
        {
            get
            {
                var droneIds = GetLocalSvc("michelle").Call("GetDrones").Attribute("items").ToDictionary<long>().Keys;
                return Entities.Where(e => droneIds.Any(d => d == e.Id)).ToList();
            }
        }

        /// <summary>
        ///     Return a dictionary of stations
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existance of this DirectEve Instance
        /// </remarks>
        public Dictionary<int, DirectStation> Stations
        {
            get { return _stations ?? (_stations = DirectStation.GetStations(this)); }
        }

        /// <summary>
        ///     Return a dictionary of solar systems
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existance of this DirectEve Instance
        /// </remarks>
        public Dictionary<int, DirectSolarSystem> SolarSystems
        {
            get { return _solarSystems ?? (_solarSystems = DirectSolarSystem.GetSolarSystems(this)); }
        }

        /// <summary>
        ///     Set destination without fetching DirectLocation ~ CPU Intensive
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public bool SetDestination(long locationId)
        {
            return DirectNavigation.SetDestination(locationId, this);
        }

        public int GetDistanceBetweenSolarsystems(int solarsystem1, int solarsystem2)
        {
            return DirectSolarSystem.GetDistanceBetweenSolarsystems(solarsystem1, solarsystem2, this);
        }

        private Dictionary<int, DirectInvType> _invtypes;

        /// <summary>
        ///     Return a dictionary of solar systems
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existance of this DirectEve Instance
        /// </remarks>
        public Dictionary<long, DirectConstellation> Constellations
        {
            get { return _constellations ?? (_constellations = DirectConstellation.GetConstellations(this)); }
        }

        /// <summary>
        ///     Return a dictionary of solar systems
        /// </summary>
        /// <remarks>
        ///     This is cached throughout the existance of this DirectEve Instance
        /// </remarks>
        public Dictionary<long, DirectRegion> Regions
        {
            get { return _regions ?? (_regions = DirectRegion.GetRegions(this)); }
        }

        public Dictionary<int, DirectInvType> InvTypes
        {
            get { return _invtypes ?? (_invtypes = DirectInvType.GetInvtypes(this)); }
        }

        /// <summary>
        ///     Is EVE rendering 3D, you can enable/disable rendering by setting this value to true or false
        /// </summary>
        /// <remarks>
        ///     Only works in space!
        /// </remarks>
        public bool Rendering3D
        {
            get
            {
                var rendering1 = (bool)GetLocalSvc("sceneManager").Attribute("registeredScenes").DictionaryItem("default").Attribute("display");
                return rendering1;
            }
            set { GetLocalSvc("sceneManager").Attribute("registeredScenes").DictionaryItem("default").SetAttribute("display", value); }
        }

        /// <summary>
        ///     Is EVE loading textures, you can enable/disable texture loading by setting this value to true or false
        /// </summary>
        /// <remarks>
        ///     Use at own risk!
        /// </remarks>
        public bool ResourceLoad
        {
            get
            {
                var disableGeometryLoad = (bool)PySharp.Import("trinity").Attribute("device").Attribute("disableGeometryLoad");
                var disableEffectLoad = (bool)PySharp.Import("trinity").Attribute("device").Attribute("disableEffectLoad");
                var disableTextureLoad = (bool)PySharp.Import("trinity").Attribute("device").Attribute("disableTextureLoad");
                return (disableGeometryLoad || disableEffectLoad || disableTextureLoad);
            }
            set
            {
                PySharp.Import("trinity").Attribute("device").SetAttribute("disableGeometryLoad", value);
                PySharp.Import("trinity").Attribute("device").SetAttribute("disableEffectLoad", value);
                PySharp.Import("trinity").Attribute("device").SetAttribute("disableTextureLoad", value);
            }
        }

        #region IDisposable Members

        /// <summary>
        ///     Dispose of DirectEve
        /// </summary>
        public void Dispose()
        {
            Hooks.RemoveHooks();

            if (_framework != null)
                _framework.Dispose();

            //if (_security != null)
            //    _security.QuitDirectEve();

            //_security = null;
            _framework = null;
        }

        #endregion

        /// <summary>
        ///     Refresh the bookmark cache (if needed)
        /// </summary>
        /// <returns></returns>
        public bool RefreshBookmarks()
        {
            return DirectBookmark.RefreshBookmarks(this);
        }

        /// <summary>
        ///     Refresh the PnPWindow
        /// </summary>
        /// <returns></returns>
        public bool RefreshPnPWindow()
        {
            return DirectBookmark.RefreshPnPWindow(this);
        }

        public bool IsTargetStillValid(long id)
        {
            //dynamic ps = PySharp;
            var target = this.GetLocalSvc("target");
            var targets = target.Attribute("targetsByID").ToDictionary<long>();
            //var targets = ps.__builtin__.sm.services["target"].targetsByID.ToDictionary<long>();

            if (targets.ContainsKey(id))
            {
                return true;
            }
            
            return false;
        }

        public bool IsTargetBeingRemoved(long id)
        {
            var target = this.GetLocalSvc("target");
            var targetsBeingRemoved = target.Attribute("deadShipsBeingRemoved"); // set object
            if(targetsBeingRemoved.IsValid && targetsBeingRemoved.PySet_Contains<long>(id))
            {
                return true;
            }

            return false;
        }

        public Dictionary<long,PyObject> GetTargets()
        {
            var target = this.GetLocalSvc("target");
            var targets = target.Attribute("targetsByID").ToDictionary<long>();
            return targets;
        }

        public bool IsTargetStillInBallParkAndNotExplodedAndNotReleased(long id)
        {
            var uix = PySharp.Import("uix");
            var ballpark = this.GetLocalSvc("michelle").Call("GetBallpark");
            var balls = ballpark.Attribute("balls").ToDictionary<long>();
            if(balls.ContainsKey(id))
            {
               var ball =  ballpark.Call("GetBall", id);
                if(ball.IsValid && !(bool)ball.Attribute("exploded") && !(bool)ball.Attribute("released"))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     OnFrame event, use this to do your eve-stuff
        /// </summary>
        public event EventHandler OnFrame;

        /// <summary>
        ///     Internal "OnFrame" handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FrameworkOnFrame(object sender, EventArgs e)
        {
            var st = new Stopwatch();
            st.Start();
            using (var pySharp = new PySharp.PySharp(true))
            {
                // Make the link to the instance
                PySharp = pySharp;

                // Get current target list
                dynamic ps = pySharp;
                // targetsByID and targeting are now dictionaries
                List<long> targets = ps.__builtin__.sm.services["target"].targetsByID.keys().ToList<long>();
                targets.AddRange(ps.__builtin__.sm.services["target"].targeting.keys().ToList<long>());

                // Update currently locked targets
                targets.ForEach(t => _lastKnownTargets[t] = DateTime.Now);
                // Remove all targets that have not been locked for 3 seconds
                foreach (var t in _lastKnownTargets.Keys.ToArray())
                {
                    if (DateTime.Now.AddSeconds(-3) < _lastKnownTargets[t])
                        continue;

                    _lastKnownTargets.Remove(t);
                }

                ////Populate the statistic variables
                if (_enableStatisticsModifying)
                {
                    CheckStatistics();
                }

                // Save last activated modules time
                DirectModule.UpdateActiveModules(this);

                // Check if we're still valid
                if (OnFrame != null)
                    OnFrame(this, new EventArgs());

                // Clear any cache that we had during this frame
                _localSvcCache.Clear();
                _entitiesById = null;
                _windows = null;
                _modules = null;
                _const = null;
                _bookmarks = null;
                _agentMissions = null;

                _containers.Clear();
                _itemHangar = null;
                _shipHangar = null;
                _shipsCargo = null;
                _shipsOreHold = null;
                _shipsModules = null;
                _shipsDroneBay = null;
                _listGlobalAssets = null;
                _me = null;
                _activeShip = null;
                _standings = null;
                _navigation = null;
                _session = null;
                _login = null;
                _skills = null;

                // Remove the link
                PySharp = null;

                st.Stop();
                _lastOnframeTook = st.ElapsedMilliseconds;
            }
        }

        /// <summary>
        ///     Open the corporation hangar
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Only works in a station!
        /// </remarks>
        private bool OpenCorporationHangar()
        {
            return ExecuteCommand(DirectCmd.OpenCorpHangar);
        }

        public bool OpenInventory()
        {
            return ExecuteCommand(DirectCmd.OpenInventory);
        }

        public int GetCorpHangarId(string divisionName)
        {
            var divisions = GetLocalSvc("corp").Call("GetDivisionNames");
            for (var i = 0; i < 7; i++)
            {
                if (string.Compare(divisionName, (string)divisions.DictionaryItem(i), true) == 0)
                    return i;
            }
            return -1;
        }

        public bool OpenCorpHangarArray(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenCorpHangarArray", itemID, global::DirectEve.PySharp.PySharp.PyNone);
        }

        public bool OpenShipMaintenanceBay(long itemID)
        {
            var OpenShipMaintenanceBayShip = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions").Attribute("OpenShipMaintenanceBayShip");
            return ThreadedCall(OpenShipMaintenanceBayShip, itemID, global::DirectEve.PySharp.PySharp.PyNone);
        }

        public bool OpenStructure(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenStructure", itemID, global::DirectEve.PySharp.PySharp.PyNone);
        }

        public bool OpenStructureCharges(long itemID, bool hasCapacity)
        {
            return ThreadedLocalSvcCall("menu", "OpenStructureCharges", itemID, global::DirectEve.PySharp.PySharp.PyNone, hasCapacity);
        }

        public bool OpenStructureCargo(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenStructureCargo", itemID, global::DirectEve.PySharp.PySharp.PyNone);
        }

        public bool OpenStrontiumBay(long itemID)
        {
            return ThreadedLocalSvcCall("menu", "OpenStrontiumBay", itemID, global::DirectEve.PySharp.PySharp.PyNone);
        }

        /// <summary>
        ///     Execute a command
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public bool ExecuteCommand(DirectCmd cmd)
        {
            return ThreadedLocalSvcCall("cmd", cmd.ToString());
        }

        /// <summary>
        ///     Return a list of locked items
        /// </summary>
        /// <returns></returns>
        public List<long> GetLockedItems()
        {
            var locks = GetLocalSvc("invCache").Attribute("lockedItems").ToDictionary<long>();
            return locks.Keys.ToList();
        }

        /// <summary>
        ///     Remove all item locks
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Do not abuse this, the client probably placed them for a reason!
        /// </remarks>
        public bool UnlockItems()
        {
            return GetLocalSvc("invCache").Attribute("lockedItems").Clear();
        }

        /// <summary>
        ///     Item hangar container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetItemHangar()
        {
            return _itemHangar ?? (_itemHangar = DirectContainer.GetItemHangar(this));
        }

        /// <summary>
        ///     Ship hangar container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipHangar()
        {
            return _shipHangar ?? (_shipHangar = DirectContainer.GetShipHangar(this));
        }

        /// <summary>
        ///     Ship's cargo container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsCargo()
        {
            return _shipsCargo ?? (_shipsCargo = DirectContainer.GetShipsCargo(this));
        }

        /// <summary>
        ///     Ship's ore hold container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsOreHold()
        {
            return _shipsOreHold ?? (_shipsOreHold = DirectContainer.GetShipsOreHold(this));
        }

        // If this is not the right place to do the calls themself, let me know. I thought placing them in DirectContainer was not neat ~ Ferox
        /// <summary>
        ///     Assets list
        /// </summary>
        /// <returns></returns>
        public List<DirectItem> GetAssets()
        {
            if (_listGlobalAssets == null)
            {
                _listGlobalAssets = new List<DirectItem>();
                var pyItemDict = GetLocalSvc("invCache").Attribute("containerGlobal").Attribute("cachedItems").ToDictionary<long>();
                foreach (var pyItem in pyItemDict)
                {
                    var item = new DirectItem(this);
                    item.PyItem = pyItem.Value;
                    _listGlobalAssets.Add(item);
                }
            }

            return _listGlobalAssets;
        }

        /// <summary>
        ///     Refresh global assets list (note: 5min delay in assets)
        /// </summary>
        /// <returns></returns>
        public bool RefreshAssets()
        {
            return ThreadedCall(GetLocalSvc("invCache").Call("GetInventory", Const.ContainerGlobal).Attribute("List"));
        }

        /// <summary>
        ///     Ship's modules container
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsModules()
        {
            return _shipsModules ?? (_shipsModules = DirectContainer.GetShipsModules(this));
        }


        /// <summary>
        ///     Ship's drone bay
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipsDroneBay()
        {
            return _shipsDroneBay ?? (_shipsDroneBay = DirectContainer.GetShipsDroneBay(this));
        }

        /// <summary>
        ///     Item container
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public DirectContainer GetContainer(long itemId)
        {
            if (!_containers.ContainsKey(itemId))
                _containers[itemId] = DirectContainer.GetContainer(this, itemId);

            return _containers[itemId];
        }

        /// <summary>
        ///     Get the corporation hangar container based on division name
        /// </summary>
        /// <param name="divisionName"></param>
        /// <returns></returns>
        public DirectContainer GetCorporationHangar(string divisionName)
        {
            return DirectContainer.GetCorporationHangar(this, divisionName);
        }

        /// <summary>
        ///     Get the corporation hangar container based on division id (1-7)
        /// </summary>
        /// <param name="divisionId"></param>
        /// <returns></returns>
        public DirectContainer GetCorporationHangar(int divisionId)
        {
            return DirectContainer.GetCorporationHangar(this, divisionId);
        }

        public DirectContainer GetCorporationHangarArray(long itemId, string divisionName)
        {
            return DirectContainer.GetCorporationHangarArray(this, itemId, divisionName);
        }

        public DirectContainer GetCorporationHangarArray(long itemId, int divisionId)
        {
            return DirectContainer.GetCorporationHangarArray(this, itemId, divisionId);
        }

        /// <summary>
        ///     Return the entity by it's id
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public DirectEntity GetEntityById(long entityId)
        {
            DirectEntity entity;
            if (EntitiesById.TryGetValue(entityId, out entity))
                return entity;

            return null;
        }

        /// <summary>
        ///     Bookmark the current location
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public bool BookmarkCurrentLocation(string name, string comment, long? folderId)
        {
            if (Session.CharacterId == null)
                return false;

            return BookmarkCurrentLocation(Session.CharacterId.Value, name, comment, folderId);
        }


        /// <summary>
        ///     Bookmark the current location
        /// </summary>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public bool CorpBookmarkCurrentLocation(string name, string comment, long? folderId)
        {
            if (Session.CorporationId == null)
                return false;

            return BookmarkCurrentLocation(Session.CorporationId.Value, name, comment, folderId);
        }

        /// <summary>
        ///     Bookmark the current location
        /// </summary>
        /// <param name="ownerId"></param>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        internal bool BookmarkCurrentLocation(long ownerId, string name, string comment, long? folderId)
        {
            if (Session.StationId.HasValue)
            {
                var station = GetLocalSvc("station").Attribute("station");
                if (!station.IsValid)
                    return false;

                return DirectBookmark.BookmarkLocation(this, ownerId, (long)station.Attribute("stationID"), name, comment, (int)station.Attribute("stationTypeID"), (long?)station.Attribute("solarSystemID"), folderId);
            }

            if (ActiveShip.Entity.IsValid && Session.SolarSystemId.HasValue)
                return DirectBookmark.BookmarkLocation(this, ownerId, ActiveShip.Entity.Id, name, comment, ActiveShip.Entity.TypeId, Session.SolarSystemId, folderId);

            return false;
        }

        /// <summary>
        ///     Bookmark an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <param name="corp"></param>
        /// <returns></returns>
        public bool BookmarkEntity(DirectEntity entity, string name, string comment, long? folderId, bool corp = false)
        {
            if (!entity.IsValid)
                return false;

            if (!corp && Session.CharacterId == null)
                return false;

            if (corp && Session.CorporationId == null)
                return false;

            if (!corp)
                return DirectBookmark.BookmarkLocation(this, Session.CharacterId.Value, entity.Id, name, comment, entity.TypeId, Session.SolarSystemId, folderId);
            else
                return DirectBookmark.BookmarkLocation(this, Session.CorporationId.Value, entity.Id, name, comment, entity.TypeId, Session.SolarSystemId, folderId);
        }


        /// <summary>
        ///     Bookmark an entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="name"></param>
        /// <param name="comment"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public bool CorpBookmarkEntity(DirectEntity entity, string name, string comment, long? folderId)
        {
            if (!entity.IsValid)
                return false;

            if (Session.CorporationId == null)
                return false;

            return DirectBookmark.BookmarkLocation(this, Session.CorporationId.Value, entity.Id, name, comment, entity.TypeId, Session.SolarSystemId, folderId);
        }

        /// <summary>
        ///     Create a bookmark folder
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CreateBookmarkFolder(string name)
        {
            if (Session.CharacterId == null)
                return false;

            return DirectBookmark.CreateBookmarkFolder(this, Session.CharacterId.Value, name);
        }

        /// <summary>
        ///     Create a bookmark folder
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CreateCorpBookmarkFolder(string name)
        {
            if (Session.CorporationId == null)
                return false;

            return DirectBookmark.CreateBookmarkFolder(this, Session.CorporationId.Value, name);
        }

        /// <summary>
        ///     Drop bookmarks into people &amp; places
        /// </summary>
        /// <param name="bookmarks"></param>
        /// <returns></returns>
        public bool DropInPeopleAndPlaces(IEnumerable<DirectItem> bookmarks)
        {
            return DirectItem.DropInPlaces(this, bookmarks);
        }

        /// <summary>
        ///     Refine items from the hangar floor
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public bool ReprocessStationItems(IEnumerable<DirectItem> items)
        {
            if (items == null)
                return false;

            if (items.Any(i => !i.PyItem.IsValid))
                return false;

            if (!Session.IsInStation)
                return false;

            if (items.Any(i => i.LocationId != Session.StationId))
                return false;

            var Refine = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("Refine");
            return ThreadedCall(Refine, items.Select(i => i.PyItem));
        }

        /// <summary>
        ///     Return an owner
        /// </summary>
        /// <param name="ownerId"></param>
        /// <returns></returns>
        public DirectOwner GetOwner(long ownerId)
        {
            return DirectOwner.GetOwner(this, ownerId);
        }

        /// <summary>
        ///     Return a location
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public DirectLocation GetLocation(int locationId)
        {
            return DirectLocation.GetLocation(this, locationId);
        }

        /// <summary>
        ///     Return the name of a location
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public string GetLocationName(long locationId)
        {
            return DirectLocation.GetLocationName(this, locationId);
        }

        /// <summary>
        ///     Return the agent by id
        /// </summary>
        /// <param name="agentId"></param>
        /// <returns></returns>
        public DirectAgent GetAgentById(long agentId)
        {
            return DirectAgent.GetAgentById(this, agentId);
        }

        /// <summary>
        ///     Return the agent by name
        /// </summary>
        /// <param name="agentName"></param>
        /// <returns></returns>
        public DirectAgent GetAgentByName(string agentName)
        {
            return DirectAgent.GetAgentByName(this, agentName);
        }

        /// <summary>
        ///     Return what "eve.LocalSvc" would return, unless the service wasn't started yet
        /// </summary>
        /// <param name="svc"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public PyObject GetLocalSvc(string svc)
        {
            PyObject service;
            // Do we have a cached version (this is to stop overloading the LocalSvc call)
            if (_localSvcCache.TryGetValue(svc, out service))
                return service;

            // First try to get it from services
            service = PySharp.Import("__builtin__").Attribute("sm").Attribute("services").DictionaryItem(svc);

            // Add it to the cache (it doesn't matter if its not valid)
            _localSvcCache.Add(svc, service);

            // If its valid, return the service
            if (service.IsValid)
                return service;

            // Start the service in a ThreadedCall
            var localSvc = PySharp.Import("__builtin__").Attribute("sm").Attribute("GetService");
            ThreadedCall(localSvc, svc);

            // Return an invalid PyObject (so that LocalSvc can start the service)
            return global::DirectEve.PySharp.PySharp.PyZero;
        }

        /// <summary>
        ///     Perform a uthread.new(pyCall, parms) call
        /// </summary>
        /// <param name="pyCall"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public bool ThreadedCall(PyObject pyCall, params object[] parms)
        {
            return ThreadedCallWithKeywords(pyCall, null, parms);
        }

        /// <summary>
        ///     Perform a uthread.new(pyCall, parms) call
        /// </summary>
        /// <param name="pyCall"></param>
        /// <param name="keywords"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public bool ThreadedCallWithKeywords(PyObject pyCall, Dictionary<string, object> keywords, params object[] parms)
        {
            // Check specifically for this, as the call has to be valid (e.g. not null or none)
            if (!pyCall.IsValid)
                return false;

            RegisterAppEventTime();
            return !PySharp.Import("uthread").CallWithKeywords("new", keywords, (new object[] { pyCall }).Concat(parms).ToArray()).IsNull;
        }

        /// <summary>
        ///     Perform a uthread.new(svc.call, parms) call
        /// </summary>
        /// <param name="svc"></param>
        /// <param name="call"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        /// <remarks>Use at your own risk!</remarks>
        public bool ThreadedLocalSvcCall(string svc, string call, params object[] parms)
        {
            var pyCall = GetLocalSvc(svc).Attribute(call);
            return ThreadedCall(pyCall, parms);
        }

        /// <summary>
        ///     Return's true if the entity has not been a target in the last 3 seconds
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        internal bool CanTarget(long id)
        {
            return !_lastKnownTargets.ContainsKey(id);
        }

        /// <summary>
        ///     Remove's the target from the last known targets
        /// </summary>
        /// <param name="id"></param>
        internal void ClearTargetTimer(long id)
        {
            _lastKnownTargets.Remove(id);
        }

        /// <summary>
        ///     Set the target's last target time
        /// </summary>
        /// <param name="id"></param>
        internal void SetTargetTimer(long id)
        {
            _lastKnownTargets[id] = DateTime.Now;
        }

        /// <summary>
        ///     Register app event time
        /// </summary>
        private void RegisterAppEventTime()
        {
            PySharp.Import("__builtin__").Attribute("uicore").Attribute("uilib").Call("RegisterAppEventTime");
        }

        /// <summary>
        ///     Open the fitting management window
        /// </summary>
        public void OpenFitingManager()
        {
            var form = PySharp.Import("form");
            ThreadedCall(form.Attribute("FittingMgmt").Attribute("Open"));
        }

        /// <summary>
        ///     Open the repairshop window
        /// </summary>
        public void OpenRepairShop()
        {
            if (!Session.IsInStation)
                return;
            var form = PySharp.Import("form");
            ThreadedCall(form.Attribute("RepairShopWindow").Attribute("Open"));
        }

        internal long getServiceMask()
        {
            if (!Session.IsInStation)
                return 0;
            return (long)PySharp.Import("__builtin__").Attribute("eve").Attribute("stationItem").Attribute("serviceMask");
        }

        public bool hasRepairFacility()
        {
            if (!Session.IsInStation || !Session.IsReady)
            {
                return false;
            }
            var serviceMask = getServiceMask();
            return (serviceMask & (long)Const["stationServiceRepairFacilities"]) != 0;
        }

        /// <summary>
        ///     Broadcast scatter events.  Use with caution.
        /// </summary>
        /// <param name="evt">The event name.</param>
        /// <returns></returns>
        public bool ScatterEvent(string evt)
        {
            var scatterEvent = PySharp.Import("__builtin__").Attribute("sm").Attribute("ScatterEvent");
            return ThreadedCall(scatterEvent, evt);
        }

        /// <summary>
        ///     Log a message.
        /// </summary>
        /// <param name="msg">A string to output to the loggers.</param>
        public void Log(string msg)
        {
            _framework.Log(msg);
        }

        /// <summary>
        ///     Does this user have available support instances?
        ///     NOTE: Do not use this function to gate mission critical features
        ///     which may cause loss of assets in the event of a license server
        ///     error!
        /// </summary>
        /// <returns>True if the user has support instances.</returns>
        public bool HasSupportInstances()
        {
            //#if !NO_DIRECTEVE_SECURITY
            //            return _security.Email != "anonymous" && _security.SupportInstances >= 0 && _security.ActiveInstances <= _security.SupportInstances;
            //#endif
            return true;
        }

        public bool Sell(DirectItem item, int StationId, int quantity, double price, int duration, bool useCorp)
        {
            if (!item.PyItem.IsValid)
                return false;
            //if (!HasSupportInstances())
            //{
            //    Log("DirectEve: Error: This method requires a support instance.");
            //    return false;
            //}
            //var pyRange = GetRange(range);
            //def SellStuff(self, stationID, typeID, itemID, price, quantity, duration = 0, useCorp = False, located = None):
            return ThreadedLocalSvcCall("marketQuote", "SellStuff", StationId, item.TypeId, item.ItemId, price, quantity, duration, useCorp); //pyRange);
        }

        internal PyObject GetRange(DirectOrderRange range)
        {
            switch (range)
            {
                case DirectOrderRange.SolarSystem:
                    return Const.RangeSolarSystem;
                case DirectOrderRange.Constellation:
                    return Const.RangeConstellation;
                case DirectOrderRange.Region:
                    return Const.RangeRegion;
                default:
                    return Const.RangeStation;
            }
        }

        public bool Buy(int StationId, int TypeId, double Price, int quantity, DirectOrderRange range, int minVolume, int duration) //, bool useCorp)
        {
            //if (!HasSupportInstances())
            //{
            //    Log("DirectEve: Error: This method requires a support instance.");
            //    return false;
            //}
            var pyRange = GetRange(range);
            //def BuyStuff(self, stationID, typeID, price, quantity, orderRange = None, minVolume = 1, duration = 0, useCorp = False):
            return ThreadedLocalSvcCall("marketQuote", "BuyStuff", StationId, TypeId, Price, quantity, pyRange, minVolume, duration); //, useCorp);
        }

        public bool InviteToFleet(long charId)
        {
            return ThreadedLocalSvcCall("menu", "InviteToFleet", charId);
        }

        public bool KickMember(long charId)
        {
            return ThreadedLocalSvcCall("menu", "KickMember", charId);
        }

        public bool LeaveFleet()
        {
            return ThreadedLocalSvcCall("menu", "LeaveFleet");
        }

        public bool MakeFleetBoss(long charId)
        {
            return ThreadedLocalSvcCall("menu", "MakeLeader", charId);
        }

        public List<DirectFleetMember> GetFleetMembers
        {
            get
            {
                var fleetMembers = new List<DirectFleetMember>();
                var pyMembers = GetLocalSvc("fleet").Attribute("members").ToDictionary<long>();
                foreach (var pyMember in pyMembers)
                {
                    fleetMembers.Add(new DirectFleetMember(this, pyMember.Value));
                }
                return fleetMembers;
            }
        }

        /// <summary>
        ///     Initiates trade window
        /// </summary>
        /// <param name="charId"></param>
        /// <returns>Fails if char is not in station, if charId is not in station and if the service is not active yet</returns>
        public bool InitiateTrade(long charId)
        {
            if (!Session.IsInStation)
                return false;

            if (!GetStationGuests.Any(i => i == charId))
                return false;

            var tradeService = GetLocalSvc("pvptrade");
            if (!tradeService.IsValid)
                return false;

            return ThreadedCall(tradeService.Attribute("StartTradeSession"), charId);
        }

        public List<long> GetStationGuests
        {
            get
            {
                var charIds = new List<long>();
                var pyCharIds = GetLocalSvc("station").Attribute("guests").ToDictionary();
                foreach (var pyChar in pyCharIds)
                    charIds.Add((long)pyChar.Key);
                return charIds;
            }
        }

        public bool AddToAddressbook(int charid)
        {
            return ThreadedLocalSvcCall("addressbook", "AddToPersonalMulti", new List<int> { charid });
        }

        /// <summary>
        ///     Reset DE caused freezes ~ Will be expanded later
        /// </summary>
        private void CheckStatistics()
        {

            Dictionary<string, PyObject> StatsDict = GetLocalSvc("clientStatsSvc").Attribute("statsEntries").ToDictionary<string>();

            //We detect change frameTimeAbove100msStat and other in python functions by client side
            // eve\client\script\sys\clientStatsSvc.py
            _frameTimeAbove100ms = (double)StatsDict["frameTimeAbove100ms"].Attribute("value");
            _frameTimeAbove200ms = (double)StatsDict["frameTimeAbove200ms"].Attribute("value");
            _frameTimeAbove300ms = (double)StatsDict["frameTimeAbove300ms"].Attribute("value");
            _frameTimeAbove400ms = (double)StatsDict["frameTimeAbove400ms"].Attribute("value");
            _frameTimeAbove500ms = (double)StatsDict["frameTimeAbove500ms"].Attribute("value");
            _timesliceWarnings = (double)StatsDict["timesliceWarnings"].Attribute("value");

            if (_lastOnframeTook > 80)
            {
                StatsDict["frameTimeAbove100ms"].Call("Set", _prevFrameTimeAbove100ms);
                StatsDict["frameTimeAbove200ms"].Call("Set", _prevFrameTimeAbove200ms);
                StatsDict["frameTimeAbove300ms"].Call("Set", _prevFrameTimeAbove300ms);
                StatsDict["frameTimeAbove400ms"].Call("Set", _prevFrameTimeAbove400ms);
                StatsDict["frameTimeAbove500ms"].Call("Set", _prevFrameTimeAbove500ms);
                StatsDict["timesliceWarnings"].Call("Set", _prevtimesliceWarnings);

            }

            _prevFrameTimeAbove100ms = _frameTimeAbove100ms;
            _prevFrameTimeAbove200ms = _frameTimeAbove200ms;
            _prevFrameTimeAbove300ms = _frameTimeAbove300ms;
            _prevFrameTimeAbove400ms = _frameTimeAbove400ms;
            _prevFrameTimeAbove500ms = _frameTimeAbove500ms;
            _prevtimesliceWarnings = _timesliceWarnings;

        }
    }
}