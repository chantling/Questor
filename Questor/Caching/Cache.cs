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

namespace Questor.Modules.Caching
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using System.Threading;
	using global::Questor.Modules.Actions;
	using global::Questor.Modules.BackgroundTasks;
	using global::Questor.Modules.Combat;
	using global::Questor.Modules.Lookup;
	using global::Questor.Modules.States;
	using global::Questor.Modules.Logging;
	using DirectEve;
	using global::Questor.Storylines;
	using Ut::EVE;
	using Ut;
	using Ut::WCF;
	
	public partial class Cache
	{
		
		private static Cache _instance = new Cache();
		
		public static Storyline storyline { get; set; }

		private DateTime LastEveAccountPoll = DateTime.MinValue;
		private EveAccount _EveAccount = null;
		public string CharName { get; set; }
		public string PipeName { get; set; }
		private DirectAgent _agent;
		public long CurrentStorylineAgentId { get; set; }
		public List<long> AgentBlacklist;
		private EntityCache _approaching;
		private List<EntityCache> _gates;
		private List<EntityCache> _bigObjectsAndGates;
		private List<EntityCache> _objects;
		private List<DirectBookmark> _safeSpotBookmarks;
		private readonly Dictionary<long, EntityCache> _entitiesById;
		private List<ModuleCache> _modules;
		public string OrbitEntityNamed;
		public DirectLocation MissionSolarSystem;
		public string DungeonId;
		private EntityCache _star;
		private List<EntityCache> _stations;
		private List<EntityCache> _stargates;
		private List<EntityCache> _containers;
		private List<EntityCache> _bigObjects;
		private EntityCache _stargate;
		private IEnumerable<EntityCache> _jumpBridges;
		private List<EntityCache> _targeting;
		private List<EntityCache> _targets;
		public List<long> _IDsinInventoryTree;
		private List<EntityCache> _unlootedContainers;
		private List<EntityCache> _unlootedWrecksAndSecureCans;
		private List<DirectWindow> _windows;
		private int? _maxLockedTargets;

		public HashSet<long> ListOfWarpScramblingEntities = new HashSet<long>();
		public HashSet<long> ListOfJammingEntities = new HashSet<long>();
		public HashSet<long> ListOfTrackingDisruptingEntities = new HashSet<long>();
		public HashSet<long> ListNeutralizingEntities = new HashSet<long>();
		public HashSet<long> ListOfTargetPaintingEntities = new HashSet<long>();
		public HashSet<long> ListOfDampenuingEntities = new HashSet<long>();
		public HashSet<long> ListofWebbingEntities = new HashSet<long>();
		public HashSet<long> ListofContainersToLoot = new HashSet<long>();
		public HashSet<string> ListofMissionCompletionItemsToLoot = new HashSet<string>();
		public long VolleyCount;
		public static int CacheInstances;
		public HashSet<long> LootedContainers { get; private set; }
		public bool ExitWhenIdle;
		public bool StopBot;
		public bool CanSafelyCloseQuestorWindow {get; set;}
		public static bool LootAlreadyUnloaded;
		public bool RouteIsAllHighSecBool;
		public double Wealth { get; set; }
		public double WealthatStartofPocket { get; set; }
		public int StackHangarAttempts { get; set; }
		public bool NormalApproach = true;
		public bool CourierMission;
		public bool doneUsingRepairWindow;
		public long AmmoHangarID = -99;
		public long LootHangarID = -99;
		public volatile bool IsLoadingSettings;
		public static D3DDetour.D3DVersion D3DVersion { get; set; }
		public static Random _random = new Random();

		bool inMission;
		
		public bool InMission {
			get {
				
				if(!InStation) {
					EntityCache station = Cache.Instance.Stations.OrderBy(s => s.Distance).FirstOrDefault();
					EntityCache stargate = Cache.Instance.Stargates.OrderBy(s => s.Distance).FirstOrDefault();
					
					if(station != null && station.Distance < 1000000) {
						return false;
					}
					
					if(stargate != null && stargate.Distance < 1000000) {
						return false;
					}
					
				}
				
				return inMission;
			}

			set { inMission = value; }
		}
		
		public bool normalNav = true;
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
		private DirectContainer _currentShipsCargo;
		private IEnumerable<ModuleCache> _weapons;
		public string Path;
		public bool _isCorpInWar = false;
		public bool CloseQuestorCMDLogoff;
		public bool CloseQuestorCMDExitGame = true;
		public bool CloseQuestorEndProcess;
		public bool GotoBaseNow;
		public bool QuestorJustStarted = true;
		public DirectEve DirectEve { get; set; }
		private DirectItem _myCurrentAmmoInWeapon;
		public Dictionary<long, long> LastModuleTargetIDs { get; private set; }
		public Dictionary<long, DateTime> TargetingIDs { get; private set; }
		public bool AllAgentsStillInDeclineCoolDown { get; set; }
		private string _currentAgent { get; set; }
		public bool Paused { get; set; }
		public long TotalMegaBytesOfMemoryUsed = 0;
		public double MyWalletBalance { get; set; }
		public string CurrentPocketAction { get; set; }
		public float AgentEffectiveStandingtoMe;
		public string AgentEffectiveStandingtoMeText;
		public float AgentCorpEffectiveStandingtoMe;
		public float AgentFactionEffectiveStandingtoMe;
		public float StandingUsedToAccessAgent;
		public bool MissionBookmarkTimerSet;
		public long AgentStationID { get; set; }
		public string AgentStationName;
		public long AgentSolarSystemID;
		public DirectContainer _containerInSpace { get; set; }
		private static readonly Func<DirectAgent, DirectSession, bool> AgentInThisSolarSystemSelector = (a, s) => a.SolarSystemId == s.SolarSystemId;
		private static readonly Func<DirectAgent, DirectSession, bool> AgentInThisStationSelector = (a, s) => a.StationId == s.StationId;
		public DirectContainer _fittedModules;
		public static DateTime QuestorProgramLaunched = DateTime.UtcNow;
		public static DateTime QuestorSchedulerReadyToLogin = DateTime.UtcNow;
		public static DateTime EVEAccountLoginStarted = DateTime.UtcNow;
		public static DateTime NextSlotActivate = DateTime.UtcNow;
		public static bool UseDx9 { get; set; }
		public static bool _humanInterventionRequired;
		public static int ServerStatusCheck = 0;
		public static DateTime _lastServerStatusCheckWasNotOK = DateTime.MinValue;
		public static DateTime DoneLoggingInToEVETimeStamp = DateTime.MaxValue;
		
		public Cache()
		{
			LastModuleTargetIDs = new Dictionary<long, long>();
			TargetingIDs = new Dictionary<long, DateTime>();
			_entitiesById = new Dictionary<long, EntityCache>();
			
			LootedContainers = new HashSet<long>();
			Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
			
			Interlocked.Increment(ref CacheInstances);
		}

		~Cache()
		{
			Interlocked.Decrement(ref CacheInstances);
		}
		
		public void InvalidateCache()
		{
			try
			{
				Logging.InvalidateCache();
				Arm.InvalidateCache();
				Drones.InvalidateCache();
				Combat.InvalidateCache();
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
				Logging.Log("Cache.InvalidateCache", "Exception [" + exception + "]", Logging.Debug);
			}
		}
		
		public EveAccount EveAccount
		{
			get  {
				
				if(_EveAccount == null || LastEveAccountPoll.AddSeconds(1) < DateTime.UtcNow) {
					LastEveAccountPoll = DateTime.UtcNow;
					_EveAccount = WCFClient.Instance.GetPipeProxy.GetEveAccount(this.CharName);
					
				}
				
				return _EveAccount;

			}
		}
		
		public WCFClient WCFClient {
			get {
				return WCFClient.Instance;
			}
		}
		
		public static bool LoadDirectEVEInstance(D3DDetour.D3DVersion version)
		{

			try
			{
				int TryLoadingDirectVE = 0;
				while (Cache.Instance.DirectEve == null && TryLoadingDirectVE < 30)
				{
					
					try
					{
						Logging.Log("Startup", "Starting Instance of DirectEVE using StandaloneFramework", Logging.Debug);
						Cache.Instance.DirectEve = new DirectEve(new StandaloneFramework(version));
						TryLoadingDirectVE++;
						Logging.Log("Startup", "DirectEVE should now be active: see above for any messages from DirectEVE", Logging.Debug);
						return true;
					}
					catch (Exception exception)
					{
						Logging.Log("Startup", "exception [" + exception + "]", Logging.Orange);
						continue;
					}
					
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Startup", "exception [" + exception + "]", Logging.Orange);
				return false;
			}

			if (Cache.Instance.DirectEve == null)
			{
				try
				{
					Logging.Log("Startup", "Error on Loading DirectEve, maybe server is down", Logging.Orange);
					Cache.Instance.CloseQuestorCMDLogoff = false;
					Cache.Instance.CloseQuestorCMDExitGame = true;
					Cache.Instance.CloseQuestorEndProcess = true;
					Cleanup.ReasonToStopQuestor = "Error on Loading DirectEve, maybe server is down";
					Cleanup.SignalToQuitQuestorAndEVEAndRestartInAMoment = true;
					Cleanup.CloseQuestor(Cleanup.ReasonToStopQuestor, true);
					return false;
				}
				catch (Exception exception)
				{
					Logging.BasicLog("Startup", "Exception while logging exception, oh joy [" + exception + "]");
					return false;
				}
			}

			return true;
			
		}
		
		public bool IsCorpInWar
		{
			get
			{
				if (DateTime.UtcNow > Time.Instance.NextCheckCorpisAtWar)
				{
					bool war = DirectEve.Me.IsAtWar;
					Cache.Instance._isCorpInWar = war;

					Time.Instance.NextCheckCorpisAtWar = DateTime.UtcNow.AddMinutes(15);
					if (!_isCorpInWar)
					{
						if (Logging.DebugWatchForActiveWars) Logging.Log("IsCorpInWar", "Your corp is not involved in any wars (yet)", Logging.Green);
					}
					else
					{
						if (Logging.DebugWatchForActiveWars) Logging.Log("IsCorpInWar", "Your corp is involved in a war, be careful", Logging.Orange);
					}

					return _isCorpInWar;
				}
				
				return _isCorpInWar;
			}
		}

		public bool LocalSafe(int maxBad, double stand)
		{
			int number = 0;
			DirectChatWindow local = (DirectChatWindow)GetWindowByName("Local");

			try
			{
				foreach (DirectCharacter localMember in local.Members)
				{
					float[] alliance = { DirectEve.Standings.GetPersonalRelationship(localMember.AllianceId), DirectEve.Standings.GetCorporationRelationship(localMember.AllianceId), DirectEve.Standings.GetAllianceRelationship(localMember.AllianceId) };
					float[] corporation = { DirectEve.Standings.GetPersonalRelationship(localMember.CorporationId), DirectEve.Standings.GetCorporationRelationship(localMember.CorporationId), DirectEve.Standings.GetAllianceRelationship(localMember.CorporationId) };
					float[] personal = { DirectEve.Standings.GetPersonalRelationship(localMember.CharacterId), DirectEve.Standings.GetCorporationRelationship(localMember.CharacterId), DirectEve.Standings.GetAllianceRelationship(localMember.CharacterId) };

					if (alliance.Min() <= stand || corporation.Min() <= stand || personal.Min() <= stand)
					{
						Logging.Log("Cache.LocalSafe", "Bad Standing Pilot Detected: [ " + localMember.Name + "] " + " [ " + number + " ] so far... of [ " + maxBad + " ] allowed", Logging.Orange);
						number++;
					}

					if (number > maxBad)
					{
						Logging.Log("Cache.LocalSafe", "[" + number + "] Bad Standing pilots in local, We should stay in station", Logging.Orange);
						return false;
					}
				}
			}
			catch (Exception exception)
			{
				Logging.Log("LocalSafe", "Exception [" + exception + "]", Logging.Debug);
			}
			
			return true;
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
					if ((Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10)) || (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(10)))
					{
						if (_currentShipsCargo == null)
						{
							_currentShipsCargo = Cache.Instance.DirectEve.GetShipsCargo();
							if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "_currentShipsCargo is null", Logging.Debug);
						}

						if (Cache.Instance.Windows.All(i => i.Type != "form.ActiveShipCargo")) // look for windows via the window (via caption of form type) ffs, not what is attached to this DirectCotnainer
						{
							if (DateTime.UtcNow > Time.Instance.NextOpenCurrentShipsCargoWindowAction)
							{
								Statistics.LogWindowActionToWindowLog("CargoHold", "Opening CargoHold");
								if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);", Logging.Debug);
								Cache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenCargoHoldOfActiveShip);
								Time.Instance.NextOpenCurrentShipsCargoWindowAction = DateTime.UtcNow.AddMilliseconds(1000 + Cache.Instance.RandomNumber(0, 2000));
							}

							if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "Waiting on NextOpenCurrentShipsCargoWindowAction [" + DateTime.UtcNow.Subtract(Time.Instance.NextOpenCurrentShipsCargoWindowAction).TotalSeconds + "sec]", Logging.Debug);
						}
						
						return _currentShipsCargo;
					}

					int EntityCount = 0;
					if (Cache.Instance.Entities.Any())
					{
						EntityCount = Cache.Instance.Entities.Count();
					}

					if (Logging.DebugCargoHold) Logging.Log("CurrentShipsCargo", "Cache.Instance.MyShipEntity is null: We have a total of [" + EntityCount + "] entities available at the moment.", Logging.Debug);
					return null;
					
				}
				catch (Exception exception)
				{
					Logging.Log("CurrentShipsCargo", "Unable to complete ReadyCargoHold [" + exception + "]", Logging.Teal);
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
			get
			{
				return Cache.Instance.DirectEve.ActiveShip;
			}
		}

		public int WeaponRange
		{
			get
			{
				// Get ammo based on current damage type
				IEnumerable<Ammo> ammo = Combat.Ammo.Where(a => a.DamageType == MissionSettings.CurrentDamageType).ToList();

				try
				{
					// Is our ship's cargo available?
					if (Cache.Instance.CurrentShipsCargo != null)
					{
						ammo = ammo.Where(a => Cache.Instance.CurrentShipsCargo.Items.Any(i => a.TypeId == i.TypeId && i.Quantity >= Combat.MinimumAmmoCharges));
					}
					else
					{
						return System.Convert.ToInt32(Combat.MaxTargetRange);
					}

					// Return ship range if there's no ammo left
					if (!ammo.Any())
					{
						return System.Convert.ToInt32(Combat.MaxTargetRange);
					}

					return ammo.Max(a => a.Range);
				}
				catch (Exception ex)
				{
					if (Logging.DebugExceptions) Logging.Log("Cache.WeaponRange", "exception was:" + ex.Message, Logging.Teal);

					// Return max range
					if (Cache.Instance.ActiveShip != null)
					{
						return System.Convert.ToInt32(Combat.MaxTargetRange);
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
						if (Cache.Instance.Weapons != null && Cache.Instance.Weapons.Any())
						{
							ModuleCache WeaponToCheckForAmmo = Cache.Instance.Weapons.FirstOrDefault();
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
					if (Logging.DebugExceptions) Logging.Log("Cache.myCurrentAmmoInWeapon", "exception was:" + ex.Message, Logging.Teal);
					return null;
				}
			}
		}

		public bool UpdateMyWalletBalance()
		{
			//we know we are connected here
			Time.Instance.LastKnownGoodConnectedTime = DateTime.UtcNow;
			Cache.Instance.MyWalletBalance = Cache.Instance.DirectEve.Me.Wealth;
			return true;
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
								
								if(MissionSettings.ListOfAgents != null && MissionSettings.ListOfAgents.Count() >= 1) {
									
									
									_currentAgent = SelectFirstAgent(true);
									Logging.Log("Cache.CurrentAgent", "Current Agent is [" + _currentAgent + "]", Logging.Debug);
									
								} else
								{
									Logging.Log("Cache.CurrentAgent", "MissionSettings.ListOfAgents == null ", Logging.White);
								}
								
							}
							catch (Exception ex)
							{
								Logging.Log("Cache.CurrentAgent", "Exception [" + ex + "]", Logging.Debug);
								return string.Empty;
							}
						}

						return _currentAgent;
					}

					return string.Empty;
				}
				catch (Exception ex)
				{
					Logging.Log("SelectNearestAgent", "Exception [" + ex + "]", Logging.Debug);
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
					Logging.Log("SelectNearestAgent", "Exception [" + ex + "]", Logging.Debug);
				}
			}
		}
		
		public string SelectFirstAgent(bool returnFirstOneIfNoneFound = false)
		{
			try
			{
				
				if (!MissionSettings.ListOfAgents.Any()) return string.Empty;
				AgentsList FirstAgent = MissionSettings.ListOfAgents.FirstOrDefault();

				if (FirstAgent != null)
				{
					return FirstAgent.Name;
				}

				Logging.Log("SelectFirstAgent", "Unable to find the first agent, are your agents configured?", Logging.Debug);
				return null;
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.SelectFirstAgent", "Exception [" + exception + "]", Logging.Debug);
				return null;
			}
		}

		
		// save the last used agentname/id to be able to retrieve the current agent by id
		private static String _agentName = String.Empty;
		private static long _agentId = 0;
		
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
								_agent = Cache.Instance.DirectEve.GetAgentByName(CurrentAgent);
								if(_agent == null) {
									Logging.Log("Cache: CurrentAgent", "Agent == null ERROR", Logging.White);
								}
							}
							
							if(_agent == null && CurrentAgent.Equals(_agentName) && _agentId > 0) {
								_agent = Cache.Instance.DirectEve.GetAgentById(_agentId);
							}

							if (_agent != null && !CurrentAgent.Equals(_agentName))
							{
								Logging.Log("Cache: CurrentAgent", "New AgentId [" + _agent.AgentId + "] AgentName [" + CurrentAgent + "]" , Logging.White);
								Cache.Instance.AgentStationName = Cache.Instance.DirectEve.GetLocationName(Cache.Instance._agent.StationId);
								Cache.Instance.AgentStationID = Cache.Instance._agent.StationId;
								Cache.Instance.AgentSolarSystemID = Cache.Instance._agent.SolarSystemId;
								
								
								_agentName = CurrentAgent;
								_agentId = _agent.AgentId;
								
							} 
							
							return _agent;
						}
						catch (Exception ex)
						{
							Logging.Log("Cache.Agent", "Unable to process agent section of [" + Logging.CharacterSettingsPath + "] make sure you have a valid agent listed! Pausing so you can fix it. [" + ex.Message + "]", Logging.Debug);
							Cache.Instance.Paused = true;
						}
					} else {

						Logging.Log("Cache.Agent", "if (!Settings.Instance.CharacterXMLExists)", Logging.Debug);
					}
					return null;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Agent", "Exception [" + exception + "]", Logging.Debug);
					return null;
				}
			}
			set {
				_agent = null;
			}
		}
		
		public IEnumerable<ModuleCache> Modules
		{
			get
			{
				try
				{
					if (_modules == null || !_modules.Any())
					{
						_modules = Cache.Instance.DirectEve.Modules.Select(m => new ModuleCache(m)).ToList();
					}

					return _modules;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Modules", "Exception [" + exception + "]", Logging.Debug);
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
						_fittedModules = Cache.Instance.DirectEve.GetShipsModules();
					}

					return _fittedModules;
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Modules", "Exception [" + exception + "]", Logging.Debug);
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
					_weapons = Modules.Where(m => m.GroupId == Combat.WeaponGroupId).ToList(); // ||
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

					if (Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(10))
					{
						if (!_weapons.Any())
						{
							int moduleNumber = 0;
							//Logging.Log("Cache.Weapons", "WeaponGroupID is defined as [" + Combat.WeaponGroupId + "] in your characters settings XML", Logging.Debug);
							foreach (ModuleCache _module in Cache.Instance.Modules)
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
								foreach (ModuleCache _weapon in Cache.Instance.Weapons)
								{
									//weaponNumber++;
									if (_weapon.AutoReload)
									{
										bool returnValueHereNotUsed = _weapon.DisableAutoReload;
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
					if (Cache.Instance.InSpace && DateTime.UtcNow > Time.Instance.LastInStation.AddSeconds(20) || (Cache.Instance.InStation && DateTime.UtcNow > Time.Instance.LastInSpace.AddSeconds(20)))
					{
						return _windows ?? (_windows = DirectEve.Windows);
					}

					return new List<DirectWindow>();
				}
				catch (Exception exception)
				{
					Logging.Log("Cache.Windows", "Exception [" + exception + "]", Logging.Debug);
				}

				return null;
			}
		}

		public DirectItem CheckCargoForItem(int typeIdToFind, int quantityToFind)
		{
			try
			{
				if (Cache.Instance.CurrentShipsCargo != null && Cache.Instance.CurrentShipsCargo.Items.Any())
				{
					DirectItem item = Cache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == typeIdToFind && i.Quantity >= quantityToFind);
					return item;
				}

				return null; // no items found
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.CheckCargoForItem", "Exception [" + exception + "]", Logging.Debug);
			}

			return null;
		}

		public bool CheckifRouteIsAllHighSec()
		{
			Cache.Instance.RouteIsAllHighSecBool = false;

			try
			{
				// Find the first waypoint
				if (DirectEve.Navigation.GetDestinationPath() != null && DirectEve.Navigation.GetDestinationPath().Count > 0)
				{
					List<int> currentPath = DirectEve.Navigation.GetDestinationPath();
					if (currentPath == null || !currentPath.Any()) return false;
					if (currentPath[0] == 0) return false; //No destination set - prevents exception if somehow we have got an invalid destination

					foreach (int _system in currentPath)
					{
						if (_system < 60000000) // not a station
						{
							DirectSolarSystem solarSystemInRoute = Cache.Instance.DirectEve.SolarSystems[_system];
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

							Logging.Log("CheckifRouteIsAllHighSec", "Jump number [" + _system + "of" + currentPath.Count() + "] in the route came back as null, we could not get the system name or sec level", Logging.Debug);
						}
					}
				}
			}
			catch (Exception exception)
			{
				Logging.Log("Cache.CheckifRouteIsAllHighSec", "Exception [" + exception +"]", Logging.Debug);
			}
			

			//
			// if DirectEve.Navigation.GetDestinationPath() is null or 0 jumps then it must be safe (can we assume we are not in lowsec or 0.0 already?!)
			//
			Cache.Instance.RouteIsAllHighSecBool = true;
			return true;
		}

		public void ClearPerPocketCache(string callingroutine)
		{
			try
			{
				if (DateTime.UtcNow > Time.NextClearPocketCache)
				{
					MissionSettings.ClearPocketSpecificSettings();
					Combat._doWeCurrentlyHaveProjectilesMounted = null;
					Combat.LastTargetPrimaryWeaponsWereShooting = null;
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

					Cache.Instance.LootedContainers.Clear();
					return;
				}

				//Logging.Log("ClearPerPocketCache", "[ " + callingroutine + " ] Attempted to ClearPocketCache within 5 seconds of a previous ClearPocketCache, aborting attempt", Logging.Debug);
			}
			catch (Exception ex)
			{
				Logging.Log("ClearPerPocketCache", "Exception [" + ex + "]", Logging.Debug);
				return;
			}
			finally
			{
				Time.NextClearPocketCache = DateTime.UtcNow.AddSeconds(5);
			}
		}
		
		public int RandomNumber(int min, int max)
		{
			Random random = new Random();
			return random.Next(min, max);
		}
		
		public void IterateShipTargetValues(string module)
		{
			string path = Logging.PathToCurrentDirectory;

			if (path != null)
			{
				string ShipTargetValuesXmlFile = System.IO.Path.Combine(path, "ShipTargetValues.xml");
				ShipTargetValues = new List<ShipTargetValue>();

				if (!File.Exists(ShipTargetValuesXmlFile))
				{
					Logging.Log(module, "IterateShipTargetValues - unable to find [" + ShipTargetValuesXmlFile + "]", Logging.White);
					return;
				}

				try
				{
					Logging.Log(module, "IterateShipTargetValues - Loading [" + ShipTargetValuesXmlFile + "]", Logging.White);
					XDocument values = XDocument.Load(ShipTargetValuesXmlFile);
					if (values.Root != null)
					{
						foreach (XElement value in values.Root.Elements("ship"))
						{
							ShipTargetValues.Add(new ShipTargetValue(value));
						}
					}
				}
				catch (Exception exception)
				{
					Logging.Log(module, "IterateShipTargetValues - Exception: [" + exception + "]", Logging.Red);
				}
			}
		}
		
		public void IterateUnloadLootTheseItemsAreLootItems(string module)
		{
			string path = Logging.PathToCurrentDirectory;

			if (path != null)
			{
				string UnloadLootTheseItemsAreLootItemsXmlFile = System.IO.Path.Combine(path, "UnloadLootTheseItemsAreLootItems.xml");
				UnloadLootTheseItemsAreLootById = new Dictionary<int, string>();

				if (!File.Exists(UnloadLootTheseItemsAreLootItemsXmlFile))
				{
					Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - unable to find [" + UnloadLootTheseItemsAreLootItemsXmlFile + "]", Logging.White);
					return;
				}

				try
				{
					Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - Loading [" + UnloadLootTheseItemsAreLootItemsXmlFile + "]", Logging.White);
					MissionSettings.UnloadLootTheseItemsAreLootItems = XDocument.Load(UnloadLootTheseItemsAreLootItemsXmlFile);

					if (MissionSettings.UnloadLootTheseItemsAreLootItems.Root != null)
					{
						foreach (XElement element in MissionSettings.UnloadLootTheseItemsAreLootItems.Root.Elements("invtype"))
						{
							UnloadLootTheseItemsAreLootById.Add((int)element.Attribute("id"), (string)element.Attribute("name"));
						}
					}
				}
				catch (Exception exception)
				{
					Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - Exception: [" + exception + "]", Logging.Red);
				}
			}
			else
			{
				Logging.Log(module, "IterateUnloadLootTheseItemsAreLootItems - unable to find [" + Logging.PathToCurrentDirectory + "]", Logging.White);
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
				Logging.Log("Cache.Instance.GetAgentMission", "DirectEve.AgentMissions failed: [" + exception + "]", Logging.Teal);
				return null;
			}
		}

	}
}
